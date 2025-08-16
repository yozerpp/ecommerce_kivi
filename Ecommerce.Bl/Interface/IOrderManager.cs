using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    public Order CreateOrder(Session session, ICollection<Shipment> shipments,ICollection<CartItem> cartItems,
        Customer? user = null, AnonymousUser? anonymousUser = null, Address? shippingAddress = null);

    public Order? GetOrder(uint orderId, bool includeItems = true , bool includeAggregates= false);
    public (OrderAggregates, ICollection<OrderItemAggregates>)? GetAggregates(uint orderId);
    public void CancelOrder(uint orderId);
    public void Complete(uint orderId);
    public void Refund(uint orderId);
    public void UpdateAddress(Address address, uint orderId);
    public void AssociateWithAnonymousUser(string email, Order? order = null, uint? orderId = null);
    public Order? GetOrderWithItems(uint orderId, bool includeItemAggregates=false);
    public List<Order> GetAllOrdersFromCustomer(Customer user,bool includeItems=false,int page = 1, int pageSize = 10);
    public List<Order> GetAllOrdersFromAnonymousUser(string email, bool includeItemAggregates=false, int page = 1, int pageSize = 10);
}
