using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _PropertiesDisplayPartial
{
    public required ICollection<(string?, PropertyInfo)> Properties { get; init; }
    public required object Model { get; init; }
    public bool Editable { get; init; }
    public string? IdPrefix { get; init; }
}