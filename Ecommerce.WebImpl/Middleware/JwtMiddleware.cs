using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
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

    // public override Task TokenValidated(TokenValidatedContext context) {
    //     if (DateTime.Now - context.SecurityToken.ValidTo > TimeSpan.FromMinutes(10)) return Task.CompletedTask;
    //     context.Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
    //     context.Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme, RefreshToken(context));
    //     return Task.CompletedTask;
    // }

    private string RefreshToken(TokenValidatedContext context) {
        _jwtManager.UnwrapToken(context.SecurityToken, out User? user,out var session );
        return _jwtManager.Serialize(_jwtManager.CreateToken(user?.Session??session, context.SecurityToken.ValidFrom - context.SecurityToken.ValidTo));
    }
    private string CreateToken (ResultContext<JwtBearerOptions> context) {
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