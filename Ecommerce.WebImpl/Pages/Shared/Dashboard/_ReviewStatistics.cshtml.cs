using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _ReviewStatistics
{
    public ICollection<ProductReview> Reviews { get; init; } =[];
    public User.UserRole ViewerRole { get; init; } = User.UserRole.Customer;
    
}