using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Dashboard;

public class _StatisticsPartial
{
    public ICollection<OrderWithAggregates> Orders { get; set; }
}