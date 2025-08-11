using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _ReviewStarPartial 
{
    public required decimal Rating { get; init; }
    public int? RatingCount { get; init; } = null;
}