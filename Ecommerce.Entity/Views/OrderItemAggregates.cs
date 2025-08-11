namespace Ecommerce.Entity.Views;

public class OrderItemAggregates
{
    public uint OrderId { get; set; }
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
    public decimal TotalDiscountPercentage { get; set; }
}
