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
    private readonly ICartManager _cartManager;
    public OrderManager(ICartManager cartManager,IRepository<Order> orderRepository) {
        _cartManager = cartManager;
        _orderRepository = orderRepository;
    }

    public Order CreateOrder(Session session, Customer? user=null, string? email = null, Address? shippingAddress = null) {
        var o = new Order{
            Date = DateTime.Now, 
            // PaymentId = payment.Id, Payment = payment.Id == 0 ? payment : null,
            Email = user?.Email ?? email ?? throw new ArgumentNullException("You must provide an email for anonymous orders."),
            ShippingAddress = user?.Address ?? shippingAddress ?? throw new ArgumentNullException("You need to specify shipping address for anonymous orders"),
            Status = OrderStatus.PENDING, UserId = user?.Id,
            User = user?.Id == 0 ? (Customer?)user : null,
            SessionId = session.Id, Session = session.Id!=0?null!:session
        };
        var cartItems = _cartManager.Get(user?.Session??session, false, true, false).Items;
        if(cartItems.Count==0) throw new ArgumentException("Cart is empty.");
        foreach (var cartItem in cartItems){
            o.Items.Add(new OrderItem(cartItem, o));
        }
        _orderRepository.Add(o);
        _cartManager.newSession(user, flush:true);
        _orderRepository.Flush();
        return o;
    }
    public Order CancelOrder(Order order)
    {
        var i  =_orderRepository.UpdateExpr([
            (o=>o.Status, OrderStatus.CANCELLED)
        ], o => o.Id == order.Id && o.UserId == order.UserId && (o.Status != OrderStatus.CANCELLED && o.Status!=OrderStatus.DELIVERED));
        order.Status = OrderStatus.CANCELLED;
        if(i == 0) throw new UnauthorizedAccessException("You can't cancel an order that is already complete.");
        return order;
    }
    public Order Complete(Order order)
    {
        if(_orderRepository.UpdateExpr([
            (o => o.Status, OrderStatus.DELIVERED)
        ], o => o.Id == order.Id && o.Status!=OrderStatus.CANCELLED ) == 0) throw new UnauthorizedAccessException("You can't complete an order that is canceled.");
        return order;
    }
    public void UpdateOrder(Order order) {
        var uid = order.User?.Id ?? order.UserId;
        var oid = order.Id;
        var c = _orderRepository.UpdateExpr([
        (o=>o.ShippingAddress.City, order.ShippingAddress.City),
        (o=>o.ShippingAddress.Neighborhood, order.ShippingAddress.Neighborhood),
        (o=>o.ShippingAddress.State, order.ShippingAddress.State),
        (o=>o.ShippingAddress.Street, order.ShippingAddress.Street),
        (o=>o.ShippingAddress.ZipCode, order.ShippingAddress.ZipCode),
        ],o=>o.Id == oid && o.UserId ==uid);
        _orderRepository.Flush();
        if(c==0) throw new UnauthorizedAccessException("Order with the given id doesn't exists or doesn't belong to the user.");
    }
    private void VerifyOrThrow(Customer user, Order order)
    {
        var oldOrder = _orderRepository.First(o1 => o1.Id == order.Id);
        if (oldOrder == null)
        {
            throw new ArgumentException("Order with the given id doesn't exists");
        }
        if (user == null || user.Id!=oldOrder.UserId)
        {
            throw new UnauthorizedAccessException("Order doesn't belong to this user.");
        }
        order.UserId = oldOrder.UserId;
        order.PaymentId = oldOrder.PaymentId;
        _orderRepository.Detach(oldOrder);
    }

    public OrderWithAggregates? GetOrderWithItems( uint orderId) {
        var ret = _orderRepository.First(OrderWithItemsAggregateProjection, o =>  o.Id == orderId, includes:[[nameof(Order.Items), nameof(OrderItem.ProductOffer), nameof(ProductOffer.Product)]]);
        if (ret == null) return null;
        ret.CouponDiscountedPrice = ret.Items.Sum(o => o.DiscountedPrice);
        ret.BasePrice = ret.Items.Sum(o => o.BasePrice);
        ret.DiscountedPrice = ret.Items.Sum(o => o.DiscountedPrice);
        ret.DiscountAmount = ret.Items.Sum(o => o.BasePrice - o.DiscountedPrice);
        ret.CouponDiscountAmount = ret.Items.Sum(o => o.DiscountedPrice - o.CouponDiscountedPrice);
        return ret;
    }

    public List<OrderWithAggregates> GetAllOrders(Customer user, bool includeItems = false,int page = 1, int pageSize = 10) {
        var uid = user.Id;
        var ret = _orderRepository.Where(OrderWithItemsAggregateProjection, o => o.UserId == uid,
            includes:[[nameof(Order.Items), nameof(OrderItem.ProductOffer), nameof(ProductOffer.Product)]],offset: (page - 1) * pageSize, limit: page*pageSize, orderBy:[(o => o.Date, false)]);
        foreach (var order in ret){
            order.DiscountAmount = order.BasePrice - order.DiscountedPrice;
            order.CouponDiscountAmount = order.DiscountedPrice - order.CouponDiscountedPrice;
        }
        return ret;
    }
    public static readonly Expression<Func<Order, OrderWithAggregates>> OrderWithoutItemsAggregateProjection = o => new OrderWithAggregates
    {
        BasePrice = o.Items.Sum(i=>(decimal?)i.Quantity*i.ProductOffer.Price) ?? 0m,
        DiscountedPrice = o.Items.Sum(i=>(decimal?)(i.Quantity * i.ProductOffer.Price *(decimal?)i.ProductOffer.Discount)) ??0m,
        CouponDiscountedPrice = o.Items.Sum(i=>i.Quantity * i.ProductOffer.Price * (decimal?)i.ProductOffer.Discount * (i.Coupon != null ? (decimal?)i.Coupon.DiscountRate : 1m))?? 0m,
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
            BasePrice = o.Items.Sum(i=>(decimal?)i.Quantity*(decimal?)i.ProductOffer.Price) ?? 0m,
            DiscountedPrice = o.Items.Sum(i=>(decimal?)(i.Quantity * (decimal?)i.ProductOffer.Price *(decimal?)i.ProductOffer.Discount)) ??0m,
            CouponDiscountedPrice = o.Items.Sum(i=>(decimal?)i.Quantity *(decimal?) i.ProductOffer.Price * (decimal?)i.ProductOffer.Discount * (i.Coupon != null ? (decimal?)i.Coupon.DiscountRate : 1m))?? 0m,
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
                DiscountedPrice =(decimal?) o.ProductOffer.Price *(decimal?) o.Quantity *(decimal) o.ProductOffer.Discount??0m,
                CouponDiscountedPrice = (decimal?)o.ProductOffer.Price * (decimal?)o.Quantity * (decimal)o.ProductOffer.Discount *
                                        (o.Coupon != null ? (decimal)o.Coupon.DiscountRate : (decimal?)1m)??0m,
                TotalDiscountPercentage =(decimal) o.ProductOffer.Discount *
                                          (o.Coupon != null ?(decimal) o.Coupon.DiscountRate : (decimal?)1m)??0m,
            })
        };
}
