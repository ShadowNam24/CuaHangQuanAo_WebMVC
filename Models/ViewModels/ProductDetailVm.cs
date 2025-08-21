namespace CuaHangQuanAo.Entities;

public class ProductDetailVm
{
    public Item Item { get; set; } = null!;
    public IEnumerable<Item> Related { get; set; } = Enumerable.Empty<Item>();

    // Các lựa chọn hiển thị
    public List<string> Sizes { get; set; } = new();            // S, M, L... hoặc 38,39...
    public List<string> Gallery { get; set; } = new();          // Url ảnh (placeholder nếu chưa có)
    public double Rating { get; set; } = 4.5;                   // demo
    public int RatingCount { get; set; } = 46;                  // demo
}
