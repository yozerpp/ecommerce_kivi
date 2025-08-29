namespace Ecommerce.Entity.Views;

public class CustomerStats
{
    public uint CustomerId { get; set; }
    public decimal? TotalSpent { get; set; }
    public decimal? TotalDiscountUsed { get; set; }
    public uint? TotalOrders { get; set; }
    public uint? TotalReviews { get; set; }
    public long? TotalKarma { get; set; }
    public uint? ReviewVotes { get; set; }
    public uint? CommentVotes { get; set; }
    public uint? TotalComments { get; set; }
}
