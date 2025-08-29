using Ecommerce.Entity;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

public class _FeaturedPartial 
{
    public required ICollection<ProductWithAggregatesCustomerView> Products { get; set; }
    public required HomepageModel.RecommendationType? Type { get; set; }
    public required IDictionary<uint,Category> Categories { get; init; }
    public required int PageIndex { get; init; }
}