using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _OfferListPartial 
{
    public required ICollection<(uint? existingQuantity, ProductOffer offer)> Offers { get; set; } 
    public required uint? ViewingSellerId { get; init; }
}