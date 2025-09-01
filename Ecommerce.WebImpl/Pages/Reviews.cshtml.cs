using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Review;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class Reviews : BaseModel
{
    private readonly IReviewManager _reviewManager;
    private readonly INotificationService _notificationService;
    private readonly DbContext _dbContext;

    public Reviews(INotificationService notificationService, IReviewManager reviewManager,
        [FromKeyedServices("DefaultDbContext")] DbContext dbContext) : base(notificationService) {
        _notificationService = notificationService;
        _reviewManager = reviewManager;
        _dbContext = dbContext;
    }

    [BindProperty] public ProductReview? SentReview { get; set; }

    public IActionResult OnPost() {
        try{
            _reviewManager.LeaveReview(CurrentSession, SentReview);
        }
        catch (ArgumentException e){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Hata",
                Message = e.Message,
            });
        }
        _notificationService.SendSingleAsync(new ReviewNotification(){
            UserId = SentReview.SellerId, ReviewId = SentReview.Id
        }).Wait();
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Yorumunuz eklendi.",
        });
    }

    public IActionResult OnPostDeleteComment([FromForm] ulong commentId) {
        _reviewManager.DeleteComment(CurrentSession, new ReviewComment(){
            Id = commentId
        });
        return new OkResult();
    }

    public IActionResult OnPostDeleteReview([FromForm] ulong reviewId) {
        _reviewManager.DeleteReview(CurrentSession, new ProductReview(){
            Id = reviewId
        });
        return new OkResult();
    }

    [BindProperty] public ReviewComment? SentComment { get; set; }

    [HasRole(nameof(Staff), nameof(Entity.Customer))]
    public IActionResult OnPostDelete() {
        if (SentComment != null && SentComment.Id != default)
            _reviewManager.DeleteComment(null, SentComment);
        else if (SentReview != null && SentReview.Id != default)
            _reviewManager.DeleteReview(null, SentReview);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Yorum silindi.",
        });
    }

    public IActionResult OnPostComment() {
        try{
            _reviewManager.CommentReview(CurrentSession, SentComment);
        }
        catch (ArgumentException e){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Message = e.Message,
                Title = "Hata",
            });
        }

        uint? uid;
        uid = GetRepliedUserId(SentComment.ParentId, SentComment.ReviewId);
        if (uid != null){
            _notificationService.SendSingleAsync(new ReviewCommentNotification(){
                CommentId = SentComment.Id,
                UserId = uid.Value,
            }).Wait();
        }

        return Partial("_InfoPartial", new _InfoPartial(){
            Success = true,
            Message = "Yorum eklendi.",
        });
    }

    private uint? GetRepliedUserId(ulong? pid, ulong? rid) {
        return pid != null
            ? _dbContext.Set<ReviewComment>().AsNoTracking().Where(c => c.Id == pid).Select(c => c.UserId)
                .FirstOrDefault()
            : _dbContext.Set<ProductReview>().AsNoTracking().Where(c => c.Id == rid).Select(c => c.ReviewerId)
                .FirstOrDefault();
    }

    private (uint? userid, int karma) GetCurrentVotes(ulong? pid, ulong? rid) {
        if (pid != null){
            var t = _dbContext.Set<ReviewComment>().AsNoTracking().Where(c => c.Id == pid)
                .Select(c => new{ c.UserId, Karma = c.Stats.Votes ?? 0 }).FirstOrDefault();
            return (t.UserId, t.Karma);
        }
        else{
            var t = _dbContext.Set<ProductReview>().AsNoTracking().Where(r => r.Id == rid)
                .Select(c => new {c.ReviewerId, Karma = c.Stats.Votes ?? 0})
                .FirstOrDefault();
            return (t.ReviewerId, t.Karma);
        }
    }

[BindProperty]
    public ReviewVote SentVote { get; set; }
    public void OnPostVote() {
        _reviewManager.Vote(CurrentSession, SentVote);
        var (uid, karma) = GetCurrentVotes(SentVote.CommentId, SentVote.ReviewId);
        if (!uid.HasValue ||karma is not (1 or 3 or 5 or 10 or 20 or 30) ) return;
        _notificationService.SendSingleAsync(new VoteNotification(){
            CommentId = SentVote.CommentId, ReviewId = SentVote.ReviewId, UserId = uid.Value,
            NumVotes = (uint)karma,
        }).Wait();
    }
    
    public void OnPostUnVote() {
        _reviewManager.UnVote(CurrentSession, SentVote);
    }
    [BindProperty(SupportsGet = true)]
    public uint ReviewsPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] 
    public uint CommentsPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public uint? ProductId { get; set; }
    [BindProperty( SupportsGet = true)]
    public uint? SellerId { get; set; }
    [BindProperty(SupportsGet = true)]
    public ulong ReviewId { get; set; }
    [BindProperty(SupportsGet = true)]
    public ulong? ParentId { get; set; }
    [BindProperty(SupportsGet = true)] 
    public int? NestLevel { get; set; }
    [BindProperty] public int Page { get; set; } = 1;
    [BindProperty] public int PageSize { get; set; } = 10;
    public IActionResult OnGetComments() {
        var comments = _reviewManager.GetCommentsWithAggregates(ReviewId, ParentId,  page: (int)CommentsPage, PageSize);
        if(comments.Count == 0) return new NotFoundResult();
        var votes = _reviewManager.GetUserVotesBatch(CurrentSession, CurrentCustomer, null, comments.Select(c => c.Id).ToArray());
        return Partial("Review/"+nameof(_CommentsPartial), new _CommentsPartial(){
            Comments = comments.Select(c=> new CommentUserView(c){
                CurrentUserVote = votes[c.Id],
            }).ToList(),
            NextPage = (int)CommentsPage +1,
            NestLevel = NestLevel ?? 1,
            IsStaff = CurrentStaff!=null,
            CurrentUserId = CurrentUser?.Id,
        });
    }
    public List<CommentUserView> Comments { get; set; }
    public IActionResult OnGetComment([FromQuery] ulong commentId, bool page = true) {
        var  c = _reviewManager.GetCommentTree(commentId, false, false);
        if (c == null){
            Comments =[];
            return page? Page(): new NoContentResult();
        }
        var ids = c.Replies.Select(c => c.Id).ToList();
        ids.Add(c.Id);
        var votes = _reviewManager.GetUserVotesBatch(CurrentSession, CurrentCustomer, null, ids);
        Comments = c.Replies.Select(c=> new CommentUserView(c){
            CurrentUserVote = votes[c.Id]
        }).ToList();
        return Page();
    }
    public List<ReviewUserView> GottenReviews { get; set; }
    public IActionResult OnGetReview([FromQuery] ulong reviewId, bool page = true) {
        var r = _reviewManager.GetProductReview(0, 0, CurrentCustomer, CurrentSession, false);
        if (r == null){
            SentReview = null;
            return page? Page(): new NoContentResult();
        }
        var votes = _reviewManager.GetUserVotesBatch(CurrentSession, CurrentCustomer, [r.Id], null);
        GottenReviews= [new ReviewUserView(r){
            CurrentUserVote = votes[r.Id],
        }];
        return Page();
    }
    public IActionResult OnGet([FromQuery] uint? selectedRating, [FromQuery] string? sortBy, [FromQuery] string? sortOrder) {
            var reviews = _reviewManager.GetReviewsWithAggregates(true, includeSeller: true, productId: ProductId,
                sellerId: SellerId, page: (int)ReviewsPage, pageSize:PageSize, selectedRating:selectedRating, sortBy: sortBy, sortDesc:sortOrder?.Equals("desc", StringComparison.OrdinalIgnoreCase));
        if (reviews.Count == 0) return new NoContentResult();
        var votes = _reviewManager.GetUserVotesBatch(CurrentSession, CurrentCustomer, reviews.Select(r => r.Id).ToArray(), null);
        return Partial("Review/"+nameof(_ReviewsPartial), new _ReviewsPartial{
            Reviews = reviews.Select(r=>new ReviewUserView(r){CurrentUserVote = votes[r.Id]}).ToArray(),
            NextPage = (int)ReviewsPage + 1,
            CurrentUserId = CurrentUser?.Id,
            IsStaff = CurrentUser is Staff,
            ProductId = ProductId,
        });
    }

}