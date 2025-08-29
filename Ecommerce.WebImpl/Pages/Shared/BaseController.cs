using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.WebImpl.Pages.Shared;

public class BaseController : Controller
{
    protected Ecommerce.Entity.Seller? CurrentSeller { get; private set; } 
    protected Entity.Customer? CurrentCustomer { get; private set; }
    protected Staff? CurrentStaff { get; private set; }
    protected Entity.User? CurrentUser { get; private set; }
    public Session CurrentSession { get; private set; }
    public override void OnActionExecuting(ActionExecutingContext context) {
        CurrentCustomer = context.HttpContext.Items[nameof(Entity.User)] as Entity.Customer;
        CurrentSeller = context.HttpContext.Items[nameof(Entity.User)] as Ecommerce.Entity.Seller;
        CurrentStaff = context.HttpContext.Items[nameof(Entity.User)] as Staff;
        CurrentSession = GetItem<Session>(nameof(Session))!;
        CurrentUser = (CurrentStaff as Entity.User ?? CurrentSeller) ?? CurrentCustomer;
        base.OnActionExecuting(context);
    }
    public T? GetItem<T>(string contextKey) where T : class {
        var c = HttpContext.Items[contextKey];
        if (c == null) return null;
        if (c is not T c1) throw new InvalidOperationException("Context item is not of the expected type. Expected:" + typeof(T).FullName + " Actual: " + c.GetType().FullName);
        return c1;
    }
}