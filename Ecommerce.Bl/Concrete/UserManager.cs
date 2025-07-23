using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
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
    public UserManager(IJwtManager manager,IRepository<Customer> customerRepository, IRepository<Staff> staffRepository, IRepository<User> userRepository, IRepository<Seller> sellerRepository, HashFunction hashFunction, ICartManager cartManager) {
        this._jwtManager = manager;
        _userRepository = userRepository;
        _staffRepository = staffRepository;
        this._customerRepository = customerRepository;
        this._hashFunction = hashFunction;
        this._cartManager = cartManager;
        _sellerRepository = sellerRepository;
    }

    public Customer? LoginCustomer(string email, string password, out SecurityToken? token)
    {
        return (Customer?)doLogin(email, password, 0, out token);
    }
    public Seller? LoginSeller(string email, string password, out SecurityToken? token)
    {
        return (Seller?) doLogin(email, password, 1, out token); 
    }

    public Staff? LoginStaff(string email, string password, out SecurityToken? token) {
        return (Staff?) doLogin(email, password, 2, out token);
    }
    private User? doLogin(string email, string password, ushort type, out SecurityToken? token) {
        User? user;
        switch (type){
            case 0:
                user = _sellerRepository.First(u => u.NormalizedEmail == email && u.PasswordHash == _hashFunction(password),
                    includes:[[nameof(Customer.Session)]]);
                break;
            case 1: 
                user = _customerRepository.First(u => u.NormalizedEmail == email && u.PasswordHash == _hashFunction(password),
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
        token = _jwtManager.CreateToken(user.Session!);
        return user;
    }
    public User Register(User newUser) {
        _cartManager.newSession(newUser);
        User ret=null!;
        try{
            switch (newUser){
                case Customer customer:
                    _customerRepository.Add(customer);
                    break;
                case Seller seller:
                    _sellerRepository.Add(seller);
                    break;
                case Staff staff: 
                    _staffRepository.Add(staff);
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
        return ret;
    }
    public void ChangePassword(User user, string oldPassword, string newPassword) {
        user.PasswordHash = this._hashFunction(newPassword);
        _userRepository.Update(user);
        _userRepository.Flush();
    }
    public void deactivate(Customer user) {
        user.Active = false;
        _userRepository.Update(user);
        _userRepository.Flush();
    }

}
