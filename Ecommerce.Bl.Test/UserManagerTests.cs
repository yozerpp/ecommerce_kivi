using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class UserManagerTests
{
    [Test]
    public void TestRegister() {
        TestContext._user = TestContext._userManager.Register(new User(){
            Email = new Faker().Internet.Email(), FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),
            BillingAddress = new Address{
                City = "İzmir", ZipCode = "35410", Street = "Atatürk Cad.", neighborhood = "Gazi", state = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }, PasswordHash = "pass"
        });
    }

    [Test]
    public void TestLogin() {
        Assert.Equals(TestContext._user, TestContext._userManager.Login(TestContext._user.Username, TestContext._user.PasswordHash, out SecurityToken token));
        TestContext._jwtmanager.UnwrapToken(token, out var user,out var session);
        Assert.Equals(TestContext._user.Id, user!.Id);
    }

    [Test]
    public void TestChangePassword() {
        var newPassword = "newpass";
        var unhashedOldPassword = TestContext._user.PasswordHash;
        TestContext._userManager.ChangePassword(unhashedOldPassword, newPassword);
        Assert.Equals(TestContext._userManager.Login(TestContext._user.Username, newPassword, out var token).Id, TestContext._user.Id);
    }

    [Test]
    public void TestDeactivate() {
        ContextHolder.Session = TestContext._session;
        TestContext._userManager.deactivate();
    }

}