using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Entity;

public class OrderItem
{
    public OrderItem() { }
    public OrderItem(CartItem cartItem, Order order) {
        ProductId = cartItem.ProductId;
        SellerId = cartItem.SellerId;
        OrderId = order.Id;
        ProductOffer = cartItem.ProductOffer;
        Order = order;
        Quantity = cartItem.Quantity;
        CouponId = cartItem.CouponId;
        Coupon = cartItem.Coupon;
    }
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public uint OrderId { get; set; }
    public uint Quantity { get; set; }
    public ProductOffer ProductOffer { get; set; }
    public Order Order { get; set; }
    public string? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public override bool Equals(object? obj) {
        if (obj is not OrderItem other) return false;
        if (ProductId == default && SellerId == default && OrderId == default) return ReferenceEquals(this, other);
        return ProductId == other.ProductId && SellerId == other.SellerId && OrderId == other.OrderId;
    }
    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && OrderId == default) return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, OrderId);
    }
}