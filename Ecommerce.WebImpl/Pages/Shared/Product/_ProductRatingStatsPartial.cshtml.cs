using Ecommerce.Entity.Views;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ProductRatingStatsPartial 
{
    public required ProductRatingStats  ReviewStats { get; init; }
}