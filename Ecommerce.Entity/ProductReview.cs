namespace Ecommerce.Entity;

public class ProductReview
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set;}
    public uint ReviewerId { get; set;}
    public float Rating { get; set; }
    public string? Comment { get; set; }
    public int Upvotes { get; set; }
    public ProductOffer Offer { get; set; }
    public User Reviewer { get; set; }
    public ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
    public override bool Equals(object? obj) {
        if (obj is not ProductReview review) return false;
        if(this.ProductId == default && this.SellerId == default && this.ReviewerId == default)
            return ReferenceEquals(this,obj);
        return ProductId==review.ProductId && SellerId == review.SellerId && ReviewerId == review.ReviewerId;
    }

    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && ReviewerId == default)
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, ReviewerId);
    }
}