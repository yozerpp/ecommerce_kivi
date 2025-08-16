using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _PromptablePartial 
{
    public required string DisplayText { get; init; }
    public required string Url { get; init; }
    public string? SwitchText { get; init; }
    public required InputModel[] Inputs { get; init; }
    public string Target { get; init; } = "popupResult";
    public string Color { get; init; }= "darkorange";
    public string Classes { get; init; }= "btn btn-primary";
    public string? FormClasses { get; init; }
}