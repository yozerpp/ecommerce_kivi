using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Org.BouncyCastle.Ocsp;

namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderListPartial
{
    public required string? Url { get; set; }
    public bool Partial { get; init; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Token { get; set; }
    public required ICollection<Entity.Order> Orders { get; set; } 
    public bool Editable { get; set; }
    public required bool Collapsable { get; set; }
    public bool ViewedBySeller { get; init; }
}