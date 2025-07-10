using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Category
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public uint? ParentId { get; set; }
    [SelfReferencingProperty]
    public Category? Parent { get; set; }

    public ISet<Category> Children { get; set; } = new HashSet<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}