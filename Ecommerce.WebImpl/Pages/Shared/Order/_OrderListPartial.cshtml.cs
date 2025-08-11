using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderListPartial
{
    public int Page { get; set; }
    public ICollection<OrderWithAggregates> Orders { get; set; } 
}