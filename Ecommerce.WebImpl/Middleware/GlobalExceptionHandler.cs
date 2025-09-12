using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Ecommerce.WebImpl.Middleware;

public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env,
    PartialRenderer partialRenderer)
{
    private readonly IHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context) {
        try{
            await next.Invoke(context);
        }
        catch (Exception e){
            var ex = e;
            while (ex is TargetInvocationException){
                ex = ex.InnerException;
            }
            bool arg;
            if ((arg= ex is ArgumentException or ValidationException or ArgumentNullException or ArgumentOutOfRangeException) || ex is UnauthorizedAccessException){
                logger.LogWarning("Caught: " + e);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                var view = await partialRenderer.RenderPartialViewAsync(context, nameof(_InfoPartial), new _InfoPartial(){
                    Success = false,
                    Title = "Hata",
                    Message = ex!.Message,
                });
                context.Response.ContentLength = view.Length;
                await context.Response.WriteAsync(view);
            }
            else throw;
        }
    }
  

}