using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _PhoneNumberPartial
{
    public PhoneNumber? PhoneNumber { get; init; }
    public bool Editable { get; init; }
    public bool IsInput { get; init; }
    public string? Id { get; init; }
}