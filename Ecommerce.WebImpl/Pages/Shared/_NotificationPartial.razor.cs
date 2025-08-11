using Ecommerce.Entity.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _NotificationPartialModel : ComponentBase
{
    private HubConnection? hubConnection;
    [BindProperty] public List<Notification> Notifications { get; set; } = new();
    [Inject]
    public NavigationManager NavigationManager { get; set; }
    [BindProperty]
    public Notification? LastNotification { get; set; }
    protected override async Task OnInitializedAsync() {
        hubConnection = new HubConnectionBuilder().WithUrl(NavigationManager.ToAbsoluteUri("/notifications"))
            .Build();
        hubConnection.On("ReceiveNotification", (Notification notification) => {
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

