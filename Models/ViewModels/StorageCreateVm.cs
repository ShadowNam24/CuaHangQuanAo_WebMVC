using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.Models.ViewModels
{
    public class StorageCreateVm
    {
        public List<Supplier> Suppliers { get; set; } = new();
        public Dictionary<string, List<ItemSelectInfo>> ItemsByCategory { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public Storage Storage { get; set; } = new();
        public string? SelectedSize { get; set; }
        public string? SelectedColor { get; set; }
        public decimal EstimatedSellPrice { get; set; }
        public bool UpdateItemPrice { get; set; }
    }

    public class ItemSelectInfo
    {
        public int ItemsId { get; set; }
        public string DisplayName { get; set; } = null!;
        public int SellPrice { get; set; }
        public int CategoryId { get; set; }
    }
}
