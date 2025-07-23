using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Customer : User
{
    public ICollection<Seller> FavoriteSellers { get; set; } = new List<Seller>();
    public ICollection<Address> RegisteredAddresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();

}
