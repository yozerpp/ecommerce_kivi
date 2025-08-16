using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderPartial 
{
    public required Entity.Order Order { get; init; }
    public bool Collapsable { get; init; }
    public string? Token { get; init; }
    public bool ViewedBySeller { get; init; }
}