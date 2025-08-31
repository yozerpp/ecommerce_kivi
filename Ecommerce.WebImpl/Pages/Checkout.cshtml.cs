using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Mail;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.Shipping.Dto;
using Ecommerce.Shipping.Dummy;
using Ecommerce.Shipping.Entity;
using Ecommerce.WebImpl.Pages.Shared;
using LinqKit;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Address = Ecommerce.Entity.Common.Address;
using Session = Ecommerce.Entity.Session;
using Shipment = Ecommerce.Shipping.Entity.Shipment;
using ShipmentStatus = Ecommerce.Entity.Common.ShipmentStatus;

namespace Ecommerce.WebImpl.Pages;

public class Checkout : BaseModel
{
    private readonly IOrderManager _orderManager;
    private readonly ICartManager _cartManager;
    private readonly IRepository<Order> _orderRepository;
    private readonly IShippingService _shippingService;
    private readonly DbContext _dbContext;
    private readonly PaymentIntentService _paymentIntentService = new();
    private readonly CustomerService _customerService = new();
    private readonly CustomerSessionService _customerSessionService = new();
    private readonly IRepository<Entity.Shipment> _shipmentRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly Aes _encryption;
    private readonly IUserManager _userManager;
    private readonly IRepository<AnonymousUser> _anonymousUserRepository;
    private readonly IMailService _mailService;
    public Checkout(INotificationService notificationService, IOrderManager orderManager, IMailService mailService, IShippingService shippingService, IUserManager userManager, ICartManager cartManager,  IRepository<Entity.Seller> sellerRepository, IRepository<Order> orderRepository, IRepository<AnonymousUser> anonymousUserRepository, IRepository<Entity.Shipment> shipmentRepository,[FromKeyedServices("DefaultDbContext")] DbContext dbContext, IRepository<OrderItem> orderItemRepository): base(notificationService) {
        _orderManager = orderManager;
        _mailService = mailService;
        _userManager = userManager;
        _shippingService = shippingService;
        _cartManager = cartManager;
        _orderRepository = orderRepository;
        _anonymousUserRepository = anonymousUserRepository;
        _shipmentRepository = shipmentRepository;
        _dbContext = dbContext;
        _orderItemRepository = orderItemRepository;
        _encryption = CreateAes();
    }

    [BindProperty]
    public int SelectedTab { get; set; } = 1;
    [BindProperty]
    public Order CreatedOrder { get; set; }  
    [BindProperty] 
    public ICollection<IGrouping<Entity.Seller,ShippingOffer>> ShippingOffersGrouped { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }
    [BindProperty(SupportsGet = true)]
    public Address? Address { get; set; } 
    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)]
    public PhoneNumber? PhoneNumber { get; set; }
    [BindProperty(SupportsGet = true)]
    public string ApiCustomerId { get; set; }
    [BindProperty(SupportsGet = true)]
    public uint OrderId { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal ItemsCost { get; set; }
    public ICollection<IGrouping<Entity.Seller, CartItem>> CartItemsGrouped { get; private set; }
    [BindProperty] public Entity.Cart Cart { get; set; }
    [BindProperty] public Dictionary<uint,ulong> SelectedShippingOffers { get; set; }
    public enum Result
    {
        Success,NonExistent,Fail,Processing
    }
    [BindProperty] public Result OrderResult { get; set; }
    public IActionResult OnGetCreated([FromQuery] string intentId, [FromQuery] uint orderId)  {
        SelectedTab = 4;
        var i= _paymentIntentService.Get(Decrypt(intentId));
        OrderResult = i?.Status switch{
            null => Result.NonExistent,
            "processing" => Result.Processing,
            "succeeded" => Result.Success,
            _ => Result.Fail
        };
        if (OrderResult != Result.Success) return Page();
        var order = _orderManager.GetOrder(orderId, true, true);
        if (order == null){
            OrderResult = Result.NonExistent;
            return Page();
        }
        CreatedOrder = order;
        if (order.Status != OrderStatus.WaitingPayment) return Page();
        _orderManager.ChangeOrderStatus(order, OrderStatus.WaitingConfirmation, true);
        _cartManager.Clear(CurrentSession.CartId);
        var t = NotificationService.SendBatchAsync(order.Items.DistinctBy(i=>i.SellerId).Select(i =>
            new OrderNotification(){
                UserId = i.SellerId,
                OrderId = order.Id,
                ProductId = i.ProductId
            }).ToArray());
        var mailTask = _mailService.SendAsync(order?.Email ?? CurrentCustomer?.Email, "Siparişiniz Alındı",
            "Siparişiniz alınmıştır, sipariş numaranız: " + order.Id + "Sipariş detaylarınızı " +
            Url.Page("/" + nameof(Orders), null, new{ OrderId = order.Id }, Request.Scheme) +
            " Sayfasından takip edebilirsiniz.");
        Task.WaitAll(t);
        return Page();
    }
    public IActionResult OnGetPayment( string intentSecret, [FromQuery] string sessionSecret, [FromQuery] decimal shippingCost, [FromQuery] decimal itemsCost, [FromQuery] string intentId, [FromQuery] uint orderId) {
        SelectedTab = 3;
        ViewData["StripePublicKey"] = Environment.GetEnvironmentVariable("STRIPE_PK");
        ViewData["IntentSecret"] = Decrypt(intentSecret);
        ViewData["SessionSecret"] = Decrypt(sessionSecret);
        ViewData["ReturnUrl"] = Url.Page('/' + nameof(Checkout), "created", new{ intentId, orderId}, Request.Scheme);
        ShippingCost = shippingCost;
        ItemsCost = itemsCost;
        return Page();
    }

    public async Task<IActionResult> OnPostShipment() {
        AnonymousUser? anonymousUser = null;
        if(CurrentCustomer==null && (anonymousUser = _userManager.FindAnonymousUser(Email)) == null) throw new UnauthorizedAccessException("Yönlendirme sırasında bir hata oluştu, lütfen tekrar deneyin.");
        var address= Address??CurrentCustomer?.PrimaryAddress ?? throw new ArgumentException("Lütfen Adres bilgilerinizi girin veya müşteri hesabınızla giriş yapın.");
        var customerEmail = CurrentCustomer?.Email ??Email?? throw new ArgumentException("Lütfen E-Posta bilgilerinizi girin veya müşteri hesabınızla giriş yapın.");
        var customerPhoneNumber = PhoneNumber??CurrentCustomer?.PhoneNumber ?? throw new ArgumentException("Lütfen Telefon Numaranızı girin veya müşteri hesabınızla giriş yapın.");
        var name = CurrentCustomer?.FullName?? Name ?? throw new ArgumentException("Lütfen Adınızı girin veya müşteri hesabınızla giriş yapın.");
        var order = _orderManager.GetOrder(OrderId, false, true);
        if (order == null) throw new ArgumentException("Invalid Order Id");
        string apiId;
        if ((apiId = CurrentCustomer?.ApiId ?? anonymousUser?.ApiId) == null){
            apiId = (await CreateStripeCustomer( customerEmail, name, address, customerPhoneNumber)).Id;
            if (CurrentCustomer != null){
                CurrentCustomer.ApiId = apiId;
                _userManager.Update(CurrentCustomer, false);
            }
            else{
                anonymousUser.ApiId = apiId;
                _anonymousUserRepository.UpdateInclude(anonymousUser, nameof(AnonymousUser.ApiId));
                _anonymousUserRepository.Flush();
            }
        }
        var apiShipments = await Task.WhenAll(SelectedShippingOffers.Values.Select(async oid =>
            await _shippingService.AcceptOffer(new AcceptOfferOptions(){
                OfferId = oid
            })));
        var shipments = apiShipments.Select(s => {
            var shipment = _shipmentRepository.Add(new Entity.Shipment(){
                ApiId = s.ApiId,
                Provider = s.Provider?.Name,
                RecepientAddress = s.Recipient.Address,
                SenderAddress = s.Sender.Address,
                Status = ShipmentStatus.Processing,
                TrackingNumber = s.TrackingNumber,
                Cost = s.Price + s.Tax,
            });
            shipment.OrderItems = s.Items.Select(si => {
                var ids = si.ItemId.Split('|');
                var oid = uint.Parse(ids[0]);
                var sid = uint.Parse(ids[1]);
                var pid = uint.Parse(ids[2]);
                var oi =  new OrderItem{
                    OrderId = oid, SellerId = sid, ProductId = pid, SentShipment = shipment,
                };
                _orderItemRepository.UpdateInclude(oi, nameof(OrderItem.SentShipment));
                return oi;
            }).ToArray();
            return shipment;
        }).ToArray();
        var cost = shipments.Sum(s => s.Cost);
        _orderItemRepository.Flush();
        _orderRepository.UpdateExpr([(o => o.ShippingCost, cost)], o => o.Id == OrderId);
        var (sessionSecret, intentSecret, intentId) = await CreatePaymentIntent(
            _cartManager.Get(CurrentSession, true, false, false, false, true), 
            cost,
            address, name, customerPhoneNumber, apiId);
        Response.Headers.Append("HX-Redirect",Url.Page(nameof(Checkout),"payment",new {
                intentSecret,
                sessionSecret,
                intentId,
                itemsCost = order.Aggregates.CouponDiscountedPrice,
                shippingCost = cost,
                orderId=OrderId,
            }
        ));
        return new OkResult();
    }
    public async Task<IActionResult> OnGetShipment([FromQuery] string addressBin, [FromQuery] string phoneNumberBin) {
        SelectedTab = 2;
        var items = _cartManager.Get(CurrentSession, false, true, false, true, true).Items;
        Address = MessagePackSerializer.Deserialize<Address>(Convert.FromBase64String(addressBin), MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance));
        PhoneNumber = MessagePackSerializer.Deserialize<PhoneNumber>(Convert.FromBase64String(phoneNumberBin), MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance));
        var total = _orderRepository.FirstP(o => (decimal?)o.Aggregates.CouponDiscountedPrice, o => o.Id == OrderId,nonTracking:true) ?? throw new ArgumentException("Invalid Order Id");
        CartItemsGrouped = items.GroupBy(i => i.ProductOffer.Seller).ToArray();
        ShippingOffersGrouped = CartItemsGrouped.SelectMany(group =>
            _shippingService.GetOffers(new GetOfferOptions{
                Items = group.Select(i => new ShipmentItem(){
                    Dimensions = i.ProductOffer.Product.Dimensions,
                    ItemId = string.Join('|',OrderId, i.ProductOffer.SellerId.ToString(), i.ProductOffer.ProductId.ToString()),
                    ItemName = i.ProductOffer.Product.Name,
                    ItemPrice = i.ProductOffer.DiscountedPrice,
                    ItemSku = i.ProductOffer.Product.Sku,
                    Quantity = i.Quantity,
                }).ToArray(),
                OrderInfo = new OrderInfo{
                    OrderId = OrderId.ToString(), Total = total,
                },
                Sender = new DeliveryInfo(){
                    Address = group.Key.Address,
                    Email = group.Key.Email,
                    Name = group.Key.FullName,
                    PhoneNumber = group.Key.PhoneNumber,
                    Id = group.Key.Address.ApiId,
                },
                Recipient = new DeliveryInfo(){
                    Address = Address,
                    Email = Email,
                    Name = Name,
                    PhoneNumber = PhoneNumber,
                    Id = Address.ApiId,
                }
            }).Result.Select(o=>(group.Key,o))).GroupBy(o=>o.Key, o=>o.o).ToArray();
        return Page();
    }
    public IActionResult OnPost() {
var customerEmail = Email ?? CurrentCustomer?.Email ?? throw new ArgumentException("Lütfen E-posta bilgilerinizi girin veya müşteri hesabınızla giriş yapın.");
        var deliveryAddress = Address ?? CurrentCustomer?.PrimaryAddress ?? throw new ArgumentException("Lütfen Adres bilgilerinizi girin veya müşteri hesabınızla giriş yapın.");
        var customerName = CurrentCustomer?.FullName ?? Name ?? throw new ArgumentException("Lütfen Adınızı girin veya müşteri hesabınızla giriş yapın.");
        var customerPhoneNumber =  PhoneNumber ?? CurrentCustomer?.PhoneNumber ?? throw new ArgumentException("Lütfen Telefon Numaranızı girin veya müşteri hesabınızla giriş yapın.");
        ValidateParams(deliveryAddress, customerEmail, customerPhoneNumber, customerName);
        var ao = new Address(){
            City = deliveryAddress.City, Country = deliveryAddress.Country, Line1 = deliveryAddress.Line1,
            Line2 = deliveryAddress.Line2, ZipCode = deliveryAddress.ZipCode, District = deliveryAddress.District
        };
        AnonymousUser? anonymousUser = null;
        var cid = CurrentSession.CartId;
        var items = _dbContext.Set<CartItem>().Include(i=>i.SelectedOptions).Where(ci => ci.CartId == cid).ToArray();
        if(CurrentCustomer==null && (anonymousUser = _userManager.FindAnonymousUser(customerEmail))==null) 
            _userManager.CreateAnonymous(anonymousUser = new AnonymousUser(){Email = customerEmail});
        var order = _orderManager.CreateOrder(CurrentSession, items, CurrentCustomer, anonymousUser,
            deliveryAddress, customerName);

        // var (sessionSecret, intentSecret, intentId) =await CreatePaymentIntent(cart, ao, customerName, customerPhoneNumber, customer);

        Response.Headers.Append("HX-Redirect",Url.Page(nameof(Checkout),"shipment",new {
                OrderId = order.Id,
                Name= customerName,
                Email=customerEmail,
                addressBin=Convert.ToBase64String(MessagePackSerializer.Serialize(ao, MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance))),
                phoneNumberBin = Convert.ToBase64String(MessagePackSerializer.Serialize(customerPhoneNumber,  MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance))),
            }
        ));
        return new OkResult();
    }
    private void ValidateParams(Address? deliveryAddress, string? customerEmail, PhoneNumber? customerPhoneNumber,
        string? customerName) {
        if (CurrentCustomer==null&& (deliveryAddress == null || string.IsNullOrWhiteSpace(deliveryAddress.City) || string.IsNullOrWhiteSpace(deliveryAddress.District) ||
                                     string.IsNullOrWhiteSpace(deliveryAddress.Country) || string.IsNullOrWhiteSpace(deliveryAddress.Line1) ||
                                     string.IsNullOrWhiteSpace(deliveryAddress.ZipCode) ) || string.IsNullOrWhiteSpace(customerEmail) || 
            string.IsNullOrWhiteSpace(customerPhoneNumber?.ToString()) || string.IsNullOrWhiteSpace(customerName) ){
            throw new ArgumentException("Lütfen Adres ve E-posta bilgilerinizi doldurun veya müşteri hesabınızla giriş yapın.");
        }
    }

    private async Task<(string sessionSecret, string intentSecret, string intentId)> CreatePaymentIntent(Entity.Cart? cart, decimal extraCosts, Address address,
        string customerName, PhoneNumber customerPhoneNumber, string customerId) {
        var ao = new AddressOptions(){
            City = address.City, Country = address.Country, Line1 = address.Line1,
            Line2 = address.Line2, PostalCode = address.ZipCode, State = address.District
        };
        var o = new PaymentIntentCreateOptions(){
            Amount = (long)(cart.Aggregates.CouponDiscountedPrice + extraCosts)*100L, CaptureMethod = "automatic",
            Shipping = new ChargeShippingOptions(){
                Address = ao,
                Name = customerName,
                Phone = customerPhoneNumber.ToString(),
            },
            Customer = customerId,
            Currency = "try",  
            Description = "E-Ticaret Siparişi",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions(){
                Enabled = true, AllowRedirects = "always"
            },
        };
        var customerSessionOptions = new CustomerSessionCreateOptions(){
            Customer = customerId,
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
            }
        };
        var customerSession = await _customerSessionService.CreateAsync(customerSessionOptions);
        var sessionSecret = Encrypt(customerSession.ClientSecret);
        var intent = await _paymentIntentService.CreateAsync(o);
        var intentSecret = Encrypt(intent.ClientSecret);
        var intentId = Encrypt(intent.Id);
        return (sessionSecret, intentSecret, intentId);
    }

    private async Task<Stripe.Customer> CreateStripeCustomer(string customerEmail, string name, Address address , PhoneNumber customerPhoneNumber) {
        var ao = new AddressOptions(){
            City = address.City, Country = address.Country, Line1 = address.Line1,
            Line2 = address.Line2, PostalCode = address.ZipCode, State = address.District
        };
        var co = new CustomerCreateOptions{
            Address = ao,
            Shipping = new ShippingOptions(){
                Address = ao, Name = name,
                Phone = customerPhoneNumber.ToString(),
            },
            Balance = 0, Email = customerEmail,
            Phone = customerPhoneNumber!.ToString()
        };
        var customer = await _customerService.CreateAsync(co);
        return customer;
    }

    public IActionResult OnGet() {
        SelectedTab = 1;
        Cart = _cartManager.Get(CurrentSession, true, true, true, true);
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