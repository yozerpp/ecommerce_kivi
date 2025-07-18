using System.ComponentModel.DataAnnotations;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Seller : User
{
    public string? ShopName { get; set; }
    [EmailAddress]
    public string? ShopEmail { get; set; }
    public PhoneNumber ShopPhoneNumber { get; set; }
    public Address ShopAddress { get; set; }
    public ICollection<ProductOffer> Offers { get; set; } = new List<ProductOffer>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public override bool Equals(object? obj)
    {
        if (obj is Seller other)
        {
            if (Id == default)
            {
                return base.Equals(obj);
            }
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (Id == default)
        {
            return base.GetHashCode();
        }
        return Id.GetHashCode();
    }
}
