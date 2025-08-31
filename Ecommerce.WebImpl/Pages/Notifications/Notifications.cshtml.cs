using Ecommerce.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Notifications;

public class Notifications : PageModel
{
    private readonly INotificationService _notificationService;

    public Notifications(INotificationService notificationService) {
        _notificationService = notificationService;
    }
    [BindProperty(SupportsGet =true)]
    public ulong NotificationId { get; set; }
    public IActionResult OnGetMarkRead() {
        _notificationService.MarkAsync(true,NotificationId).Wait();
        return new OkResult();
    }

    public IActionResult OnGetMarkUnRead() {
        _notificationService.MarkAsync(false, NotificationId).Wait();
        return new OkResult();
    }
}