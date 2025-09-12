using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _InfoPartial
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ErrorCause_ ErrorCause { get; set; }
    public string? Title { get; set;  }
    public string? Link { get;init; }
    public string? Redirect { get; set; }
    public int TimeOut { get; set; } = 3000;

    public enum ErrorCause_
    {
        Input,
        Authorization,
    }
}