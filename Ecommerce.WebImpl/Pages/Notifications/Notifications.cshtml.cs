using Ecommerce.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Notifications;

[Authorize(Policy = nameof(Entity.User), AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class Notifications : PageModel
{
    private readonly INotificationService _notificationService;

    public Notifications(INotificationService notificationService) {
        _notificationService = notificationService;
    }
    [BindProperty(SupportsGet =true)]
    public ulong NotificationId { get; set; }
    [BindProperty(SupportsGet = true)]
    public uint UserId { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;
    
    public IActionResult OnGet() {
        var notifications = _notificationService.Get(UserId, false, PageNumber, PageSize);
        return Partial(nameof(_NotificationsPartial), new _NotificationsPartial(){
            Notifications = notifications
        });
    }
    public IActionResult OnGetMarkRead() {
        _notificationService.MarkAsync(true,NotificationId).Wait();
        return new OkResult();
    }

    public IActionResult OnGetMarkUnRead() {
        _notificationService.MarkAsync(false, NotificationId).Wait();
        return new OkResult();
    }
}