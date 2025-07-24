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

    public List<ReviewWithAggregates> GetReviewsWithAggregates(bool includeComments, Customer? customer=null, Session? session = null,bool includeSeller = false, uint? productId=null, uint? sellerId = null, int page=1, int pageSize= 20) {
        var includes = GetReviewIncludes(includeComments, includeSeller);
        var sessionId = customer?.Session?.Id ?? customer?.SessionId ?? session?.Id;
        var ret =  _reviewRepository.Where(ReviewProjection(sessionId),r => (productId == null || r.ProductId == productId) && (sellerId == null || r.SellerId == sellerId), includes: includes, 
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

    public ReviewWithAggregates? GetReviewWithAggregates(uint productId, uint sellerId,Customer? customer=null, Session? session = null,
        bool includeComments=false, bool includeSeller =true) {
        var sessionId = customer?.Session?.Id ?? customer?.SessionId ?? session?.Id ?? throw new ArgumentException("Either commenterId or reviewerId must be provided.");
        var reviewerId = customer?.Id;
        var includes = GetReviewIncludes(includeComments,includeSeller );
        var r= _reviewRepository.First(ReviewProjection(sessionId),
            r => r.ProductId == productId && r.SellerId == sellerId&&r.ReviewerId == reviewerId&& r.SessionId==sessionId, includes: includes);
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

    public ProductReview LeaveReview(Session session, ProductReview review) {
        review.SessionId = session.Id;
        review.HasBought = _orderItemRepository.Exists(oi => oi.Order.SessionId == session.Id && oi.ProductId==review.ProductId && oi.SellerId==review.SellerId, includes:[[nameof(OrderItem.Order)]]);
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
    
    public void UpdateReview(Session session, ProductReview review) {
        var c = _reviewRepository.UpdateExpr([
                (r => r.Rating, review.Rating),
                (r=>r.Comment, review.Comment),
            ],
            r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.SessionId == session.Id);
        if(c==0) {
            throw new ArgumentException("Review or offer cannot be found.");
        }
    }

    public void DeleteReview(Session session, ProductReview review) {
        var c = _reviewRepository.Delete(r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.SessionId == session.Id);
        if (c == 0)
            throw new ArgumentException("Review or offer cannot be found.");
    }

    public ReviewComment CommentReview(Session session, ReviewComment comment) {
        comment.CommenterId = session.Id;
        var ret =_reviewCommentRepository.Add(comment);
        _reviewCommentRepository.Flush();
        return ret;
    }

    public void UpdateComment(Session session, ReviewComment comment) {
        var c =_reviewCommentRepository.UpdateExpr([
            (r=>r.Comment, comment.Comment )
        ], r => r.Id==comment.Id && r.CommenterId == session.Id); // Use comment.Id
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public void DeleteComment(Session session, ReviewComment comment) {
        var c = _reviewCommentRepository.Delete(r => r.Id==comment.Id && r.CommenterId == session.Id); // Use comment.Id
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public ReviewVote Vote(Session session, ReviewVote vote) {
        vote.VoterId = session.Id;
        return _reviewVoteRepository.Save(vote);
    }

    public void UnVote(Session session, ReviewVote vote) {
        var c = _reviewVoteRepository.Delete(v=>v.Id==vote.Id && v.VoterId == session.Id);
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
                Id = c.Id, // Use c.Id
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
