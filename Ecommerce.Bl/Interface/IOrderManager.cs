using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    public Order CreateOrder(Session session, Customer? user = null, string? email = null,
        Address? shippingAddress = null); 
    public Order CancelOrder(Order order);
    public Order Complete(Order order);
    public void UpdateOrder(Order order);
    public OrderWithAggregates? GetOrderWithItems(uint orderId);
    public List<OrderWithAggregates> GetAllOrders(Customer user,bool includeItems=false,int page = 1, int pageSize = 10);
}
