using System.ComponentModel.DataAnnotations;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class User
{
    public uint Id { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    [EmailAddress]

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    public PhoneNumber PhoneNumber { get; set; }
    public bool Active { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }
}
