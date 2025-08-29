using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ListView 
{
    public required IDictionary<uint,Category> Categories { get; set; }
    public required ICollection<Entity.Product> Products { get; init; }
}