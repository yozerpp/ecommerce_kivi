namespace Ecommerce.Entity;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<Item> Items { get; set; }
}
