using Ecommerce.Bl.Concrete;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Mail;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Order;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class Orders : BaseModel
{
    private readonly IMailService _mailService;
    private readonly IJwtManager _jwtManager;
    private readonly IOrderManager _orderManager;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly ISellerManager _sellerManager;
    private readonly StaffBag _staves;
    private readonly DbContext _dbContext;
    private IShippingService _shippingService;

    public Orders(IMailService mailService, IJwtManager jwtManager, IOrderManager orderManager, IRepository<Order> orderRepository, INotificationService notificationService, IRepository<OrderItem> orderItemRepository, StaffBag staves, IShippingService shippingService, [FromKeyedServices("DefaultDbContext")]DbContext dbContext, ISellerManager sellerManager) : base(notificationService) {
        _mailService = mailService;
        _jwtManager = jwtManager;
        _orderManager = orderManager;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _staves = staves;
        _shippingService = shippingService;
        _dbContext = dbContext;
        _sellerManager = sellerManager;
    }
    public enum ViewType
    {
        Auth,
        Show,
    }
    public ViewType View { get; set; }
    public ICollection<Order> OrderCollection { get; set; }
    [BindProperty(SupportsGet = true, Name = "token")]
    public string Token { get; set; }
    [BindProperty]
    public Address NewAddress { get; set; }
    [BindProperty(SupportsGet = true)]
    public uint OrderId { get; set; }
    [BindProperty]
    public uint SellerId { get; set; }
    [BindProperty]
    public uint ProductId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public IActionResult OnGet([FromQuery] bool partial = false, [FromQuery] int page = 1,  [FromQuery] int pageSize = 5) {
        PageNumber = page;
        PageSize = pageSize;
        if (CurrentCustomer !=null){
            View = ViewType.Show;
            OrderCollection = OrderId != default 
                ? [_orderManager.GetOrder(OrderId, true, true)] 
                : _orderManager.GetAllOrdersFromCustomer(CurrentCustomer, true, page, pageSize);
        }else if (CurrentSeller != null){
            View = ViewType.Show;
            OrderCollection = _sellerManager.GetOrders(CurrentSeller.Id, OrderId, true);
        }else if (Token == null){
            View = ViewType.Auth;
            return Page();
        }else{
            var email = _jwtManager.ReadAuthToken(Token);
            if (email == null){
                return RedirectToPage("/Unauthorized", new {Message = $"Oturumun süresi doldu, lütfen emailinizle yeni link alın."});
            }
            View = ViewType.Show;
            
            OrderCollection = OrderId==default
            ?_orderManager.GetAllOrdersFromAnonymousUser(email, true, page, pageSize)
            :[_orderManager.GetOrder(OrderId, true, true)];
        }
        if (OrderCollection.Count == 0 && partial) return new NoContentResult();
        return Page();
    }
    public IActionResult OnPost([FromForm] string email) {
        var token = _jwtManager.CreateAuthToken(email, TimeSpan.FromMinutes(10));
        _mailService.SendAsync(email, "Sipariş Bilgileriniz",
            "Siparişlerinizi görüntülemek için lütfen aşağıdaki bağlantıya tıklayın:\n" + Url.Page(nameof(Orders), "view", new {token, OrderId},Request.Scheme));
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Title = "Doğrulama Bekleniyor",
            Message =
                "Lütfen e-posta adresinizi kontrol edin ve sipariş bilgilerinizi görüntülemek için bağlantıya tıklayın.",
            TimeOut = 1000000,
        });
    }
    private void DoAuth() {
        uint? id;
        if ((id=CurrentCustomer?.Id) != null && _orderRepository.Exists(o => o.Id == OrderId && o.UserId == id.Value) ||
            (_jwtManager.ReadAuthToken(Token)?.Equals(_orderRepository.FirstP(o => o.Email, o => o.Id == OrderId, nonTracking: true)) ?? false))
            return;
        throw new UnauthorizedAccessException("Bu siparişi görüntüleme yetkiniz yok.");
    }
    public async Task<IActionResult> OnPostCancel() {
        DoAuth();
        if (!_orderRepository.Exists(o =>
                o.Items.All(i => i.Status <= OrderStatus.WaitingConfirmation || i .Status == OrderStatus.Cancelled || i.Status == OrderStatus.CancellationRequested) && o.Id == OrderId))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Message =
                    "Siparişinizdeki bazı ürünler işleme alındığı için iptal edilemiyor. Ürünleri tek, tek iptal etmeyi deneyin."
            });
        _orderManager.ChangeOrderStatus(new Order(){Id = OrderId}, OrderStatus.CancellationRequested, true);
        _orderManager.RefreshOrderStatus(OrderId);
        await NotificationService.SendBatchAsync(_staves.WithPermission(Permission.EditOrder).Select(s =>
            new CancellationRequest(){
                UserId = s.Id,
                RequesterId = CurrentCustomer?.Id,
                OrderId = OrderId,
            }).ToArray());
        return Partial(nameof(_InfoPartial),new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "İptal etme isteğiniz bildirildi.", Redirect = "refresh",
        });
    }

    public IActionResult OnPostProgressShipment() {
        var oit = _orderItemRepository.First(o=>o.OrderId ==OrderId && o.ProductId == ProductId && o.SellerId == SellerId, includes: [[nameof(OrderItem.SentShipment)]], nonTracking:false);
        var apiShipment = _shippingService.GetStatus(oit.SentShipment.ApiId).Result;
        if ((oit.SentShipment.Status = apiShipment.ShipmentStatus) == ShipmentStatus.Delivered){
            oit.Status = OrderStatus.Delivered;
        }
        _dbContext.ChangeTracker.DetectChanges();
        _dbContext.SaveChanges();
        _orderManager.RefreshOrderStatus(OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Gönderi durumu güncellendi.",Redirect = "refresh"
        });
    }
    public async Task<IActionResult> OnPostCancelItem() {
        DoAuth();
        var oldStatus = _orderItemRepository.FirstP(o=>o.Status, item => item.OrderId == OrderId && item.ProductId==ProductId && item.SellerId == SellerId, nonTracking:true);
        _orderManager.ChangeItemStatus([new OrderItem(){OrderId = OrderId, SellerId = SellerId, ProductId = ProductId, Status = OrderStatus.Cancelled}]);
        if (oldStatus > OrderStatus.WaitingConfirmation){
            await NotificationService.SendSingleAsync(new CancellationRequest(){
                UserId = SellerId, OrderId = OrderId, RequesterId = CurrentCustomer?.Id
            });
        }
        else{
            await NotificationService.SendBatchAsync(_staves.WithPermission(Permission.EditOrder).Select(s =>
                new CancellationRequest(){
                    UserId = s.Id,
                    OrderId = OrderId,
                    RequesterId = CurrentCustomer?.Id
                }).ToArray());
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Siparişinizi iptal etme talebi iletildi.",Redirect = "refresh"
        });
    }
    // public IActionResult OnPostChangeAddress() {
    //     DoAuth(); 
    //     _shippingService
    //     _orderManager.ChangeAddress(NewAddress, OrderId);
    //     return Partial(nameof(_InfoPartial), new _InfoPartial(){
    //         Success = true, TimeOut = 2000, Title = "Adresiniz Değiştirildi.",
    //         Redirect = "refresh"
    //     });
    // }
    public IActionResult OnPostConfirm() {
        DoAuth();
        _orderManager.ChangeItemStatus([new OrderItem(){OrderId = OrderId, SellerId = SellerId, ProductId = ProductId, Status = OrderStatus.Complete}]);
        NotificationService.SendSingleAsync(new OrderCompletionNotification(){
            OrderId = OrderId, UserId = SellerId, ProductId = ProductId
        }).Wait();
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "Sipariş Teslimatı Onaylandı",
            Redirect = "refresh"
        });
    }
    public IActionResult OnPostRefund() {
        DoAuth();
        var i= new OrderItem(){OrderId = OrderId, SellerId = SellerId, ProductId = ProductId};
        var e= _dbContext.Attach(i);
        e.Property(s => s.Status).CurrentValue = OrderStatus.ReturnRequested;
        var r = e.Reference<RefundRequest>(i=>i.RefundRequest).CurrentValue = new RefundRequest(){
            OrderId = OrderId, UserId = SellerId, ProductId = ProductId, RequesterId = CurrentCustomer?.Id
        };
        _dbContext.SaveChanges();
        _orderManager.RefreshOrderStatus(OrderId);
        NotificationService.SendSingleAsync(r);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "İade Talebiniz Satıcıya İletildi.", Redirect = "refresh"
        });
    }
}
