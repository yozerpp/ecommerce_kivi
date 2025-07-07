namespace Ecommerce.Entity;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; }
    public ICollection<Item> Items { get; set; }
}
