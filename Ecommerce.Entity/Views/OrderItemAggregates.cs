namespace Ecommerce.Entity.Views;

public class OrderItemAggregates
{
    public uint OrderId { get; set; }
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
    public decimal TotalDiscountPercentage { get; set; }
    public string? CouponId { get; set; }
    public uint ShipmentId { get; set; }
    public uint? RefundShipmentId { get; set; }
    public uint ProductOfferId { get; set; } // Assuming this is a unique identifier for ProductOffer
}
