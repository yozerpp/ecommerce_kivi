namespace Ecommerce.Entity.Projections;

public class CartWithAggregates : Cart
{
    public uint ItemCount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
}