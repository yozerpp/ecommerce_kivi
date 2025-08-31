using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;
using LinqKit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly ICartManager _cartManager;
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly DbContext _dbContext; 
    public OrderManager(ICartManager cartManager,IRepository<Order> orderRepository, IRepository<Shipment> shipmentRepository, IRepository<OrderItem> orderItemRepository, DbContext dbContext) {
        _cartManager = cartManager;
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
        _orderItemRepository = orderItemRepository;
        _dbContext = dbContext;
    }

    public Order CreateOrder(Session session, ICollection<CartItem> cartItems, Customer? user=null, AnonymousUser? anonymousUser = null, Address? shippingAddress = null, string? name=null) {
        var p = new CardPayment(){
            Status = PaymentStatus.Preparing,
            Method = PaymentMethod.Card,
            PayerName = name ?? user?.FullName ??
                throw new ArgumentException("You need to specify name for anonymous orders", nameof(name)),
            PaymentDate = DateTimeOffset.Now.LocalDateTime,
        };
        var o = new Order{
            Date = DateTimeOffset.Now.LocalDateTime, 
            // PaymentId = payment.Id, Payment = payment.Id == 0 ? payment : null,
            Email = user!=null?null:anonymousUser?.Email??throw new ArgumentException("You need to specify email for anonymous orders",nameof(anonymousUser)),
            ShippingAddress =  shippingAddress ?? user?.PrimaryAddress??throw new ArgumentException("You need to specify shipping address for anonymous orders",nameof(shippingAddress)),
            Status = OrderStatus.WaitingPayment,
            UserId = user?.Id,
            User = user?.Id == 0 ? (Customer?)user : null,
            SessionId = session.Id, 
            Session = session.Id!=0?null!:session,
            Payment = p,
        };
        if(cartItems.Count==0) throw new ArgumentException("Cart is empty.");
        var items = cartItems.OrderBy(s => s.SellerId).ToArray(); //Shipments are ordered by the sellerId, we are aligning the cartItems with them.
        _dbContext.AttachRange(items.SelectMany(i=>i.SelectedOptions).Distinct());
        foreach (var item in items){
            var i = new OrderItem(item);
            i.ProductOffer = null;
            i.SelectedOptions = null;
            o.Items.Add(_orderItemRepository.Add(i));
            i.SelectedOptions = item.SelectedOptions;
        }
        _dbContext.Attach(o).State = EntityState.Added;
        _orderRepository.Flush();
        return o;
    }

    public Order? GetOrder(uint orderId, bool includeItems = true, bool includeAggregates = false) {
        return _orderRepository.FirstP(GetProjection(includeItems, includeAggregates), o=>o.Id == orderId, includes:includeItems?[[nameof(Order.Items),nameof(OrderItem.SelectedOptions), nameof(ProductOption.Property), nameof(ProductCategoryProperty.CategoryProperty)]]:[]);
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

    public void ChangeOrderStatus(Order order, OrderStatus status, bool propogateToItems=true) {
        var oid = order.Id;
        if(order.ShippingAddress==null)
            order.ShippingAddress =  _orderRepository.FirstP(o => o.ShippingAddress, o => o.Id == oid, nonTracking: true);
        order.Status = status;
        _orderRepository.UpdateInclude(order,nameof(Order.Status));
        _orderRepository.Flush();
        if (propogateToItems){
            if (order.Items != null && order.Items.Count > 0)
                order.Items.ForEach(i => i.Status = status);
            _orderItemRepository.UpdateExpr([
                (ıtem => ıtem.Status,
                    status
                )
            ], ıtem => ıtem.OrderId == oid);
        }
    }

    public void CancelOrder(uint orderId) {
        var addr =  _orderRepository.FirstP(o => o.ShippingAddress, o => o.Id == orderId, nonTracking: true);
        _orderRepository.UpdateInclude(new Order(){ Id = orderId, 
            Status = OrderStatus.Cancelled, 
            // Status = OrderStatus.CancellationRequested,
            ShippingAddress = addr},nameof(Order.Status));
        _orderRepository.Flush();
        _orderItemRepository.UpdateExpr([
            (ıtem => ıtem.Status,
                OrderStatus.Cancelled//OrderStatus.CancellationRequested
            )
        ], ıtem => ıtem.OrderId == orderId);
    }
    public OrderStatus? RefreshOrderStatus(uint orderId) {
        var itemStatuses = _orderItemRepository.WhereP(i => i.Status, i => i.OrderId == orderId, nonTracking: true);
        OrderStatus? newStatus = null;
        var orderStatus = _orderRepository.FirstP(o => o.Status, o => o.Id == orderId,nonTracking:true);
        if (itemStatuses.All(s=>orderStatus != s)) newStatus = itemStatuses.OrderByDescending(s=>(int)s).FirstOrDefault();
        if (newStatus != null)
            _orderRepository.UpdateExpr([(order => order.Status, newStatus.Value)], o => o.Id == orderId);
        return newStatus;
    }
    public void ChangeItemStatus(ICollection<OrderItem> items) {
        if(items.Count==0) return;
        foreach (var orderItem in items){
            _orderItemRepository.UpdateInclude(orderItem, nameof(OrderItem.Status));
        }
        _orderItemRepository.Flush();
        RefreshOrderStatus(items.First().OrderId);
        
    }

    public ICollection<OrderItem> GetOrderItemsBySellerIdOrderId(uint orderId, uint sellerId, string[][]? includes = null) {
        var v = _orderItemRepository.Where(o => o.OrderId == orderId && o.SellerId == sellerId, includes:includes);
        if(v.Count == 0) throw new ArgumentException("Siparişte ürününüz yok.");
        return v;
    }

    public void ChangeStatusBySellerIdOrderId(uint orderId, uint sellerId, OrderStatus status) {
        var v = _orderItemRepository.Where(o => o.OrderId == orderId && o.SellerId == sellerId);
        if(v.Count == 0) throw new ArgumentException("Siparişte ürününüz yok.");
        v.ForEach(i => {
            i.Status = status;
            _orderItemRepository.UpdateInclude(i, nameof(OrderItem.Status));
        });
        _orderItemRepository.Flush();
    }

    public void ChangeAddress(Address address, uint orderId, bool onlyNonComplete = true) {
        var c = _orderItemRepository.UpdateExpr([
        (o=>o.SentShipment.RecepientAddress.City, address.City),
        (o=>o.SentShipment.RecepientAddress.District, address.District),
        (o=>o.SentShipment.RecepientAddress.Country, address.Country),
        (o=>o.SentShipment.RecepientAddress.Line1, address.Line1),
        (o=>o.SentShipment.RecepientAddress.Line2, address.Line2),
        (o=>o.SentShipment.RecepientAddress.ZipCode, address.ZipCode),
        ],o=>o.OrderId == orderId && (!onlyNonComplete || o.Status < OrderStatus.Returned));
        if(c==0) throw new UnauthorizedAccessException("Sipariş yok veya değitirmenize izin verilmiyor.");
    }
    public void AssociateWithAnonymousUser( string email,Order? order=null,uint?orderId=null) {
        if (orderId == null && order == null){
            throw new ArgumentException(nameof(orderId));
        }
        if (order == null){
            _orderRepository.UpdateIgnore(new Order(){Id = orderId!.Value, Email = email}, true);
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

    private string[][] GetOrderIncludes(bool includeItems, bool includeAggregates, bool? includeItemAggregates = null) {
        includeItemAggregates??=includeAggregates;
        var ret = new List<string[]>();
        if (includeItems){
            ret.Add([nameof(Order.Items), nameof(OrderItem.SelectedOptions), nameof(ProductOption.Property), nameof(ProductCategoryProperty.CategoryProperty)]);
            if (includeItemAggregates.Value){
                ret.Add([nameof(Order.Items) ,nameof(OrderItem.Aggregates)]);
            }
        }
        if(includeAggregates)
            ret.Add([nameof(Order.Aggregates)]);
        return ret.ToArray();
    }
    public List<Order> GetAllOrdersFromCustomer(Customer user, bool includeItems = false,int page = 1, int pageSize = 10) {
        var uid = user.Id;
        var ret = _orderRepository.WhereP(includeItems?OrderWithItemsAggregateProjection:OrderWithoutItemsProjection,o => o.UserId == uid,
            includes:GetOrderIncludes(includeItems, true,true),offset: (page - 1) * pageSize, limit: page*pageSize, orderBy:[(o => o.Date, false)]);
        return ret;
    }

    public List<Order> GetAllOrdersFromAnonymousUser(string email, bool includeItemAggregates = false, int page = 1, int pageSize = 10) {
        return GetWithAggregates(o => o.Email == email, page, pageSize,
            GetOrderIncludes(true, true,includeItemAggregates));
    }
    private List<Order> GetWithAggregates(Expression<Func<Order,bool>> predicates, int page, int pageSize, string[][]? includes=null) {
        var orders = _orderRepository.WhereP(OrderWithoutAggregatesWithItemsProjection, predicates, offset: (page - 1) * pageSize,
            page * pageSize, includes: includes);
        var ids = orders.Select(o => o.Id).ToArray();
        var aggregates =_orderRepository.WhereP(OnlyOrderAggregatesProjection, o => ids.Contains(o.Id), nonTracking: true).Where(o=>o.OrderId!=0).ToDictionary(o=>o.OrderId, o=>o);
        orders.ForEach(o=>o.Aggregates = aggregates.GetValueOrDefault(o.Id)??new OrderAggregates());
        return orders;
    }
    private static Expression<Func<OrderItemAggregates, OrderItemAggregates>> OrderItemAggregatesProjection = i => new OrderItemAggregates
    {
        OrderId = i.OrderId,
        ProductId = i.ProductId,
        SellerId = i.SellerId,
        BasePrice = i.BasePrice,
        DiscountedPrice = i.DiscountedPrice,
        CouponDiscountedPrice = i.CouponDiscountedPrice,
        TotalDiscountPercentage = i.TotalDiscountPercentage,
        
    };
    private static Expression<Func<OrderAggregates,OrderAggregates>> OrderAggregatesProjection = o => new OrderAggregates
    {
        OrderId = o.OrderId,
        BasePrice = o.BasePrice,
        DiscountedPrice = o.DiscountedPrice,
        CouponDiscountedPrice = o.CouponDiscountedPrice,
        CouponDiscountAmount = o.CouponDiscountAmount,
        DiscountAmount = o.DiscountAmount,
        TotalDiscountPercentage = o.TotalDiscountPercentage,
        ItemCount = o.ItemCount,
        TotalDiscountAmount = o.TotalDiscountAmount
    };
    private static Expression<Func<Order, OrderAggregates>> OnlyOrderAggregatesProjection =
        ((Expression<Func<Order,OrderAggregates>>)(o => OrderAggregatesProjection.Invoke(o.Aggregates))).Expand();
    private static Expression<Func<OrderItem, OrderItem>> OrderItemProjection = i => new OrderItem
    {
        OrderId = i.OrderId,
        SellerId = i.SellerId,
        ProductId = i.ProductId,
        Aggregates = OrderItemAggregatesProjection.Invoke(i.Aggregates),
        Coupon = i.Coupon,
        Status = i.Status,
        CouponId = i.CouponId,
        SelectedOptions = i.SelectedOptions,
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
                BasePrice = o.Aggregates.BasePrice,
                DiscountedPrice = o.Aggregates.DiscountedPrice,
                CouponDiscountedPrice = o.Aggregates.CouponDiscountedPrice,
                CouponDiscountAmount = o.Aggregates.CouponDiscountAmount,
                DiscountAmount = o.Aggregates.DiscountAmount,
                TotalDiscountPercentage = o.Aggregates.TotalDiscountPercentage,
                ItemCount = o.Aggregates.ItemCount,
                TotalDiscountAmount = o.Aggregates.TotalDiscountAmount,
        }
    };

    public static readonly Expression<Func<Order, Order>> OrderWithoutAggregatesWithItemsProjection =
        ((Expression<Func<Order, Order>>)(o => new Order(){
            Id = o.Id,
            PaymentId = o.PaymentId,
            UserId = o.UserId,
            Date = o.Date,
            ShippingAddress = o.ShippingAddress,
            Status = o.Status,
            Payment = o.Payment,
            User = o.User,
            Email = o.Email,
            Aggregates = null,
            Items = o.Items.Select(i =>OrderItemProjection.Invoke(i)).ToArray(),
        })).Expand();
    public static readonly Expression<Func<Order, Order>> OrderWithoutItemAggregatesProjection = ((Expression<Func<Order,Order>>)(o => new Order{
        Id = o.Id,
        PaymentId = o.PaymentId,
        UserId = o.UserId,
        Date = o.Date,
        ShippingAddress = o.ShippingAddress,
        Status = o.Status,
        Payment = o.Payment,
        User = o.User,
        Email = o.Email,
        Items = o.Items.Select(i => new OrderItem{
            OrderId = i.OrderId,
            SellerId = i.SellerId,
            ProductId = i.ProductId,
            Aggregates = null,
            Coupon = i.Coupon,
            Status = i.Status,
            CouponId = i.CouponId,
            ProductOffer = i.ProductOffer,
            Quantity = i.Quantity,
            ShipmentId = i.ShipmentId,
            RefundShipmentId = i.RefundShipmentId,
        }).ToArray(),
        Aggregates = OrderAggregatesProjection.Invoke(o.Aggregates),
    })).Expand();
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
