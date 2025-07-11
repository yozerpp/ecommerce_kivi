using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Order
{
    public uint Id { get; set; }
    public uint UserId { get; set; }
    public uint CartId { get; set; }
    public uint PaymentId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    [Generated]
    public decimal Total { get; set; }
    public OrderStatus? Status { get; set; }
    public Address ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public User? User { get; set; }
    public Payment? Payment { get; set; }

    public override bool Equals(object? obj) {
        if (obj is not Order other) return false;
        if (Id == default) return ReferenceEquals(this,obj);
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        if (Id == default) return base.GetHashCode();
        return Id.GetHashCode();
    }
}
