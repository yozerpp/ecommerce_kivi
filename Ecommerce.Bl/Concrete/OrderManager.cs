using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly CartManager _cartManager;
    public OrderManager(CartManager cartManager,IRepository<Order> orderRepository) {
        _cartManager = cartManager;
        _orderRepository = orderRepository;
    }
    public bool isComplete(string transactionId)
    {
        return true;
    }
    public Order CreateOrder(Payment payment) {
        var user = ContextHolder.GetUserOrThrow();
        if (payment.TransactionId == null || !isComplete(payment.TransactionId))
        {
            throw new UnauthorizedAccessException("Payment Incomplete.");
        }
        if (user == null) throw new UnauthorizedAccessException("You must be logged in to create an order.");
        var cart = ContextHolder.Session!.Cart;
        var o = new Order{
            Date = DateTime.Now, PaymentId = payment.Id, Payment = payment.Id == 0 ? payment : null,
            ShippingAddress = user.ShippingAddress, Status = OrderStatus.PENDING, UserId = user.Id,
            User = user.Id == 0 ? user : null
        };
        foreach (var cartItem in cart.Items){
            o.Items.Add(new OrderItem(cartItem, o));
        }
        _orderRepository.Add(o);
        _cartManager.newCart();
        return o;
    }
    public Order CancelOrder(Order order)
    {
        var user = ContextHolder.Session.User;
        if (user == null) throw new UnauthorizedAccessException("You must be logged in to create an order.");
        if (order.UserId==0){ //not attached
           var actual = _orderRepository.First(o=>o.Id == order.Id);
           order = actual ?? throw new ArgumentException("Order with the given id doesn't exists");
        }
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
        var oldOrder = _orderRepository.First(o1 => o1.Id == order.Id);
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

    public OrderWithAggregates? GetOrderWithItems( uint orderId) {
        var uid = ContextHolder.Session!.UserId;
        var ret = _orderRepository.First(OrderWithItemsAggregateProjection, o => o.UserId == uid && o.Id == orderId, includes:[[nameof(Order.Items), nameof(OrderItem.ProductOffer), nameof(ProductOffer.Product)]]);
        if (ret == null) return null;
        ret.CouponDiscountedPrice = ret.Items.Sum(o => o.DiscountedPrice);
        ret.BasePrice = ret.Items.Sum(o => o.BasePrice);
        ret.DiscountedPrice = ret.Items.Sum(o => o.DiscountedPrice);
        ret.DiscountAmount = ret.Items.Sum(o => o.BasePrice - o.DiscountedPrice);
        ret.CouponDiscountAmount = ret.Items.Sum(o => o.DiscountedPrice - o.CouponDiscountedPrice);
        return ret;
    }

    public List<OrderWithAggregates> getAllOrders(int page = 1, int pageSize = 10) {
        var uid = ContextHolder.Session.UserId;
        var ret = _orderRepository.Where(OrderWithoutItemsAggregateProjection, o => o.UserId == uid,
            includes:[[nameof(Order.Items), nameof(OrderItem.ProductOffer)]],offset: (page - 1) * pageSize, limit: page*pageSize, orderBy:[(o => o.Date, false)]);
        foreach (var order in ret){
            order.DiscountAmount = order.BasePrice - order.DiscountedPrice;
            order.CouponDiscountAmount = order.DiscountedPrice - order.CouponDiscountedPrice;
        }
        return ret;
    }
    public static readonly Expression<Func<Order, OrderWithAggregates>> OrderWithoutItemsAggregateProjection = o => new OrderWithAggregates
    {
        BasePrice = o.Items.Sum(i=>i.Quantity*i.ProductOffer.Price),
        DiscountedPrice = o.Items.Sum(i=>i.Quantity * i.ProductOffer.Price * (decimal) i.ProductOffer.Discount),
        CouponDiscountedPrice = o.Items.Sum(i=>i.Quantity * i.ProductOffer.Price * (decimal) i.ProductOffer.Discount *
                                    (decimal)(i.Coupon != null ? i.Coupon.DiscountRate : 1f)),
        ItemCount = 0,
        Id = o.Id,
        PaymentId = o.PaymentId,
        UserId = o.UserId,
        Date = o.Date,
        ShippingAddress = o.ShippingAddress,
        Status = o.Status,
        Payment = o.Payment,
        User = o.User,
    };

    public static readonly Expression<Func<Order, OrderWithAggregates>> OrderWithItemsAggregateProjection = o =>
        new OrderWithAggregates{
            ItemCount = o.Items.Count(),
            Id = o.Id,
            PaymentId = o.PaymentId,
            UserId = o.UserId,
            Date = o.Date,
            ShippingAddress = o.ShippingAddress,
            Status = o.Status,
            Payment = o.Payment,
            User = o.User,
            Items = o.Items.Select(o => new OrderItemWithAggregates{
                ProductId = o.ProductId,
                SellerId = o.SellerId,
                OrderId = o.OrderId,
                ProductOffer = o.ProductOffer,
                Order = o.Order,
                Quantity = o.Quantity,
                BasePrice = o.ProductOffer.Price * o.Quantity,
                CouponId = o.CouponId,
                Coupon = o.Coupon,
                DiscountedPrice = o.ProductOffer.Price * o.Quantity * (decimal)o.ProductOffer.Discount,
                CouponDiscountedPrice = o.ProductOffer.Price * o.Quantity * (decimal)o.ProductOffer.Discount *
                                        (decimal)(o.Coupon != null ? o.Coupon.DiscountRate : 1f),
                TotalDiscountPercentage = (decimal)o.ProductOffer.Discount *
                                          (decimal)(o.Coupon != null ? o.Coupon.DiscountRate : 1f),
            })
        };
}
