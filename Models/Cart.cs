namespace CuaHangQuanAo.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public int? GrandTotal => Items.Sum(x => x.Total);

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(x => x.ItemsID == item.ItemsID);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                Items.Add(item);
        }

        public void RemoveItem(int id) => Items.RemoveAll(x => x.ItemsID == id);

        public void Clear() => Items.Clear();
    }
}