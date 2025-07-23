using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ICustomerManager
{
    Customer Update(Customer customer);
    Customer? GetCustomer(uint id);
    CustomerWithAggregates? GetWithAggregates(uint id);
}