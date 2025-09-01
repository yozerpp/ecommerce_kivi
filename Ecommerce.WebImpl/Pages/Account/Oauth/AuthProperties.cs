namespace Ecommerce.WebImpl.Pages.Account.Oauth;

public class AuthProperties
{
    public Type AuthType { get; set; }
    public RetrieveScope Retrieves { get; set; }
    public Entity.User.UserRole Role { get; set; }
    public enum RetrieveScope
    {
        Phone,
        Address,
    }
    public enum Type {
        Identity,
        Permission,
    }
    public string ReturnUrl { get; set; }
    public uint UserId { get; set; }
}