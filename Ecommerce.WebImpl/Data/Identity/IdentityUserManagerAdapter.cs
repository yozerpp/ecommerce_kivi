using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.WebImpl.Data.Identity;

public class IdentityUserManagerAdapter : UserManager<User>, IUserManager
{
    private readonly IUserManager _userManager;
    public IdentityUserManagerAdapter(IUserStore<User> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<User> passwordHasher, IEnumerable<IUserValidator<User>> userValidators, IEnumerable<IPasswordValidator<User>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<User>> logger, IUserManager userManager) :
        base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
        _userManager = userManager;
    }

    public Customer? LoginCustomer(string Email, string password, bool rememberMe, out string? token) {
        return _userManager.LoginCustomer(Email, password, rememberMe, out token);
    }

    public Seller? LoginSeller(string Email, string password, bool rememberMe, out string? token) {
        return _userManager.LoginSeller(Email, password, rememberMe, out token);
    }

    public Staff? LoginStaff(string email, string password, bool rememberMe, out string? token) {
        return _userManager.LoginStaff(email, password, rememberMe, out token);
    }

    public T Register<T>(T newUser) where T : User{
        return _userManager.Register(newUser);
    }

    public void Update(User user) {
        _userManager.Update(user);
    }

    public AnonymousUser? FindAnonymousUser(string? email) {
        return _userManager.FindAnonymousUser(email);
    }

    public void ChangePassword(User user, string oldPassword, string newPassword) {
        _userManager.ChangePassword(user, oldPassword, newPassword);
    }

    public void ChangeEmail(User user, string oldPassword, string newEmail) {
        _userManager.ChangeEmail(user, oldPassword, newEmail);
    }

    public void Deactivate(Customer customer) {
        _userManager.Deactivate(customer);
    }

    public void CreateAnonymous(AnonymousUser anonymousUser) {
        _userManager.CreateAnonymous(anonymousUser);
    }
}