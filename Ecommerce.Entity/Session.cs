namespace Ecommerce.Entity;

public class Session
{
    public ulong Id { get; set; }
    public uint CartId { get; set; }
    public Cart Cart { get; set; }
    public uint? UserId {get;set;}
    public User? User {get;set;}
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public override bool Equals(object? obj)
    {
        if (obj is Session other)
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
