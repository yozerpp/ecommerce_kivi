using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;
using LinqKit;

namespace Ecommerce.Bl.Concrete;

public class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly ICartManager _cartManager;
    private readonly IRepository<Shipment> _shipmentRepository;
    public OrderManager(ICartManager cartManager,IRepository<Order> orderRepository, IRepository<Shipment> shipmentRepository) {
        _cartManager = cartManager;
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
    }

    public Order CreateOrder(Session session, ICollection<Shipment> shipments, ICollection<CartItem> cartItems, Customer? user=null, AnonymousUser? anonymousUser = null, Address? shippingAddress = null) {
        var o = new Order{
            Date = DateTime.Now, 
            // PaymentId = payment.Id, Payment = payment.Id == 0 ? payment : null,
            Email = user!=null?null:anonymousUser?.Email,
            ShippingAddress =  shippingAddress ?? user?.PrimaryAddress??throw new ArgumentNullException("You need to specify shipping address for anonymous orders"),
            Status = OrderStatus.WaitingConfirmation, 
            UserId = user?.Id,
            User = user?.Id == 0 ? (Customer?)user : null,
            SessionId = session.Id, 
            Session = session.Id!=0?null!:session
        };
        if(cartItems.Count==0) throw new ArgumentException("Cart is empty.");
        var items = cartItems.OrderBy(s => s.SellerId.GetHashCode()).ToArray(); //Shipments are ordered by the sellerId, we are aligning the cartItems with them.
        for(int i = 0; i < items.Length;i++){
            var item = new OrderItem(items[i]){
                SentShipment = shipments.ElementAt(i)
            };
            o.Items.Add(item);
        }
        _orderRepository.Add(o);
        _orderRepository.Flush();
        // for (int i = 0; i < items.Length; i++){
        //     var p =o.Items.ElementAt(i);
        //     p.ProductOffer = items[i].ProductOffer;
        //     p.Coupon = items[i].Coupon;
        // }
        return o;
    }

    public Order? GetOrder(uint orderId, bool includeItems = true, bool includeAggregates = false) {
        return _orderRepository.FirstP(GetProjection(includeItems, includeAggregates), o=>o.Id == orderId);
    }
    private static Expression<Func<Order, Order>> GetProjection(bool includeItems, bool includeAggregates) 
    {
        if (includeItems && includeAggregates)
            return OrderWithItemsAggregateProjection;
        if (includeItems)
            return OrderWithoutItemAggregatesProjection;
        return OrderWithoutItemsProjection;
    }
    public (OrderAggregates, ICollection<OrderItemAggregates>)? GetAggregates(uint orderId)
    {
        var r =  _orderRepository.FirstP(o=> new {o.Aggregates, Items = o.Items.Select(i=>i.Aggregates).ToArray()},o => o.Id == orderId, nonTracking:true);
        if(r==null) return null;
        return (r.Aggregates, r.Items);
    }
    public void CancelOrder(uint orderId) {
        var addr =  _orderRepository.FirstP(o => o.ShippingAddress, o => o.Id == orderId, nonTracking: true);
        _orderRepository.Update(new Order(){ Id = orderId, Status = OrderStatus.Cancelled, ShippingAddress = addr}, false, nameof(Order.Status));
        _orderRepository.Flush();
    }
    public void Complete(uint orderId) {
        var addr =  _orderRepository.FirstP(o => o.ShippingAddress, o => o.Id == orderId, nonTracking: true);
        _orderRepository.Update(new Order(){
            Id = orderId, Status = OrderStatus.Complete, ShippingAddress = addr
        },false, nameof(Order.Status));
    }

    public void Refund(uint orderId)
    {
        var addr =  _orderRepository.FirstP(o => o.ShippingAddress, o => o.Id == orderId, nonTracking: true);
        _orderRepository.Update(new Order()
        {
            Id = orderId, Status = OrderStatus.Returned,
            ShippingAddress = addr
        }, false ,nameof(Order.Status));
        _orderRepository.Flush();
    }

    public void UpdateAddress(Address address, uint orderId) {
        var c = _orderRepository.UpdateExpr([
        (o=>o.ShippingAddress.City, address.City),
        (o=>o.ShippingAddress.District, address.District),
        (o=>o.ShippingAddress.Country, address.Country),
        (o=>o.ShippingAddress.Line1, address.Line1),
        (o=>o.ShippingAddress.Line2, address.Line2),
        (o=>o.ShippingAddress.ZipCode, address.ZipCode),
        ],o=>o.Id == orderId);
        if(c==0) throw new UnauthorizedAccessException("Sipariş Yok.");
    }

    public void AssociateWithAnonymousUser( string email,Order? order=null,uint?orderId=null) {
        if (orderId == null && order == null){
            throw new ArgumentException(nameof(orderId));
        }
        if (order == null){
            _orderRepository.Update(new Order(){Id = orderId!.Value, Email = email}, true);
        }
        else{
            order.Email = email;
            _orderRepository.Update(order);
        }
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
    public List<Order> GetAllOrdersFromCustomer(Customer user, bool includeItems = false,int page = 1, int pageSize = 10) {
        var uid = user.Id;
        var ret = _orderRepository.WhereP(includeItems?OrderWithItemsAggregateProjection:OrderWithoutItemsProjection,o => o.UserId == uid,
            includes:[[nameof(Order.Aggregates)]],offset: (page - 1) * pageSize, limit: page*pageSize, orderBy:[(o => o.Date, false)]);
        return ret;
    }

    public List<Order> GetAllOrdersFromAnonymousUser(string email, bool includeItemAggregates = false, int page = 1, int pageSize = 10) {
        return _orderRepository.WhereP(GetProjection(true, includeItemAggregates), o => o.Email == email,
            offset: (page - 1) * pageSize, limit: page * pageSize);
    }

    private static Expression<Func<OrderAggregates,OrderAggregates>> OrderAggregatesProjection = o => new OrderAggregates
    {
        OrderId = o.OrderId,
        BasePrice = o.BasePrice ?? 0,
        DiscountedPrice = o.DiscountedPrice ?? 0,
        CouponDiscountedPrice = o.CouponDiscountedPrice ?? 0,
        CouponDiscountAmount = o.CouponDiscountAmount ?? 0,
        DiscountAmount = o.DiscountAmount ?? 0,
        TotalDiscountPercentage = o.TotalDiscountPercentage ?? 0,
        ItemCount = o.ItemCount ?? 0,
        TotalDiscountAmount = o.TotalDiscountAmount ?? 0
    };
    private static Expression<Func<OrderItemAggregates, OrderItemAggregates>> OrderItemAggregatesProjection = i => new OrderItemAggregates
    {
        OrderId = i.OrderId,
        ProductId = i.ProductId,
        SellerId = i.SellerId,
        BasePrice = i.BasePrice ?? 0,
        DiscountedPrice = i.DiscountedPrice ?? 0,
        CouponDiscountedPrice = i.CouponDiscountedPrice ?? 0,
        TotalDiscountPercentage = i.TotalDiscountPercentage ?? 0,
    };
    private static Expression<Func<OrderItem, OrderItem>> OrderItemProjection = i => new OrderItem
    {
        OrderId = i.OrderId,
        SellerId = i.SellerId,
        ProductId = i.ProductId,
        Aggregates = OrderItemAggregatesProjection.Invoke(i.Aggregates),
        Coupon = i.Coupon,
        CouponId = i.CouponId,
        ProductOffer = CartManager.ProductOfferProjection.Invoke(i.ProductOffer),
        Quantity = i.Quantity,
        ShipmentId = i.ShipmentId,
        SentShipment = i.SentShipment,
        RefundShipment = i.RefundShipment,
        RefundShipmentId = i.RefundShipmentId
    };
    public static readonly Expression<Func<Order, Order>> OrderWithoutItemsProjection = o => new Order
    {
        Id = o.Id,
        PaymentId = o.PaymentId,
        UserId = o.UserId,
        Date = o.Date,
        Email = o.Email,
        ShippingAddress = o.ShippingAddress,
        Status = o.Status,
        Payment = o.Payment,
        User = o.User,
        Aggregates = new OrderAggregates(){
                OrderId = o.Aggregates.OrderId,
                BasePrice = o.Aggregates.BasePrice??0,
                DiscountedPrice = o.Aggregates.DiscountedPrice??0,
                CouponDiscountedPrice = o.Aggregates.CouponDiscountedPrice??0,
                CouponDiscountAmount = o.Aggregates.CouponDiscountAmount??0,
                DiscountAmount = o.Aggregates.DiscountAmount??0,
                TotalDiscountPercentage = o.Aggregates.TotalDiscountPercentage??0,
                ItemCount = o.Aggregates.ItemCount??0,
                TotalDiscountAmount = o.Aggregates.TotalDiscountAmount??0,
        }
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
        Email = o.Email,
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
        Aggregates = new OrderAggregates(){
            OrderId = o.Aggregates.OrderId,
            BasePrice = o.Aggregates.BasePrice??0,
            DiscountedPrice = o.Aggregates.DiscountedPrice??0,
            CouponDiscountedPrice = o.Aggregates.CouponDiscountedPrice??0,
            CouponDiscountAmount = o.Aggregates.CouponDiscountAmount??0,
            DiscountAmount = o.Aggregates.DiscountAmount??0,
            TotalDiscountPercentage = o.Aggregates.TotalDiscountPercentage??0,
            ItemCount = o.Aggregates.ItemCount??0,
            TotalDiscountAmount = o.Aggregates.TotalDiscountAmount??0,
        }

    };
    public static readonly Expression<Func<Order, Order>> OrderWithItemsAggregateProjection = ((Expression<Func<Order, Order>>)(o =>
        new Order{
            Id = o.Id,
            PaymentId = o.PaymentId,
            UserId = o.UserId,
            Date = o.Date,
            Email = o.Email,
            ShippingAddress = o.ShippingAddress,
            Status = o.Status,
            Payment = o.Payment,
            User = o.User,
            Items = o.Items.Select(i=>OrderItemProjection.Invoke(i)).ToArray(),
            Aggregates = OrderAggregatesProjection.Invoke(o.Aggregates),
        })).Expand();
}
