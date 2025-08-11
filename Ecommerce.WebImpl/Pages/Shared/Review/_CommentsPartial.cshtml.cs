using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _CommentsPartial 
{
    [BindProperty]
    public int NextPage { get; set; }

    [BindProperty] public int NestLevel { get; set; } = 1;
    public List<ReviewComment> Comments { get; set; } 
    public uint? CurrentUserId { get; init; }
    public bool IsStaff { get; init; } 
}
