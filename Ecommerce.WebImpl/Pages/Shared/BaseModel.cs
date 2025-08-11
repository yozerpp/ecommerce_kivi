using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.WebImpl.Middleware;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class BaseModel: PageModel
{
    public Ecommerce.Entity.Seller? CurrentSeller { get; private set; } 
    public Entity.Customer? CurrentCustomer { get; private set; }
    public Staff? CurrentStaff { get; private set; }
    public User? CurrentUser { get; private set; }
    public Session CurrentSession { get; private set; }

    public override void OnPageHandlerSelected(PageHandlerSelectedContext context) {
        if (!context.HandlerMethod?.MethodInfo.GetCustomAttribute<HasRoleAttribute>()?.Roles.Any(r =>
                r == nameof(Entity.User) &&
                context.HttpContext.User.HasClaim(c => c.Type.Equals(ClaimTypes.Role)) ||
                context.HttpContext.User.HasClaim(ClaimTypes.Role, r)) ?? false){
        throw new UnauthorizedAccessException("You do not have permission to access this page. Please log in or contact the administrator.");
        }
        base.OnPageHandlerSelected(context);
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context) {
        CurrentCustomer = context.HttpContext.Items[nameof(Entity.User)] as Entity.Customer;
        CurrentSeller = context.HttpContext.Items[nameof(Entity.User)] as Ecommerce.Entity.Seller;
        CurrentStaff = context.HttpContext.Items[nameof(Entity.User)] as Staff;
        CurrentSession = GetItem<Session>(nameof(Session))!;
        ViewData[nameof(Entity.Cart)] = CurrentSession.Cart;
        CurrentUser = (CurrentStaff as User ?? CurrentSeller) ?? CurrentCustomer;
        base.OnPageHandlerExecuting(context);
    }

    protected string GetUserPageLink() {
        return CurrentUser switch{
            Entity.Seller => Url.Page("/Seller", new{ CurrentUser.Id }),
            Entity.Customer => Url.Page("/Customer", new{ CustomerId = CurrentUser.Id }),
            _ => throw new NotImplementedException(),
        };
    }
    public T? GetItem<T>(string contextKey) where T : class {
        var c = HttpContext.Items[contextKey];
        if (c == null) return null;
        if (c is not T c1) throw new InvalidOperationException("Context item is not of the expected type. Expected:" + typeof(T).FullName + " Actual: " + c.GetType().FullName);
        return c1;
    }
}