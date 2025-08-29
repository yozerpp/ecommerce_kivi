using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _FeaturedItemsPartial 
{    
    public required ICollection<ProductWithAggregatesCustomerView> Items { get; init; } 
    public required IDictionary<uint,Category> Categories { get; init; }
    public required int PageIndex { get; init; }
}