using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Order
{
    public uint Id { get; set; }
    public uint UserId { get; set; }
    public uint CartId { get; set; }
    public uint PaymentId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public OrderStatus? Status { get; set; }
    public Address ShippingAddress { get; set; }
    public Cart? Cart { get; set; }
    public User? User { get; set; }
    
    public Payment? Payment { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Order other)
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
