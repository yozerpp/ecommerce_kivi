using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;

namespace Ecommerce.Entity;

public class Seller : User
{
    public string ShopName { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public Address Address { get; set; }
    public ICollection<ProductOffer> Offers { get; set; } = new List<ProductOffer>();
    public ICollection<ReviewNotification> ReviewNotifications { get; set; } = new List<ReviewNotification>();
    public ICollection<OrderNotification> OrderNotifications { get; set; } = new List<OrderNotification>();
    public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public ICollection<Customer> FavoredCustomers { get; set; } = new List<Customer>();
}
