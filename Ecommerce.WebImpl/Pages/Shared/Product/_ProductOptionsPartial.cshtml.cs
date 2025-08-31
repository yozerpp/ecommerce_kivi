using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class _ProductOptionsPartial
{
    /// <summary>
    /// Requires CategoryProperty in navigation too.
    /// </summary>
    public ICollection<(bool selected ,ProductOption option)> Options { get; init; } =[]; 
    public bool Creating { get; init; }
    public bool Editable { get; init; }
    public ICollection<CategoryProperty> PropertyCandidates { get; init; } =[];
}