using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;

public class Customer : BaseModel
{
    private readonly IRepository<Entity.Customer> _customersRepository;
    private readonly ICustomerManager _customerManager;
    public Customer(IRepository<Entity.Customer> customersRepository, ICustomerManager customerManager) {
        _customersRepository = customersRepository;
        _customerManager = customerManager;
    }
    [BindProperty]
    public Entity.Projections.CustomerWithAggregates ViewedCustomer { get; set; }
    [BindProperty]
    public ICollection<Address> Addresses { get; set; }
    [BindProperty(SupportsGet = true)]
    public uint CustomerId { get; set; }
    public IActionResult OnGet() {
        var c = _customerManager.GetWithAggregates(CustomerId);
        if (c == null) return NotFound($"Müşteri numarası {CustomerId} ile müşteri Bulunamadı");
        ViewedCustomer = c;
        return Page();
    }
    [BindProperty]
    public bool IsImageEdited { get; set; }
    public IActionResult OnPostUpdate() {
        _customerManager.Update(ViewedCustomer, IsImageEdited);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Profiliniz Güncellendi",
            Title = "İşlem Başarılı", Redirect = Url.Page('/' + nameof(Customer), new{ CustomerId })
        });
    }
    public IActionResult OnPostEditAddress() {
        var id = CustomerId;
        _customersRepository.UpdateExpr([
            (c => c.Addresses, Addresses)
        ], c => c.Id == id);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Telefon Numaranız Güncellendi",
            Title = "İşlem Başarılı"
        });
    }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; }
    public ICollection<Order> Orders { get; set; }
    [HasRole(nameof(Staff), nameof(Entity.Customer))]
    public IActionResult OnGetOrders() {
        if (CustomerId == 0){
            if (CurrentCustomer == null)
                throw new ArgumentNullException("Kullanıcı",
                    "Giriş yapmalı veya görüntülemek istediğiniz müşterinin kimlik bilgilerini yazmalısınız.");
            CustomerId = CurrentCustomer.Id;
        }
        var orders = _customerManager.GetOrders(CustomerId, PageNumber,PageSize);
        if (orders.Count == 0) return new NoContentResult();
        return Partial("Shared/Order/_OrderListPartial", new _OrderListPartial(){
            Page = PageNumber, Orders = orders
        });
    }
    [BindProperty] public PhoneNumber PhoneNumber { get; set; }
    public IActionResult OnPostEditPhone() {
        var id = CustomerId;
        _customersRepository.UpdateExpr([
            (c => c.PhoneNumber, PhoneNumber)
        ], c => c.Id == id);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Adresiniz Güncellendi",
            Title = "İşlem Başaraılı"
        });
    }
}