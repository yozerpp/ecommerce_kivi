using System.ComponentModel.DataAnnotations;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Pages.Seller;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.CartPartials;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.WebImpl.Pages;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(AnonymousCustomer))]
public class Cart : BaseModel
{
    private readonly ICartManager _cartManager;
    private readonly IProductManager _productManager;
    public Cart(INotificationService notificationService, ICartManager cartManager, IProductManager productManager): base(notificationService) {
        _cartManager = cartManager;
        _productManager = productManager;
    }

    [BindProperty] public ICollection<ProductOption> Options { get; set; } =[];
    [BindProperty( Name = "productId")]
    public uint ProductId { get; set; }
    [BindProperty( Name = "sellerId")]
    public uint? SellerId { get; set; }
    [BindProperty( Name = "quantity")]
    public int? Quantity { get; set; }
    [BindProperty]
    public Entity.Cart ViewedCart { get; set; }

    [BindProperty] public bool FromCart { get; set; }

    public IActionResult OnPostDelete() {
        var s = (Session) HttpContext.Items[nameof(Session)];
        _cartManager.Remove(s.Cart, new ProductOffer(){SellerId = (uint)SellerId!, ProductId = ProductId});
        return FromCart?
            Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()})
            :Partial(nameof(_InfoPartial), new _InfoPartial(){
            Message = "Ürün sepetten kaldırıldı.", Success = true,
            Redirect = "/Cart"
        });
    }

    public IActionResult OnPostClear() {
        _cartManager.Clear(CurrentSession.CartId);
        return FromCart
            ? Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()})
            : Partial(nameof(_InfoPartial), new _InfoPartial(){Success = true, Message = "Sepetiniz boşaltıldı.", Redirect = "/Cart"});
    }
    [BindProperty] public string CouponId { get; set; }
    public IActionResult OnPostCoupon() {
        if (SellerId == null) throw new ArgumentNullException(nameof(SellerId));
        var s = (Session) HttpContext.Items[nameof(Session)];
        try{ 
            _cartManager.AddCoupon(CurrentSession.Cart, new ProductOffer(){ProductId = ProductId, SellerId = (uint)SellerId!}, couponId:CouponId);
        }
        catch (ValidationException e){
            Console.WriteLine(e);
            Response.StatusCode = 400;
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = e.Message, Title = "İşlem Başarısız",
            });
        }
        
        return FromCart? 
            Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()})
            :Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Ürüne kupon eklendi", Title = "İşlem Başarılı",
            Redirect = "refresh", TimeOut = 1500
        });
    }

    public IActionResult OnPostDeleteCoupon() {
        if (SellerId == null) throw new ArgumentNullException(nameof(SellerId));
        _cartManager.RemoveCoupon(CurrentSession.Cart, new ProductOffer(){ProductId = ProductId, SellerId = (uint)SellerId!});
        return FromCart
            ?Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()})
        :Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Kupon üründen kaldırıldı.", Title = "İşlem Başarılı",
            Redirect = "/Cart", TimeOut = 1500
        });
    }

    public IActionResult OnGetPartial() {
        return Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()});
    }
    public IActionResult OnGetCoupon() {
        var coupons= _cartManager.GetAvailableCoupons(CurrentSession);
        if (coupons.Count == 0) return new NoContentResult();
        return Partial("Shared/"+nameof(_CouponsPartial), new _CouponsPartial(){Coupons = coupons, Editable = false, ShowSeller = true});
    }
    public IActionResult OnPost() {
        SellerId ??= _productManager.GetOffers(ProductId,includeAggregates:false).OrderBy(o=>o.Price).FirstOrDefault()?.SellerId;
        if (SellerId == null){
            Response.StatusCode = 400;
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = "Ürünü satan bir satıcı bulunamadı.",
                Title = "Ürün Sepete Eklenemdi"
            });
        }
            
        _cartManager.Add(new CartItem(){ ProductId = ProductId, SellerId = (uint)SellerId, CartId = CurrentSession.CartId,SelectedOptions = Options}, Quantity??1);
        
        return FromCart
            ? Partial("Shared/CartPartials/"+nameof(_CartItemsPartial), new _CartItemsPartial(){ViewedCart= GetCart()})
            : Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Sepet Güncellendi."
        });
    }
    // render items partial.
    // public IActionResult OnGetItemsAsync() {
    // }
    private Entity.Cart GetCart() => _cartManager.Get(CurrentSession, true, true, true, includeSeller: true);
    public void OnGet() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        ViewedCart = GetCart();
    }
}
