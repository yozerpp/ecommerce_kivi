namespace Ecommerce.Entity.Events;

public class PermissionRequest : Request
{
    public uint PermissionId { get; set; }
    public Permission Permission { get; set; }

    protected bool Equals(PermissionRequest other) {
        return base.Equals(other) && PermissionId == other.PermissionId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||PermissionId!=default&& obj is PermissionRequest other && Equals(other);
    }

    public override int GetHashCode() {
        if (PermissionId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), PermissionId);
    }
}