using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Ecommerce.WebImpl.Pages.Shared;

public class PartialRenderer
{
    private readonly ICompositeViewEngine viewEngine;
    private readonly ITempDataProvider tempDataProvider;
    public PartialRenderer(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider) {
        this.viewEngine = viewEngine;
        this.tempDataProvider = tempDataProvider;
    }
    public async Task<string> RenderPartialViewAsync(HttpContext context, string viewName, object? model)
    {
        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
        await using var sw = new StringWriter();

        var viewResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);

        if (viewResult.View == null)
        {
            throw new ArgumentNullException($"Partial view '{viewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(context, tempDataProvider);

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