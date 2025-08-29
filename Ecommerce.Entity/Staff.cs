using System.Collections;
using System.Collections.Concurrent;
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
        return PermissionClaims.Any(grant =>grant.NotExpired && grant.Permission.Equals( permission));
    }
}
public class StaffBag(params IEnumerable<Staff> staves)
{
    private readonly List<Staff> _staves = staves.ToList();
    public Staff this[int index]
    {
        get {
            lock (_staves){
             return _staves[index];
            }
        }
        set{
            lock(_staves){
                _staves[index] = value;
            }
        }
    }
    public void Add(Staff staff) {
        lock (_staves){
            _staves.Add(staff);
        }
    }
    public IEnumerable<Staff> WithPermission(Permission permission) {
        lock (_staves){
            return _staves.Where(s => s.HasPermission(permission));
        }
    }
}