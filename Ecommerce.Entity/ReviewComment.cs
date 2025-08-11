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
    public DateTime Created { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public ICollection<ReviewComment> Replies { get; set; } = new List<ReviewComment>();
    protected bool Equals(ReviewComment other) {
        return Id == other.Id; // Rely solely on Id for equality
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default && obj is ReviewComment other && Equals(other);
    }
    public override int GetHashCode() {
        if (Id == default) return base.GetHashCode();
        return Id.GetHashCode(); // Rely solely on Id for hash code
    }
}
