using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class CartManager : ICartManager
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    public CartManager(IRepository<Order> orderRepository, IRepository<Cart> cartRepository, IRepository<CartItem> cartItemRepository, IRepository<User> userRepository)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }
    /**
     * updates user too.
     */
    public Session newCart()
    {
        var session = new Session();
        var user = ContextHolder.Session?.User;
        if (user!=null)
        {
            session.User = user;
            session.UserId = user.Id;
            _sessionRepository.Delete(s=>s.UserId==user.Id);
        }
        session.Cart = _cartRepository.Add(new Cart());
        session.CartId = session.Cart.Id;
        session = _sessionRepository.Add(session);
        if (user == null) return session;
        user.SessionId = session.Id;
        user.Session = session;
        _userRepository.Update(user);
        ContextHolder.Session = session;
        return session;
    }
    public CartItem Add(ProductOffer offer, int amount = 1)
    {
        return Add(new CartItem()
        {
            ProductId = offer.ProductId,SellerId = offer.SellerId, Quantity = amount
        });
    }
    public CartItem Add(CartItem item)
    {
        var cart = ContextHolder.Session!.Cart;
        item.Cart = cart;
        item.CartId = cart.Id;
        if (item.Quantity<=0){
            throw new ArgumentException("Quantity must be greater than 0.");
        }
        CartItem existing;
        if ((existing = _cartItemRepository.Find(ci=>ci.CartId == cart.Id && ci.ProductId == item.ProductId && ci.SellerId == item.SellerId))!=null){
            existing.Quantity += item.Quantity;
            return _cartItemRepository.Update(existing);
        } 
        return _cartItemRepository.Add(item);
    }

    public CartItem? Decrement(ProductOffer productOffer,int amount)
    {
        var cart = ContextHolder.Session!.Cart;
        var item = _cartItemRepository.Find(ci =>
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
}
