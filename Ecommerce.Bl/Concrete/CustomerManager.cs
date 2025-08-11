using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Concrete;

public class CustomerManager : ICustomerManager
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Image> _imageRepository;
    public CustomerManager(IRepository<Image> imageRepository,IRepository<Customer> customerRepository, IRepository<Order> orderRepository) {
        _customerRepository = customerRepository;
        _imageRepository = imageRepository;
        _orderRepository = orderRepository;
    }
    public Customer Update(Customer customer, bool updateImage =false) {
        if (updateImage){
            customer.ProfilePicture = _imageRepository.Add(new Image(){
                Data = customer.ProfilePicture.Data,
            });
        }
        else{
            customer.ProfilePictureId = null;
            customer.ProfilePicture = null;
        }
        var ret =  _customerRepository.Update(customer, true);
        _customerRepository.Flush();
        return ret;
    }

    public ICollection<OrderWithAggregates> GetOrders(uint customerId, int page = 1, int pageSize =10) {
        return _orderRepository.WhereP(OrderManager.OrderWithItemsAggregateProjection, o => o.UserId == customerId, offset:
            (page -1)*pageSize, pageSize*pageSize);
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
            Addresses = u.Addresses,
            PhoneNumber = u.PhoneNumber,
            Active = u.Active, 
            Session= u.Session,
            SessionId = u.SessionId,
        };
}