namespace CuaHangQuanAo.Entities
{
    public class HomeProductVm
    {
        public int ItemsId { get; set; }
        public string ItemsName { get; set; } = null!;
        public int? SellPrice { get; set; }
        public string? CategoryName { get; set; }
        public string? Image { get; set; }
        public int SoldQuantity { get; set; }
    }

    public class HomeIndexVm
    {
        public IEnumerable<HomeProductVm> NewProducts { get; set; } = Enumerable.Empty<HomeProductVm>();
        public IEnumerable<HomeProductVm> HotProducts { get; set; } = Enumerable.Empty<HomeProductVm>();
    }
}
