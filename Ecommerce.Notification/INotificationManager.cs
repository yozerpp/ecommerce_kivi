namespace Ecommerce.Notification;

public interface INotificationManager
{
    public Task SendAsync<T>(T notification) where T
}