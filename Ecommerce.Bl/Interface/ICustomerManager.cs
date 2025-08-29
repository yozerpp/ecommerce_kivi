using Ecommerce.Entity;
using Ecommerce.Entity.Common;

namespace Ecommerce.Bl.Interface;

public interface ICustomerManager
{
    public ICollection<Order> GetOrders(uint customerId, int page = 1, int pageSize = 10);
    Customer? GetCustomer(uint id);
    Customer? GetWithAggregates(uint id);
    void UpdateAddresses(uint id,ICollection<Address> addresses);
}