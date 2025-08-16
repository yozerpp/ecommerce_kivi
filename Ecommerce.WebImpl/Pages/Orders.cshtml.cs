using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Mail;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;

public class Orders : BaseModel
{
    private readonly IMailService _mailService;
    private readonly IJwtManager _jwtManager;
    private readonly IOrderManager _orderManager;
    private readonly IRepository<Order> _orderRepository;
    public Orders(IMailService mailService, IJwtManager jwtManager, IOrderManager orderManager, IRepository<Order> orderRepository) {
        _mailService = mailService;
        _jwtManager = jwtManager;
        _orderManager = orderManager;
        _orderRepository = orderRepository;
    }
    public enum PageType
    {
        Email,
        View
    }
    public PageType Type { get; set; }
    public ICollection<Order> OrderCollection { get; set; }
    [BindProperty(SupportsGet = true, Name = "token")]
    public string Token { get; set; }
    [BindProperty]
    public Address NewAddress { get; set; }
    [BindProperty(SupportsGet = true)]
    public uint OrderId { get; set; }

    public void OnGet() {
        Type = PageType.Email;
    }
    public IActionResult OnPost([FromForm] string email) {
        var token = _jwtManager.CreateAuthToken(email, TimeSpan.FromMinutes(10));
        _mailService.SendAsync(email, "Sipariş Bilgileriniz",
            "Siparişlerinizi görüntülemek için lütfen aşağıdaki bağlantıya tıklayın:\n" + Url.Page(nameof(Orders), "view", new {token},Request.Scheme));
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Title = "Doğrulama Bekleniyor",
            Message =
                "Lütfen e-posta adresinizi kontrol edin ve sipariş bilgilerinizi görüntülemek için bağlantıya tıklayın."
        });
    }
    public IActionResult OnGetView([FromQuery] int page = 1, [FromQuery] int pageSize = 10 ) {
        var email = _jwtManager.ReadAuthToken(Token);
        if (email == null) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Lütfen e-posta adresinizi kontrol edin ve tekrar deneyin.",
                TimeOut = 10000,
            });
        }
        Type = PageType.View;
        OrderCollection = _orderManager.GetAllOrdersFromAnonymousUser(email, true, page: page, pageSize: pageSize);
        if (OrderCollection.Count == 0) return new NoContentResult();
        return Page();
    }

    private void DoAuth() {
        var e = _orderRepository.FirstP(o => o.Email, o => o.Id == OrderId, nonTracking:true);
        if (CurrentCustomer == null && _jwtManager.ReadAuthToken(Token)!=e){
            throw new UnauthorizedAccessException("Kayıtlı olmanız ve Siparişlerim sayfasından sipariş görüntülüyor olmanız lazım.(Zaten böyle yapıyorsanız oturum süresi dolmuş olabilir.)");
        }
    }
    public IActionResult OnPostCancel() {
        DoAuth();
        _orderManager.CancelOrder(OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "Siparişiniz Başarıyla İptal Edildi.", Redirect = "refresh",
        });
    }

    public IActionResult OnPostChangeAddress() {
        DoAuth();
        _orderManager.UpdateAddress(NewAddress, OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "Adresiniz Değiştirildi.",
            Redirect = "refresh"
        });
    }

    public IActionResult OnPostConfirm() {
        DoAuth();
        _orderManager.Complete(OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "Sipariş Onaylandı",
            Redirect = "refresh"
        });
    }

    public IActionResult OnPostRefund() {
        DoAuth();
        _orderManager.Refund(OrderId);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, TimeOut = 2000, Title = "Sipariş İade Edildi."
        });
    }
}
