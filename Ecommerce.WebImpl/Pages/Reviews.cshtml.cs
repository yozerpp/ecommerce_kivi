using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Review;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

public class Reviews : BaseModel
{
    private readonly IReviewManager _reviewManager;

    public Reviews(IReviewManager reviewManager) {
        _reviewManager = reviewManager;
    }
    [BindProperty] 
    public ProductReview? SentReview { get; set; }
    public IActionResult OnPost() {
        try{
            _reviewManager.LeaveReview(CurrentSession, SentReview);
        }catch (ArgumentException e){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Hata",
                Message = e.Message,
            });
        }
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
    [BindProperty]
    public ReviewComment? SentComment { get; set; }
    [HasRole(nameof(Staff), nameof(Entity.Customer))]
    public IActionResult OnPostDeleteAsync() {
        if(SentComment!=null && SentComment.Id!=default)
            _reviewManager.DeleteComment(null, SentComment);
        else if (SentReview != null && SentReview.Id != default)
            _reviewManager.DeleteReview(null, SentReview);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Yorum silindi.",
        });
    }
    public IActionResult OnPostCommentAsync() {
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
        return Partial("_InfoPartial", new _InfoPartial(){
            Success = true,
            Message = "Yorum eklendi.",
        });
    }
    [BindProperty]
    public ReviewVote SentVote { get; set; }
    public void OnPostVote() {
        _reviewManager.Vote(CurrentSession, SentVote);
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