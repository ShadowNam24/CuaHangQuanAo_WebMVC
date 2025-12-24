namespace CuaHangQuanAo.Models
{
    public class HomeProductVm
    {
        public int ItemsId { get; set; }
        public string ItemsName { get; set; } = string.Empty;
        public int? SellPrice { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Image { get; set; } = "no-image.png";
        public int SoldQuantity { get; set; }
    }

    public class HomeIndexVm
    {
        public List<HomeProductVm> NewProducts { get; set; } = new List<HomeProductVm>();
        public List<HomeProductVm> HotProducts { get; set; } = new List<HomeProductVm>();
    }
}
