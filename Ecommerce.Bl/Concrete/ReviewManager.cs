using Ecommerce.Dao;
using Ecommerce.Dao.Spi;

namespace Ecommerce.Bl.Concrete;

using System.Linq.Expressions;
using Entity;
using Entity.Projections;


public class ReviewManager
{
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<ProductReview> _reviewRepository;
    private readonly IRepository<ReviewComment> _reviewCommentRepository;
    private readonly IRepository<ReviewVote> _reviewVoteRepository;

    public ReviewManager(IRepository<ProductReview> reviewRepository, IRepository<ReviewComment> reviewCommentRepository, IRepository<ReviewVote> reviewVoteRepository, IRepository<OrderItem> orderItemRepository) {
        _reviewRepository = reviewRepository;
        _orderItemRepository = orderItemRepository;
        _reviewCommentRepository = reviewCommentRepository;
        _reviewVoteRepository = reviewVoteRepository;
    }

    public List<ReviewWithAggregates> GetReviewsWithAggregates(int productId, int sellerId, bool includeComments) {
        var includes = GetReviewIncludes(includeComments);
        var ret =  _reviewRepository.Where(ReviewProjection(ContextHolder.Session!.Id),r => r.ProductId == productId && r.SellerId == sellerId, includes: includes);
        foreach (var r in ret){
            if (!r.CensorName) continue;
            r.Reviewer.FirstName = r.Reviewer.FirstName[0] + "***";
            r.Reviewer.LastName = r.Reviewer.LastName[0] + "***";
        }
        return ret;
    }

    private static string[][] GetReviewIncludes(bool includeComments) {
        ICollection<string[]> includes = new List<string[]>();
        includes.Add([nameof(ProductReview.Reviewer)]);
        includes.Add([nameof(ProductReview.Votes)]);            
        if (includeComments){
            includes.Add([nameof(ProductReview.Comments),nameof(ReviewComment.Votes)]);
        }

        return includes.ToArray();
    }

    public ProductReview LeaveReview(ProductReview review) {
        var user = ContextHolder.Session!.User;
        review.HasBought = user != null && _orderItemRepository.Exists(oi => oi.Order.UserId ==user.Id && oi.ProductId==review.ProductId && oi.SellerId==review.SellerId, includes:[[nameof(OrderItem.Order)]]);
        return _reviewRepository.Add(review);
    }

    public void UpdateReview(ProductReview review) {
        var user = ContextHolder.GetUserOrThrow();
        var c = _reviewRepository.Update([
                (r => r.Rating, review.Rating),
                (r=>r.Comment, review.Comment),
            ],
            r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.ReviewerId == user.Id);
        if(c==0) {
            throw new ArgumentException("Review or offer cannot be found.");
        }
    }

    public void DeleteReview(ProductReview review) {
        var user = ContextHolder.GetUserOrThrow();
        var c = _reviewRepository.Delete(r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.ReviewerId == user.Id);
        if (c == 0)
            throw new ArgumentException("Review or offer cannot be found.");
    }

    public ReviewComment CommentReview(ReviewComment comment) {
        var commenterId = ContextHolder.Session!.Id;
        comment.CommenterId = commenterId;
        return _reviewCommentRepository.Add(comment);
    }

    public void UpdateComment(ReviewComment comment) {
        comment.CommenterId = ContextHolder.Session!.Id;
        var c =_reviewCommentRepository.Update([
            (r=>r.Comment, comment.Comment )
        ], r => r.ProductId == comment.ProductId&& r.SellerId==comment.SellerId &&r.ReviewerId==comment.ReviewerId && r.CommenterId == comment.CommenterId);
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public void DeleteComment(ReviewComment comment) {
        comment.CommenterId = ContextHolder.Session!.Id;
        var c = _reviewCommentRepository.Delete(r => r.ProductId == comment.ProductId && r.SellerId == comment.SellerId && r.ReviewerId == comment.ReviewerId && r.CommenterId == comment.CommenterId);
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public ReviewVote Vote(ReviewVote vote) {
        vote.VoterId = ContextHolder.Session!.Id;
        return _reviewVoteRepository.Save(vote);
    }

    public void UnVote(ReviewVote vote) {
        vote.VoterId = ContextHolder.Session!.Id;
        var c = _reviewVoteRepository.Delete(v=>v.ProductId == vote.ProductId && v.SellerId == vote.SellerId &&v.ReviewerId == vote.ReviewerId && v.VoterId == ContextHolder.Session!.Id);
        if (c==0){
            throw new ArgumentException("Either your comment or the review cannot be found.");
        }
    }
    private static Expression<Func<ProductReview, ReviewWithAggregates>> ReviewProjection(ulong sessionId) {
        return r => new ReviewWithAggregates{
            SellerId = r.SellerId,
            ProductId = r.ProductId,
            ReviewerId = r.ReviewerId,
            Comment = r.Comment,
            Comments = r.Comments.Select(c=>new ReviewCommentWithAggregates(){
                SellerId = c.SellerId,
                ProductId = c.ProductId,
                ReviewerId = c.ReviewerId,
                CommenterId = c.CommenterId,
                Comment = c.Comment,
                OwnVote = c.Votes.Where(v=>v.VoterId==sessionId).Select(v=>v.Up?1:-1).FirstOrDefault() as int? ??0,
                Votes = c.Votes.Sum(v=>v.Up?1:-1),
                Review = c.Review,
            }),
            CommentCount = r.Comments.Count(),
            HasBought = r.HasBought,
            Offer = r.Offer,
            Rating = r.Rating,
            Reviewer = new User{
                FirstName = r.Reviewer.FirstName,
                LastName = r.Reviewer.LastName
            },
            Votes =r.Votes.Sum(v => v.Up ? 1 : -1),
            OwnVote = (int?)r.Votes.Where(v => v.VoterId == sessionId).Select(v=>v.Up?1:-1).FirstOrDefault() ?? 0,
        };
    }
}
