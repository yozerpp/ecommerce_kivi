using Ecommerce.Notifications.UserIdProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Ecommerce.Notifications;

public class NotificationHub : Hub
{
    public async Task JoinProductAsync(uint productId) {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProductFavorGroup + productId);
    }
    public async Task LeaveProductAsync(uint productId) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProductFavorGroup + productId);
    }
    public async Task JoinSellerAsync(uint sellerId) {
        await Groups.AddToGroupAsync(Context.ConnectionId, SellerFavorGroup + sellerId);
    }
    public async Task LeaveSellerAsync(uint sellerId) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SellerFavorGroup + sellerId);
    }
    public const string SellerFavorGroup = "seller_favor-";
    public const string ProductFavorGroup = "product_favor-";
}