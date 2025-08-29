namespace Ecommerce.WebImpl.Pages.Shared.CartPartials;

public class _CartItemsPartial 
{
    public Entity.Cart ViewedCart { get; init; }
    public string ContainerId { get; init; } = "cartContent";
    public string? Message { get; init; }
} 