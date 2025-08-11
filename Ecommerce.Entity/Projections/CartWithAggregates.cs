namespace Ecommerce.Entity.Projections;

public class CartWithAggregates : Cart
{
    public uint ItemCount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CouponDiscountPercentage { get; set; }
    public decimal TotalDiscountedPrice { get; set; }
    public decimal TotalDiscountPercentage { get; set; }
    public new IEnumerable<CartItemWithAggregates> Items { get; set; }
}