namespace Ecommerce.Entity.Events;

public class ReviewNotification : Notification
{
    public ulong ReviewId { get; set; }
    public ProductOffer ProductOffer { get; set; }
    public uint ProductId { get; set; }
    public Seller Seller { get; set; }
    protected bool Equals(ReviewNotification other) {
        return base.Equals(other) && ReviewId == other.ReviewId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || ReviewId!=default&& obj is ReviewNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if (ReviewId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), ReviewId);
    }
}