using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Concrete;

public class CustomerManager : ICustomerManager
{
    private readonly IRepository<Customer> _customerRepository;

    public CustomerManager(IRepository<Customer> customerRepository) {
        _customerRepository = customerRepository;
    }
    public Customer Update(Customer customer) {
        var ret =  _customerRepository.Update(customer);
        _customerRepository.Flush();
        return ret;
    }

    public Customer? GetCustomer(uint id) {
        return _customerRepository.First(c => c.Id == id);

    }

    public CustomerWithAggregates? GetWithAggregates(uint id) {
        return _customerRepository.First(UserAggregateProjection, u => u.Id == id, includes:[[nameof(Customer.Session)]]);

    }
    
    private static readonly Expression<Func<Customer, CustomerWithAggregates>> UserAggregateProjection = 
        u => new CustomerWithAggregates {
            Id = u.Id, 
            NormalizedEmail = u.NormalizedEmail, 
            FirstName = u.FirstName, 
            LastName = u.LastName,
            // TotalSpent = u.Orders.SelectMany(o=>o.Items ).Sum(i=>
            // (i.Quantity * (decimal?)i.ProductOffer.Discount * (decimal?)i.ProductOffer.Price *(decimal?) (i.Coupon != null ? (decimal?)i.Coupon.DiscountRate :(decimal?) 1m ) ))??0m,
            TotalOrders = ((int?)u.Orders.Count()) ?? 0,
            // TotalDiscountUsed = u.Orders.SelectMany(o=>o.Items).Sum(i=>
            // (decimal?)((decimal?)i.Quantity * (1m-(decimal?)i.ProductOffer.Discount) *(decimal?) i.ProductOffer.Price *(decimal?) (i.Coupon != null ? (1m-(decimal?)i.Coupon.DiscountRate) : (decimal?)0m)))??0m,
            TotalReviews = ((int?)u.Reviews.Count())??0,
            TotalReplies = ((int?)u.ReviewComments.Count())??0,
            TotalKarma = ((int?) u.Reviews.SelectMany(r=>r.Votes).Sum(v=>(int?)((v.Up) ? 1 : -1)) )??0,
            Address = u.Address,
            PhoneNumber = u.PhoneNumber,
            Active = u.Active, 
            Session= u.Session,
            SessionId = u.SessionId,
            Orders = u.Orders,
            Reviews = u.Reviews,
            ReviewComments = u.ReviewComments,
            
        };
}