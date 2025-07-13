using System.Linq.Expressions;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class OrderManagerTests
{
    [OneTimeSetUp]
    public void Register() {
        UserManagerTests.TestRegister();
    }

    [SetUp]
    public void Login() {
        TestContext._userManager.LoginUser(TestContext._user.Email, TestContext._user.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);
    }
    [Test, Order(1)]
    public void CreateOrder() {
        var payment = new Payment(){ TransactionId = "21345", Amount = 100,  PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var o = TestContext._orderManager.CreateOrder(payment);
        TestContext._orderRepository.Flush();
        TestContext._userRepository.Detach(TestContext._user);
        var userWithOrders = TestContext._userRepository.First(u=>u.Id == TestContext._user.Id, includes:[[nameof(User.Orders)]]);
        TestContext._orderRepository.Detach(o);
        var o1=TestContext._orderRepository.First(or => or.Id == o.Id, includes:[[nameof(Order.Items)]]);
        Assert.That(o1, Is.Not.Null);
        Assert.That(o.Id, Is.EqualTo(o1.Id));
        Assert.That(o1.Items.Count(), Is.EqualTo(1));
        Assert.That(o.Payment.Id, Is.EqualTo(o1.Payment.Id));
        Assert.That(o.User.Id, Is.EqualTo(o1.User.Id));
        Assert.That(userWithOrders.Orders, Contains.Item(o1));
        TestContext._user = userWithOrders;
    }

    [Test]
    public void CancelOrder()
    {
        // First, create an order to cancel
        var payment = new Payment() { TransactionId = "98765", Amount = 50, Id = 0, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var orderToCancel = TestContext._orderManager.CreateOrder(payment);
        TestContext._orderRepository.Flush();
        var cancelledOrder = TestContext._orderManager.CancelOrder(orderToCancel);
        Assert.That(cancelledOrder, Is.Not.Null);
        Assert.That(cancelledOrder.Status, Is.EqualTo(OrderStatus.CANCELLED));
        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == cancelledOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.Status, Is.EqualTo(OrderStatus.CANCELLED));
    }

    [Test]
    public void CompleteOrder()
    {
        // First, create an order to complete
        var payment = new Payment() { TransactionId = "11223", Amount = 75, Id = 0, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var orderToComplete = TestContext._orderManager.CreateOrder(payment);
        TestContext._orderRepository.Flush();
        // Now, complete the order
        var completedOrder = TestContext._orderManager.complete(orderToComplete);

        // Assertions
        Assert.That(completedOrder, Is.Not.Null);
        Assert.That(completedOrder.Status, Is.EqualTo(OrderStatus.DELIVERED));

        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == completedOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.Status, Is.EqualTo(OrderStatus.DELIVERED));
    }

    [Test, Order(2)]
    public void UpdateOrder()
    {
        // First, create an order to update
        var payment = new Payment() { TransactionId = "44556", Amount = 120, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var orderToUpdate = TestContext._orderManager.CreateOrder(payment);
        // Change a property of the order (e.g., ShippingAddress)
        var originalAddress = orderToUpdate.ShippingAddress;
        orderToUpdate.ShippingAddress = new Address
        {
            City = "NewCity",
            ZipCode = "99999",
            Street = "NewStreet",
            Neighborhood = "NewNeighborhood",
            State = "NewState"
        };
        TestContext._orderRepository.Flush();
        var updatedOrder = TestContext._orderManager.UpdateOrder(orderToUpdate);

        // Assertions
        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.Id, Is.EqualTo(orderToUpdate.Id));
        Assert.That(updatedOrder.ShippingAddress.City, Is.EqualTo("NewCity"));
        Assert.That(updatedOrder.ShippingAddress.ZipCode, Is.EqualTo("99999"));

        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == updatedOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.ShippingAddress.City, Is.EqualTo("NewCity"));
        Assert.That(fetchedOrder.ShippingAddress.ZipCode, Is.EqualTo("99999"));
    }
}
