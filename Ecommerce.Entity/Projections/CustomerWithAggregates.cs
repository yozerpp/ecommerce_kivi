namespace Ecommerce.Entity.Projections;

public class CustomerWithAggregates: Customer
{
    public decimal TotalSpent { get; set; }
    public decimal TotalDiscountUsed { get; set; }
    public int TotalOrders { get; set; }
    public int TotalReviews { get; set; }
    public int TotalKarma { get; set; }
    public int TotalReplies { get; set; }
}