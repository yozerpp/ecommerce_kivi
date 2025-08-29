namespace Ecommerce.Entity;

// Represents a single attribute-value pair for a product's category properties
public class ProductCategoryProperty
{
    public uint ProductId { get; set; }
    public Product Product { get; set; } = null!; // Navigation property to Product
    public uint CategoryPropertyId { get; set; }
    public CategoryProperty CategoryProperty { get; set; } = null!; // Navigation property to CategoryProperty
    public string? Key { get; set; }
    public string Value { get; set; } = string.Empty; // The value of the property
}
