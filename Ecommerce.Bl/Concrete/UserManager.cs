using System.IdentityModel.Tokens.Jwt;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;
public class UserManager : IUserManager
{
    private delegate string HashFunction(string inpur);

    private JwtManager _jwtManager;
    private HashFunction _hashFunction = str=>str;
    private IRepository<User> _userRepository;
    private readonly CartManager _cartManager;
    public UserManager(JwtManager manager,IRepository<User> UserRepository, Func<string,string> HashFunction, CartManager cartManager) {
        this._jwtManager = manager;
        this._userRepository = UserRepository;
        this._hashFunction = new HashFunction(HashFunction);
        this._cartManager = cartManager;
    }
    public User Login(string username, string password, out SecurityToken token)
    {
        var user = _userRepository.Find(u => u.Username == username && u.PasswordHash == this._hashFunction(password));
        if (user == null)
        {
            throw new ArgumentException("No user found for the provided credentials.");
        }
        if (user.Session==null){
            ContextHolder.Session!.User = user;
            _cartManager.newCart();
        }
        token = _jwtManager.CreateToken(user.Session!);
        return user;
    }

    public User Current() {
        if (ContextHolder.Session?.User==null){
            throw new UnauthorizedAccessException("You aren't logged in.");
        }
        return ContextHolder.Session.User;
    }
    public User Register(User user)
    {
        return _userRepository.Add(user);
    }
    public void ChangePassword(string oldPassword, string newPassword) {
        User user;
        if ((user=ContextHolder.Session?.User) ==null){
            throw new UnauthorizedAccessException("You aren't logged in.");
        }
        if (!user.PasswordHash.Equals(this._hashFunction(oldPassword))){
            throw new UnauthorizedAccessException("Old password is incorrect.");
        }
        user.PasswordHash = this._hashFunction(newPassword);
        _userRepository.Update(user);
    }
    public void Logout() {
        throw new NotImplementedException("Logout should be handled by the controller/ui layer.");
    }
    public User Update(User user)
    {
       return  _userRepository.Update(user);
    }
    public void deactivate() {
        User? user;
        if ((user =ContextHolder.Session?.User)==null){
            throw new UnauthorizedAccessException("You aren't logged in.");
        }
        user.Active = false;
        _userRepository.Update(user);
    }

}
