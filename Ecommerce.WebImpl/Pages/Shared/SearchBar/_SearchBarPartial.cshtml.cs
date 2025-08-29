using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _SearchBarPartial
{
    public string? Target { get; init; }
    public required ICollection<Category> Categories { get; init; }
    public HomepageModel.ViewType_ ViewType { get; init; }
}