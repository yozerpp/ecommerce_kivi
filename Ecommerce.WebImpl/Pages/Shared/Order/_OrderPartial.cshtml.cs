using Ecommerce.Entity.Projections;

namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderPartial
{
    public Entity.Order Order { get; set; }
    public bool IsCollapsable { get; set; }
}  