using System.ComponentModel.DataAnnotations;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.WebImpl.Pages.Seller;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.WebImpl.Pages;

public class Cart : BaseModel
{
    private readonly ICartManager _cartManager;
    private readonly IProductManager _productManager;
    public Cart(ICartManager cartManager, IProductManager productManager) {
        _cartManager = cartManager;
        _productManager = productManager;
    }
    [BindProperty( Name = "productId")]
    public uint ProductId { get; set; }
    [BindProperty( Name = "sellerId")]
    public uint? SellerId { get; set; }
    [BindProperty( Name = "quantity")]
    public int? Quantity { get; set; }
    [BindProperty]
    public Entity.Cart ViewedCart { get; set; }
    public IActionResult OnPostDelete() {
        var s = (Session) HttpContext.Items[nameof(Session)];
        _cartManager.Remove(s.Cart, new ProductOffer(){SellerId = (uint)SellerId!, ProductId = ProductId});
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Message = "Ürün sepetten kaldırıldı.", Success = true,
            Redirect = "/Cart"
        });
    }
    [BindProperty] public string CouponId { get; set; }
    public IActionResult OnPostCoupon() {
        if (SellerId == null) throw new ArgumentNullException(nameof(SellerId));
        var s = (Session) HttpContext.Items[nameof(Session)];
        try{ 
            _cartManager.AddCoupon(s.Cart, new ProductOffer(){ProductId = ProductId, SellerId = (uint)SellerId!}, couponId:CouponId);
        }
        catch (ValidationException e){
            Console.WriteLine(e);
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = e.Message, Title = "İşlem Başarısız",
            });
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Ürüne kupon eklendi", Title = "İşlem Başarılı",
            Redirect = "/Cart", TimeOut = 1500
        });
    }

    public IActionResult OnDeleteCoupon() {
        if(CouponId == null) throw new ArgumentNullException(nameof(CouponId));
        _cartManager
    }


    public PartialViewResult OnGetCoupon() {
        var coupons= _cartManager.GetAvailableCoupons(CurrentSession);
        return Partial("Shared/"+nameof(_CouponsPartial), new _CouponsPartial(){Coupons = coupons, Editable = false, ShowSeller = true});
    }
    public IActionResult OnPost() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        SellerId ??= _productManager.GetOffers(ProductId,includeAggregates:false).OrderBy(o=>o.Price).FirstOrDefault()?.SellerId;
        if (SellerId == null)
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = "Ürünü satan bir satıcı bulunamadı.",
                Title = "Ürün Sepete Eklenemdi"
            });
        _cartManager.Add(s.Cart, new ProductOffer(){ ProductId = ProductId, SellerId = (uint)SellerId }, Quantity??1);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Sepet Güncellendi."
        });
    }
    // render items partial.
    // public IActionResult OnGetItemsAsync() {
    // }
    public void OnGet() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        ViewedCart = _cartManager.Get(s, true,true, true,includeSeller:true);
    }
}