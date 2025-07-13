namespace Ecommerce.Entity.Projections;

public class ReviewCommentWithAggregates : ReviewComment
{
    public int Votes { get; set; }
    public int OwnVote { get; set; }
}