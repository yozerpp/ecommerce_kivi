using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Payment
{
    public uint Id { get; set; }
    
    public string? TransactionId { get; set; }
    public uint OrderId { get; set; }
    public Order Order { get; set; }
    [Generated]
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Payment other)
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
