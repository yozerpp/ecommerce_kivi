namespace Ecommerce.Entity.Events;

public class VoteNotification : Notification
{
    public ulong? ReviewId { get; set; }
    public ulong? CommentId { get; set; }
    public ProductReview? Review { get; set; }
    public ReviewComment? Comment { get; set; }
    public uint NumVotes { get; set; }

    protected bool Equals(VoteNotification other) {
        return base.Equals(other) && ReviewId == other.ReviewId && CommentId == other.CommentId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||ReviewId!=default&&CommentId!=default&& obj is VoteNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if (ReviewId == default && CommentId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), ReviewId, CommentId);
    }
}