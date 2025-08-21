namespace CuaHangQuanAo.Entities; // dùng chung

public class ProductListVm
{
    // Input filter
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? Size { get; set; }      // chắc là lọc size
    public string? Sort { get; set; }      // price_asc | price_desc | name_asc | name_desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    // Output
    public IEnumerable<Item> Items { get; set; } = Enumerable.Empty<Item>();
    public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
    public int Total { get; set; }
}
