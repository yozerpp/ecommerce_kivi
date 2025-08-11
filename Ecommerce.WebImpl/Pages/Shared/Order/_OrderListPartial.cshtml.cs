using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderListPartial
{
    public int Page { get; set; }
    public ICollection<Entity.Order> Orders { get; set; } 
}