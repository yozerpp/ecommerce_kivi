using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Bl.Concrete;

using System.Linq.Expressions;
using Entity;


public class ReviewManager : IReviewManager
{
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<ProductReview> _reviewRepository;
    private readonly IRepository<ReviewComment> _reviewCommentRepository;
    private readonly IRepository<ReviewVote> _reviewVoteRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    private readonly DbContext _context;
    public ReviewManager(IRepository<ProductOffer> productOfferRepository,IRepository<ProductReview> reviewRepository, IRepository<ReviewComment> reviewCommentRepository, IRepository<ReviewVote> reviewVoteRepository, IRepository<OrderItem> orderItemRepository, [FromKeyedServices("DefaultDbContext")]DbContext context) {
        _reviewRepository = reviewRepository;
        _productOfferRepository = productOfferRepository;
        _orderItemRepository = orderItemRepository;
        _context = context;
        _reviewCommentRepository = reviewCommentRepository;
        _reviewVoteRepository = reviewVoteRepository;
    }
    public List<ProductReview> GetReviewsWithAggregates(bool includeComments, bool includeSeller = false, 
        uint? productId=null, uint? sellerId = null, uint?selectedRating = null, string? sortBy = null, bool? sortDesc = null, int page=1,int pageSize=20) {
        var includes = GetReviewIncludes(includeComments, includeSeller);
        var ordering = GetOrdering(sortBy, sortDesc);
        var ret =  _reviewRepository.WhereP(WithAggregates,GetPredicateOrThrow(productId, sellerId, selectedRating), includes: includes, 
            offset:(page - 1) * pageSize, limit:pageSize * page,orderBy:ordering);
        foreach (var r in ret){
            if (r.Reviewer==null || !r.CensorName ) continue;
            r.Reviewer.FirstName = r.Reviewer.FirstName[0] + "***";
            r.Reviewer.LastName = r.Reviewer.LastName[0] + "***";
        }
        return ret;
    }

    public ReviewComment? GetCommentTree(ulong commentId, bool includeChildren, bool includeParent,
        bool includeAggregates = true) {
        var includes = new List<string[]>();
        if (includeChildren)includes.Add([nameof(ReviewComment.Replies), nameof(ReviewComment.Stats)]);
        if(includeParent) includes.Add([nameof(ReviewComment.Parent)]);
        return _reviewCommentRepository.First(r => r.Id == commentId, includes: includes.ToArray());
    }
    private static Expression<Func<ProductReview, bool>> GetPredicateOrThrow(uint? productId = null, uint? sellerId = null,
        uint? selectedRating = null) {
        var param  = Expression.Parameter(typeof(ProductReview), "r");
        Expression predicate = Expression.Constant(true);
        if(productId ==null&& sellerId ==null&& selectedRating == null) throw new ArgumentNullException(nameof(ProductReview.SellerId));
        if(productId !=null)
            predicate = Expression.AndAlso(Expression.Equal(Expression.Property(param, nameof(ProductReview.ProductId)), Expression.Constant(productId.Value)), predicate);
        if (sellerId != null)
            predicate = Expression.AndAlso(Expression.Equal(Expression.Property(param, nameof(ProductReview.SellerId)), Expression.Constant(sellerId.Value)), predicate);
        if(selectedRating != null)
            predicate = Expression.AndAlso(
                Expression.AndAlso(
                    Expression.GreaterThanOrEqual(
                        Expression.Property(param, nameof(ProductReview.Rating)), 
                        Expression.Convert(Expression.Constant(selectedRating.Value), typeof(decimal))), 
                    Expression.LessThan(
                        Expression.Property(param, nameof(ProductReview.Rating)),
                        Expression.Convert(Expression.Constant(selectedRating.Value + 1),typeof(decimal)))),
                predicate);
        return Expression.Lambda<Func<ProductReview, bool>>(predicate, param);
    }
    public ICollection<(Expression<Func<ProductReview, object>>, bool)> GetOrdering(string? sortBy, bool? sortDesc) {
        var ret = new List<(Expression<Func<ProductReview, object>>, bool)>();
        if (nameof(ProductReview.Rating).Equals(sortBy, StringComparison.OrdinalIgnoreCase)){
            ret.Add((review => review.Rating, !sortDesc??false));
        } else if(nameof(ProductReview.Created).Equals(sortBy,StringComparison.OrdinalIgnoreCase))
            ret.Add((review => review.Created,!sortDesc??false));
        return ret;
    }


    public Dictionary<ulong,int> GetUserVotesBatch(Session session, User? customer,  ICollection<ulong>? reviewIds ,ICollection<ulong>? commentIds){
        if(reviewIds==null && commentIds==null){
            throw new ArgumentNullException(nameof(reviewIds));
        }
        var sid = customer?.SessionId ?? session.Id;
        var cid = customer?.Id;
        if (reviewIds != null){
            var votes = _reviewVoteRepository.WhereP(v =>new {v.Up, v.ReviewId},
                v => v.ReviewId != null && reviewIds.Contains(v.ReviewId.Value) && (cid!=null && v.UserId == cid || v.VoterId == sid), nonTracking:true).DistinctBy(t=>t.ReviewId).ToDictionary(v=>(ulong)v.ReviewId, v=>v.Up);
            return reviewIds.ToDictionary(i=>i,i => votes.TryGetValue(i, out var val) ? (val ? 1 : -1) : 0);
        }
        else{
            var votes = _reviewVoteRepository.WhereP(v =>new {v.Up, v.CommentId},
                v => v.CommentId != null && commentIds.Contains(v.CommentId.Value) && (cid!=null && v.UserId == cid || v.VoterId == sid), nonTracking:true).DistinctBy(t=>t.CommentId).ToDictionary(v=>(ulong)v.CommentId, v=>v.Up);
            return commentIds.ToDictionary(i=>i,i => votes.TryGetValue(i, out var val) ? (val ? 1 : -1) : 0);
        }
    }

    public int GetUserVote(ulong sessionId, ulong? reviewId, ulong? commentId) {
        bool? v = _reviewVoteRepository.FirstP(v=>v.Up, v=> (v.CommentId == commentId||v.ReviewId == reviewId) && v.VoterId == sessionId,nonTracking:true);
        return v==null ? 0 : (v.Value ? 1 : -1);
    }
    public List<ReviewComment> GetCommentsWithAggregates(ulong reviewId, ulong? commentId=null, int page = 1, int pageSize = 20) {
        return _reviewCommentRepository.WhereP(CommentWithAggregates,rc =>
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
        if (review.SellerId == default)
            review.SellerId = _productOfferRepository.FirstP(o => o.SellerId, o => o.ProductId == review.ProductId,
                nonTracking: true);
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
            throw new ArgumentException("Bu ürüne zaten değerlendirme yaptınız.");
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
            throw new ArgumentException("Bu yoruma zaten cevap verdiniz.");
        }
    }

    public void DeleteReview(Session? session, ProductReview review) {
        using var t = _context.Database.BeginTransaction();
        _context.Set<ReviewVote>().Where(v => v.ReviewId == review.Id && v.CommentId == null).ExecuteDelete();
        var c= _context.Set<ProductReview>().Where(r => r.Id == review.Id && (r.SessionId == session.Id || r.ReviewerId == session.UserId)).ExecuteDelete();
        if (c == 0){
            t.Rollback();
            throw new ArgumentException("Değerlendirme bulunamadı ya da size ait değil.");
        }
        t.Commit();
    }

    public void UpdateComment(Session? session, ReviewComment comment) {
        var c =_reviewCommentRepository.UpdateExpr([
            (r=>r.Comment, comment.Comment )
        ], r => r.Id==comment.Id && r.CommenterId == session.Id); // Use comment.Id
        if (c == 0)
            throw new ArgumentException("Either your comment or the review cannot be found.");
    }

    public void DeleteComment(Session? session, ReviewComment comment) {
        using var t = _context.Database.BeginTransaction();
        _context.Set<ReviewVote>().Where(v => v.CommentId == comment.Id && v.ReviewId ==comment.ReviewId).ExecuteDelete();
        var c=_context.Set<ReviewComment>().Where(c=>c.Id == comment.Id && (c.CommenterId == session.Id || c.UserId == session.UserId)).ExecuteDelete();
        if (c == 0){
            t.Rollback();
            throw new ArgumentException("yorum bulunamadı ya da size ait değil.\"");
        }
        t.Commit();
    }

    public ReviewVote Vote(Session session, ReviewVote vote) {
        var uid = session.User?.Id;
        _reviewVoteRepository.Delete(v=> (vote.ReviewId != null && v.ReviewId == vote.ReviewId || vote.CommentId!=null && v.CommentId == vote.CommentId) &&
                                         (v.VoterId == session.Id || uid!=null && v.UserId == uid));
        return _reviewVoteRepository.Save(new ReviewVote(){
            VoterId = session.Id,
            Up = vote.Up,
            ReviewId = vote.ReviewId,
            CommentId = vote.CommentId,
            UserId = vote.UserId
        });
    }

    public void UnVote(Session session, ReviewVote vote) {
        var c = _reviewVoteRepository.Delete(v=> (v.ReviewId == vote.ReviewId || v.CommentId == vote.CommentId) &&(v.VoterId == session.Id || session.UserId!=null && v.UserId == session.UserId));
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
    };
}
