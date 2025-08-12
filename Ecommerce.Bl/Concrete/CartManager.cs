using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class CartManager : ICartManager
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Product> _productRepository;
    public CartManager(IRepository<User> userRepository, IRepository<Product> productRepository,IRepository<Session> sessionRepository, IRepository<Coupon> couponRepository, IRepository<Cart> cartRepository, IRepository<CartItem> cartItemRepository) {
        _productRepository = productRepository;
        _userRepository = userRepository;
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _sessionRepository = sessionRepository;
    }

    public Cart? Get(Session session, bool includeAggregates = false, bool getItems = false, bool includeItemAggregates = false, bool includeSeller =false) {
        var includes = GetIncludes(getItems, includeSeller);
        var cid = session.CartId;
        if(includeItemAggregates) 
            return _cartRepository.First(c => c.Id == cid, includes: includes);
        Expression<Func<Cart, Cart>> projection;
        if (includeAggregates)
            return _cartRepository.FirstP(WithoutItemAggregates, c => c.Id == cid, includes: includes);
        else return _cartRepository.FirstP(WithoutAggregates, c => c.Id == cid, includes: includes);
    }
    private static string[][] GetIncludes(bool items, bool seller) {
        var includes = new List<string[]>();
        if (!items) return includes.ToArray();
        includes.Add([nameof(Cart.Items), nameof(CartItem.Coupon)]);
        includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer), nameof(ProductOffer.Product), nameof(Product.Images)]);
        if (seller) includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller)]);
        return includes.ToArray();
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

    public void AddCoupon(Cart cart, ProductOffer offer, Coupon coupon) {
        _cartItemRepository.Update(new CartItem(){
            CartId = cart.Id, CouponId = coupon.Id, ProductId = offer.ProductId,
            SellerId = offer.SellerId
        });
        // var cartId = ContextHolder.Session?.Cart.Id?? ContextHolder.Session.CartId;
        // var c = _cartItemRepository.UpdateExpr([
            // ( ci=>ci.CouponId, coupon.Id)
        // ], item => item.CartId == cartId && item.ProductId == offer.ProductId && item.SellerId ==offer.SellerId);
        // if (c == 0){
            // throw new ArgumentException("You do not have this item in your cart.");
        // }
    }

    public ICollection<Coupon> GetAvailableCoupons(Session session) {
        var cid = session.CartId;
        var coupons = _cartItemRepository.WhereP(ci => ci.ProductOffer.Seller!.Coupons, ci => ci.CartId == cid,
            includes:[[nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller), nameof(Seller.Coupons)]]).SelectMany(c=>c).ToList();
        return coupons;
    }
    public ICollection<Product> GetMoreProductsFromSellers(Session session, int page = 1, int pageSize = 20) {
        var cid = session.CartId;
        var items = _cartItemRepository.WhereP(ci=>ci.SellerId,ci => ci.CartId == cid);
        return _productRepository.Where(p => p.Offers.Any(o => items.Contains(o.SellerId)), offset:
            (page-1)*pageSize, limit:
            page*pageSize).ToArray();
    }   


    public CartItem Add(Cart cart, ProductOffer offer, int amount = 1)
    {
        return Add(new CartItem()
        {
            CartId = cart.Id, Cart = cart.Id==0?cart:null!, ProductId = offer.ProductId, ProductOffer = offer.ProductId!=0?null!:offer,SellerId = offer.SellerId, Quantity = amount
        }, amount);
    }
    public CartItem Add(CartItem item, int amount = 1) {
        var cartId = item.Cart?.Id??item.CartId;

        CartItem? existing;
        CartItem ret;
        if ((existing = _cartItemRepository.First(ci=>ci.CartId == cartId && ci.ProductId == item.ProductId && ci.SellerId == item.SellerId))!=null){
            existing.Quantity += amount;
            ret = _cartItemRepository.Update(existing);
        }
        else{
            if (item.Quantity<=0){
                throw new ArgumentException("Quantity must be greater than 0.");
            }
            ret = _cartItemRepository.Add(item);
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
        item.Quantity= (int)(item.Quantity - amount);
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
    private static readonly Expression<Func<Cart, Cart>> WithoutAggregates = c => new Cart{
        Id = c.Id,
        Session = c.Session,
        Items = c.Items.Select(i=>new CartItem(){
            Aggregates = null,
            CartId = i.CartId,
            ProductId = i.ProductId,
            SellerId = i.SellerId,
            Coupon = i.Coupon,
            CouponId = i.CouponId,
            Quantity = i.Quantity,
            ProductOffer = i.ProductOffer            
        }).ToArray(),
        Aggregates = null,
    };
    
    private static readonly Expression<Func<Cart, Cart>> WithoutItemAggregates = c => new Cart{
        Session = c.Session,
        Id = c.Id,
        Aggregates = c.Aggregates,
        Items = c.Items.Select(i => new CartItem{
            Aggregates = null,
            CartId = i.CartId,
            ProductId = i.ProductId,
            SellerId = i.SellerId,
            Coupon = i.Coupon,
            CouponId = i.CouponId,
            Quantity = i.Quantity,
            ProductOffer = i.ProductOffer
        }).ToArray()
    };
}
