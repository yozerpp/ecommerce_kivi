namespace Ecommerce.WebImpl.Pages.Shared.Order;

public class _OrderItemsPartial
{
    
    public bool ViewedBySeller { get; set; }
    public string? Token { get; set; }
    public Entity.Order Order { get; set; }
    public bool IsCollapsable { get; set; }
    public bool Editable { get; set; }
}  