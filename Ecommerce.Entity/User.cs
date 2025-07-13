using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class User
{
    public uint Id { get; set; }
    [MaxLength(24),MinLength(12)]
    public string PasswordHash { get; set; }
    [EmailAddress]
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    public PhoneNumber PhoneNumber { get; set; }
    public bool Active { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is User other)
        {
            if (Id == default)
            {
                return base.Equals(obj);
            }
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (Id == default)
        {
            return base.GetHashCode();
        }
        return Id.GetHashCode();
    }
}
