using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ICustomerManager
{
    Customer Update(Customer customer, bool updateImage = false);
    public ICollection<Order> GetOrders(uint customerId, int page = 1, int pageSize = 10);
    Customer? GetCustomer(uint id);
    CustomerWithAggregates? GetWithAggregates(uint id);
}