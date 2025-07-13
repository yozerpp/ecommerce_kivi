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
    public CartManager(IRepository<Session> sessionRepository, IRepository<Cart> cartRepository, IRepository<CartItem> cartItemRepository)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _sessionRepository = sessionRepository;
    }

    public Cart? Get(bool includeAggregates = true, bool getItems = true) {
        string[][] includes = getItems ?[[nameof(Cart.Items)]] :[];
        if (!includeAggregates)
            return _cartRepository.First(c => c.Id == ContextHolder.Session!.CartId, includes: includes);
        return GetWithAggregates(includes);
    }
    private Cart? GetWithAggregates(string[][] includes) {
        var ret = _cartRepository.First(CartAggregateProjection, c => c.Id == ContextHolder.Session!.CartId,
            includes: includes);
        if (ret == null) return null;
        ret.TotalPrice = ret.Items.Sum(i => i.BasePrice);
        ret.DiscountedPrice = ret.Items.Sum(i => i.DiscountedPrice);
        ret.CouponDiscountedPrice = ret.Items.Sum(i => i.CouponDiscountedPrice);
        ret.DiscountAmount = ret.Items.Sum(i => i.BasePrice - i.DiscountedPrice);
        ret.CouponDiscountAmount = ret.Items.Sum(i => i.DiscountedPrice - i.CouponDiscountedPrice);
        return ret;
    }

    /**
     * updates user too.
     */
    public Session newCart(User? newUser=null) {
        Session? session;
        if ((session = ContextHolder.Session) != null){
            uint cartId = session.CartId;
            session.Cart = new Cart{ SessionId = session.Id };
            session = _sessionRepository.Update(session);
            _cartRepository.Delete(c=>c.Id==cartId);
        }
        else{
            session = new Session(){Cart = new Cart{} };
            session.Cart.Session = session;
            session = _sessionRepository.Add(session);
        }
        if (newUser != null) newUser.Session = session;
        else ContextHolder.Session = session;
        return session;
    }
    public CartItem Add(ProductOffer offer, uint amount = 1)
    {
        return Add(new CartItem()
        {
            ProductId = offer.ProductId,SellerId = offer.SellerId, Quantity = amount
        });
    }
    public CartItem Add(CartItem item)
    {
        var cart = ContextHolder.Session!.Cart;
        item.CartId = cart.Id;
        if (item.Quantity<=0){
            throw new ArgumentException("Quantity must be greater than 0.");
        }
        CartItem existing;
        if ((existing = _cartItemRepository.First(ci=>ci.CartId == cart.Id && ci.ProductId == item.ProductId && ci.SellerId == item.SellerId))!=null){
            existing.Quantity += item.Quantity;
            return _cartItemRepository.Update(existing);
        } 
        return _cartItemRepository.Add(item);
    }

    public CartItem? Decrement(ProductOffer productOffer,uint amount =1)
    {
        var cart = ContextHolder.Session!.Cart;
        var item = _cartItemRepository.First(ci =>
                ci.ProductId == productOffer.ProductId && ci.SellerId == productOffer.SellerId && ci.CartId == cart.Id);
        if (item == null) throw new ArgumentException("Offer is not in your cart.");
        item.Quantity -= amount;
        if (item.Quantity<=0){
            _cartItemRepository.Delete(item);
            item = null;
        } else item = _cartItemRepository.Update(item);
        return item;
    }
    public void Remove(ProductOffer offer) {
        var cart = ContextHolder.Session!.Cart;
        var item = new CartItem(){Cart = cart, CartId = cart.Id, ProductId = offer.ProductId, SellerId = offer.SellerId};
        _cartItemRepository.Delete(item);
    }

    private static readonly Expression<Func<Cart, CartWithAggregates>> CartAggregateProjection= c => new CartWithAggregates{
            ItemCount = (uint)c.Items.Count,
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
                    DiscountedPrice = ci.ProductOffer.Price * ci.Quantity * (decimal)ci.ProductOffer.Discount,
                    CouponDiscountedPrice = ci.ProductOffer.Price * ci.Quantity * (decimal)ci.ProductOffer.Discount *
                                            (decimal)(ci.Coupon != null ? ci.Coupon.DiscountRate : 1f),
                    TotalDiscountPercentage = (decimal)ci.ProductOffer.Discount *
                                              (decimal)(ci.Coupon != null ? ci.Coupon.DiscountRate : 1f),
                }),
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
            DiscountedPrice = ci.ProductOffer.Price * ci.Quantity * (decimal)ci.ProductOffer.Discount,
            CouponDiscountedPrice = ci.ProductOffer.Price * ci.Quantity * (decimal)ci.ProductOffer.Discount *
                                    (decimal)(ci.Coupon != null ? ci.Coupon.DiscountRate : 1f),
            TotalDiscountPercentage = (decimal)ci.ProductOffer.Discount *
                                      (decimal)(ci.Coupon != null ? ci.Coupon.DiscountRate : 1f),
        };
}
