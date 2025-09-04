using System.Net;
using Ecommerce.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ecommerce.WebImpl.Components;

public class NotificationService :IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly NavigationManager _navigationManager;
    private bool _started = false;
    public NotificationService(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/notifications"), options => {
                options.AccessTokenProvider = () => Task.FromResult(httpContextAccessor.HttpContext.Request.Cookies[JwtBearerDefaults.AuthenticationScheme]);
            }).ConfigureLogging(c=>c.AddConsole().SetMinimumLevel(LogLevel.Information))
            .AddJsonProtocol(options => 
            {
                options.PayloadSerializerOptions.Converters.Add(new NotificationJsonConverter());
            })
            .Build();
        _hubConnection.On<Entity.Events.Notification>("ReceiveNotification", async notification => {
            await OnNotificationReceived(notification);
        });
    }
    public event Func<Entity.Events.Notification, Task> OnNotificationReceived = notification => Task.CompletedTask;

    public async ValueTask DisposeAsync() {
        await _hubConnection.DisposeAsync();
    }
}