using Bogus;
using Ecommerce.Bl.Concrete;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Dao.Default;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Test;

public static class TestContext
{
    public static readonly CartManager _cartManager;
    public static readonly OrderManager _orderManager;
    public static readonly SellerManager _sellerManager;
    public static readonly UserManager _userManager;
    public static readonly ProductManager _productManager;
    public static readonly ReviewManager _reviewManager;
    public static User _user;
    public static Session _session;
    public static readonly IRepository<Session> _sessionRepository;
    public static readonly IRepository<Cart> _cartRepository;
    public static readonly JwtManager _jwtmanager;
    public static readonly IRepository<User> _userRepository;
    public static readonly IRepository<ProductOffer> _offerRepository;
    public static readonly IRepository<Product> _productRepository;
    public static readonly IRepository<Category> _categoryRepository;
    public static readonly IRepository<Payment> _paymentRepository;
    public static readonly IRepository<Order> _orderRepository;
    public static readonly IRepository<CartItem> _cartItemRepository;
    public static readonly IRepository<Seller> _sellerRepository;
    public static readonly IRepository<Coupon> _couponRepository;
    public static readonly IRepository<OrderItem> _orderItemRepository;
    public static readonly IRepository<ProductReview> _reviewRepository;
    public static readonly IRepository<ReviewComment> _reviewCommentRepository;
    public static readonly IRepository<ReviewVote> _reviewVoteRepository;
    static TestContext(){
        DefaultDbContext context = new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;")
            .EnableSensitiveDataLogging().Options);
        _cartRepository = RepositoryFactory.Create<Cart>(context);
        _orderRepository = RepositoryFactory.Create<Order>(context);
        _sellerRepository = RepositoryFactory.Create<Seller>(context);
        _userRepository= RepositoryFactory.Create<User>(context);
        _productRepository= RepositoryFactory.Create<Product>(context);
        _cartItemRepository = RepositoryFactory.Create<CartItem>(context);
        _paymentRepository = RepositoryFactory.Create<Payment>(context);
        _categoryRepository = RepositoryFactory.Create<Category>(context);
        _sessionRepository = RepositoryFactory.Create<Session>(context);
        _offerRepository = RepositoryFactory.Create<ProductOffer>(context);
        _couponRepository = RepositoryFactory.Create<Coupon>(context);
        _reviewCommentRepository = RepositoryFactory.Create<ReviewComment>(context);
        _reviewVoteRepository = RepositoryFactory.Create<ReviewVote>(context);
        _reviewRepository = RepositoryFactory.Create<ProductReview>(context);
        _orderItemRepository = RepositoryFactory.Create<OrderItem>(context);
        _couponRepository= RepositoryFactory.Create<Coupon>(context);
        _jwtmanager = new JwtManager(_userRepository,_sellerRepository,_sessionRepository);
        _productManager = new ProductManager(_productRepository);
        _cartManager = new CartManager(_sessionRepository, _cartRepository, _cartItemRepository);
        _orderManager = new OrderManager(_cartManager,_orderRepository);
        _userManager = new UserManager(_jwtmanager,_userRepository, _sellerRepository, s => s, _cartManager);
        _sellerManager = new SellerManager(_couponRepository,_productRepository,_sellerRepository, _offerRepository);
        _reviewManager = new ReviewManager(_reviewRepository, _reviewCommentRepository, _reviewVoteRepository,
            _orderItemRepository);
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

}