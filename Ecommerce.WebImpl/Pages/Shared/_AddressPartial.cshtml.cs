using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _AddressPartial
{
    public required Address Address { get; init; }
    public bool Editable { get; init; }
    public string? Id { get; init; }
    public bool AsInput { get; init; } = false;
    public string? InputPrefix { get; init; }
}