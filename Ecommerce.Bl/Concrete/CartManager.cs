using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Views;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Bl.Concrete;

public class CartManager : ICartManager
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly DbContext _dbContext;
    public CartManager(IRepository<User> userRepository, IRepository<Product> productRepository,IRepository<Session> sessionRepository, IRepository<Coupon> couponRepository, IRepository<Cart> cartRepository, IRepository<CartItem> cartItemRepository,[FromKeyedServices("DefaultDbContext")] DbContext dbContext) {
        _productRepository = productRepository;
        _userRepository = userRepository;
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _dbContext = dbContext;
        _sessionRepository = sessionRepository;
    }

    public Cart? Get(Session session, bool includeAggregates = false, bool getItems = false, bool includeItemAggregates = false, bool includeSeller =false, bool nonTracking = false) {
        // var includes = GetIncludes(getItems, includeSeller);
        var projection = GetProjection(includeAggregates, includeItemAggregates);
        var cid = session.CartId;
        var c = _cartRepository.FirstP(projection,c => c.Id == cid, includes: [], nonTracking:nonTracking);
        if (c.Items.Count > 0)
            c.Aggregates = _cartRepository.FirstP(c => c.Aggregates, c => c.Id == cid, nonTracking: true);
        else
            c.Aggregates = new CartAggregates(){
                BasePrice = 0,
                DiscountedPrice = 0,
                CouponDiscountedPrice = 0,
                CouponDiscountAmount = 0,
                DiscountAmount = 0,
                TotalDiscountPercentage = 0,
                ItemCount = 0,
                CartId = c.Id,
            };
        return c;
    }
    private static string[][] GetIncludes(bool items, bool seller) {
        var includes = new List<string[]>();
        if (!items) return includes.ToArray();
        includes.Add([nameof(Cart.Items), nameof(CartItem.Coupon)]);
        includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer)]);
        if (seller) includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller)]);
        return includes.ToArray();
    }
    private static Expression<Func<Cart, Cart>> GetProjection(bool includeAggregates, bool includeItemAggregates) {
        if (includeAggregates) return WithAggregates;
        if (includeItemAggregates) return WithoutItemAggregates;
        return WithoutAggregates;
    }
    /**
     * updates user too.
     */
    public Session newSession(User? newUser, bool flush = false) {
        var session = new Session(){Cart = _cartRepository.Add(new Cart{}) };
        session.Cart.Session = session;
        session = _sessionRepository.Add(session);
        if (newUser != null){
            session.UserId = newUser.Id;
            session.User = newUser.Id != 0 ? null! : newUser;
            newUser.Session = session;
            if (newUser.Id != 0)
                _userRepository.Update(newUser);
            else _userRepository.Add(newUser);
        }
        if(flush) _sessionRepository.Flush();
        return session;
    }

    public void Clear(uint cartId) {
        _cartItemRepository.Delete(i=>i.CartId == cartId);
    }

    public void AddCoupon(Cart cart, ProductOffer offer, string couponId) {
        _cartItemRepository.UpdateInclude(new CartItem(){
            CartId = cart.Id, CouponId = couponId, ProductId = offer.ProductId,
            SellerId = offer.SellerId
        }, nameof(CartItem.CouponId));
        _cartItemRepository.Flush();
        // var cartId = ContextHolder.Session?.Cart.Id?? ContextHolder.Session.CartId;
        // var c = _cartItemRepository.UpdateExpr([
            // ( ci=>ci.CouponId, coupon.Id)
        // ], item => item.CartId == cartId && item.ProductId == offer.ProductId && item.SellerId ==offer.SellerId);
        // if (c == 0){
            // throw new ArgumentException("You do not have this item in your cart.");
        // }
    }

    public void RemoveCoupon(Cart cart, ProductOffer offer) {
        var i =_cartItemRepository.First(c=>c.CartId == cart.Id && c.ProductId == offer.ProductId && c.SellerId == offer.SellerId);
        i.CouponId = null;
        
        _cartItemRepository.Flush();
    }

    public ICollection<Coupon> GetAvailableCoupons(Session session) {
        var cid = session.CartId;
        var coupons = _cartItemRepository.WhereP(ci => ci.ProductOffer.Seller!.Coupons, ci => ci.CartId == cid,
            includes:[[nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller), nameof(Seller.Coupons), nameof(Coupon.Seller)]]).SelectMany(c=>c).ToList();
        return coupons;
    }
    public ICollection<Product> GetMoreProductsFromSellers(Session session, int page = 1, int pageSize = 20) {
        var cid = session.CartId;
        var items = _cartItemRepository.WhereP(ci=>ci.SellerId,ci => ci.CartId == cid);
        return _productRepository.WhereP(ProductManager.CardProjection,p => p.Offers.Any(o => items.Contains(o.SellerId)), offset:
            (page-1)*pageSize, limit:
            page*pageSize).ToArray();
    }   


    public CartItem Add(Cart cart, ProductOffer offer, int amount = 1)
    {
        return Add(new CartItem()
        {
            CartId = cart.Id, Cart = cart.Id==0?cart:null!, ProductId = offer.ProductId, ProductOffer = offer.ProductId!=0?null!:offer,SellerId = offer.SellerId
        }, amount);
    }
    public CartItem Add(CartItem item, int amount = 1) {
        var cartId = item.Cart?.Id??item.CartId;
        CartItem ret;
        CartItem? existing = _cartItemRepository.First(ci=>ci.CartId == cartId && ci.ProductId == item.ProductId && ci.SellerId == item.SellerId, [[nameof(CartItem.SelectedOptions)]]);
        if (existing!=null && existing.Equals(item) ){
            var c = ((int)existing.Quantity)  + amount;
            if (c <= 0){
                _cartItemRepository.Delete(i => i.ProductId == item.ProductId && i.SellerId == item.SellerId && i.CartId == item.CartId);
                return new CartItem(){ProductId = item.ProductId, SellerId = item.SellerId, Quantity = 0, CartId = item.CartId, CouponId = item.CouponId};
            }
            ret = existing;
            if (item.SelectedOptions != null && item.SelectedOptions.Count > 0){
                var ope = _dbContext.Entry(existing).Collection(s => s.SelectedOptions);
                var oldOptions= (ICollection<ProductOption>)ope.CurrentValue;
                var oldCpy = oldOptions.ToArray();
                oldOptions.Clear();
                _dbContext.SaveChanges();
                oldCpy.ForEach(old => _dbContext.Entry(old).State = EntityState.Detached);
                item.SelectedOptions.ForEach(o =>o.SellerId = item.SellerId);
            _dbContext.AttachRange(item.SelectedOptions);
            _dbContext.Entry(existing).Collection(i=>i.SelectedOptions).CurrentValue = item.SelectedOptions;
            }
            existing.Quantity = (uint)c;
        }
        else{
            if (amount<=0){
                throw new ArgumentOutOfRangeException(nameof(amount), amount,"Miktar 0'dan büyük olmalıdır.");
            }
            var options = item.SelectedOptions;
            if (options == null || options.Count == 0){
                options = item.SelectedOptions = _dbContext.Set<ProductOption>().Where(p=>p.ProductId == item.ProductId && p.SellerId == item.SellerId).OrderBy(o=>o.CategoryPropertyId).GroupBy(o=>o.CategoryPropertyId).Select(g=>g.First()).ToList();
            } else _dbContext.AttachRange(options);
            item.Quantity = (uint)amount;
            _dbContext.Entry(item).State = EntityState.Added;
            ret = item;
        }
        _cartItemRepository.Flush();
        // _cartItemRepository.Detach(ret);
        return ret;
    }

    public CartItem? Decrement(Cart cart, ProductOffer productOffer,uint amount =1)
    {
        var item = _cartItemRepository.First(ci =>
                ci.ProductId == productOffer.ProductId && ci.SellerId == productOffer.SellerId && ci.CartId == cart.Id);
        if (item == null) throw new ArgumentException("Offer is not in your cart.");
        return Decrement(item, amount);
    }

    public CartItem? Decrement(CartItem item, uint amount = 1)
    {
        item.Quantity= (item.Quantity - amount);
        if (item.Quantity <= 0)
        {
            Remove(item);
            return null;
        }

        uint productId = item.ProductId;
        uint sellerId = item.SellerId;
        var quantity = item.Quantity;
        _cartItemRepository.UpdateExpr([
            (c=>c.Quantity, quantity)
            ], c=>c.ProductId == productId && c.SellerId ==sellerId);
        return item;
    }
    public void Remove(Cart cart, ProductOffer offer) {
        Remove(new CartItem() { Cart = cart, CartId = cart.Id, ProductId = offer.ProductId, SellerId = offer.SellerId });
    }

    public void Remove(CartItem item)
    {
        _cartItemRepository.Delete(c => c.ProductId == item.ProductId && c.SellerId == item.SellerId);
        _cartItemRepository.Detach(item);
        _cartItemRepository.Flush();
    }
    public static Expression<Func<Session, Session>> SessionWithCartItems = s => new Session(){
        Cart = new Cart(){
            Id = s.CartId,
            Aggregates = new CartAggregates(){
                ItemCount = s.Cart.Aggregates.ItemCount,
            }
        },
        CartId = s.CartId,
        Id = s.Id,
        UserId = s.UserId,
    };
    private static readonly Expression<Func<Product, Product>> ProductProjection = p => new Product(){
        Id = p.Id,
        Active = p.Active,
        CategoryId = p.CategoryId,
        Dimensions = p.Dimensions, 
        MainImage = ProductManager.MainImageProjection.Invoke(p.Images),
        Name = p.Name,
    };

    public static readonly Expression<Func<ProductOffer, ProductOffer>> ProductOfferProjection = po => new ProductOffer(){
        Price = po.Price,
        ProductId = po.ProductId,
        SellerId = po.SellerId,
        Seller = po.Seller,
        Discount = po.Discount,
        Stock = po.Stock,
        Product = ProductProjection.Invoke(po.Product)
    };
    private static readonly Expression<Func<Cart, Cart>> WithoutAggregates = ((Expression<Func<Cart,Cart>>)(c => new Cart{
        Id = c.Id,
        Session = c.Session,
        Items = c.Items.Select(i => new CartItem(){
            Aggregates = null,
            CartId = i.CartId,
            ProductId = i.ProductId,
            SellerId = i.SellerId,
            Coupon = i.Coupon,
            CouponId = i.CouponId,
            Quantity = i.Quantity,
            ProductOffer = ProductOfferProjection.Invoke(i.ProductOffer),
        }).ToArray(),
        Aggregates = null,
    })).Expand();
    private static readonly Expression<Func<CartAggregates, CartAggregates?>> CartAggregatesProjection = ca => (CartAggregates?) new CartAggregates(){
        BasePrice = ca.BasePrice,
        DiscountedPrice = ca.DiscountedPrice,
        CouponDiscountedPrice = ca.CouponDiscountedPrice,
        CouponDiscountAmount = ca.CouponDiscountAmount,
        DiscountAmount = ca.DiscountAmount,
        TotalDiscountPercentage = ca.TotalDiscountPercentage,
        ItemCount = ca.ItemCount,
        CartId = ca.CartId,
    }??null;
    private static readonly Expression<Func<CartItemAggregates, CartItemAggregates?>> CartItemAggregatesProjection = cia => (CartItemAggregates?) new CartItemAggregates(){
        BasePrice = cia.BasePrice,
        DiscountedPrice = cia.DiscountedPrice,
        CouponDiscountedPrice = cia.CouponDiscountedPrice,
        TotalDiscountPercentage = cia.TotalDiscountPercentage,
        CartId = cia.CartId,
        ProductId = cia.ProductId,
        SellerId = cia.SellerId,
    }??null;
    public static readonly Expression<Func<Cart, Cart>> WithoutItemAggregates = ((Expression<Func<Cart,Cart>>)(c => new Cart{
        Session = c.Session,
        Id = c.Id,
        Aggregates = CartAggregatesProjection.Invoke(c.Aggregates),
        Items = c.Items.Select(i => new CartItem{
            Aggregates = null,
            CartId = i.CartId,
            ProductId = i.ProductId,
            SellerId = i.SellerId,
            Coupon = i.Coupon,
            CouponId = i.CouponId,
            Quantity = i.Quantity,
            ProductOffer = ProductOfferProjection.Invoke(i.ProductOffer),   
        }).ToArray()
    })).Expand();
    private static readonly Expression<Func<Cart, Cart>> WithAggregates = ((Expression<Func<Cart,Cart>>)(c => new Cart(){
        //Leave aggregates for split query
        Session = c.Session,
        Id = c.Id,
        Items = c.Items.Select(i=>new CartItem(){
            CartId = i.CartId,
            CouponId = i.CouponId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            ProductOffer = ProductOfferProjection.Invoke(i.ProductOffer),
            Coupon = i.Coupon,
            SellerId = i.SellerId,
            SelectedOptions = i.SelectedOptions,
            Aggregates = CartItemAggregatesProjection.Invoke(i.Aggregates),
        }).ToArray(),
    })).Expand();
}
