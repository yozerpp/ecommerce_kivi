namespace Ecommerce.Entity.Views;

public class ReviewStats
{
    public ulong? ReviewId { get; set; }
    public uint? CommentCount { get; set; }
    public int? Votes { get; set; }
    public uint? VoteCount { get; set; }
}