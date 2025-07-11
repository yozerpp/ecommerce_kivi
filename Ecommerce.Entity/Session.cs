namespace Ecommerce.Entity;

public class Session
{
    public ulong Id { get; set; }
    public uint? CartId { get; set; }
    public Cart? Cart { get; set; }
    public uint? UserId {get;set;}
    public User? User {get;set;}

    public override bool Equals(object? obj)
    {
        if (obj is Session other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
