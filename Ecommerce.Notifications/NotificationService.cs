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

    public async Task SendSingleAsync<T>(T notification) where T : Notification {
        var n=  await _notificationRepository.TryAddAsync(notification);
        _notificationRepository.Flush();
        await _notificationHub.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", n);
    }

    public async Task SendBatchAsync<T>(ICollection<T> notifications) where T : Notification {
        foreach (var notification in notifications){
            await _notificationRepository.TryAddAsync(notification);
        }
        _notificationRepository.Flush();
        foreach (var notification in notifications){
            _notificationHub.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", notification);
        }
    }

    public async Task BroadcastDiscountAsync(DiscountNotification notification) {
        var n = (DiscountNotification)await _notificationRepository.SaveAsync(notification);
        await _notificationHub.Clients.Groups(NotificationHub.ProductFavorGroup + notification.ProductOffer.ProductId)
            .SendAsync("ReceiveNotification", n);
    }
    public async Task BroadcastCouponAsync(CouponNotification notification) {
        var n  =(CouponNotification)await _notificationRepository.SaveAsync(notification);
        await _notificationHub.Clients.Groups(NotificationHub.SellerFavorGroup + notification.SellerId).SendAsync("ReceiveNotification", n);
    }
    public ICollection<Notification> Get(uint userId, bool onlyUnread = false, int page = 1, int pageSize = 20) {
        return _notificationRepository.Where(n => n.UserId == userId && (!onlyUnread || onlyUnread && !n.IsRead), offset:
            (page - 1) * pageSize, limit: page * pageSize);
    }
}