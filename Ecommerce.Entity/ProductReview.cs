namespace Ecommerce.Entity;

public class ProductReview
{
    public ulong SessionId { get; set;}
    public uint? UserId { get; set; }
    public uint SellerId { get; set;}
    public uint ProductId { get; set; }
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public Session Session { get; set; }
    public User? Reviewer { get; set; }
    public ProductOffer Offer { get; set; }
    public bool CensorName { get; set; }
    public bool HasBought { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
    public override bool Equals(object? obj) {
        if (obj is not ProductReview review) return false;
        if(ProductId == default && SellerId == default && SessionId == default)
            return ReferenceEquals(this,obj);
        return ProductId==review.ProductId && SellerId == review.SellerId && SessionId == review.SessionId;
    }
    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && SessionId == default)
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, SessionId);
    }
}