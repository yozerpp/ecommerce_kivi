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
    public IActionResult OnPostAsync() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        try{
            _reviewManager.LeaveReview(s, SentReview);
        }
        catch (ArgumentException e){
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
        var s = (Session)HttpContext.Items[nameof(Session)];
        try{
            _reviewManager.CommentReview(s, SentComment);
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
    public void OnPostVoteAsync() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        _reviewManager.Vote(s, SentVote);
    }

    public void OnPostUnVoteAsync() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        _reviewManager.UnVote(s, SentVote);
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
    [BindProperty] public int PageSize { get; set; } = 20;
    public PartialViewResult OnGetComments() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        var c = (Entity.Customer?)HttpContext.Items[nameof(Entity.Customer)];
        var comments = _reviewManager.GetCommentsWithAggregates(ReviewId, ParentId, c, s, page: (int)CommentsPage);
        return Partial("Review/"+nameof(_CommentsPartial), new _CommentsPartial(){
            Comments = comments,
            NextPage = (int)CommentsPage +1,
            NestLevel = NestLevel ?? 1,
            IsStaff = CurrentStaff!=null,
            CurrentUserId = CurrentUser?.Id,
        });
    }
    public PartialViewResult OnGet() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        var c = (Entity.Customer?)HttpContext.Items[nameof(Entity.Customer)];
        var reviews = _reviewManager.GetReviewsWithAggregates(true, c, s, includeSeller: true, productId: ProductId,
                sellerId: SellerId, page: (int)ReviewsPage, pageSize:PageSize);
        return Partial("Review/"+nameof(_ReviewsPartial), new _ReviewsPartial{
            Reviews = reviews,
            NextPage = (int)ReviewsPage + 1,
            CurrentUserId = CurrentUser?.Id,
            IsStaff = CurrentUser is Staff,
        });
    }

}