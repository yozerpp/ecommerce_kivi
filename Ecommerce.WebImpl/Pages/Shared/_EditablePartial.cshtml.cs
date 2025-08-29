using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _EditablePartial
{
    public string Target { get; set; } = "previous";
    public string? OnClick { get; set; }
}