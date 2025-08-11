namespace Ecommerce.Entity.Events;

// Notification => User is the Requestee.
public abstract class Request : Notification
{
    public bool IsApproved { get; set; }
    public uint RequesterId { get; set; }
    public User Requester { get; set; }

    protected bool Equals(Request other) {
        return base.Equals(other) && RequesterId == other.RequesterId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || RequesterId!=default&& obj is Request other && Equals(other);
    }

    public override int GetHashCode() {
        if (RequesterId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), RequesterId);
    }
}