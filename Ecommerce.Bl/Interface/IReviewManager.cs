using Ecommerce.Entity;

namespace Ecommerce.Bl.Interface;

public interface IReviewManager
{
    public List<ProductReview> GetReviewsWithAggregates(bool includeComments, bool includeSeller = false,
        uint? productId = null, uint? sellerId = null, uint? selectedRating = null, string? sortBy = null,
        bool? sortDesc = null, int page = 1, int pageSize = 20);
    public List<ReviewComment> GetCommentsWithAggregates(ulong reviewId, ulong? commentId = null, int page = 1, int pageSize = 20);
    public ProductReview? GetProductReview(uint productId, uint sellerId, Customer? customer = null,
        Session? session = null,
        bool includeComments = false, bool includeSeller = true);

    public Dictionary<ulong, int> GetUserVotesBatch(Session session, User? customer, ICollection<ulong>? reviewIds,
        ICollection<ulong>? commentIds);
    ProductReview LeaveReview( Session session, ProductReview review);
    void UpdateReview(Session? session, ProductReview review);
    void DeleteReview(Session? session, ProductReview review);
    ReviewComment CommentReview(Session session, ReviewComment comment);
    void UpdateComment(Session? session, ReviewComment comment);
    void DeleteComment(Session? session, ReviewComment comment);
    ReviewVote Vote(Session session, ReviewVote vote);
    void UnVote(Session session, ReviewVote vote);
}
