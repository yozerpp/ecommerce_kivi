using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _VerticalListPartial
{
    public required ICollection<ProductWithAggregatesCustomerView> Products { get; init; }
    public required Dictionary<uint, Category> Categories { get; init; } 
    public ICollection<ProductWithAggregatesCustomerView>? CategoryRecommendations { get; init; }
    public ICollection<ProductWithAggregatesCustomerView>? SellerRecommendations { get; init; }
    public required int PageIndex { get; init; } 
}