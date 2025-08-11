using System.Security.Claims;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.WebImpl.Middleware;

public class HasRoleAttribute : Attribute, IAsyncActionFilter, IActionFilter
{

    public ICollection<string> Roles { get; private set; }
    
    public HasRoleAttribute(params string[] roles) {
        Roles = roles;
    }
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        if (!Roles.Any(r=>context.HttpContext.User.HasClaim(ClaimTypes.Role, r))){
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }
        return next();
    }

    public void OnActionExecuting(ActionExecutingContext context) {
        if (Roles.Any(r => context.HttpContext.User.HasClaim(ClaimTypes.Role, r))) return;
        context.Result = new UnauthorizedResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) {
    }
}