using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _CategoryPropertiesPartial
{
    public required ICollection<Category.CategoryProperty> Properties { get; init; }
    public string? InputNamePrefix { get; init; }
    public bool IsEditable { get; init; }
    public bool AsFilter { get; init; } = true;
}