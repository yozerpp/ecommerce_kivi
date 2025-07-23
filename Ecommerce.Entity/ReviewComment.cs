namespace Ecommerce.Entity;

public class ReviewComment
{
    public ulong Id { get; set; }
    public ulong ReviewId { get; set; }
    public ulong CommenterId { get; set; }
    public ulong? ParentId { get; set; }
    public uint? UserId { get; set; }
    public ProductReview Review { get; set; }
    public Session Commenter { get; set; }
    public Customer? User { get; set; }
    public ReviewComment? Parent { get; set; }
    public string Comment { get; set; }
    public string? Name { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public ICollection<ReviewComment> Replies { get; set; } = new List<ReviewComment>();
    protected bool Equals(ReviewComment other) {
        return Id == other.Id && ReviewId == other.ReviewId &&  CommenterId == other.CommenterId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default && ReviewId!=default &&CommenterId!=default&& obj is ReviewComment other && Equals(other);
    }
    public override int GetHashCode() {
        if (Id == default && ReviewId == default && CommenterId==default) return base.GetHashCode();
        return HashCode.Combine(Id, ReviewId, CommenterId);
    }
}