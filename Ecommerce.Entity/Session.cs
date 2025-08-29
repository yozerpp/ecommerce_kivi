using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Entity;

public class Session
{
    public ulong Id { get; set; }
    public uint CartId { get; set; }
    [NotMapped]
    public uint? ItemCount { get; set; }
    public Cart Cart { get; set; }
    public uint? UserId {get;set;}
    public User? User {get;set;}
    public ICollection<Category> VisitedCategories { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is not Session other) return false;
        if (Id == default)
        {
            return base.Equals(obj);
        }
        return Id == other.Id;
    }

    public override int GetHashCode() {
        return Id == default ? base.GetHashCode() : Id.GetHashCode();
    }
}
