namespace Ecommerce.Entity;

public class ReviewComment
{
    public uint SellerId { get; set; }
    public uint ReviewerId { get; set; }
    public uint ProductId { get; set; }
    public ProductReview Review { get; set; }
    public User Reviewer { get; set; }
    public ulong CommenterId { get; set; }
    public Session Commenter { get; set; }
    public string? Comment { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public override bool Equals(object? obj) {
        if (obj is not ReviewComment reviewComment) return false;
        if (ProductId == default&& SellerId ==default&&ReviewerId==default&&CommenterId==default) return ReferenceEquals(this,reviewComment);
        return ProductId == reviewComment.ProductId && SellerId == reviewComment.SellerId
            && CommenterId == reviewComment.CommenterId&& ReviewerId == reviewComment.ReviewerId;
    }
    
    public override int GetHashCode() {
        if (ProductId == default&& SellerId ==default&&ReviewerId==default&&CommenterId==default) 
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, ReviewerId, CommenterId);
    }
}