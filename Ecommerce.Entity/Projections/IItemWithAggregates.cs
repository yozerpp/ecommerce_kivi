namespace Ecommerce.Entity.Projections;

public interface IItemWithAggregates : IItem
{
    public decimal BasePrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal CouponDiscountedPrice { get; set; }
    public decimal TotalDiscountPercentage { get; set; }
}