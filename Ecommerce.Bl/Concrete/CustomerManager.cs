using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;

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

    public ICollection<Entity.Order> GetOrders(uint customerId, int page = 1, int pageSize =10) {
        return _orderRepository.WhereP(OrderManager.OrderWithItemsAggregateProjection, o => o.UserId == customerId, offset:
            (page -1)*pageSize, pageSize*page);
    }
    public Customer? GetCustomer(uint id) {
        return _customerRepository.FirstP(UserWithoutAggregateProjection,c => c.Id == id, includes:[[nameof(Customer.Session)]]);
    }
    public Customer? GetWithAggregates(uint id) {
        return _customerRepository.FirstP(WithAggregates,u => u.Id == id, includes:[[nameof(Customer.Session)]]);
    }

    public void UpdateAddresses(uint id,ICollection<Address> addresses) {
        _customerRepository.UpdateExpr([(customer => customer.Addresses, addresses)], c => c.Id == id);
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

    private static Expression<Func<Customer, Customer>> WithAggregates = c => new Customer{
        Id = c.Id,
        ApiId = c.ApiId,
        NormalizedEmail = c.NormalizedEmail,
        Email = c.Email,
        FirstName = c.FirstName,
        LastName = c.LastName,
        PasswordHash = c.PasswordHash,
        Stats = new CustomerStats(){
            CustomerId = ((uint?)c.Stats.CustomerId) ?? 0,
            CommentVotes = c.Stats.CommentVotes??0,
            ReviewVotes = c.Stats.ReviewVotes??0,
            TotalComments = c.Stats.TotalComments??0,
            TotalDiscountUsed = c.Stats.TotalDiscountUsed ?? 0,
            TotalSpent = c.Stats.TotalSpent ?? 0,
            TotalKarma = (c.Stats.CommentVotes ?? 0) + (c.Stats.ReviewVotes??0),
            TotalOrders = c.Stats.TotalOrders??0,
            TotalReviews = c.Stats.TotalReviews??0,
        },
        Addresses = c.Addresses,
        PhoneNumber = c.PhoneNumber,
        Active = c.Active,
        Session = c.Session,
        SessionId = c.SessionId,
        ProfilePicture = c.ProfilePicture,
        ProfilePictureId = c.ProfilePictureId,
    };
}