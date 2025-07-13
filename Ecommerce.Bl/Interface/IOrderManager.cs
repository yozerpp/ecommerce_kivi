using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    bool isComplete(string transactionId);
    Order CreateOrder(Payment payment);
    Order CancelOrder(Order order);
    Order complete(Order order);
    Order UpdateOrder(Order order);
    OrderWithAggregates? GetOrderWithItems( uint orderId);
    List<OrderWithAggregates> getAllOrders(int page = 1, int pageSize = 10);
}
