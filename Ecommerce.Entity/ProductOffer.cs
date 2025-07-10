namespace Ecommerce.Entity;

public class ProductOffer
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public Product? Product { get; set; }
    public Seller? Seller { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is ProductOffer other)
        {
            return ProductId == other.ProductId && SellerId == other.SellerId;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProductId, SellerId);
    }
}
