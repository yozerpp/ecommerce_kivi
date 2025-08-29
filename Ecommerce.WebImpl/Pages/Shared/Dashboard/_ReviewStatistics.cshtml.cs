using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _ReviewStatistics
{
    public ICollection<ProductReview> Reviews { get; init; } = new List<ProductReview>();
    public Entity.User.UserRole ViewerRole { get; init; } = Entity.User.UserRole.Customer;
    
}
