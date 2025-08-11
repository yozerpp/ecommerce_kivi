using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Seller;

public class _OffersPartial
{
    [BindProperty]
    public ICollection<ProductOffer> ProductOffers { get; init; }
    [BindProperty]
    public uint Id { get; set; }
    [BindProperty]
    public bool Editable { get; init; }
    [BindProperty]
    public int OffersPage { get; init; } = 1;

    [BindProperty] public int OffersPageSize { get; init; } = 20;
}