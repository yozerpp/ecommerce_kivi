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
    ICompositeViewEngine viewEngine,
    ITempDataProvider tempDataProvider)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _env = env;
    private readonly ICompositeViewEngine _viewEngine = viewEngine;
    private readonly ITempDataProvider _tempDataProvider = tempDataProvider;
    public async Task InvokeAsync(HttpContext context) {
        try{
            await _next.Invoke(context);
        }
        catch (Exception e){
            var ex = e;
            while (ex is TargetInvocationException){
                ex = ex.InnerException;
            }
            bool arg;
            if ((arg= ex is ArgumentException or ValidationException or ArgumentNullException or ArgumentOutOfRangeException) || ex is UnauthorizedAccessException){
                _logger.LogWarning("Caught: " + e);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                var view = await RenderPartialViewAsync(context, nameof(_InfoPartial), new _InfoPartial(){
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
    private async Task<string> RenderPartialViewAsync(HttpContext context, string viewName, object? model)
    {
        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
        await using var sw = new StringWriter();

        var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);

        if (viewResult.View == null)
        {
            throw new ArgumentNullException($"Partial view '{viewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(context, _tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

}