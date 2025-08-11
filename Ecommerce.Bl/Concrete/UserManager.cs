using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;
public class UserManager :IUserManager
{ 
    public delegate string HashFunction(string input);
    
    private readonly IJwtManager _jwtManager;
    private readonly HashFunction _hashFunction;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ICartManager _cartManager;
    private readonly IRepository<Staff> _staffRepository;
    private readonly IRepository<AnonymousUser> _anonymousUserRepository;
    public UserManager(IJwtManager manager,IRepository<Customer> customerRepository, IRepository<AnonymousUser> anonymousUserRepository, IRepository<Staff> staffRepository, IRepository<User> userRepository, IRepository<Seller> sellerRepository, HashFunction hashFunction, ICartManager cartManager) {
        this._jwtManager = manager;
        _anonymousUserRepository = anonymousUserRepository;
        _userRepository = userRepository;
        _staffRepository = staffRepository;
        this._customerRepository = customerRepository;
        this._hashFunction = hashFunction;
        this._cartManager = cartManager;
        _sellerRepository = sellerRepository;
    }

    public Customer? LoginCustomer(string email, string password, bool rememberMe, out string? token)
    {
        return (Customer?)doLogin(email, password, 0 ,rememberMe, out token);
    }
    public Seller? LoginSeller(string email, string password, bool rememberMe, out string? token)
    {
        return (Seller?) doLogin(email, password, 1, rememberMe, out token); 
    }

    public Staff? LoginStaff(string email, string password, bool rememberMe, out string? token) {
        return (Staff?) doLogin(email, password, 2, rememberMe, out token);
    }
    private User? doLogin(string email, string password, ushort type, bool rememberMe, out string? token) {
        User? user;
        switch (type){
            case 0:
                user = _customerRepository.First(u => u.NormalizedEmail == email && u.PasswordHash == _hashFunction(password),
                    includes:[[nameof(Customer.Session)]]);
                break;
            case 1: 
                user = _sellerRepository.First(u => u.NormalizedEmail == email && u.PasswordHash == _hashFunction(password),
                    includes:[[nameof(Customer.Session)]]);
                break;
            case 2:
                user = _staffRepository.First(s=>s.NormalizedEmail == email && s.PasswordHash == _hashFunction(password),
                    includes:[[nameof(Staff.Session)]]);
                break;
            default: user = null;
                break;
        }
        if (user == null){
            token = null;
            return null;
        }
        token = _jwtManager.Serialize(_jwtManager.CreateToken(user.Session!, rememberMe));
        return user;
    }
    public T Register<T>(T newUser) where T :User
    {
        _cartManager.newSession(newUser);
        User ret;
        try{
            switch (newUser){
                case Customer customer:
                    ret = _customerRepository.Add(customer);
                    break;
                case Seller seller:
                    ret = _sellerRepository.Add(seller);
                    break;
                case Staff staff: 
                    ret = _staffRepository.Add(staff);
                    break;
                default: throw new ArgumentException("User cannot base class " + newUser.GetType().Name);
            }
            _customerRepository.Flush();
        } catch (Exception e){
            if(!typeof(DbUpdateException).IsInstanceOfType(e) && !typeof(InvalidOperationException).IsInstanceOfType(e))
                throw;
            if(e.Message.Contains("already",StringComparison.InvariantCultureIgnoreCase) || e.Message.Contains("conflict",StringComparison.InvariantCultureIgnoreCase) || e.Message.Contains("same",StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("User with this email already exists.");
            throw;
        }
        return (T)ret;
    }

    public void Update(User user) {
        _userRepository.Update(user);
        _userRepository.Flush();
    }

    public AnonymousUser? FindAnonymousUser(string? email) {
        return _anonymousUserRepository.First(a => a.Email == email);
    }

    public void ChangePassword(User user, string oldPassword, string newPassword) {
        var id = user.Id;
        newPassword = _hashFunction(newPassword);
        oldPassword = _hashFunction(oldPassword);
        var res = _userRepository.UpdateExpr([(u=>u.PasswordHash, newPassword)], u=>u.Id == id && u.PasswordHash == oldPassword)>0;
        if(!res)
            throw new ArgumentException("Girdiğiniz şifre yanlış");
    }
    public void ChangeEmail(User user, string oldPassword, string newEmail) {
        var id = user.Id;
        if(newEmail.Count(c=>c=='@')!=1 || newEmail.Count(c=>c=='.')<1)
            throw new ArgumentException("Geçersiz E-Posta Adresi");
        oldPassword = _hashFunction(oldPassword);
        var res = _userRepository.UpdateExpr([(u => u.Email, newEmail)],
            u => u.Id == id && u.PasswordHash == oldPassword)>0;
        if (!res)
            throw new ArgumentException("Girdiğiniz şifre yanlış");
    }
    public void Deactivate(Customer user) {
        user.Active = false;
        _userRepository.Update(user);
        _userRepository.Flush();
    }

    public void CreateAnonymous(AnonymousUser anonymousUser) {
        _anonymousUserRepository.Save(anonymousUser);
        _anonymousUserRepository.Flush();
    }
}
