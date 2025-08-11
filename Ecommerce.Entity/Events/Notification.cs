namespace Ecommerce.Entity.Events;

public abstract class Notification
{
    public ulong Id { get; set; }
    public required uint UserId { get; set; }
    public User User { get; set; }
    public bool IsRead { get; set; }
    public DateTime Time { get; set; }
    protected bool Equals(Notification other) {
        return Id == other.Id && UserId == other.UserId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default&& UserId!=default && obj is Notification other && Equals(other);
    }

    public override int GetHashCode() {
        if (Id == default && UserId == default) return base.GetHashCode();
        return HashCode.Combine(Id, UserId);
    }
}