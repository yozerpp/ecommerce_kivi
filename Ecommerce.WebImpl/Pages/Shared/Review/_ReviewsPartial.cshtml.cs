using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _ReviewsPartial
{
    public uint? ProductId { get; init; }
    public ICollection<ReviewUserView> Reviews { get; init; }
    public int NextPage { get; init; }
    public uint? CurrentUserId { get; init; }
    public bool IsStaff { get; init; }
}