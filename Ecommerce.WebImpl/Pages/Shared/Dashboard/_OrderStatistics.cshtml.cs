using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _OrderStatisticsPartial
{
    public ICollection<Entity.Order> Orders { get; init; }
    public Entity.User.UserRole ViewerRole { get; init; }
}