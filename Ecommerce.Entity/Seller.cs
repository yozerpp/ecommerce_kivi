namespace Ecommerce.Entity;

public class Seller : User
{
    public string? ShopName { get; set; }
    public ICollection<ProductOffer> Offers { get; set; } = new List<ProductOffer>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    public ICollection<Customer> FavoredCustomers { get; set; } = new List<Customer>();
}
