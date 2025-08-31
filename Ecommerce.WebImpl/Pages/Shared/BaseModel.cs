using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public  abstract class BaseModel: PageModel
{
    protected readonly INotificationService NotificationService;

    public BaseModel(INotificationService notificationService) {
        NotificationService = notificationService;
    }

    public Ecommerce.Entity.Seller? CurrentSeller { get; private set; } 
    public Entity.Customer? CurrentCustomer { get; private set; }
    public Staff? CurrentStaff { get; private set; }
    public Entity.User? CurrentUser { get; private set; }
    public Session CurrentSession { get; private set; }

    public override void OnPageHandlerSelected(PageHandlerSelectedContext context) {
        AuthorizeRoles(context);
        // AssignProps(context);
        base.OnPageHandlerSelected(context);
    }

    private void AuthorizeRoles(PageHandlerSelectedContext context) {
        if (!context.HandlerMethod?.MethodInfo.GetCustomAttribute<HasRoleAttribute>()?.Roles.Any(r =>
                r == nameof(Entity.User) &&
                context.HttpContext.User.HasClaim(c => c.Type.Equals(ClaimTypes.Role)) ||
                context.HttpContext.User.HasClaim(ClaimTypes.Role, r)) ?? false)
            throw new UnauthorizedAccessException("You do not have permission to access this page. Please log in or contact the administrator.");
    }

    private void AssignProps(PageHandlerExecutingContext context) {
        ViewData[nameof(Entity.Session)] = CurrentSession = context.HttpContext.Items[nameof(Session)] as Session;
        CurrentCustomer = context.HttpContext.Items[nameof(Entity.User)] as Entity.Customer;
        CurrentSeller = context.HttpContext.Items[nameof(Entity.User)] as Ecommerce.Entity.Seller;
        CurrentStaff = context.HttpContext.Items[nameof(Entity.User)] as Staff;
        CurrentUser = (CurrentStaff as Entity.User ?? CurrentSeller) ?? CurrentCustomer;
        CurrentSession.Cart = new Entity.Cart(){ Id = CurrentSession.CartId, };
        if (CurrentUser != null){
            ViewData[nameof(Entity.User)] = CurrentUser;
            ViewData[nameof(Notification)] = NotificationService.Get(CurrentUser.Id, false);
        }
        
    }
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context) {
        AssignProps(context);
        base.OnPageHandlerExecuting(context);
    }

}