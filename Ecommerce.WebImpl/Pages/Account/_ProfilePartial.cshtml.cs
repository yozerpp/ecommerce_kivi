using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Account;

public class _ProfilePartial
{
    public required Entity.User User { get; init; }
    public bool Editable { get; init; }
    public bool IsOwner { get; init; }
}