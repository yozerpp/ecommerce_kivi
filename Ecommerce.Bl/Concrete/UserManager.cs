using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;
public class UserManager : IUserManager
{ 
    private delegate string HashFunction(string input);
    
    private readonly JwtManager _jwtManager;
    private readonly HashFunction _hashFunction;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly CartManager _cartManager;
    public UserManager(JwtManager manager,IRepository<User> userRepository, IRepository<Seller> sellerRepository, Func<string,string> hashFunction, CartManager cartManager) {
        this._jwtManager = manager;
        this._userRepository = userRepository;
        this._hashFunction = new HashFunction(hashFunction);
        this._cartManager = cartManager;
        _sellerRepository = sellerRepository;
    }
    
    public User? LoginUser(string email, string password, out SecurityToken? token)
    {
        return doLogin(email, password, false, out token);
    }
    public Seller? LoginSeller(string email, string password, out SecurityToken? token)
    {
        return (Seller?) doLogin(email, password, true, out token); 
    }
    private User? doLogin(string email, string password, bool isSeller, out SecurityToken? token) {
        User? user;
        if (isSeller)
            user = _sellerRepository.First(u => u.Email == email && u.PasswordHash == _hashFunction(password),
                includes:[[nameof(User.Session)]]);
        else
            user = _userRepository.First(u => u.Email == email && u.PasswordHash == _hashFunction(password),
                includes:[[nameof(User.Session)]]);
        if (user == null){
            token = null;
            return null;
        }
        ContextHolder.Session = user.Session;
        token = _jwtManager.CreateToken(user.Session!);
        return user;
    }
    public User Register(User newUser) {
        var existingUser = ContextHolder.Session!.User;
        if (existingUser!=null&& newUser is not Seller){
            throw new ArgumentException("You are already logged in as a user. You can only register as a seller when you're logged in as a user.");
        }
        _cartManager.newCart(newUser);
        User ret;
        if (newUser is Seller s){
            ret  = _sellerRepository.Add(s);
        }
        else ret =  _userRepository.Add(newUser);
        return ret;
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
