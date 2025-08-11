using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class DeletablePartial
{
    public IHtmlContent Inner { get; init; }
    public string HandlerUrl { get; init; }
    public ICollection<(string,string)> Inputs { get; init; }

    public DeletablePartial(IHtmlContent inner, string handlerUrl, params ICollection<(string,string)>? inputs) {
        Inner = inner;
        HandlerUrl = handlerUrl;
        Inputs = inputs?? [];
    }
}