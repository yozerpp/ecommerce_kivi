using Ecommerce.Entity.Iface;

namespace Ecommerce.Entity.Views;

public class CartAggregates : IPrice
{
    public uint CartId { get; set; }
    public uint ItemCount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal BasePrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CouponDiscountPercentage { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
    public decimal TotalDiscountPercentage { get; set; }
}
