using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;

namespace Ecommerce.Entity;

public class Staff : User
{
    public uint? ManagerId {get;set;}
    public Staff? Manager { get; set; }

    public ICollection<Staff> TeamMembers { get; set; } = new List<Staff>();
    public ICollection<PermissionClaim> PermissionGrants { get; set; } = new List<PermissionClaim>();
    public ICollection<PermissionClaim> PermissionClaims { get; set; } = new List<PermissionClaim>();
    public ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();
    public ICollection<PermissionRequest> SentPermissionRequests { get; set; } = new List<PermissionRequest>();
    public ICollection<PermissionRequest> ReceivedPermissionRequests { get; set; } = new List<PermissionRequest>();
    public bool HasPermission(Permission permission) {
        return PermissionGrants.Any(grant => grant.Permission.Equals( permission));
    }
}