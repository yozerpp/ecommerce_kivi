using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Ecommerce.WebImpl.Middleware;

public class JwtMiddleware :JwtBearerEvents
{
    private readonly IJwtManager _jwtManager;
    private readonly ISessionManager _sessionManager;   
    public JwtMiddleware(IJwtManager jwtManager, ISessionManager sessionManager) {
        _jwtManager = jwtManager;
        _sessionManager = sessionManager;
    }

    public override Task MessageReceived(MessageReceivedContext context) {
        if (!context.Request.Cookies.TryGetValue(JwtBearerDefaults.AuthenticationScheme, out var cookie))
            context.Token = CreateToken(context);
        else context.Token = cookie;
        return Task.CompletedTask;
    }
    private string CreateToken (MessageReceivedContext context) {
        var s = _sessionManager.newSession(null, true);
        var tok =_jwtManager.Serialize(_jwtManager.CreateToken(s, TimeSpan.FromDays(30)));
        context.HttpContext.Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme, tok,new CookieOptions(){
            MaxAge = TimeSpan.FromDays(30),
        });
        context.Principal = new ClaimsPrincipal([
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, s.Id.ToString())],
                JwtBearerDefaults.AuthenticationScheme)
        ]);
        return tok;
    }
}