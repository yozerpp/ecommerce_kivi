using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _TabsPartial(params List<(string, IHtmlContent)> tabs)
{
    public List<(string, IHtmlContent)> Tabs { get; } = tabs;
    public bool NavAtTop { get; init; } = true;
}