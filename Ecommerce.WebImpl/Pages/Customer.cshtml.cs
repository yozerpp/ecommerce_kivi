using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Order;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

[Authorize(Policy = nameof(Entity.Customer), AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class Customer : BaseModel
{
    private ICustomerManager _customerManager;
    public Customer(INotificationService notificationService, ICustomerManager customerManager) : base(notificationService){
        _customerManager = customerManager;
    }
    [BindProperty(SupportsGet = true)]
    public uint CustomerId { get; set; }
    public ICollection<Order> Orders { get; set; }
    public IActionResult OnGetOrders([FromQuery] int page=1,  [FromQuery] int pageSize=5) {
        if (CustomerId == 0){
            if (CurrentCustomer == null)
                throw new ArgumentNullException("Kullanıcı",
                    "Giriş yapmalı veya görüntülemek istediğiniz müşterinin kimlik bilgilerini yazmalısınız.");
            CustomerId = CurrentCustomer.Id;
        }
        var orders = _customerManager.GetOrders(CustomerId, page,pageSize);
        if (orders.Count == 0) return new NoContentResult();
        return Partial("Shared/Order/_OrderListPartial", new _OrderListPartial(){
            Page = page +1, Orders = orders, PageSize = pageSize, 
            Collapsable = true,
            Editable = true,
            Url = Url.Page(nameof(Customer), "orders", new { CustomerId}),
            Partial = true,
        });
    }
}