
using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Seller;

public class _CreateCategoryPartial 
{
    public required ICollection<Category> Categories { get; init; }
    public bool ForProductTemplate { get; init; }
    public required string Target { get; init; } 
}