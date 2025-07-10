using System.ComponentModel.DataAnnotations;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Seller : User
{
    public string? ShopName { get; set; }
    [EmailAddress]
    public string? SellerEmail { get; set; }
    public PhoneNumber SellerPhoneNumber { get; set; }
    public Address Address { get; set; }
    public ICollection<ProductOffer> Offers { get; set; } = new List<ProductOffer>();

    public override bool Equals(object? obj)
    {
        if (obj is Seller other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
