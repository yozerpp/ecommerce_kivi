using Ecommerce.Notifications;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebImpl.Pages.Notifications;

[Authorize(Policy = nameof(Entity.User), AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class Notifications : BaseModel
{

    public Notifications(INotificationService notificationService) : base(notificationService) {
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
        var notifications = NotificationService.Get(CurrentUser?.Id ?? throw new UnauthorizedAccessException("Sadece Kayıtlı Kullanıcılar Bildirim Görebilir."), false, PageNumber, PageSize);
        return Partial("Notifications/" + nameof(_NotificationsPartial), new _NotificationsPartial(){
            Notifications = notifications
        });
    }
    public IActionResult OnGetMarkRead() {
        NotificationService.MarkAsync(true,NotificationId).Wait();
        return new OkResult();
    }

    public IActionResult OnGetMarkUnRead() {
        NotificationService.MarkAsync(false, NotificationId).Wait();
        return new OkResult();
    }
}