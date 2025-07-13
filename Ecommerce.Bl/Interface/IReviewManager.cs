using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IReviewManager
{
    List<ReviewWithAggregates> GetReviewsWithAggregates(int productId, int sellerId, bool includeComments);
    ProductReview LeaveReview(ProductReview review);
    void UpdateReview(ProductReview review);
    void DeleteReview(ProductReview review);
    ReviewComment CommentReview(ReviewComment comment);
    void UpdateComment(ReviewComment comment);
    void DeleteComment(ReviewComment comment);
    ReviewVote Vote(ReviewVote vote);
    void UnVote(ReviewVote vote);
}
