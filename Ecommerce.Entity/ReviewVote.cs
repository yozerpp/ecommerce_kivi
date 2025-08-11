namespace Ecommerce.Entity;

public class ReviewVote
{
    public ulong Id { get; set; }
    public ulong? ReviewId {get;set;}
    public ulong? CommentId {get;set;}
    public ulong VoterId {get;set;}
    public uint? UserId { get; set; }
    public Session Voter { get; set; }
    public User? User { get; set; }
    public bool Up { get; set; }
    public ProductReview? ProductReview { get; set; }
    public ReviewComment? ReviewComment { get; set; }

    protected bool Equals(ReviewVote other) {
        return Id == other.Id;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||Id!=default && obj is ReviewVote other && Equals(other);
    }

    public override int GetHashCode() {
        if (Id == default ) return base.GetHashCode();
        return HashCode.Combine(Id, ReviewId, CommentId);
    }
}