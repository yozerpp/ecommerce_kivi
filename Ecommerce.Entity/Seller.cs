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
}
