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
}
