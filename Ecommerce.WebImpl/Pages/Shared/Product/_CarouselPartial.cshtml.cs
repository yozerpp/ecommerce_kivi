using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _CarouselPartial 
{
    public bool Editable { get; init; }
    public required ICollection<Image> Images { get; init; }
}