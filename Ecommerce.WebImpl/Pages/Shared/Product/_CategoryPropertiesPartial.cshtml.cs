using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _CategoryPropertiesPartial
{
    public required ICollection<ProductCategoryProperty> Properties { get; init; }
    public string? InputNamePrefix { get; init; }
    public bool Editable { get; init; }
    [Flags]
    public enum DisplayMode
    {
        Filter = 1,
        View = 2,
        Create = 4
    }
    public DisplayMode Mode { get; init; }
}