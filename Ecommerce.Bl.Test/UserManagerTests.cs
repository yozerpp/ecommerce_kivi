using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class UserManagerTests
{
    private static User _user;
    public static User Register() {
        return TestContext._userManager.Register(new User{
            Email = new Faker().Internet.Email(), FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),ShippingAddress = new Address{
                City = "Trabzon", ZipCode = "35450", Street = "SFSD", Neighborhood = "Other", State = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }, PasswordHash = "pass"
        });
    }

    public static User Login(User user, out SecurityToken token) {
        return TestContext._userManager.LoginUser(user.Email, user.PasswordHash, out token);
    }
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _user = Register();
    }
    [Test,Order(1)]
    public static void TestLogin() {
        Assert.That(_user, Is.EqualTo(Login(_user, out var token) ));
        TestContext._jwtmanager.UnwrapToken(token, out var user,out var session);
        Assert.That(_user.Id,Is.EqualTo( user!.Id));
    }
    [Test, Order(2)]
    public void TestChangePassword() {
        var newPassword = "newpass";
        var unhashedOldPassword = _user.PasswordHash;
        TestContext._userManager.ChangePassword(unhashedOldPassword, newPassword); //this requires flush for some reason??
        TestContext._userRepository.Flush();
        Assert.That(TestContext._userManager.LoginUser(_user.Email, newPassword, out var token).Id,Is.EqualTo( _user.Id));
    }

    [Test, Order(3)]
    public void TestDeactivate() {
        // ContextHolder.Session = TestContext._session;
        TestContext._userManager.deactivate();
        Assert.That(TestContext._userRepository.First(u => u.Id == _user.Id).Active, Is.False);
    }
}