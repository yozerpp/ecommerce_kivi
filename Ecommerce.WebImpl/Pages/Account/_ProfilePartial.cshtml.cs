using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Account;

public class _ProfilePartial
{
    public required Entity.User User { get; init; }
    public bool Editable { get; init; }
    public bool Registering { get; init; }
    public required string PostUrl { get; init; }
}