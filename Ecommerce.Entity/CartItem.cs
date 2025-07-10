namespace Ecommerce.Entity;

public class CartItem
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public uint CartId { get; set; }
    public int Quantity { get; set; }
    public ProductOffer? ProductOffer { get; set; }
    public Cart? Cart { get; set; }
}
