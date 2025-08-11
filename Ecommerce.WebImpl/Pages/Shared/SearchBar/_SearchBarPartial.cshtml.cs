using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _SearchBarPartial
{
    public EntityMapper EntityMapper { get; init; }
    public string? Target { get; init; }
    public bool IsJson { get; init; }
    public required ICollection<Category> Categories { get; init; }
}