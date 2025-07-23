using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    Customer? LoginCustomer(string Email, string password, out SecurityToken? token);

    Seller? LoginSeller(string Email, string password, out SecurityToken? token);
    Staff? LoginStaff(string email, string password, out SecurityToken? token);
    User Register(User newUser);
    void ChangePassword(User user,string oldPassword, string newPassword);
    void deactivate(Customer customer);
}
