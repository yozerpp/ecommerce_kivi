using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.WebImpl.Pages.Seller;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

public class SellerModel : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    private readonly IReviewManager _reviewManager;
    public SellerModel(IProductManager productManager, ISellerManager sellerManager, IReviewManager reviewManager) {
        _productManager = productManager;
        _reviewManager = reviewManager;
        _sellerManager = sellerManager;
    }
    [BindProperty]
    public ProductOffer OfferToEdit { get; set; }

    public PartialViewResult OnGetOffers() {
        var offers = _sellerManager.GetOffers(Id, OffersPage, OffersPageSize);
        return Partial(nameof(_OffersPartial), new _OffersPartial(){
            Editable = CurrentSeller?.Id == Id,
            ProductOffers = offers,
            OffersPage = OffersPage,
            OffersPageSize = OffersPageSize,
            Id = Id,
        });
    }
    public IActionResult OnPostDeleteOffer() {
        var s = (Entity.Seller?) HttpContext.Items[nameof(Entity.Seller)];
        if (s == null || s.Id != OfferToEdit.SellerId)
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Message = "Bu ürüne ait bir ilanınız yok.",
                Title = "Yetkisiz işlem"
            });
        _productManager.UnlistOffer(OfferToEdit);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "İlanınız silindi.",
        });
    }
    [BindProperty(SupportsGet = true)]
    public uint Id { get; set; }

    [BindProperty(SupportsGet = true)] public int OffersPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int OffersPageSize { get; set; } = 20;
    [BindProperty(SupportsGet = true)] public int ReviewsPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int ReviewsPageSize { get; set; } = 20;
    [BindProperty] public Entity.Seller ViewedSeller { get; set; }
    public IActionResult OnGet() {
        var s = _sellerManager.GetSeller(Id, false, false, true);
        if (s == null!) return new NotFoundResult();
        ViewedSeller = s;
        return Page();
    }
}