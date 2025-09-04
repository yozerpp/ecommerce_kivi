using System.Security.Claims;
using Ecommerce.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.SignalR.Client;
using RenderMode = Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Ecommerce.WebImpl.Components;
public partial class NotificationPartial : ComponentBase, IAsyncDisposable
{
    [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
    [Inject] public PersistentComponentState ComponentState { get; set; }
    private HubConnection hubConnection;
    [Parameter]
    [BindProperty] public ICollection<Entity.Events.Notification> Notifications { get; set; }
    private PersistingComponentStateSubscription? _subscription = null;
    [Inject]
    public NavigationManager NavigationManager { get; set; }
    [BindProperty]
    public Entity.Events.Notification? LastNotification { get; set; }

    protected override async Task OnInitializedAsync() {
        ComponentState.TryTakeFromJson<ICollection<Entity.Events.Notification>>(nameof(Notifications), out var ns);
        if (ns != null) Notifications = ns;
        // ComponentState.TryTakeFromJson("First", out bool first);//on server side
        // ComponentState.RegisterOnPersisting(() => RegisterFirstRender(true),RenderMode.InteractiveServer);
        if (  Notifications == null) return;
        if (ns != null){
            _subscription = ComponentState.RegisterOnPersisting(RegisterNotification, RenderMode);
            
        }
        hubConnection = new HubConnectionBuilder().WithUrl(NavigationManager.ToAbsoluteUri("/notifications"),
                options => { 
                    options.AccessTokenProvider = ()=> Task.FromResult(HttpContextAccessor.HttpContext.Request.Cookies[JwtBearerDefaults.AuthenticationScheme]);
                }).ConfigureLogging(l => {
                l.AddConsole();
                l.SetMinimumLevel(LogLevel.Information);
            }).AddJsonProtocol(j => {
                j.PayloadSerializerOptions.Converters.Add(new NotificationJsonConverter());
            })
            .Build();
        hubConnection.On<Entity.Events.Notification>("ReceiveNotification",notification => {
            Notifications = Notifications.Prepend(notification).ToList();
            LastNotification = notification;
            ComponentState.RegisterOnPersisting(RegisterNotification, RenderMode);
            InvokeAsync(StateHasChanged);
        });
        await hubConnection.StartAsync();
        _subscription = ComponentState.RegisterOnPersisting(RegisterNotification, RenderMode);
    }

    private Task RegisterFirstRender(bool b) {
        ComponentState.PersistAsJson("First", b);
    return Task.CompletedTask;
    }
    private Task RegisterNotification() {
        ComponentState.PersistAsJson(nameof(Notifications), Notifications);
        return Task.CompletedTask;
    }


    public async ValueTask DisposeAsync() {
        _subscription?.Dispose();
        if(hubConnection!=null)await hubConnection.DisposeAsync();
    }
}

