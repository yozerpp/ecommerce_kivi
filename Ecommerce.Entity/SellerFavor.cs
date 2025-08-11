namespace Ecommerce.Entity;

public class SellerFavor
{
    public uint SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public uint CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    protected bool Equals(SellerFavor other) {
        return SellerId == other.SellerId && CustomerId.Equals(other.CustomerId);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || SellerId != default && CustomerId!=default && obj is SellerFavor other && Equals(other);
    }

    public override int GetHashCode() {
        if(SellerId==default || CustomerId==default) {
            return base.GetHashCode();
        }
        return HashCode.Combine(SellerId, CustomerId);
    }
}