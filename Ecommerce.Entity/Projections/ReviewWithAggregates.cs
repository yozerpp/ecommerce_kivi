namespace Ecommerce.Entity.Projections;

public class ReviewWithAggregates : ProductReview
{
    public int CommentCount { get; set; }
    public int Votes { get; set; }
    public int OwnVote { get; set; }
    public new IEnumerable<ReviewCommentWithAggregates> Comments { get; set; }
}