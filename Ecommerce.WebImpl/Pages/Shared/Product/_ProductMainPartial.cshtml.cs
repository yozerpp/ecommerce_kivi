using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ProductMainPartial 
{
    public bool Editable { get; init; }
    public bool StaffVisiting { get; init; }
    public Entity.Product ViewedProduct { get; init; }
    public ProductOffer SellerOffer { get; init; }
    public ICollection<uint> Favorites { get; init; }
    public bool Creating { get; init; }
}