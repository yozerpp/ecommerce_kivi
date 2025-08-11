namespace Ecommerce.Entity;

public class ProductFavor
{
    public Customer Customer { get; set; }
    public Product Product { get; set; }
    public uint CustomerId { get; set; }
    public uint ProductId { get; set; }
    protected bool Equals(ProductFavor other) {
        return CustomerId == other.CustomerId && ProductId == other.ProductId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || CustomerId!=default && ProductId != default && obj is ProductFavor other && Equals(other);
    }
    public override int GetHashCode() {
        if (ProductId == default || CustomerId == default) return base.GetHashCode();
        return HashCode.Combine(CustomerId, ProductId);
    }
}