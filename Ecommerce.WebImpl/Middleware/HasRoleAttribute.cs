using System.Security.Claims;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.WebImpl.Middleware;

public class HasRoleAttribute(params string[] roles) : Attribute
{
    public ICollection<string> Roles { get; } = roles;
}