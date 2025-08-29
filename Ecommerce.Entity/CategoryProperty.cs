namespace Ecommerce.Entity;

public class CategoryProperty
{
    public const string EnumValuesSeparator = "|";
    public uint Id { get; set; } // Primary key for CategoryProperty
    public uint? CategoryId { get; set; } // Foreign key to Category
    public Category? Category { get; set; } // Navigation property to Category
    public string PropertyName { get; set; }
    public string? EnumValues { get; set; }
    public bool IsRequired { get; set; }
    public bool IsNumber { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? MinValue { get; set; }
}