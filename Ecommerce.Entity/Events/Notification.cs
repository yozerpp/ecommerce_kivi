namespace Ecommerce.Entity.Events;

public abstract class Notification
{
    public enum NotificationType
    {
        Coupon,
        Review,
        Vote,
        Discount,
        Order,
        ReviewComment,
        OrderCompletion,
        RefundRequest,
        PermissionRequest,
        CancellationRequest
    }

    public ulong Id { get; set; }
    public required uint UserId { get; set; }
    public User User { get; set; }
    public bool IsRead { get; set; }
    public DateTime Time { get; set; }
    public NotificationType Type { get; set; }
    
    protected bool Equals(Notification other) {
        return Id == other.Id && UserId == other.UserId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default&& obj is Notification other && Equals(other);
    }

    public override int GetHashCode() {
        return Id == default ? base.GetHashCode() : HashCode.Combine(Id, UserId);
    }
}
