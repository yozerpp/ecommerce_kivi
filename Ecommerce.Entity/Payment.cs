using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public abstract class Payment
{
    public uint Id { get; set; }
    public string PayerName { get; set; }
    public Order? Order { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is Payment other)
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
