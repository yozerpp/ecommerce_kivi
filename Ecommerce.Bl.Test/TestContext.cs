using Bogus;
using Ecommerce.Bl.Concrete;
using Ecommerce.Dao.Concrete;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public static class TestContext
{
    public static readonly CartManager _cartManager;
    public static readonly OrderManager _orderManager;
    public static readonly SellerManager _sellerManager;
    public static readonly UserManager _userManager;
    public static readonly ProductManager<Product> _productManager;
    public static User _user;
    public static Session _session;
    public static readonly IRepository<Session> _sessionRepository;
    public static readonly IRepository<Cart> _cartRepository;
    public static readonly JwtManager _jwtmanager;
    public static readonly IRepository<User> _userRepository;
    public static readonly IRepository<ProductOffer> _offerRepository;
    public static readonly IRepository<Product> _productRepository;
    public static readonly IRepository<Payment> _paymentRepository;
    public static readonly IRepository<Order> _orderRepository;
    static TestContext(){
        DefaultDbContext context = new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;")
            
            .EnableSensitiveDataLogging().Options);
        var cartRepository = RepositoryFactory.CreateEf<Cart, DefaultDbContext>(context);
        var orderRepository = RepositoryFactory.CreateEf<Order, DefaultDbContext>(context);
        var sellerRepository = RepositoryFactory.CreateEf<Seller, DefaultDbContext>(context);
        var userRepository = RepositoryFactory.CreateEf<User, DefaultDbContext>(context);
        var productRepository = RepositoryFactory.CreateEf<Product, DefaultDbContext>(context);
        var cartItemRepository = RepositoryFactory.CreateEf<CartItem, DefaultDbContext>(context);
        var paymentRepository = RepositoryFactory.CreateEf<Payment, DefaultDbContext>(context);
        var sessionRepository = RepositoryFactory.CreateEf<Session, DefaultDbContext>(context);
        var offerRepository = RepositoryFactory.CreateEf<ProductOffer, DefaultDbContext>(context);
        _jwtmanager = new JwtManager();
        _productManager = new ProductManager<Product>(productRepository);
        _cartManager = new CartManager(orderRepository, cartRepository, cartItemRepository, userRepository);
        _orderManager = new OrderManager(_cartManager,orderRepository, userRepository, paymentRepository, sessionRepository,
            cartRepository);
        _userManager = new UserManager(_jwtmanager,userRepository, s => s, _cartManager);
        _sellerManager = new SellerManager(productRepository, sellerRepository, offerRepository);
        _sessionRepository = sessionRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _paymentRepository = paymentRepository;
        _offerRepository = offerRepository;
        _orderRepository = orderRepository;
        _session = getNewSession();
    }

    public static bool DeepEquals<T>(params T[] objects) {
        for (int i = 0; i < objects.Length; i++){
            if (typeof(T).GetProperties()
                .Any(property => !property.GetValue(objects[i])?.Equals(property.GetValue(objects[i + 1]))??false)){
                return false;
            }
        }
        return true;
    }
    public static Session getNewSession() {
        return _cartManager.newCart();
    }
}