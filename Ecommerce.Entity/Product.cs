using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Product
{
    public Product(){}
    public uint Id { get; set; }
    public string Name { get; set; }
    [Image]
    public string? Image { get; set; }
    public string Description { get; set; }
    public uint CategoryId { get; set; }
    public Category Category { get; set; }
    public ICollection<ProductOffer> Offers { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is Product other)
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
