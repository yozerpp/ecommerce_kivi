namespace Ecommerce.Entity;

public class ReviewVote
{
    public ulong ReviewSessionId { get; set; }
    public uint SellerId { get; set; }
    public uint ProductId { get; set; }
    public ulong VoterId { get; set; }
    public ulong? CommenterId { get; set; }
    public uint? VoterUserId { get; set; }
    public Session Voter { get; set; }    
    public User? VoterUser { get; set; }
    public bool Up { get; set; }
    
    public ProductReview ProductReview { get; set; }
    public ReviewComment? ReviewComment { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is not ReviewVote commentVote) return false;
        if (ProductId == default && SellerId == default && ReviewSessionId == default && CommenterId == default && VoterId == default) 
            return ReferenceEquals(this, commentVote);
        return ProductId == commentVote.ProductId && SellerId == commentVote.SellerId
               && ReviewSessionId == commentVote.ReviewSessionId && CommenterId == commentVote.CommenterId
               && VoterId == commentVote.VoterId;
    }

    public override int GetHashCode() {
        if (ProductId == default && SellerId == default && ReviewSessionId == default && CommenterId == default && VoterId == default) 
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, ReviewSessionId, CommenterId, VoterId);
    }
}