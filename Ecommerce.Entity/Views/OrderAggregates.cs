using Ecommerce.Entity.Iface;

namespace Ecommerce.Entity.Views;

public class OrderAggregates : IPrice
{
    public uint OrderId { get; set; }
    public uint? ItemCount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? CouponDiscountAmount { get; set; }
    public decimal? TotalDiscountAmount { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? CouponDiscountedPrice { get; set; }
    public decimal? TotalDiscountPercentage { get; set; }
}
