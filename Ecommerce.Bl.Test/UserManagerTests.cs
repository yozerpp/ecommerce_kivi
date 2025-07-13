using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class UserManagerTests
{
    [Test, Order(1)]
    public static void TestRegister() {
        TestContext._user = TestContext._userManager.Register(new User{
            Email = new Faker().Internet.Email(), FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),
            BillingAddress = new Address{
                City = "İzmir", ZipCode = "35410", Street = "Atatürk Cad.", Neighborhood = "Gazi", State = "Gaziemir"
            },ShippingAddress = new Address{
                City = "Trabzon", ZipCode = "35450", Street = "SFSD", Neighborhood = "Other", State = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }, PasswordHash = "pass"
        });
        TestContext._userRepository.Flush();
    }

    [Test,Order(2)]
    public static void TestLogin() {
        Assert.That(TestContext._user, Is.EqualTo( TestContext._userManager.LoginUser(TestContext._user.Email, TestContext._user.PasswordHash, out SecurityToken token)));
        TestContext._jwtmanager.UnwrapToken(token, out var user,out var session);
        Assert.That(TestContext._user.Id,Is.EqualTo( user!.Id));
    }

    [Test]
    public void TestChangePassword() {
        var newPassword = "newpass";
        var unhashedOldPassword = TestContext._user.PasswordHash;
        TestContext._userManager.ChangePassword(unhashedOldPassword, newPassword); //this requires flush for some reason??
        TestContext._userRepository.Flush();
        Assert.That(TestContext._userManager.LoginUser(TestContext._user.Email, newPassword, out var token).Id,Is.EqualTo( TestContext._user.Id));
    }

    [Test]
    public void TestDeactivate() {
        // ContextHolder.Session = TestContext._session;
        TestContext._userManager.deactivate();
        Assert.That(TestContext._userRepository.First(u => u.Id == TestContext._user.Id).Active, Is.False);
    }
}