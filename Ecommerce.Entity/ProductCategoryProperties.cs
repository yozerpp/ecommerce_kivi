namespace Ecommerce.Entity;

// Represents a single attribute-value pair for a product's category properties
public class ProductCategoryProperties
{
    public uint ProductId { get; set; }
    public Product Product { get; set; } = null!; // Navigation property to Product
    public int CategoryPropertyId { get; set; }
    public Category.CategoryProperty CategoryProperty { get; set; } = null!; // Navigation property to CategoryProperty
    public string Value { get; set; } = string.Empty; // The value of the property
}
