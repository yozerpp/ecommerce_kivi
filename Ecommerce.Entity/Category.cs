using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Category
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public uint? ParentId { get; set; }
    [SelfReferencingProperty(BreakCycle = true)]
    public Category? Parent { get; set; }
    public ISet<Category> Children { get; set; } = new HashSet<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<CategoryProperty> CategoryProperties { get; set; } = new List<CategoryProperty>();
    public class CategoryProperty
    {
        public int Id { get; set; }
        public string PropertyName { get; set; }
        public string[]? EnumValues { get; set; }
        public bool IsRequired { get; set; }
        public bool IsNumber { get; set; }
    }
    public override bool Equals(object? obj)
    {
        if (obj is Category other)
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
