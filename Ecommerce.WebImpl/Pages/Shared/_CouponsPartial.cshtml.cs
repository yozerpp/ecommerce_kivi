using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Seller;

public class _CouponsPartial
{
    public required ICollection<Coupon> Coupons { get; set; }
    public bool ShowSeller { get; set; } = true;
    public bool Editable { get; set; } = false;
}