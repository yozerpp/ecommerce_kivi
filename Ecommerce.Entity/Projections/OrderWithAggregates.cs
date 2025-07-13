namespace Ecommerce.Entity.Projections;

public class OrderWithAggregates : Order
{
    public int ItemCount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public double TotalDiscountPercentage { get; set; }
    public decimal BasePrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
    public new IEnumerable<OrderItemWithAggregates> Items { get; set; } =[];
}