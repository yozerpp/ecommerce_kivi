namespace Ecommerce.Entity;

public class ProductOffer
{
    public uint ProductId { get; set; }
    public uint SellerId { get; set; }
    public Product? Product { get; set; }
    public Seller? Seller { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}