using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.WebImpl.Data.Identity;

public class IdentityUserManagerAdapter : UserManager<Customer>, IUserManager
{
    private readonly IUserManager _userManager;
    public IdentityUserManagerAdapter(IUserStore<Customer> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<Customer> passwordHasher, IEnumerable<IUserValidator<Customer>> userValidators, IEnumerable<IPasswordValidator<Customer>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<Customer>> logger, IUserManager userManager) :
        base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
        _userManager = userManager;
    }

    public Customer? LoginCustomer(string Email, string password, out SecurityToken? token) {
        return _userManager.LoginCustomer(Email, password, out token);
    }

    public Seller? LoginSeller(string Email, string password, out SecurityToken? token) {
        return _userManager.LoginSeller(Email, password, out token);
    }

    public Staff? LoginStaff(string email, string password, out SecurityToken? token) {
        return _userManager.LoginStaff(email, password, out token);
    }

    public User Register(User newUser) {
        return _userManager.Register(newUser);
    }

    public void ChangePassword(User user, string oldPassword, string newPassword) {
        _userManager.ChangePassword(user, oldPassword, newPassword);
    }

    public void deactivate(Customer customer) {
        _userManager.deactivate(customer);
    }
}