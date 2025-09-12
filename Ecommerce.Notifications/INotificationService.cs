using Ecommerce.Entity.Events;

namespace Ecommerce.Notifications;

public interface INotificationService
{
    public Task SendSingleAsync<T>(T notification, object? transform=default) where T : Notification;
    public Task SendBatchAsync<T>(ICollection<T> notifications,object?transform = default) where T : Notification;
    public Task MarkAsync(bool isRead, params ICollection<ulong> notificationIds);
    // public Task BroadcastDiscountAsync(DiscountNotification notification);
    // public Task BroadcastCouponAsync(CouponNotification notification);
    public ICollection<Notification>Get(uint userId, bool onlyUnread = false, int page = 1, int pageSize = 20);
}