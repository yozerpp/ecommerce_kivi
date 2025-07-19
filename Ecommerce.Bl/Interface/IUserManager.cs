using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    User? LoginUser(string Email, string password, out SecurityToken? token);
    User GetUser();
    UserWithAggregates GetWithAggregates(uint? id=null);
    Seller? LoginSeller(string Email, string password, out SecurityToken? token);
    User Register(User newUser);
    void ChangePassword(string oldPassword, string newPassword);
    void Logout();
    User Update(User user);
    void deactivate();
}
