namespace Ecommerce.Entity;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ShippingAddress { get; set; }
    public string BillingAddress { get; set; }
    public string PhoneNumber { get; set; }
    public ICollection<Order> Orders { get; set; }
    public Cart Cart { get; set; }
}
