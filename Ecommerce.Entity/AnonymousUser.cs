namespace Ecommerce.Entity;

public class AnonymousCustomer
{
    public string Email { get; set; }
    public string? ApiId {get; set;}
    public ICollection<Order> Orders { get; set; }
}