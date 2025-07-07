namespace Ecommerce.Entity;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public string ShippingAddress { get; set; }
    public ICollection<Item> Items { get; set; }
    public ICollection<Payment> Payments { get; set; }
}
