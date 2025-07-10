using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    User Login(string username, string password, out SecurityToken token);
    User Register(User user);
    void ChangePassword(string oldPassword, string newPassword);
    User Update(User user);
    void deactivate();
}
