using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class UserManagerTests
{
    private static Customer _customer;
    public static Customer Register() {
        return (Customer)TestContext._userManager.Register(new Customer{
            NormalizedEmail = new Faker().Internet.Email(), FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),Address = new Address{
                City = "Trabzon", ZipCode = "35450", Street = "SFSD", Neighborhood = "Other", State = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }, PasswordHash = "pass"
        });
    }

    public static Customer Login(User customer, out SecurityToken token) {
        return TestContext._userManager.LoginCustomer(customer.NormalizedEmail, customer.PasswordHash, out token);
    }
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _customer = Register();
    }
    [Test,Order(1)]
    public static void TestLogin() {
        Assert.That(_customer, Is.EqualTo(Login(_customer, out var token) ));
        TestContext._jwtmanager.UnwrapToken(token, out var user,out var session);
        Assert.That(_customer.Id,Is.EqualTo( user!.Id));
    }
    [Test, Order(2)]
    public void TestChangePassword() {
        var newPassword = "newpass";
        var unhashedOldPassword = _customer.PasswordHash;
        TestContext._userManager.ChangePassword(unhashedOldPassword, newPassword); //this requires flush for some reason??
        TestContext._userRepository.Flush();
        Assert.That(TestContext._userManager.LoginCustomer(_customer.NormalizedEmail, newPassword, out var token).Id,Is.EqualTo( _customer.Id));
    }

    [Test, Order(3)]
    public void TestDeactivate() {
        // ContextHolder.Session = TestContext._session;
        TestContext._userManager.deactivate();
        Assert.That(TestContext._userRepository.First(u => u.Id == _customer.Id).Active, Is.False);
    }
}