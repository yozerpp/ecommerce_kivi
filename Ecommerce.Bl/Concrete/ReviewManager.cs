using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

using System.Linq.Expressions;
using Entity;


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

    public List<ProductReview> GetReviewsWithAggregates(bool includeComments, 
        Customer? customer=null, Session? session = null,bool includeSeller = false, 
        uint? productId=null, uint? sellerId = null, int page=1,int pageSize=20) {
        var includes = GetReviewIncludes(includeComments, includeSeller);
        var sessionId = customer?.Session?.Id ?? customer?.SessionId ?? session?.Id;
        var rid = customer?.Id;
        var ret =  _reviewRepository.WhereP(WithAggregates,r => (productId == null || r.ProductId == productId) && (sellerId == null || r.SellerId == sellerId), includes: includes, 
            offset:(page - 1) * pageSize, limit:pageSize * page);
        foreach (var r in ret){
            if (r.Reviewer==null || !r.CensorName ) continue;
            r.Reviewer.FirstName = r.Reviewer.FirstName[0] + "***";
            r.Reviewer.LastName = r.Reviewer.LastName[0] + "***";
        }
        return ret;
    }

    public List<ReviewComment> GetCommentsWithAggregates(ulong reviewId, ulong? commentId=null,
        Customer? customer = null, Session? session = null, int page = 1, int pageSize = 20) {
        return _reviewCommentRepository.Where(rc =>
            rc.ParentId == commentId && rc.ReviewId == reviewId, offset: (page - 1)*pageSize, limit:
            page*pageSize);
    }
    public ProductReview? GetProductReview(uint productId, uint sellerId, Customer? customer=null, Session? session = null,
        bool includeComments=false, bool includeSeller =true) {
        var sessionId = customer?.Session?.Id ?? customer?.SessionId ?? session?.Id ?? throw new ArgumentException("Either commenterId or reviewerId must be provided.");
        var reviewerId = customer?.Id;
        var includes = GetReviewIncludes(includeComments,includeSeller );
        var r= _reviewRepository.FirstP(WithAggregates,
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
        var uid = session.User?.Id ?? session.UserId;
        review.HasBought = _orderItemRepository.Exists(oi => oi.Order.UserId==uid && oi.ProductId==review.ProductId && oi.SellerId==review.SellerId, includes:[[nameof(OrderItem.Order)]]);
        try{
            var ret = _reviewRepository.Add(review);
            _reviewRepository.Flush();
            return ret;
        }
        catch (Exception e){
            if ((e.InnerException is not DbUpdateException du ||
                 !(du.InnerException?.Message.Contains("duplicate") ?? false)) &&
                (e.InnerException is not InvalidOperationException io || !io.Message.Contains("already"))) throw;
            throw new ArgumentException("You have already left a review for this product.");
        }
    }

    public void UpdateReview(Session? session, ProductReview review) {
        var c = _reviewRepository.UpdateExpr([
                (r => r.Rating, review.Rating),
                (r=>r.Comment, review.Comment),
            ],
            r => r.ProductId == review.ProductId && r.SellerId == review.SellerId && r.SessionId == session.Id);
        if(c==0) {
            throw new ArgumentException("Review or offer cannot be found.");
        }
    }

    public ReviewComment CommentReview(Session session, ReviewComment comment) {
        comment.CommenterId = session.Id;
        if (session.User != null){
            comment.UserId = session.User.Id;
        }
        try{
            var ret = _reviewCommentRepository.Add(comment);
            _reviewCommentRepository.Flush();
            return ret;
        }
        catch (Exception e){
            if ((e.InnerException is not DbUpdateException du ||
                 !(du.InnerException?.Message.Contains("duplicate") ?? false)) &&
                (e.InnerException is not InvalidOperationException io || !io.Message.Contains("already"))) throw;
            throw new ArgumentException("You have already left a review for this product.");
        }
    }

    public void DeleteReview(Session? session, ProductReview review) {
        var c = _reviewRepository.Delete(r => r.Id==review.Id|| r.ProductId == review.ProductId && r.SellerId == review.SellerId && session!=null &&r.SessionId == session.Id);
        if (c == 0)
            throw new ArgumentException("Review or offer cannot be found.");
    }

    public void UpdateComment(Session? session, ReviewComment comment) {
        var c =_reviewCommentRepository.UpdateExpr([
            (r=>r.Comment, comment.Comment )
        ], r => r.Id==comment.Id && r.CommenterId == session.Id); // Use comment.Id
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public void DeleteComment(Session? session, ReviewComment comment) {
        var c = _reviewCommentRepository.Delete(r => r.Id==comment.Id && r.CommenterId == session.Id); // Use comment.Id
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public ReviewVote Vote(Session session, ReviewVote vote) {
        vote.VoterId = session.Id;
        return _reviewVoteRepository.Save(vote);
    }

    public void UnVote(Session session, ReviewVote vote) {
        var c = _reviewVoteRepository.Delete(v=> (v.ReviewId == vote.ReviewId || v.CommentId == vote.CommentId) && v.VoterId == session.Id);
        if (c==0){
            throw new ArgumentException("Either your comment or the review cannot be found.");
        }
    }

    private static readonly Expression<Func<ReviewComment, ReviewComment>> CommentWithoutAggregates = c =>
        new ReviewComment(){
            Id = c.Id,
            ParentId = c.ParentId,
            CommenterId = c.CommenterId,
            ReviewId = c.ReviewId,
            UserId = c.UserId,
            Created = c.Created,
            Name = c.Name,
            Comment = c.Comment,
            Stats = null,
        };

    private static readonly Expression<Func<ReviewComment, ReviewComment>> CommentWithAggregates = c =>
        new ReviewComment(){
            Id = c.Id,
            ParentId = c.ParentId,
            CommenterId = c.CommenterId,
            ReviewId = c.ReviewId,
            UserId = c.UserId,
            Created = c.Created,
            Name = c.Name,
            Comment = c.Comment,
            Stats = new ReviewCommentStats(){
                CommentId = null,
                Votes = c.Stats.Votes ?? 0,
                ReplyCount = c.Stats.ReplyCount ?? 0,
                VoteCount = c.Stats.VoteCount ?? 0,
            }
        };
    private static readonly Expression<Func<ProductReview, ProductReview>> WithoutAggregates = r => new ProductReview(){
        Id = r.Id,
        SessionId = r.SessionId,
        SellerId = r.SellerId,
        ProductId = r.ProductId,
        ReviewerId = r.ReviewerId,
        CensorName = r.CensorName,
        Comment = r.Comment,
        Created = r.Created,
        HasBought = r.HasBought,
        Name = r.Name,
        Rating = r.Rating,
        Stats = null,
        Comments = r.Comments.Select(c=>new ReviewComment(){
            Id = c.Id,
            ParentId = c.ParentId,
            CommenterId = c.CommenterId,
            ReviewId = c.ReviewId,
            UserId = c.UserId,
            Created = c.Created,
            Name = c.Name,
            Comment = c.Comment,
            Stats = null,
        }).ToArray(),
    };
    private static readonly Expression<Func<ProductReview, ProductReview>> WithAggregates = r => new ProductReview(){
        Id = r.Id,
        SessionId = r.SessionId,
        SellerId = r.SellerId,
        ProductId = r.ProductId,
        ReviewerId = r.ReviewerId,
        CensorName = r.CensorName,
        Comment = r.Comment,
        Created = r.Created,
        HasBought = r.HasBought,
        Name = r.Name,
        Rating = r.Rating,
        Stats = new ReviewStats(){
            CommentCount = r.Stats.CommentCount ?? 0,
            Votes = r.Stats.Votes ?? 0,
            VoteCount = r.Stats.VoteCount??0,
        },
        // Comments = r.Comments.Select(c=>new ReviewComment(){
        //     Id = c.Id,
        //     ParentId = c.ParentId,
        //     CommenterId = c.CommenterId,
        //     ReviewId = ((ulong?)c.ReviewId)??0,
        //     UserId = c.UserId,
        //     Created = c.Created,
        //     Name = c.Name,
        //     Comment = c.Comment,
        //     Stats = new ReviewCommentStats(){
        //         Votes = c.Stats.Votes??0,
        //         CommentId = c.Stats.CommentId??0,
        //         ReplyCount = c.Stats.ReplyCount??0,
        //         VoteCount = c.Stats.VoteCount??0,
        //     }
        // }).ToArray(),
    };
}
