using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ImagePartial
{
    public int Idx { get; init; }
    public required string Src { get; init; }
    public bool Editable { get; init; }
    public bool IsBase64 { get; init; } = true;
}