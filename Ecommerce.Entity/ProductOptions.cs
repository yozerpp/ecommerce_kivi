using System.Diagnostics;

namespace Ecommerce.Entity;

public class ProductOption
{
    public uint Id { get; set; }
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public ProductOffer ProductOffer { get; set; }
    public uint? CategoryPropertyId { get; set; }
    public ProductCategoryProperty? Property { get; set; }
    public string Value { get; set; }
    public string? Key { get; set; }
    protected bool Equals(ProductOption other) {
        return Id==other.Id;
    }
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default && obj is ProductOption other && Equals(other);
    }
    public override int GetHashCode() {
        return Id!=default
            ? Id.GetHashCode()
            : base.GetHashCode();
    }
}