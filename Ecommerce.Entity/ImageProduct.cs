namespace Ecommerce.Entity;

public class ImageProduct
{
    public uint ImageId { get; set; }
    public uint ProductId { get; set; }
    public Product Product { get; set; }
    public Image Image { get; set; }
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is ImageProduct other && ProductId != default && ImageId != default &&
            ProductId == other.ProductId && ImageId == other.ImageId;
    }
    public override int GetHashCode() {
        if (ProductId == default || ImageId == default) return base.GetHashCode();
        return HashCode.Combine(ProductId, ImageId);
    }
}