using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.SearchBar;

public class _DimensionsPartial
{
    public string ClassNames { get; init; } = string.Empty;
    public required Dimensions Dimensions { get; init; }
    public bool Editable { get; init; }
}