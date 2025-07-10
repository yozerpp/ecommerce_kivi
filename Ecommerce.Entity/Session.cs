namespace Ecommerce.Entity;

public class Session
{
    public uint Id { get; set; }
    public uint? CartId { get; set; }
    public Cart? Cart { get; set; }
    public uint? UserId {get;set;}
    public User? User {get;set;}
}