using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

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

    public ICollection<Entity.Order> GetOrders(uint customerId, int page = 1, int pageSize =10) {
        return _orderRepository.WhereP(OrderManager.OrderWithItemsAggregateProjection, o => o.UserId == customerId, offset:
            (page -1)*pageSize, pageSize*pageSize);
    }
    public Customer? GetCustomer(uint id) {
        return _customerRepository.First(c => c.Id == id);
    }
    public Customer? GetWithAggregates(uint id) {
        return _customerRepository.First(u => u.Id == id, includes:[[nameof(Customer.Session)]]);
    }
    
    private static readonly Expression<Func<Customer, Customer>> UserWithoutAggregateProjection = 
        u => new Customer {
            Id = u.Id, 
            NormalizedEmail = u.NormalizedEmail,
            Email = u.Email,
            FirstName = u.FirstName,
            PasswordHash = u.PasswordHash,
            Stats = null,
            LastName = u.LastName,
            Addresses = u.Addresses,
            PhoneNumber = u.PhoneNumber,
            Active = u.Active, 
            Session= u.Session,
            SessionId = u.SessionId,
        };
}