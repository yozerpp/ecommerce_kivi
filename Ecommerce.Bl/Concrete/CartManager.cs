using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class CartManager : ICartManager
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IRepository<Coupon> _couponRepository;
    public CartManager(IRepository<Session> sessionRepository, IRepository<Coupon> couponRepository, IRepository<Cart> cartRepository, IRepository<CartItem> cartItemRepository) {
        _couponRepository = couponRepository;
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _sessionRepository = sessionRepository;
    }

    public Cart? Get(Session session, bool includeAggregates = true, bool getItems = true, bool includeSeller =true) {
        var includes = GetIncludes(getItems, includeSeller);
        if (!includeAggregates)
            return _cartRepository.First(c => c.Id == session.CartId, includes: includes);
        return GetWithAggregates(session.CartId, includes);
    }
    private static string[][] GetIncludes(bool items, bool seller) {
        var includes = new List<string[]>();
        if (!items) return includes.ToArray();
        includes.Add([nameof(Cart.Items), nameof(CartItem.Coupon)]);
        includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer), nameof(ProductOffer.Product)]);
        if (seller) includes.Add([nameof(Cart.Items), nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller)]);
        return includes.ToArray();
    }
    private Cart? GetWithAggregates(uint cartId, string[][] includes) {
        var ret = _cartRepository.First(CartAggregateProjection, c => c.Id == cartId, 
            includes: includes);
        if (ret == null) return null;
        ret.TotalPrice = ret.Items.Sum(i => i.BasePrice);
        ret.DiscountedPrice = ret.Items.Sum(i => i.DiscountedPrice);
        ret.TotalDiscountedPrice = ret.Items.Sum(i => i.CouponDiscountedPrice);
        ret.DiscountAmount = ret.Items.Sum(i => i.BasePrice - i.DiscountedPrice);
        ret.CouponDiscountAmount = ret.Items.Sum(i => i.DiscountedPrice - i.CouponDiscountedPrice);
        var bottom = ret.Items.Sum(i => i.BasePrice);
        ret.TotalDiscountPercentage = bottom!=0?1-(ret.Items.Sum(i => i.CouponDiscountedPrice)/bottom):0;
        
        return ret;
    }

    /**
     * updates user too.
     */
    public Session newSession(User? newUser, bool flush = false) {
        var session = new Session(){Cart = _cartRepository.Add(new Cart{}) };
        session.Cart.Session = session;
        session.UserId = newUser.Id;
        session.User = newUser.Id != 0 ? null! : newUser;
        session = _sessionRepository.Add(session);
        session.User = newUser;
        newUser.Session = session;
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
    public CartItem Add(Cart cart, ProductOffer offer, uint amount = 1)
    {
        return Add(new CartItem()
        {
            Cart = cart, CartId = cart.Id, ProductId = offer.ProductId, ProductOffer = offer.ProductId!=0?null:offer,SellerId = offer.SellerId, Quantity = amount
        });
    }
    public CartItem Add(CartItem item, uint amount = 1) {
        var cartId = item.Cart?.Id??item.CartId;
        if (item.Quantity<=0){
            throw new ArgumentException("Quantity must be greater than 0.");
        }
        CartItem? existing;
        CartItem ret;
        if ((existing = _cartItemRepository.First(ci=>ci.CartId == cartId && ci.ProductId == item.ProductId && ci.SellerId == item.SellerId))!=null){
            existing.Quantity += amount;
            ret = _cartItemRepository.Update(existing);
        } 
        else ret = _cartItemRepository.Add(item);
        _cartItemRepository.Flush();
        _cartItemRepository.Detach(ret);
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
        item.Quantity-=amount;
        if (item.Quantity <= 0)
        {
            Remove(item);
            return null;
        }

        uint productId = item.ProductId;
        uint sellerId = item.SellerId;
        uint quantity = item.Quantity;
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
    private static readonly Expression<Func<Cart, CartWithAggregates>> CartAggregateProjection= c => new CartWithAggregates{
            Id = c.Id,
            SessionId = c.SessionId,
            Session = c.Session,
            Items = c.Items.Select<CartItem,CartItemWithAggregates>(ci =>
                new CartItemWithAggregates{
                    ProductId = ci.ProductId,
                    SellerId = ci.SellerId,
                    CartId = ci.CartId,
                    ProductOffer = ci.ProductOffer,
                    Cart = ci.Cart,
                    Quantity = ci.Quantity,
                    BasePrice = ci.ProductOffer.Price * ci.Quantity,
                    CouponId = ci.CouponId,
                    Coupon = ci.Coupon,
                    DiscountedPrice = ci.ProductOffer.Price * ci.Quantity * ci.ProductOffer.Discount,
                    CouponDiscountedPrice = ci.ProductOffer.Price * ci.Quantity * ci.ProductOffer.Discount *
                                            (ci.Coupon != null ? ci.Coupon.DiscountRate : 1m),
                    TotalDiscountPercentage = (1m - ci.ProductOffer.Discount) * ( ci.Coupon != null ? 1m- ci.Coupon.DiscountRate : 0m),
                }),
            ItemCount = c.Items.Sum(ci=>(uint?)ci.Quantity) as uint? ?? 0,
    };

    private static readonly Expression<Func<CartItem, CartItemWithAggregates>> CartItemAggregateProjection = ci =>
        new CartItemWithAggregates{
            ProductId = ci.ProductId,
            SellerId = ci.SellerId,
            CartId = ci.CartId,
            ProductOffer = ci.ProductOffer,
            Cart = ci.Cart,
            Quantity = ci.Quantity,
            BasePrice = ci.ProductOffer.Price * ci.Quantity,
            CouponId = ci.CouponId,
            Coupon = ci.Coupon,
            DiscountedPrice = ci.ProductOffer.Price * ci.Quantity * ci.ProductOffer.Discount,
            CouponDiscountedPrice = ci.ProductOffer.Price * ci.Quantity * ci.ProductOffer.Discount *
                                    (ci.Coupon != null ? ci.Coupon.DiscountRate : 1m),
            TotalDiscountPercentage = ci.ProductOffer.Discount *
                                      (ci.Coupon != null ? ci.Coupon.DiscountRate : 1m),
        };
}
