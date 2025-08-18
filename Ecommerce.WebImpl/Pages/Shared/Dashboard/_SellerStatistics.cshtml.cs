using Ecommerce.Entity.Views;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _SellerStatistics : PageModel
{
    public SellerStats Stats { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<ProductReview> Reviews { get; set; } = new();
    public Entity.User.UserRole ViewerRole { get; set; } = Entity.User.UserRole.Seller;
    
    public decimal TotalRevenue => Orders.Sum(o => o.Aggregates.CouponDiscountedPrice);
    public decimal TotalDiscountGiven => Orders.Sum(o => o.Aggregates.TotalDiscountAmount);
    public int TotalOrdersCount => Orders.Count;
    public double AverageOrderValue => Orders.Any() ? (double)TotalRevenue / TotalOrdersCount : 0;
    
    public int TotalReviewsCount => Reviews.Count;
    public double AverageRating => Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;
    public int FiveStarReviews => Reviews.Count(r => r.Rating == 5);
    public int FourStarReviews => Reviews.Count(r => r.Rating == 4);
    public int ThreeStarReviews => Reviews.Count(r => r.Rating == 3);
    public int TwoStarReviews => Reviews.Count(r => r.Rating == 2);
    public int OneStarReviews => Reviews.Count(r => r.Rating == 1);
    
    public double ReviewSatisfactionRate => Reviews.Any() ? (double)Reviews.Count(r => r.Rating >= 4) / TotalReviewsCount * 100 : 0;
}
