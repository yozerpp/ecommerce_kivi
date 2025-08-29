using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _CarouselPartial 
{
    public bool Editable { get; init; }
    public bool Creating { get; init; }
    public required ICollection<ImageProduct> Images { get; init; }
    public int PageSize { get; init; }= 3;
    public required string FetchUrl { get; init; }
}