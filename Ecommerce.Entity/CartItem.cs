using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Entity;

public class CartItem
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public uint CartId { get; set; }
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
    public ProductOffer ProductOffer { get; set; }
    public Cart Cart { get; set; }
    public string? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public override bool Equals(object? obj) {
        if (obj is not CartItem other) return false;
        if (ProductId == default && SellerId == default && CartId == default) return ReferenceEquals(this, other);
        return ProductId == other.ProductId && SellerId == other.SellerId && CartId == other.CartId;
    }

    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && CartId == default) return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, CartId);
    }
}
