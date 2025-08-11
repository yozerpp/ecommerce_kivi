using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Headers;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;

namespace Ecommerce.Entity;

public class Customer : User
{
    public string? StripeId { get; set; }
    public ICollection<Seller> FavoriteSellers { get; set; } = new List<Seller>();
    public ICollection<Product> FavoriteProducts { get; set; } = new List<Product>();
    public ICollection<CouponNotification> CouponNotifications { get; set;} = new List<CouponNotification>();
    public ICollection<DiscountNotification> DiscountNotifications { get; set; } = new List<DiscountNotification>();
    public ICollection<VoteNotification> VoteNotifications { get; set; } = new List<VoteNotification>();
    public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
    public ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();
    public IList<Address> Addresses { get; set; } = new List<Address>();

    public Address? PrimaryAddress => Addresses.FirstOrDefault();
    public ICollection<Order> Orders { get; init; } 
    public ICollection<ProductReview> Reviews { get; init; }
    public ICollection<ReviewComment> ReviewComments { get; init; }

}
