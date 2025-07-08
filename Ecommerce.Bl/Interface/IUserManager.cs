using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface IUserManager
{
    User Login(string username, string password);
    User Register(User user);
    void ChangePassword(string username, string oldPassword, string newPassword);
    User Update(User user);
    void deactivate();
}
