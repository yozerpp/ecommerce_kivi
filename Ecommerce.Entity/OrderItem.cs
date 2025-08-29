using System.ComponentModel.DataAnnotations;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;

namespace Ecommerce.Entity;

public class OrderItem : IItem
{
    public OrderItem() { }
    public OrderItem(CartItem cartItem) {
        ProductId = cartItem.ProductId;
        SellerId = cartItem.SellerId;
        Quantity = cartItem.Quantity;
        CouponId = cartItem.CouponId;
        SelectedOptions = cartItem.SelectedOptions;
        Status = OrderStatus.WaitingPayment;
    }
    public ulong? ShipmentId { get; set; }
    public ulong? RefundShipmentId { get; set; }
    public uint SellerId { get; set; }
    public uint ProductId { get; set; }
    public uint OrderId { get; set; }
    public uint Quantity { get; set; }
    public OrderStatus Status { get; set; }
    public Shipment? SentShipment { get; set; }
    public Shipment? RefundShipment { get; set; }
    public ProductOffer? ProductOffer { get; set; }
    public RefundRequest? RefundRequest { get; set; }
    public OrderItemAggregates Aggregates { get; set; }
    public Order Order { get; set; }
    public string? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public ICollection<ProductOption> SelectedOptions { get; set; }
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