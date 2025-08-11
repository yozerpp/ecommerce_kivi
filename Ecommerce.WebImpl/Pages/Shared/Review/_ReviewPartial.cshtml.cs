using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _ReviewsPartial
{
    [BindProperty]
    public ICollection<ReviewWithAggregates> Reviews { get; init; }
    [BindProperty]
    public int NextPage { get; init; }
    public uint? CurrentUserId { get; init; }
    public bool IsStaff { get; init; }
}