namespace Ecommerce.Entity;

public class PermissionClaim
{
    public ulong Id { get; set; }
    public uint GranterId { get; set; }
    public uint GranteeId { get; set; }
    public string PermissionId { get; set; }
    public Staff Granter { get; set; }
    public Staff Grantee { get; set; }
    public Permission Permission { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool NotExpired => ValidUntil > DateTime.Now;
    protected bool Equals(PermissionClaim other) {
        return Id == other.Id && GranterId == other.GranterId && GranteeId == other.GranteeId && PermissionId == other.PermissionId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default&& obj is PermissionClaim other && Equals(other);
    }

    public override int GetHashCode() {
        if (Id == default)
            return base.GetHashCode();
        return HashCode.Combine(Id, GranterId, GranteeId, PermissionId);
    }
}