using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Microsoft.AspNetCore.SignalR;

namespace Ecommerce.Notifications;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IHubContext<NotificationHub> _notificationHub;
    public NotificationService(IRepository<Notification> notificationRepository, IHubContext<NotificationHub> notificationHub) {
        _notificationRepository = notificationRepository;
        _notificationHub = notificationHub;
    }

    public async Task SendSingleAsync<T>(T notification, object? transform =null) where T : Notification {
        var n=  await _notificationRepository.TryAddAsync(notification);
        if(n)
            await _notificationHub.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", transform??notification);
    }

    public async Task SendBatchAsync<T>(ICollection<T> notifications, object? transform=null) where T : Notification {
        foreach (var notification in notifications){
            await _notificationRepository.TryAddAsync(notification);
        }
        _notificationRepository.Flush();
        await _notificationHub.Clients.Users(notifications.Select(n=>n.UserId.ToString()).ToArray()).SendAsync("ReceiveNotification", transform??notifications.First());
    }

    public Task MarkAsync(bool state,params ICollection<ulong> notificationIds) {
        if (notificationIds == null || notificationIds.Count == 0) return Task.CompletedTask;
        _notificationRepository.UpdateExpr([(n => n.IsRead, state)], n => notificationIds.Contains(n.Id));
        return Task.CompletedTask;
    }
    public ICollection<Notification> Get(uint userId, bool onlyUnread = false, int page = 1, int pageSize = 20) {
        return _notificationRepository.Where(n => n.UserId == userId && (!onlyUnread || onlyUnread && !n.IsRead), offset:
            (page - 1) * pageSize, limit: page * pageSize, orderBy:[(n=>n.Time, false)]);
    }
    // public async Task BroadcastDiscountAsync(DiscountNotification notification) {
    //     var n = (DiscountNotification)await _notificationRepository.SaveAsync(notification);
    //     
    //     await _notificationHub.Clients.Groups(NotificationHub.ProductFavorGroup + notification.ProductId)
    //         .SendAsync("ReceiveNotification", n);
    //     await _notificationHub.Clients.Groups(NotificationHub.ProductFavorGroup + notification.ProductId)
    //         .SendAsync("Refresh", n);
    // }
    // public async Task BroadcastCouponAsync(CouponNotification notification) {
    //     var n  =(CouponNotification)await _notificationRepository.SaveAsync(notification);
    //     await _notificationHub.Clients.Groups(NotificationHub.SellerFavorGroup + notification.SellerId).SendAsync("ReceiveNotification", n);
    //     await _notificationHub.Clients.Groups(NotificationHub.ProductFavorGroup + notification.Seller)
    //         .SendAsync("Refresh", n);
    // }

}