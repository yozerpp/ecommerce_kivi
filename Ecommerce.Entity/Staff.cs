namespace Ecommerce.Entity;

public class Staff : User
{
    public uint? ManagerId {get;set;}
    public Staff? Manager { get; set; }
    public ICollection<Staff> TeamMembers { get; set; } = new List<Staff>();
    public ICollection<PermissionClaim> PermissionGrants { get; set; } = new List<PermissionClaim>();
    public ICollection<PermissionClaim> PermissionClaims { get; set; } = new List<PermissionClaim>();
    
}