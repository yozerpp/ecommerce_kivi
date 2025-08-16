namespace Ecommerce.Entity.Iface;

public interface IPrice
{    
    public decimal? BasePrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? CouponDiscountedPrice { get; set; }
}