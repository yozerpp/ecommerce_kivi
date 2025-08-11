using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    Customer? LoginCustomer(string Email, string password, bool rememberMe, out string? token);
    Seller? LoginSeller(string Email, string password, bool rememberMe, out string? token);
    Staff? LoginStaff(string email, string password, bool rememberMe, out string? token);
    T Register<T>(T newUser) where T : User;
    void Update(User user);
    public AnonymousUser? FindAnonymousUser(string? email);
    void ChangePassword(User user,string oldPassword, string newPassword);
    void ChangeEmail(User user, string oldPassword, string newEmail);
    void Deactivate(Customer customer);
    void CreateAnonymous(AnonymousUser anonymousUser);
}
