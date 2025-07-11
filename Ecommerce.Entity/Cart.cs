using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Entity;

public class Cart
{
    public uint Id { get; set; }
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public override bool Equals(object? obj)
    {
        if (obj is Cart other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
