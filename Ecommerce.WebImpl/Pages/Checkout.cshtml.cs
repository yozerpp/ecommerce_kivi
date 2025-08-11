using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Projections;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.Shipping.Dummy;
using Ecommerce.Shipping.Entity;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Address = Ecommerce.Entity.Common.Address;
using Session = Ecommerce.Entity.Session;
using Shipment = Ecommerce.Shipping.Entity.Shipment;

namespace Ecommerce.WebImpl.Pages;

public class Checkout : BaseModel
{
    private readonly IOrderManager _orderManager;
    private readonly ICartManager _cartManager;
    private readonly IJwtManager _jwtManager;
    private readonly IShippingService _shippingService;
    private readonly PaymentIntentService _paymentIntentService = new();
    private readonly CustomerService _customerService = new();
    private readonly CustomerSessionService _customerSessionService = new();
    private readonly INotificationService _notificationService;
    private readonly Aes _encryption;
    private readonly IUserManager _userManager;

    public Checkout(IOrderManager orderManager, IShippingService shippingService, IUserManager userManager, ICartManager cartManager, IJwtManager jwtManager, INotificationService notificationService, IRepository<Entity.Seller> sellerRepository) {
        _orderManager = orderManager;
        _userManager = userManager;
        _shippingService = shippingService;
        _cartManager = cartManager;
        _jwtManager = jwtManager;
        _notificationService = notificationService;
        _encryption = CreateAes();
    }

    [BindProperty]
    public int SelectedTab { get; set; } = 1;
    [BindProperty]
    public OrderWithAggregates CreatedOrder { get; set; }  
    [BindProperty] 
    public Dictionary<uint,ShippingOffer[]> ShippingOffersGrouped { get; set; }
    [BindProperty]
    public string? Email { get; set; }
    [BindProperty]
    public Address? Address { get; set; } 
    [BindProperty] public CartWithAggregates Cart { get; set; }
    
    [BindProperty]
    public PhoneNumber? PhoneNumber { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty]
    public Dictionary<uint,ulong> SelectedShippingOffers { get; set; }
    public enum Result
    {
        Success,NonExistent,Fail
    }
    [BindProperty] public Result OrderResult { get; set; }
    public IActionResult OnGetCreated([FromQuery] string intentId) {
        SelectedTab = 4;
        var i= _paymentIntentService.Get(Decrypt(intentId));
        if (i==null)
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = "Aktif siparişiniz yok, anasayfaya yönlendiriliyorsunuz",
                Redirect = "/Index"
            });
        if(i.Status!= "succeeded")
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = "Ödeme işlemi tamamlanmadı, lütfen tekrar deneyin.",
                Redirect = "/"+ nameof(Checkout)
            });
        var address = new Address(){
            Line1 = i.Shipping.Address.Line1,
            City = i.Shipping.Address.City,
            District = i.Shipping.Address.State,
            Country = i.Shipping.Address.Country,
            ZipCode = i.Shipping.Address.PostalCode,
            Line2 = i.Shipping.Address.Line2
        };
        var phoneNumber  = new PhoneNumber(){
            CountryCode = int.Parse(i.Shipping.Phone.Split(' ').First()),
            Number = i.Shipping.Phone.Split(' ').Last()
        };
        var email = i.ReceiptEmail;
        var items = _cartManager.Get(CurrentSession, false, true, true).Items;
        var shippingItems = SelectedShippingOffers //TODO make shipment item a seperate entity
            .OrderBy(o => o.Key.GetHashCode()).Select(o => {
                var shippedItems = items.Where(i => i.SellerId == o.Key);
                var seller = shippedItems.First().ProductOffer.Seller;
                return (shippedItems, new IShippingService.ShipmentCreateOptions(){
                    OfferId = o.Value, RecepientEmail = email,
                    SenderEmail = seller.Email,
                    RecepientPhoneNumber = phoneNumber,
                    SenderPhoneNumber = seller.PhoneNumber
                });
            }).ToList();
        var shippings = _shippingService.AcceptOffer(shippingItems.Select(s=>s.Item2).ToArray()).Select(s=>new Entity.Shipment(){
                Provider = s.Provider.Name,TrackingNumber = s.TrackingNumber,Status = ShipmentStatus.Shipping,
                RecepientAddress = s.DeliveryAddress, SenderAddress = s.ShippingAddress
            }).ToList();
        CreatedOrder = _orderManager.CreateOrder(CurrentSession, out var newSession, shippings, CurrentCustomer,email,address);
        if (CurrentUser == null){
            Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
            var token = _jwtManager.CreateToken(newSession);
            Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme,_jwtManager.Serialize(token), new CookieOptions(){
                MaxAge = TimeSpan.FromDays(3),//TODO: Overrrides RememberMe choice
            });
        }
        _paymentIntentService.Update(i.Id, new PaymentIntentUpdateOptions(){
            Shipping = new ChargeShippingOptions(){
                Address = new AddressOptions(){
                    City = i.Shipping.Address.City,
                    Country = i.Shipping.Address.Country,
                    Line1 = i.Shipping.Address.Line1,
                    Line2 = i.Shipping.Address.Line2,
                    PostalCode = i.Shipping.Address.PostalCode,
                    State = i.Shipping.Address.State
                },
                Name = i.Shipping.Name, Phone = i.Shipping.Phone,
                Carrier = string.Join('|', shippings.Select(s => s.Provider)),
                TrackingNumber = string.Join('|', shippings.Select(s => s.TrackingNumber))
            }
        });
        _notificationService.SendBatchAsync(shippingItems.SelectMany(s => s.shippedItems).Select(i =>
            new OrderNotification(){
                UserId = i.SellerId,
                OrderId = CreatedOrder.Id,
                ProductId = i.ProductId
            }));
        return Page();
    }
    public IActionResult OnGetPayment([FromQuery] string intentSecret, [FromQuery] string sessionSecret, [FromQuery] string intentId) {
        SelectedTab = 3;
        ViewData["StripePublicKey"] = Environment.GetEnvironmentVariable("STRIPE_PK");
        ViewData["IntentSecret"] = Decrypt(intentSecret);
        ViewData["SessionSecret"] = Decrypt(sessionSecret);
        ViewData["ReturnUrl"] = Url.Page('/' + nameof(Checkout), "created", new{ intentId= intentId });
        return Page();
    }

    public void OnPost() {
        var customerEmail = CurrentCustomer?.Email ?? Email;
        var deliveryAddress = Address ?? CurrentCustomer?.PrimaryAddress;
        var customerName = CurrentCustomer?.FullName ?? Name;
        var customerPhoneNumber = CurrentCustomer?.PhoneNumber ?? PhoneNumber;

        if (CurrentCustomer==null&& (deliveryAddress == null || string.IsNullOrWhiteSpace(deliveryAddress.City) || string.IsNullOrWhiteSpace(deliveryAddress.District) ||
            string.IsNullOrWhiteSpace(deliveryAddress.Country) || string.IsNullOrWhiteSpace(deliveryAddress.Line1) ||
            string.IsNullOrWhiteSpace(deliveryAddress.ZipCode) ) || string.IsNullOrWhiteSpace(customerEmail) || 
            string.IsNullOrWhiteSpace(customerPhoneNumber?.ToString()) || string.IsNullOrWhiteSpace(customerName) ){
            throw new ArgumentException("Lütfen Adres ve E-posta bilgilerinizi doldurun veya müşteri hesabınızla giriş yapın.");
        }
        var ao = new AddressOptions(){
            City = deliveryAddress.City, Country = "TR",
            Line1 = deliveryAddress.Line1,
            Line2 = deliveryAddress.Line2,  State = deliveryAddress.District, PostalCode = deliveryAddress.ZipCode
        };
        Stripe.Customer customer;
        AnonymousUser? anonymousUser;
        if (CurrentCustomer?.StripeId == null || (anonymousUser = _userManager.FindAnonymousUser(customerEmail)) == null){
            customer = _customerService.Create(new CustomerCreateOptions{
                Address = ao,
                Shipping = new ShippingOptions(){
                    Address = ao, Name = Name ?? CurrentCustomer?.FullName,
                    Phone = customerPhoneNumber.ToString(),
                },
                Balance = 0, Email = customerEmail,
                Phone = customerPhoneNumber!.ToString()
            });
            if (CurrentCustomer != null){
                CurrentCustomer.StripeId = customer.Id;
                _userManager.Update(CurrentCustomer);
            }
            else
                _userManager.CreateAnonymous(anonymousUser = new AnonymousUser
                    { Email = customerEmail, StripeId = customer.Id });
        }
        else customer = _customerService.Get(CurrentCustomer?.StripeId ?? anonymousUser.StripeId);

        var s = (Session)HttpContext.Items[nameof(Session)];
        var cart = (CartWithAggregates)_cartManager.Get(s, true,true, false);

        var o = new PaymentIntentCreateOptions(){
            Amount = ((long)cart.TotalDiscountedPrice)*100, CaptureMethod = "automatic",
            Shipping = new ChargeShippingOptions(){
                Address = ao,
                Name = customerName,
                Phone = customerPhoneNumber.ToString(),
            },
            Customer = customer.Id,
            Currency = "try",  
            Description = "E-Ticaret Siparişi",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions(){
                Enabled = true, AllowRedirects = "always"
            },
        };
        var customerSessionOptions = new CustomerSessionCreateOptions(){
            Customer = customer.Id,
            Components = new CustomerSessionComponentsOptions(){
                PaymentElement = new CustomerSessionComponentsPaymentElementOptions(){
                    Enabled = true,
                    Features = new CustomerSessionComponentsPaymentElementFeaturesOptions(){
                        PaymentMethodRedisplay = "enabled",
                        PaymentMethodSave = "enabled",
                        PaymentMethodSaveUsage = "on_session",
                        PaymentMethodRemove = "enabled",
                    }
                },
                PricingTable = new CustomerSessionComponentsPricingTableOptions(){
                    Enabled = true
                },
            }
        };
        var customerSession = _customerSessionService.Create(customerSessionOptions);
        var sessionSecret = Encrypt(customerSession.ClientSecret);
        var intent = _paymentIntentService.Create(o);
        var intentSecret = Encrypt(intent.ClientSecret);
        var intentId = Encrypt(intent.Id);
        Response.Headers.Append("HX-Redirect",Url.Page(nameof(Checkout),"payment",new {intentId,intentSecret, customerSessionSecret = sessionSecret}) );
    }


    public IActionResult OnGet() {
        SelectedTab = 1;
        var s= (Session)HttpContext.Items[nameof(Session)];
        Cart = (CartWithAggregates)_cartManager.Get(s, true, true, true);
        ShippingOffersGrouped = Cart.Items.GroupBy(i => i.SellerId).ToDictionary(items =>
            items.Key ,items => _shippingService.GetOffers(items.OrderBy(i=>i.ProductId.GetHashCode()).Select(i => i.ProductOffer.Product.Dimensions), new Address(),
                new Address()).ToArray());
        return Page();
    }
    private string Encrypt(string secret) {
        var bytes = Encoding.UTF8.GetBytes(secret);
        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(
                   memoryStream, _encryption.CreateEncryptor(), CryptoStreamMode.Write)){
            cryptoStream.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(_encryption.IV) + "!"+ Convert.ToBase64String(memoryStream.ToArray());
    }
    public string Decrypt(string secret) {
        var parts = secret.Split('!');
        _encryption.IV = Convert.FromBase64String(parts[0]);
        var encrypted = Convert.FromBase64String(parts[1]);
        using var ms = new MemoryStream(encrypted);
        using var cryptoStream = new CryptoStream(
            ms, _encryption.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }

    private Aes CreateAes() {
        var aes = Aes.Create();
        var b = Encoding.UTF8.GetBytes("EcommerceWebImplKey");
        var hash = MD5.HashData(b);
        aes.Key = hash;
        aes.GenerateIV();
        return aes;
    }
    // public IActionResult OnPost() {
        // if (CurrentUser == null && (Email == null || Address == null)){
            // return Partial(nameof(_InfoPartial), new _InfoPartial(){
                // Success = false, Message = "Lütfen giriş yapın veya e-posta ve adres bilgilerinizi girin.",
                // TimeOut = 100,
                // Title = "Yetersiz Bilgi"
            // });
        // }
        // var cart = _cartManager.Get( (Session)HttpContext.Items[nameof(Session)], false, true, false);
        // var stripeOptions = new SessionCreateOptions{
            // PaymentMethodTypes =["card"],
            // LineItems = cart.Items.Select(ci => new SessionLineItemOptions(){
                // Quantity = ci.Quantity,
                // PriceData = new SessionLineItemPriceDataOptions(){
                    // Currency = "try",
                    // ProductData = new SessionLineItemPriceDataProductDataOptions(){
                        // Description = ci.ProductOffer.Product.Description,
                        // Name = ci.ProductOffer.Product.Name,
                        // Images = ci.ProductOffer.Product.Images
                            // .Select(i=>!i.Data.StartsWith("data:")?"data:image/jpeg;base64, " + i.Data:i.Data)
                            // .ToList(),
                    // },UnitAmountDecimal = ci.Quantity * (ci.Coupon?.DiscountRate??1m)*ci.ProductOffer.Price * ci.ProductOffer.Discount,
                // },
                
            // }).ToList(),
            // Mode = "payment",
            // SuccessUrl =Url.Page(nameof(Checkout),"Created") + $"&sessionId={{CHECKOUT_SESSION_ID}}" +
                        // (Email != null ? $"&Email={UrlEncoder.Default.Encode(Email)}":"")+
                        // (Address!=null?$"&AddressJson={UrlEncoder.Default.Encode(JsonSerializer.Serialize(Address))}":""),
        // };
        // var session = _sessionService.Create(stripeOptions);
        // return Redirect(session.Url);
    // }
}