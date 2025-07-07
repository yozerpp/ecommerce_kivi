namespace Ecommerce.Entity;

public class Item
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtTimeOfAddition { get; set; }
    public int? CartId { get; set; }
    public Cart Cart { get; set; }
    public int? OrderId { get; set; }
    public Order Order { get; set; }
}
