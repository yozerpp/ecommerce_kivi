using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class UserManager : IUserManager
{
    private delegate string HashFunction(string inpur);

    private HashFunction _hashFunction = str=>str;
    private IRepository<User, DbContext> _userRepository;
    public UserManager(IRepository<User, DbContext> UserRepository, Func<string,string> HashFunction)
    {
        this._userRepository = UserRepository;
        this._hashFunction = new HashFunction(HashFunction);
    }

    public User Login(string username, string password)
    {
        var user = _userRepository.Find(u => u.Username == username && u.PasswordHash == this._hashFunction(password));
        if (user == null)
        {
            throw new ArgumentException("No user found for the provided credentials.");
        }
        return user;
    }
    public User Register(User user)
    {
        return _userRepository.Add(user);
    }

    public void ChangePassword(string username, string oldPassword, string newPassword)
    {
        var user = this.Login(username, oldPassword);
        user.PasswordHash = this._hashFunction(newPassword);
        _userRepository.Update(user);
    }

    public User Update(User user)
    {
       return  _userRepository.Update(user);
        
    }

    public void deactivate()
    {
        var user = UserContextHolder.User;
        if (user == null) throw new UnauthorizedAccessException("You aren't logged in.");
        user.Active = false;
        _userRepository.Update(user);
    }
}
