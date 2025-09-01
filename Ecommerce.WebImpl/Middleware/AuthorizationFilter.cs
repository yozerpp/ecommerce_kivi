using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.WebImpl.Middleware;

public class AuthorizationFilter : IAuthorizationFilter
{
    private readonly ICartManager _cartManager;
    private readonly IJwtManager _jwtManager;

    public AuthorizationFilter(ICartManager cartManager, IJwtManager jwtManager) {
        _cartManager = cartManager;
        _jwtManager = jwtManager;
    }
    public void OnAuthorization(AuthorizationFilterContext context) {
        var auth = context.HttpContext.Features.Get<IAuthenticateResultFeature>();
        var schmee = auth?.AuthenticateResult?.Ticket?.AuthenticationScheme;
        if(schmee != null && schmee!= JwtBearerDefaults.AuthenticationScheme) return;
        
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
        if (context.HttpContext.Items["RefreshJwt"] is not true) return;
        context.HttpContext.Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
        context.HttpContext.Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme,
            _jwtManager.Serialize(_jwtManager.CreateToken(session)), new CookieOptions(){
                MaxAge = TimeSpan.FromHours(1),
            });
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        return Task.Run(()=> OnAuthorization(context));
    }
}