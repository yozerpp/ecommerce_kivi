namespace Ecommerce.Entity;

public class SessionVisitedCategory
{
    public ulong SessionId { get; set; }
    public uint CategoryId { get; set; }
    public Category Category { get; set; }
    public Session Session { get; set; }
    protected bool Equals(SessionVisitedCategory other) {
        return SessionId == other.SessionId && CategoryId == other.CategoryId;
    }
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || SessionId!=default && CategoryId!=default&&obj is SessionVisitedCategory other && Equals(other);
    }
    public override int GetHashCode() {
        if (SessionId == default || CategoryId == default) return base.GetHashCode();
        return HashCode.Combine(SessionId, CategoryId);
    }
}