using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.Models
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalInventory { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalEmployees { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<Item> LowStockItems { get; set; } = new List<Item>();
        public List<Supplier> TopSuppliers { get; set; } = new List<Supplier>();
    }
}
