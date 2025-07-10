namespace Ecommerce.Entity;

public class Cart
{
    public uint Id { get; set; }
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
