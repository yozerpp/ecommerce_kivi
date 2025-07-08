using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class CartManager : ICartManager
{
    private readonly IRepository<Cart, DbContext> _cartRepository;
    private readonly IRepository<Session, DbContext> _sessionRepository;
    private readonly IRepository<Order, DbContext> _orderRepository;
    private readonly IRepository<User, DbContext> _userRepository;
    private readonly IRepository<CartItem, DbContext> _cartItemRepository;
    public CartManager(IRepository<Order, DbContext> orderRepository, IRepository<Cart, DbContext> cartRepository, IRepository<CartItem, DbContext> cartItemRepository, IRepository<User, DbContext> userRepository)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }
    
    public Cart newCart(User? user)
    {
        Session session = new Session();
        if (user!=null)
        {
            session.User = user;
            session.UserId = user.Id;
            _sessionRepository.Delete(s=>s.UserId==user.Id);
        }
        var s =_sessionRepository.Add(session);
        s.Cart = new Cart() { Session = s, SessionId = s.Id };
        _cartRepository.Add(s.Cart);
        _sessionRepository.Update(s);
        if (user!=null)
        {
            user.SessionId = s.Id;
            user.Session = s;
            _userRepository.Update(user);
        }
        return CartContextHolder.Cart = s.Cart;
    }
    public CartItem Add(ProductOffer offer)
    {
        return Add(new CartItem()
        {
            ProductId = offer.ProductId,SellerId = offer.SellerId,OrderId = offer.SellerId
        });
    }
    public CartItem Add(CartItem item)
    {
        var cart = CartContextHolder.Cart!;
        item.Cart = cart;
        item.CartId = cart.Id;
        return _cartItemRepository.Add(item);
    }

    public CartItem Decrement(ProductOffer productOffer)
    {
        var cart = CartContextHolder.Cart;
        var item = _cartItemRepository.Find(ci =>
                ci.ProductId == productOffer.ProductId && ci.SellerId == productOffer.SellerId && ci.CartId == cart.Id);
        if (item == null) throw new ArgumentException("Offer is not in your cart.");
        item.Quantity -= 1;
        _cartItemRepository.Update(item);
        return item;
    }
}
