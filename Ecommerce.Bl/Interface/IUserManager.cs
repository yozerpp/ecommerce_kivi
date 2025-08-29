using Ecommerce.Entity;
using Ecommerce.Entity.Common;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    Customer? LoginCustomer(string Email, string password, bool rememberMe, out string? token);
    Seller? LoginSeller(string Email, string password, bool rememberMe, out string? token);
    Staff? LoginStaff(string email, string password, bool rememberMe, out string? token);
    T Register<T>(T newUser) where T : User;
    void Update(User user, bool updateImage);
    public AnonymousUser? FindAnonymousUser(string? email);
    void ChangePassword(User user,string oldPassword, string newPassword);
    void ChangeEmail(User user, string oldPassword, string newEmail);
    void Deactivate(Customer customer);
    void CreateAnonymous(AnonymousUser anonymousUser);
    public User? Get(uint id, bool includeImage=false);
    public void UpdatePhone(uint id, PhoneNumber phoneNumber);
}
