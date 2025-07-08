using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface IOrderManager
{
    bool isComplete(string transactionId);
    Order CreateOrder(Payment payment);
    Order CancelOrder(Order order);
    Order complete(Order order);
    Order UpdateOrder(Order order);
}
