using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IReviewManager
{
    public List<ReviewWithAggregates> GetReviewsWithAggregates(bool includeComments, bool includeSeller = false,uint? productId=null,
        uint? sellerId = null, int page = 1, int pageSize = 20);
    ProductReview LeaveReview(ProductReview review);
    void UpdateReview(ProductReview review);
    void DeleteReview(ProductReview review);
    ReviewComment CommentReview(ReviewComment comment);
    void UpdateComment(ReviewComment comment);
    void DeleteComment(ReviewComment comment);
    ReviewVote Vote(ReviewVote vote);
    void UnVote(ReviewVote vote);
}
