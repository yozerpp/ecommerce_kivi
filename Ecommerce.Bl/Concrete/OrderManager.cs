using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly CartManager _cartManager;
    public OrderManager(CartManager cartManager,IRepository<Order> orderRepository, IRepository<User> userRepository,IRepository<Payment> paymentRepository, IRepository<Session> sessionRepository, IRepository<Cart> cartRepository) {
        _cartManager = cartManager;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
        _cartRepository = cartRepository;
    }
    public bool isComplete(string transactionId)
    {
        //api call.
        return true;
    }
    public Order CreateOrder(Payment payment)
    {
        if (ContextHolder.Session?.User == null){
            throw new UnauthorizedAccessException("You must be logged in to Create an Order.");            
        }
        if (payment.TransactionId == null || !isComplete(payment.TransactionId))
        {
            throw new UnauthorizedAccessException("Payment Incomplete.");
        }
        var user = ContextHolder.Session.User;
        if (user == null) throw new UnauthorizedAccessException("You must be logged in to create an order.");
        var cart = ContextHolder.Session.Cart!;
        var o = _orderRepository.Add(new Order
        {
            Date = DateTime.Now, Id = 0, Cart = cart, PaymentId = payment.Id,Payment = payment,
            ShippingAddress = user.ShippingAddress,Status = OrderStatus.PENDING,
        });
        _cartManager.newCart();
        return o;
    }
    public Order CancelOrder(Order order)
    {
        var user = ContextHolder.Session;
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
        var user = ContextHolder.Session?.User;
        if (user == null || user.Id!=oldOrder.UserId)
        {
            throw new UnauthorizedAccessException("Order doesn't belong to this user.");
        }
        order.UserId = oldOrder.UserId;
        order.PaymentId = oldOrder.PaymentId;
    }
}
