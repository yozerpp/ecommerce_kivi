using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _StatisticsPartial
{
    public ICollection<Entity.Order> Orders { get; init; }
    public User.UserRole ViewerRole { get; init; }
}