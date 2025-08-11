using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Views;

namespace Ecommerce.Entity;

public class Cart
{
    public uint Id { get; set; }
    public Session? Session { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public CartAggregates Aggregates { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Cart other)
        {
            if (Id == default)
            {
                return base.Equals(obj);
            }
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (Id == default)
        {
            return base.GetHashCode();
        }
        return Id.GetHashCode();
    }
}
