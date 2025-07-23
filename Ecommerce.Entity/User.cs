using System.ComponentModel.DataAnnotations;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;

namespace Ecommerce.Entity;

public class User
{
    public uint Id { get; set; }
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    [EmailAddress]
    public string NormalizedEmail { get; set; }
    public string PasswordHash { get; set; }
    public Address Address { get; set; }
    public PhoneNumber PhoneNumber { get; set; }
    public bool Active { get; set; }
    public ICollection<Request> Requests { get; set; }
    public ICollection<Notification> Notifications { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is Customer other)
        {
            if (Id == default && NormalizedEmail == default)
            {
                return base.Equals(obj);
            }
            return Id == other.Id&&NormalizedEmail == other.NormalizedEmail;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (Id == default&&NormalizedEmail==default)
        {
            return base.GetHashCode();
        }
        return HashCode.Combine(Id, NormalizedEmail);
    }
}