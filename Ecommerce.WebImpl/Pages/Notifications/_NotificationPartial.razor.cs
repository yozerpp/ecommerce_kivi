using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ecommerce.WebImpl.Pages.Notifications;

public class _NotificationPartialModel : ComponentBase
{
    private HubConnection? hubConnection;
    [Parameter]
    [BindProperty] public ICollection<Entity.Events.Notification> Notifications { get; set; }
    [Inject]
    public NavigationManager NavigationManager { get; set; }
    [BindProperty]
    public Entity.Events.Notification? LastNotification { get; set; }
    protected override async Task OnInitializedAsync() {
        hubConnection = new HubConnectionBuilder().WithUrl(NavigationManager.ToAbsoluteUri("/notifications"))
            .Build();
        hubConnection.On("ReceiveNotification", (Entity.Events.Notification notification) => {
            Notifications.Add(notification);
            LastNotification = notification;
            InvokeAsync(StateHasChanged);
        });
        await hubConnection.StartAsync();
    }
    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
            await hubConnection.DisposeAsync();
    }

}

