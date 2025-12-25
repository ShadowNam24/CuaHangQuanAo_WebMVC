using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class VnPayLibrary
{
    private readonly VnPayOptions _options;

    public VnPayLibrary(IOptions<VnPayOptions> options) => _options = options.Value;

    public string CreateRequestUrl(Dictionary<string, string> requestData)
    {
        var sorted = requestData
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var queryBuilder = new StringBuilder();
        foreach (var kv in sorted)
            queryBuilder.Append($"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}&");

        var signData = queryBuilder.ToString().TrimEnd('&');
        var secureHash = HmacSHA512(_options.HashSecret, signData);
        return $"{_options.Url}?{signData}&vnp_SecureHash={secureHash}";
    }

    public Dictionary<string, string> GetResponseData(IQueryCollection query)
    {
        var data = new Dictionary<string, string>();
        foreach (var kv in query)
            if (kv.Key.StartsWith("vnp_")) data[kv.Key] = kv.Value;
        return data;
    }

    public bool ValidateSignature(Dictionary<string, string> responseData)
    {
        if (!responseData.TryGetValue("vnp_SecureHash", out var receivedHash))
            return false;

        var signData = string.Join("&", responseData
            .Where(kv => kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));

        var calculatedHash = HmacSHA512(_options.HashSecret, signData);
        return string.Equals(receivedHash, calculatedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSHA512(string key, string input)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}