using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ProductOptionPartial
{
    public required ICollection<(bool selected, ProductOption option)> Options{get;init;}
    public required uint? PropertyId { get; init; }
    public required string? CustomKey { get; init; }
    public bool Editable { get; init; }
}