namespace CuaHangQuanAo.Entities
{
    public class HomeIndexVm
    {
        public IEnumerable<Item> NewProducts { get; set; } = Enumerable.Empty<Item>();
        public IEnumerable<Item> HotProducts { get; set; } = Enumerable.Empty<Item>();
    }
}
