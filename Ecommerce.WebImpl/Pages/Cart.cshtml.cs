using System.ComponentModel.DataAnnotations;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Ecommerce.WebImpl.Pages.Seller;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Mvc;

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
    public CartWithAggregates ViewedCart { get; set; }
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
            _cartManager.AddCoupon(s.Cart, new ProductOffer(){ProductId = ProductId, SellerId = (uint)SellerId!}, new Coupon(){Id = CouponId});
        }
        catch (ValidationException e){
            Console.WriteLine(e);
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = e.Message, Title = "İşlem Başarısız",
            });
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Ürüne kupon eklendi", Title = "İşlem Başarılı",
            Redirect = "/Cart"
        });
    }



    public PartialViewResult OnGetCoupon() {
        var coupons= _cartManager.GetAvailableCoupons(CurrentSession);
        return Partial("Shared/"+nameof(_CouponsPartial), new _CouponsPartial(){Coupons = coupons, Editable = false, ShowSeller = true});
    }
    public IActionResult OnPost() {
        var s = (Session)HttpContext.Items[nameof(Session)];
        SellerId ??= _productManager.Search([new SearchPredicate(){Operator = SearchPredicate.OperatorType.Equals, PropName = "Id", Value = ProductId.ToString()}], [new SearchOrder(){Ascending = true, PropName = string.Join('_', nameof(Entity.Product.Offers), nameof(ProductOffer.Price))}],pageSize:1,includeImage:false)
            .First().Offers.OrderBy(o=>o.Price).First().SellerId;
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
        ViewedCart = (CartWithAggregates)_cartManager.Get(s, true,true);
    }
}