using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    public Order CreateOrder(Session session, out Session newSession,ICollection<Shipment> shipments,  Customer? user = null,string? email = null, Address deliveryAddress = null); 
    public Order CancelOrder(Order order);
    public Order Complete(Order order);
    public void UpdateOrder(Order order);
    public Order? GetOrderWithItems(uint orderId);
    public List<Order> GetAllOrders(Customer user,bool includeItems=false,int page = 1, int pageSize = 10);
}
