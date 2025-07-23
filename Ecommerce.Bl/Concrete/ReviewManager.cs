using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

using System.Linq.Expressions;
using Entity;
using Entity.Projections;


public class ReviewManager : IReviewManager
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

    public List<ReviewWithAggregates> GetReviewsWithAggregates(Customer customer, bool includeComments,bool includeSeller = false, uint? productId=null, uint? sellerId = null, int page=1, int pageSize= 20) {
        var includes = GetReviewIncludes(includeComments, includeSeller);
        var ret =  _reviewRepository.Where(ReviewProjection(customer.Session?.Id),r => (productId == null || r.ProductId == productId) && (sellerId == null || r.SellerId == sellerId), includes: includes, 
            offset:(page - 1) * pageSize, limit:pageSize * page);
        foreach (var r in ret){
            if (!r.CensorName) continue;
            else{
                r.Reviewer.FirstName = r.Reviewer.FirstName[0] + "***";
                r.Reviewer.LastName = r.Reviewer.LastName[0] + "***";
            }
        }
        return ret;
    }

    public ReviewWithAggregates? GetReviewWithAggregates(Customer customer, uint productId, uint sellerId, uint ReviewerId,
        bool includeComments, bool includeSeller =true) {
        var includes = GetReviewIncludes(includeComments,includeSeller );
        var r= _reviewRepository.First(ReviewProjection(customer.Session?.Id),
            r => r.ProductId == productId && r.SellerId == sellerId&&r.ReviewerId == ReviewerId, includes: includes);
        if (!(r?.CensorName ?? false)) return r;
        r.Reviewer.FirstName = r.Reviewer.FirstName[0] + "***";
        r.Reviewer.LastName = r.Reviewer.LastName[0] + "***";
        return r;
    }
    private static string[][] GetReviewIncludes(bool includeComments, bool includeSeller) {
        ICollection<string[]> includes = new List<string[]>();
        includes.Add([nameof(ProductReview.Reviewer)]);
        if (includeSeller){
            includes.Add([nameof(ProductReview.Offer), nameof(ProductOffer.Seller)]);
        }
        if (includeComments){
            includes.Add([nameof(ProductReview.Comments),nameof(ReviewComment.Votes)]);
        }

        return includes.ToArray();
    }

    public ProductReview LeaveReview(ProductReview review) {
        var rid =GetSessionId(review);
        review.HasBought = _orderItemRepository.Exists(oi => oi.Order.SessionId == rid && oi.ProductId==review.ProductId && oi.SellerId==review.SellerId, includes:[[nameof(OrderItem.Order)]]);
        try{
            var ret = _reviewRepository.Add(review);
            _reviewRepository.Flush();
            return ret;
        }
        catch (Exception e){
            if (e.InnerException is not DbUpdateException && e.InnerException is not InvalidOperationException ) throw;
            if (e.InnerException.Message.Contains("already") || e.InnerException.Message.Contains("duplicate")){
                throw new ArgumentException("You have already left a review for this product.");
            }
            throw;
        }
    }
    private static ulong GetSessionId(ProductReview review) {
        return review.SessionId != 0 ? review.SessionId : review.Session?.Id ?? throw new ArgumentException("Review is not associated with a reviewer.");
    }
    public void UpdateReview(ProductReview review) {
        var rid = GetSessionId(review);
        var c = _reviewRepository.UpdateExpr([
                (r => r.Rating, review.Rating),
                (r=>r.Comment, review.Comment),
            ],
            r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.SessionId == rid);
        if(c==0) {
            throw new ArgumentException("Review or offer cannot be found.");
        }
    }

    public void DeleteReview(ProductReview review) {
        var rid = GetSessionId(review);
        var c = _reviewRepository.Delete(r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.SessionId == rid);
        if (c == 0)
            throw new ArgumentException("Review or offer cannot be found.");
    }

    public static ulong GetCommenterId(ReviewComment reviewComment) {
        return reviewComment.CommenterId!=0?reviewComment.CommenterId : reviewComment.Commenter?.Id ?? throw new ArgumentException("Comment is not associated with a review.");
    }
    public ReviewComment CommentReview(ReviewComment comment) {
        var ret =_reviewCommentRepository.Add(comment);
        _reviewCommentRepository.Flush();
        return ret;
    }

    public void UpdateComment(ReviewComment comment) {
        var c =_reviewCommentRepository.UpdateExpr([
            (r=>r.Comment, comment.Comment )
        ], r => r.ProductId == comment.ProductId&& r.SellerId==comment.SellerId &&r.SessionId==comment.SessionId && r.CommenterId == comment.CommenterId);
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public void DeleteComment(ReviewComment comment) {
        var c = _reviewCommentRepository.Delete(r => r.ProductId == comment.ProductId && r.SellerId == comment.SellerId && r.SessionId == comment.SessionId && r.CommenterId == comment.CommenterId);
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public ReviewVote Vote(ReviewVote vote) {
        return _reviewVoteRepository.Save(vote);
    }

    public void UnVote(ReviewVote vote) {
        var c = _reviewVoteRepository.Delete(v=>v.ProductId == vote.ProductId && v.SellerId == vote.SellerId &&v.SessionId == vote.SessionId && v.VoterId == vote.VoterId);
        if (c==0){
            throw new ArgumentException("Either your comment or the review cannot be found.");
        }
    }
    private static Expression<Func<ProductReview, ReviewWithAggregates>> ReviewProjection(ulong? sessionId) {
        return r => new ReviewWithAggregates{
            SellerId = r.SellerId,
            ProductId = r.ProductId,
            ReviewerId = r.ReviewerId,
            Comment = r.Comment,
            CensorName = r.CensorName,
            Comments = r.Comments.Select(c=>new ReviewCommentWithAggregates(){
                SellerId = c.SellerId,
                ProductId = c.ProductId,
                SessionId = c.SessionId,
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
            Reviewer = new Customer{
                FirstName = r.Reviewer.FirstName,
                LastName = r.Reviewer.LastName
            },
            Votes =r.Votes.Sum(v => v.Up ? 1 : -1),
            OwnVote = (int?)r.Votes.Where(v => v.VoterId == sessionId).Select(v=>v.Up?1:-1).FirstOrDefault() ?? 0,
        };
    }
}
