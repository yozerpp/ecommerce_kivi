namespace Ecommerce.Entity;

public class ReviewVote
{
    public int ProductId { get; set; }
    public int SellerId { get; set; }
    public int ReviewerId { get; set; }
    public ulong? CommenterId { get; set; }
    public ulong VoterId { get; set; }
    public bool Up { get; set; }
    public ProductReview? ProductReview { get; set; }
    public ReviewComment? ReviewComment { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is not ReviewVote commentVote) return false;
        if (ProductId == default && SellerId == default && ReviewerId == default && CommenterId == default && VoterId == default) 
            return ReferenceEquals(this, commentVote);
        return ProductId == commentVote.ProductId && SellerId == commentVote.SellerId
               && ReviewerId == commentVote.ReviewerId && CommenterId == commentVote.CommenterId
               && VoterId == commentVote.VoterId;
    }

    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && ReviewerId == default && CommenterId == default && VoterId == default) 
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, ReviewerId, CommenterId, VoterId);
    }
}