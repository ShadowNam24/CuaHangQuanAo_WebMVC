namespace CuaHangQuanAo.Entities
{
    public class VnPayOptions
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string IpnUrl { get; set; } = string.Empty;
    }
}
