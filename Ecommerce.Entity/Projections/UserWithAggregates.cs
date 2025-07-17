namespace Ecommerce.Entity.Projections;

public class UserWithAggregates: User
{
    public decimal TotalSpent { get; set; }
    public decimal TotalDiscountUsed { get; set; }
    public int TotalOrders { get; set; }
}