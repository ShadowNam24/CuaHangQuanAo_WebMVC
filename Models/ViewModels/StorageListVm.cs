using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.Models.ViewModels
{
    public class StorageListVm
    {
        public string? SearchTerm { get; set; }
        public bool LowStockOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public List<Storage> Storages { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public decimal TotalCost { get; set; }
        public string? LatestImportDate { get; set; }
    }
}
