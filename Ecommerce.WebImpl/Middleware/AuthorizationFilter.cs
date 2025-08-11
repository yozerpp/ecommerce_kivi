using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.WebImpl.Middleware;

public class AuthorizationFilter : IAuthorizationFilter, IAsyncAuthorizationFilter
{
    private readonly ICartManager _cartManager;
    private readonly IJwtManager _jwtManager;

    public AuthorizationFilter(ICartManager cartManager, IJwtManager jwtManager) {
        _cartManager = cartManager;
        _jwtManager = jwtManager;
    }
    public void OnAuthorization(AuthorizationFilterContext context) {
        if (context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value==null){ //if no id is present.
            var s = _cartManager.newSession(null, true);
            var tok = _jwtManager.CreateToken(s, true);
            context.HttpContext.Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme, _jwtManager.Serialize(tok),new CookieOptions(){
                MaxAge = TimeSpan.FromDays(3),
            });
            var redirectUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result =
                new RedirectResult(redirectUrl);
            return;
        }
        _jwtManager.ParsePrincipal(out var user, out var session, context.HttpContext.User);
        if (user != null){
            context.HttpContext.Items[nameof(User)] = user;
            context.HttpContext.Items[nameof(Session)] = user.Session ?? session;
        }
        else if (session == null){
            context.HttpContext.Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
            context.Result =
                new RedirectResult(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
            return;
        }
        else 
            context.HttpContext.Items[nameof(Session)] = session;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        return Task.Run(()=> OnAuthorization(context));
    }
}