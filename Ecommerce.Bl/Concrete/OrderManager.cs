using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;
using Microsoft.AspNetCore.Components.RenderTree;
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

    public Order CreateOrder(Session session,out Session? newSession, ICollection<Shipment> shipments, Customer? user=null, string? email = null, Address? shippingAddress = null) {
        var o = new Order{
            Date = DateTime.Now, 
            // PaymentId = payment.Id, Payment = payment.Id == 0 ? payment : null,
            
            Email = user?.Email ?? email ?? throw new ArgumentNullException("You must provide an email for anonymous orders."),
            ShippingAddress =  shippingAddress ?? user?.PrimaryAddress??throw new ArgumentNullException("You need to specify shipping address for anonymous orders"),
            Status = OrderStatus.WaitingConfirmation, UserId = user?.Id,
            User = user?.Id == 0 ? (Customer?)user : null,
            SessionId = session.Id, Session = session.Id!=0?null!:session
        };
        var cartItems = _cartManager.Get(user?.Session??session, false, true, false).Items;
        if(cartItems.Count==0) throw new ArgumentException("Cart is empty.");
        var items = cartItems.OrderBy(s => s.SellerId.GetHashCode()).ToArray(); //Shipments are ordered by the sellerId, we are aligning the cartItems with them.
        for(int i = 0; i < items.Length;i++){
            var item = new OrderItem(items[i]);
            item.ShipmentId = shipments.ElementAt(i).Id;
            o.Items.Add(item);
        }
        _orderRepository.Add(o);
        var s = _cartManager.newSession(user, flush:true);
        newSession = s;
        _orderRepository.Flush();
        return o;
    }
    public Order CancelOrder(Order order)
    {
        var i  =_orderRepository.UpdateExpr([
            (o=>o.Status, OrderStatus.Cancelled)
        ], o => o.Id == order.Id && o.UserId == order.UserId && (o.Status != OrderStatus.Cancelled && o.Status!=OrderStatus.Delivered));
        order.Status = OrderStatus.Cancelled;
        if(i == 0) throw new UnauthorizedAccessException("You can't cancel an order that is already complete.");
        return order;
    }
    public Order Complete(Order order)
    {
        if(_orderRepository.UpdateExpr([
            (o => o.Status, OrderStatus.Delivered)
        ], o => o.Id == order.Id && o.Status!=OrderStatus.Cancelled ) == 0) throw new UnauthorizedAccessException("You can't complete an order that is canceled.");
        return order;
    }
    public void UpdateOrder(Order order) {
        var uid = order.User?.Id ?? order.UserId;
        var oid = order.Id;
        var c = _orderRepository.UpdateExpr([
        (o=>o.ShippingAddress.City, order.ShippingAddress.City),
        (o=>o.ShippingAddress.District, order.ShippingAddress.District),
        (o=>o.ShippingAddress.Country, order.ShippingAddress.Country),
        (o=>o.ShippingAddress.Line1, order.ShippingAddress.Line1),
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

    public Order? GetOrderWithItems( uint orderId, bool includeItemAggregates = false) {
        var ret = _orderRepository.FirstP(includeItemAggregates?OrderWithItemsAggregateProjection:OrderWithoutItemAggregatesProjection,o =>  o.Id == orderId, includes:[[nameof(Order.Items), nameof(OrderItem.ProductOffer), nameof(ProductOffer.Product)], [nameof(Order.Aggregates)]]);
        return ret;
    }

    public Order? GetAnonymousOrder(string email, uint id) {
        return _orderRepository.First(o => o.Email == email && o.UserId == null && o.Id==id, includes:[[nameof(Order.Aggregates)]]);
    }
    public List<Order> GetAllOrders(Customer user, bool includeItems = false,int page = 1, int pageSize = 10) {
        var uid = user.Id;
        var ret = _orderRepository.WhereP(includeItems?OrderWithItemsAggregateProjection:OrderWithoutItemsProjection,o => o.UserId == uid,
            includes:[[nameof(Order.Aggregates)]],offset: (page - 1) * pageSize, limit: page*pageSize, orderBy:[(o => o.Date, false)]);
        return ret;
    }
    public static readonly Expression<Func<Order, Order>> OrderWithoutItemsProjection = o => new Order
    {
        Id = o.Id,
        PaymentId = o.PaymentId,
        UserId = o.UserId,
        Date = o.Date,
        ShippingAddress = o.ShippingAddress,
        Status = o.Status,
        Payment = o.Payment,
        User = o.User,
        Aggregates = o.Aggregates
    };

    public static readonly Expression<Func<Order, Order>> OrderWithoutItemAggregatesProjection = o => new Order{
        Id = o.Id,
        PaymentId = o.PaymentId,
        UserId = o.UserId,
        Date = o.Date,
        ShippingAddress = o.ShippingAddress,
        Status = o.Status,
        Payment = o.Payment,
        User = o.User,
        Items = o.Items.Select(i=>new OrderItem{
            OrderId = i.OrderId,
            SellerId = i.SellerId,
            ProductId = i.ProductId,
            Aggregates = null,
            Coupon = i.Coupon,
            CouponId = i.CouponId,
            ProductOffer = i.ProductOffer,
            Quantity = i.Quantity,
            ShipmentId = i.ShipmentId,
            RefundShipmentId = i.RefundShipmentId,
        }).ToArray(),
        Aggregates = o.Aggregates

    };
    public static readonly Expression<Func<Order, Order>> OrderWithItemsAggregateProjection = o =>
        new Order{
            Id = o.Id,
            PaymentId = o.PaymentId,
            UserId = o.UserId,
            Date = o.Date,
            ShippingAddress = o.ShippingAddress,
            Status = o.Status,
            Payment = o.Payment,
            User = o.User,
            Items = o.Items,
            Aggregates = o.Aggregates
        };
}
