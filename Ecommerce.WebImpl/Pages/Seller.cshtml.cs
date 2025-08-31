using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Seller;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Order;
using Ecommerce.WebImpl.Pages.Shared.Product;
using LinqKit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

public class SellerModel : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    private readonly IRepository<Coupon> _couponRepository;
    private IOrderManager _orderManager;
    private IShippingService _shippingService;
    private readonly DbContext _dbContext;

    public SellerModel(INotificationService notificationService,IProductManager productManager, ISellerManager sellerManager, IReviewManager reviewManager, IRepository<Coupon> couponRepository, IOrderManager orderManager, IRepository<Order> orderRepository, IShippingService shippingService, [FromKeyedServices("DefaultDbContext")]DbContext dbContext): base(notificationService){
        _productManager = productManager;
        _couponRepository = couponRepository;
        _orderManager = orderManager;
        _shippingService = shippingService;
        _dbContext = dbContext;
        _sellerManager = sellerManager;
    }
    [BindProperty]
    public ProductOffer OfferToEdit { get; set; }
    [BindProperty] public Entity.Seller ViewedSeller { get; set; }
    [BindProperty]
    public uint ProductId { get; set; }
    public PartialViewResult OnGetOffers() {
        var offers = _sellerManager.GetOffers(Id, OffersPage, OffersPageSize);
        return Partial("Seller/_OffersPartial", new _OffersPartial(){
            Editable = CurrentSeller?.Id == Id,
            ProductOffers = offers,
            OffersPage = OffersPage,
            OffersPageSize = OffersPageSize,
            Id = Id,
        });
    }
    [HasRole(nameof(Entity.Seller))]
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
    public uint OrderId { get; set; }
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostConfirmOrder() {
        _orderManager.ChangeStatusBySellerIdOrderId(OrderId, CurrentSeller.Id, OrderStatus.WaitingShipment);
        return Partial("Shared/_InfoPartial", new _InfoPartial(){
            Success = true, Message = "Sipariş Durumu Güncellendi.", Redirect = "refresh"
        });
    }    
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostConfirmShipment() {
        var items = _orderManager.GetOrderItemsBySellerIdOrderId(OrderId, CurrentSeller.Id, [[nameof(OrderItem.SentShipment)]]);
        items.ForEach(i=> {
            var e =_dbContext.ChangeTracker.Entries<OrderItem>().FirstOrDefault(s => s.Entity.Equals(i))
             ?? _dbContext.Entry(i);
            e.State = EntityState.Modified; 
            e.Property<OrderStatus>(e => e.Status).CurrentValue = OrderStatus.Shipped;
        });
        var shipment = items.First().SentShipment;
        if(shipment == null) throw new InvalidOperationException("Bu siparişe ait gönderi bilgisi bulunamadı.");
        var e = _dbContext.ChangeTracker.Entries<Shipment>().FirstOrDefault(s => s.Entity.Equals(shipment))
            ?? _dbContext.Entry(shipment);
        e.State = EntityState.Modified;
        e.Property<ShipmentStatus>(e=>e.Status).CurrentValue =ShipmentStatus.InTransit; 
        shipment.Status = ShipmentStatus.InTransit;
        _dbContext.SaveChanges();
        return Partial("Shared/_InfoPartial", new _InfoPartial(){
            Success = true, Message = "Sipariş Durumu Güncellendi.", Redirect = "refresh"
        });
    }

    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostAcceptRefund() {
        var sid = CurrentSeller.Id;
        var i = _dbContext.Set<OrderItem>().Include(o=>o.SentShipment).FirstOrDefault(i=>i.ProductId == ProductId && i.OrderId == OrderId && i.SellerId == sid);
        if (i == null) return NotFound("Öğe bulunamadı.");
        i.Status = OrderStatus.ReturnApproved;
        var refShipment = _shippingService.Refund(i.SentShipment.ApiId, (int)i.Quantity).Result;
        i.RefundShipment = new Entity.Shipment(){
            ApiId = refShipment.ApiId,
            Status = refShipment.ShipmentStatus,
            TrackingNumber = refShipment.TrackingNumber,
            Cost = refShipment.Price + refShipment.Tax,
            OrderItems = new List<OrderItem>{ i },
            Provider = refShipment.Provider.Name,
            RecepientAddress = refShipment.Recipient.Address,
            SenderAddress = refShipment.Sender.Address,
        };
        _dbContext.SaveChanges();
        _orderManager.RefreshOrderStatus(OrderId);
        return Partial("Shared/_InfoPartial", new _InfoPartial(){
            Success = true, Message = "İade onaylandı.", Redirect = "refresh"
        });
    }

    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostDenyRefund() {
        var i = _dbContext.Set<OrderItem>().Include(i=>i.RefundRequest)
            .FirstOrDefault(i => i.ProductId == ProductId && i.OrderId == OrderId && i.SellerId == CurrentSeller.Id);
        if(i==null) return NotFound("Öğe bulunamadı.");
        i.Status = OrderStatus.ReturnDenied;
        var r = i.RefundRequest;
        r.IsApproved = false;
        r.TimeAnswered = DateTimeOffset.Now.DateTime;
        _dbContext.ChangeTracker.DetectChanges();
        _dbContext.SaveChanges();
        _orderManager.RefreshOrderStatus(OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "İade reddedildi.", Redirect = "refresh"
        });
    }
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostCancelOrder() {
        var sid = CurrentSeller.Id;
        _orderManager.ChangeStatusBySellerIdOrderId(OrderId, sid, OrderStatus.CancelledBySeller);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Siparişte size ait bulunan ürünler iade edildi.", Redirect = "refresh"
        });
    }
    public IActionResult OnGetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        if(Id != CurrentSeller?.Id) {        
            throw new UnauthorizedAccessException("Başkasının siparişlerini göremezsiniz.");
        }
        return Partial("Shared/Order/_OrderListPartial", new _OrderListPartial(){
            Collapsable = true, Editable = true, Orders = _sellerManager.GetOrders(Id,page: page,pageSize: pageSize),
            Page = page, PageSize = pageSize, Url = Url.Page(nameof(Seller), "orders", new { CurrentSeller.Id}),
            ViewedBySeller = true
        });
    }

    public IActionResult OnGetOrder([FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var orders = _sellerManager.GetOrders(CurrentSeller.Id, orderId: OrderId, true, page: page, pageSize: pageSize);
        if(orders.Count == 0) throw new UnauthorizedAccessException("Bu siparişi görüntüleme yetkiniz yok.");
        return Partial("Shared/Order/_OrderListPartial", new _OrderListPartial{
            Url = null,
            Orders = orders,
            Collapsable = false,
            ViewedBySeller = true,Editable = true
        });
    }
    [BindProperty]
    public Coupon Coupon { get; set; }
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostCoupon() {
        _sellerManager.CreateCoupon(CurrentSeller, Coupon);
        return Partial("Shared/_InfoPartial", new _InfoPartial(){
            Success = true, Message = "Kuponunuz oluşturuldu.", Redirect = "refresh"
        });
    }
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostDeleteOffer([FromQuery] string couponId) {
        var cid = CurrentSeller?.Id;
        if (_couponRepository.UpdateExpr([(c=>c.ExpirationDate, default(DateTime))],c => c.Id == couponId && c.SellerId == cid) 
            == 0){
            throw new UnauthorizedAccessException("Başkasınını Kuponunu Silemezsiniz.");
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Kuponunuz silindi.",
        });
    }
    [HasRole(nameof(Entity.Seller))]
    public IActionResult OnPostEdit() {
        var sid = CurrentSeller?.Id;
        if(sid != ViewedSeller.Id) throw new UnauthorizedAccessException("Başkasının Bilgilerini Düzenleyemezsiniz.");
        ViewedSeller.NormalizedEmail = ViewedSeller.Email.ToUpperInvariant();
        var e = _dbContext.ChangeTracker.Entries<Entity.Seller>().FirstOrDefault(s => s.Entity.Equals(CurrentSeller));
        if (e != null) e.State = EntityState.Detached;
        _sellerManager.UpdateSeller(ViewedSeller);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Bilgileriniz güncellendi.",Redirect = "refresh"
        });
    }
    [BindProperty(SupportsGet = true)]
    public uint Id { get; set; }

    [BindProperty(SupportsGet = true)] public int OffersPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int OffersPageSize { get; set; } = 20;
    [BindProperty(SupportsGet = true)] public int ReviewsPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int ReviewsPageSize { get; set; } = 20;
    public IActionResult OnGet() {
        var s = _sellerManager.GetSeller(Id, false, false, true);
        if (s == null!) return new NotFoundResult();
        ViewedSeller = s;
        return Page();
    }
}