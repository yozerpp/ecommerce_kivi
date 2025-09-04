using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Ecommerce.Notifications.UserIdProvider;

public class JwtUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection) {
        if (!connection.User.HasClaim(c => c.Type == ClaimTypes.Role)) return null;
        return connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}