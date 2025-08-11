using System.Security.Principal;
using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Data.Identity;

public class IdentityUser : User, IIdentity
{
    public string? AuthenticationType { get; }
    public bool IsAuthenticated { get; }
    public string? Name { get; }
}