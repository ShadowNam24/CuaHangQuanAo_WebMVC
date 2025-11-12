using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class PayPalApiService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public PayPalApiService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var clientId = _config["PayPal:ClientId"];
        var clientSecret = _config["PayPal:ClientSecret"];
        var baseUrl = _config["PayPal:BaseUrl"];

        _httpClient.DefaultRequestHeaders.Clear();
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var body = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await _httpClient.PostAsync($"{baseUrl}/v1/oauth2/token", body);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task<string> CreateOrderAsync(decimal total, string returnUrl, string cancelUrl)
    {
        var token = await GetAccessTokenAsync();
        var baseUrl = _config["PayPal:BaseUrl"];

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new {
                    amount = new {
                        currency_code = "USD",
                        value = total.ToString("F2") // use real total
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{baseUrl}/v2/checkout/orders", content);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<(bool ok, string json, string? redirectUrl)> TryCaptureOrderAsync(string orderId)
    {
        var token = await GetAccessTokenAsync();
        var baseUrl = _config["PayPal:BaseUrl"];

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders/{orderId}/capture")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return (true, json, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var links = doc.RootElement.TryGetProperty("links", out var l) ? l.EnumerateArray() : default;
            string? redirect = null;
            foreach (var x in links)
            {
                var rel = x.TryGetProperty("rel", out var r) ? r.GetString() : null;
                if (rel == "redirect" || rel == "approve" || rel == "payer-action")
                {
                    redirect = x.GetProperty("href").GetString();
                    break;
                }
            }
            return (false, json, redirect);
        }
        catch { return (false, json, null); }
    }
}
