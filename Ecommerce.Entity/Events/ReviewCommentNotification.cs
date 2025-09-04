using System.Linq.Expressions;

namespace Ecommerce.Entity.Events;

public class ReviewCommentNotification : Notification //UserId is the SellerId
{
    public ulong CommentId { get; set; }

    protected bool Equals(ReviewCommentNotification other) {
        return base.Equals(other) && CommentId == other.CommentId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||CommentId!=default&& obj is ReviewCommentNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if (CommentId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), CommentId);
    }
}