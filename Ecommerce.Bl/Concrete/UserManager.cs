using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;
public class UserManager : IUserManager
{ 
    public delegate string HashFunction(string input);
    
    private readonly JwtManager _jwtManager;
    private readonly HashFunction _hashFunction;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly CartManager _cartManager;
    public UserManager(JwtManager manager,IRepository<User> userRepository, IRepository<Seller> sellerRepository, HashFunction hashFunction, CartManager cartManager) {
        this._jwtManager = manager;
        this._userRepository = userRepository;
        this._hashFunction = hashFunction;
        this._cartManager = cartManager;
        _sellerRepository = sellerRepository;
    }

    public User GetUser() {
        return ContextHolder.GetUserOrThrow();
    }

    public UserWithAggregates GetWithAggregates(uint? id=null) {
        id ??= ContextHolder.GetUserOrThrow().Id;
        return _userRepository.First(UserAggregateProjection, u => u.Id == id, includes:[[nameof(User.Session)]]);
    }
    private static readonly Expression<Func<User, UserWithAggregates>> UserAggregateProjection = 
        u => new UserWithAggregates {
            Id = u.Id, 
            Email = u.Email, 
            FirstName = u.FirstName, 
            LastName = u.LastName,
            // TotalSpent = u.Orders.SelectMany(o=>o.Items ).Sum(i=>
                // (i.Quantity * (decimal?)i.ProductOffer.Discount * (decimal?)i.ProductOffer.Price *(decimal?) (i.Coupon != null ? (decimal?)i.Coupon.DiscountRate :(decimal?) 1m ) ))??0m,
            TotalOrders = ((int?)u.Orders.Count()) ?? 0,
            // TotalDiscountUsed = u.Orders.SelectMany(o=>o.Items).Sum(i=>
                // (decimal?)((decimal?)i.Quantity * (1m-(decimal?)i.ProductOffer.Discount) *(decimal?) i.ProductOffer.Price *(decimal?) (i.Coupon != null ? (1m-(decimal?)i.Coupon.DiscountRate) : (decimal?)0m)))??0m,
            TotalReviews = ((int?)u.Reviews.Count())??0,
            TotalReplies = ((int?)u.ReviewComments.Count())??0,
            TotalKarma = ((int?) u.Reviews.SelectMany(r=>r.Votes).Sum(v=>(int?)((v.Up) ? 1 : -1)) )??0,
            ShippingAddress = u.ShippingAddress,
            PhoneNumber = u.PhoneNumber,
            Active = u.Active, 
            Session= u.Session,
            SessionId = u.SessionId,
            Orders = u.Orders,
            Reviews = u.Reviews,
            ReviewComments = u.ReviewComments,
            
        };
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
        var existingUser = ContextHolder.Session?.User;
        if (existingUser!=null&& newUser is not Seller){
            throw new ArgumentException("You are already logged in as a user. You can only register as a seller when you're logged in as a user.");
        }
        _cartManager.newCart(newUser);
        User ret;
        try{
            if (newUser is Seller s){
                ret  = _sellerRepository.Add(s);
            }
            else ret =  _userRepository.Add(newUser);
            _userRepository.Flush();
        } catch (Exception e){
            if(!typeof(DbUpdateException).IsInstanceOfType(e) && !typeof(InvalidOperationException).IsInstanceOfType(e))
                throw;
            if(e.Message.Contains("already",StringComparison.InvariantCultureIgnoreCase) || e.Message.Contains("conflict",StringComparison.InvariantCultureIgnoreCase) || e.Message.Contains("same",StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("User with this email already exists.");
            throw;
        }
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
        _userRepository.Flush();
    }
    /// <summary>
    /// Ui layer should also clear the persisted login information. (Such as cookies)
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Logout() {
        throw new NotImplementedException("Logout should be handled by the controller/ui layer.");
    }
    public User Update(User user)
    {
       var ret =  _userRepository.Update(user);
       _userRepository.Flush();
       return ret;
    }
    public void deactivate() {
        User? user;
        if ((user =ContextHolder.Session?.User)==null){
            throw new UnauthorizedAccessException("You aren't logged in.");
        }
        user.Active = false;
        _userRepository.Update(user);
        _userRepository.Flush();
    }

}
