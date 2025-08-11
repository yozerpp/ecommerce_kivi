using Ecommerce.Entity;

namespace Ecommerce.Bl.Interface;

public interface IReviewManager
{
    public List<ProductReview> GetReviewsWithAggregates(bool includeComments, Customer? customer = null,
        Session? session = null, bool includeSeller = false, uint? productId = null, uint? sellerId = null,
        int page = 1, int pageSize = 20);
    public List<ReviewComment> GetCommentsWithAggregates(ulong reviewId, ulong? commentId=null, Customer? customer = null,
        Session? session = null, int page = 1, int pageSize = 20);
    public ProductReview? GetProductReview(uint productId, uint sellerId, Customer? customer = null,
        Session? session = null,
        bool includeComments = false, bool includeSeller = true);
    
    ProductReview LeaveReview( Session session, ProductReview review);
    void UpdateReview(Session? session, ProductReview review);
    void DeleteReview(Session? session, ProductReview review);
    ReviewComment CommentReview(Session session, ReviewComment comment);
    void UpdateComment(Session? session, ReviewComment comment);
    void DeleteComment(Session? session, ReviewComment comment);
    ReviewVote Vote(Session session, ReviewVote vote);
    void UnVote(Session session, ReviewVote vote);
}
