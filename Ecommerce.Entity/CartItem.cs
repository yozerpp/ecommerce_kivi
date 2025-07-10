namespace Ecommerce.Entity;

public class CartItem
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public uint CartId { get; set; }
    public int Quantity { get; set; }
    public ProductOffer? ProductOffer { get; set; }
    public Cart? Cart { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is CartItem other)
        {
            return ProductId == other.ProductId && SellerId == other.SellerId && CartId == other.CartId;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProductId, SellerId, CartId);
    }
}
