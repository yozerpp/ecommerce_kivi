namespace Ecommerce.Entity;

public class AnonymousUser
{
    public string Email { get; set; }
    public string StripeId {get; set;}
    public ICollection<Order> Orders { get; set; }
}