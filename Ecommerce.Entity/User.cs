using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;

namespace Ecommerce.Entity;

public abstract class User
{
    public uint Id { get; set; }
    public ulong SessionId { get; set; }
    public Session? Session { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [NotMapped]
    public string FullName => FirstName + " " + LastName;
    public string Email { get; set; }
    [EmailAddress]
    public string NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    public uint? ProfilePictureId { get; set; }
    public Image? ProfilePicture { get; set; }
    public PhoneNumber PhoneNumber { get; set; }
    public string? GoogleId { get; set; }
    public bool Active { get; set; }
    // public ICollection<Request> Requests { get; set; }
    public ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ReviewCommentNotification> ReviewCommentNotifications { get; set; } = new List<ReviewCommentNotification>();
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
    public UserRole Role { get; set; }
    public enum UserRole
    {
        Customer,Seller,Staff
    }
    public override int GetHashCode() {
        return Id == default ? base.GetHashCode() : Id.GetHashCode();
    }
}