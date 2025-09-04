using Ecommerce.Entity.Events;

namespace Ecommerce.WebImpl.Pages.Notifications;

public class _NotificationsPartial
{
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}