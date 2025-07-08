using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class OrderManager : IOrderManager
{
    private readonly IRepository<Order, DbContext> _orderRepository;
    private readonly IRepository<User, DbContext> _userRepository;
    private readonly IRepository<Payment, DbContext> _paymentRepository;
    private readonly IRepository<Cart, DbContext> _cartRepository;
    private readonly IRepository<Session, DbContext> _sessionRepository;
    public OrderManager(IRepository<Order, DbContext> orderRepository, IRepository<User, DbContext> userRepository,IRepository<Payment, DbContext> paymentRepository, IRepository<Session, DbContext> sessionRepository, IRepository<Cart, DbContext> cartRepository)
    {
        _orderRepository = orderRepository;
        this._userRepository = userRepository;
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
        this._cartRepository = cartRepository;
    }
    public bool isComplete(string transactionId)
    {
        //api call.
        return true;
    }
    public Order CreateOrder(Payment payment)
    {
        if (payment.TransactionId == null || !isComplete(payment.TransactionId))
        {
            throw new UnauthorizedAccessException("Payment Incomplete.");
        }
        var user = UserContextHolder.User;
        if (user == null) throw new UnauthorizedAccessException("You must be logged in to create an order.");
        var cart = CartContextHolder.Cart!;
        var o = _orderRepository.Add(new Order
        {
            Date = DateTime.Now, Id = 0, Cart = cart, PaymentId = payment.Id,Payment = payment,
            ShippingAddress = user.ShippingAddress,Status = OrderStatus.PENDING,
        });
        _sessionRepository.Delete(s=>s.UserId==user.Id);
        var s =_sessionRepository.Add( new Session{ User = user,UserId = user.Id});
        s.Cart = new Cart() { Session = s, SessionId = s.Id };
        _cartRepository.Add(s.Cart);
        _sessionRepository.Update(s);
        user.SessionId = s.Id;
        user.Session = s;
        _userRepository.Update(user);
        CartContextHolder.Cart = s.Cart;
        return o;
    }
    public Order CancelOrder(Order order)
    {
        var user = UserContextHolder.User;
        if (user == null) throw new UnauthorizedAccessException("You must be logged in to create an order.");
        if (user.Id != order.UserId)
        {
            throw new UnauthorizedAccessException("Order doesn't belong to the user.");
        }
        order.Status = OrderStatus.CANCELLED;
        _orderRepository.Update(order);
        return order;
    }
    public Order complete(Order order)
    {
        verifyOrThrow(order);
        order.Status = OrderStatus.DELIVERED;
        UpdateOrder(order);
        return order;
    }
    public Order UpdateOrder(Order order)
    {
        verifyOrThrow(order);
        _orderRepository.Update(order);
        return order;
    }

    private void verifyOrThrow(Order order)
    {
        var oldOrder = _orderRepository.Find(o1 => o1.Id == order.Id);
        if (oldOrder == null)
        {
            throw new ArgumentException("Order with the given id doesn't exists");
        }
        var user = UserContextHolder.User;
        if (user == null || user.Id!=oldOrder.UserId)
        {
            throw new UnauthorizedAccessException("Order doesn't belong to this user.");
        }
        order.UserId = oldOrder.UserId;
        order.PaymentId = oldOrder.PaymentId;
    }
}
