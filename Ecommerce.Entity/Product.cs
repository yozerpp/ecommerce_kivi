using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using Ecommerce.Entity.Views;

namespace Ecommerce.Entity;

public class Product
{
    public Product(){}
    public uint Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public uint CategoryId { get; set; }
    public Category Category { get; set; }
    public Dimensions Dimensions { get; set; }
    public ProductStats Stats { get; set; }
    [NotMapped] public Image? MainImage => Images.FirstOrDefault(i => i.IsMain) ?? Images.FirstOrDefault();
    public ICollection<Seller> Sellers { get; set; } = new List<Seller>();
    public IList<Image> Images { get; set; } = new List<Image>();
    public ICollection<ProductOffer> Offers { get; set; } = new List<ProductOffer>();
    public ICollection<Customer> FavoredCustomers { get; set; } = new List<Customer>();
    public Dictionary<string, string> CategoryProperties { get; set; } = new();
    public bool Active { get; set; }


    public override bool Equals(object? obj)
    {
        if (obj is not Product other) return false;
        if (Id == default)
        {
            return ReferenceEquals(this,obj);
        }
        return Id == other.Id;
    }
    
    public override int GetHashCode() {
        return Id == default ? base.GetHashCode() : Id.GetHashCode();
    }
}
