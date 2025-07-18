namespace Ecommerce.Entity;

public class ReviewComment
{
    public uint SellerId { get; set; }
    public ulong ReviewSessionId { get; set; }
    public uint ProductId { get; set; }
    public ulong SessionId { get; set; }
    public uint? UserId { get; set; }
    public string Comment { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public ProductReview Review { get; set; }
    public Session Session { get; set; }
    public User? User { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public override bool Equals(object? obj) {
        if (obj is not ReviewComment reviewComment) return false;
        if (ProductId == default&& SellerId ==default&&ReviewSessionId==default) return ReferenceEquals(this,reviewComment);
        return ProductId == reviewComment.ProductId && SellerId == reviewComment.SellerId && ReviewSessionId == reviewComment.ReviewSessionId;
    }
    
    public override int GetHashCode() {
        if (ProductId == default&& SellerId ==default&&ReviewSessionId==default) 
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, ReviewSessionId);
    }
}