namespace Ecommerce.Entity.Projections;

public class OrderWithAggregates : Order
{
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}