namespace CuaHangQuanAo.Entities; // dùng chung

public class ProductListVm
{
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? Size { get; set; }
    public string? Sort { get; set; } = "name_asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public List<Category> Categories { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public int Total { get; set; }
}
