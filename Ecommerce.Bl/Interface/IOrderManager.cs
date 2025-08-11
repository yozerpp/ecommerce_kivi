using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    public OrderWithAggregates CreateOrder(Session session, out Session newSession,ICollection<Shipment> shipments,  Customer? user = null,string? email = null, Address deliveryAddress = null); 
    public Order CancelOrder(Order order);
    public Order Complete(Order order);
    public void UpdateOrder(Order order);
    public OrderWithAggregates? GetOrderWithItems(uint orderId);
    public List<OrderWithAggregates> GetAllOrders(Customer user,bool includeItems=false,int page = 1, int pageSize = 10);
}
