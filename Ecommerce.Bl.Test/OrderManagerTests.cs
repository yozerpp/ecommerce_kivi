using System.Linq.Expressions;
using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Test;

public class OrderManagerTests
{
    private static Customer _customer;
    private static ProductOffer _offer1, _offer2;
    private static Session _session;

    [OneTimeSetUp]
    public void SetupUsersAndProducts() {
        var e = new Faker().Internet.Email(); 
        // Register and login a customer to get a session
        _customer = (Customer)TestContext._userManager.Register(new Customer
        {
            NormalizedEmail = e.ToUpper(),
            Email = e,
            PasswordHash = "customerpass",
            FirstName = "Test",
            LastName = "Customer",
            Address = new Address()
                { City = "Trabzon", Country = "Trabzon", District = "Ortahisar", Line1 = "Main St", ZipCode = "61000" },
            PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551112233" }
        });
        _customer = UserManagerTests.Login(_customer, out _);
        _session = _customer.Session;
        // Register and login a seller to list product offers
        e = new Faker().Internet.Email();
        var _testSeller = new Seller
        {
            NormalizedEmail = e.ToUpper(),
            Email = e,
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop",
            Address = new Address() { City = "ads", Country = "state", District = "basd", Line1 = "casd", ZipCode = "asd" },
            PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551234567" }
        };
        _testSeller = (Seller)TestContext._userManager.Register(_testSeller);
        // Login as the seller to list the product offer
        var sellerSession1 = TestContext._userManager.LoginSeller(_testSeller.NormalizedEmail, _testSeller.PasswordHash, out SecurityToken sellerToken1);

        // Create a product and offer for testing reviews
        var category = TestContext._categoryRepository.First(_ => true);
        var product1 = new Product { Name = "Review Test Product 1", Description = "Description 1", CategoryId = category.Id };
        _offer1 = new ProductOffer
        {
            Product = product1,
            Price = 100,
            Stock = 10,
            SellerId = _testSeller.Id // Use the registered seller's ID
        };
        _offer1 = TestContext._sellerManager.ListOffer(_testSeller, _offer1);

        // Simulate a purchase by _reviewerUser for _testOffer
        // First, login as the reviewer user
        e= new Faker().Internet.Email();
        var newSeller = new Seller
        {
            NormalizedEmail = e.ToUpper(),
            Email = e,
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop2",
            Address = new Address() { City = "ads", Country = "state", District = "basd", Line1 = "casd", ZipCode = "asd" },
            PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551234567" }
        };
        TestContext._userManager.Register(newSeller);
        var sellerSession2 = TestContext._userManager.LoginSeller(newSeller.NormalizedEmail, newSeller.PasswordHash, out SecurityToken sellerToken2);

        var product2 = TestContext._productRepository.Detach(product1); // Detach to avoid tracking issues if product1 is still tracked
        product2.Offers.Clear(); // Clear offers if any were attached from previous operations
        _offer2 = TestContext._sellerManager.ListOffer(newSeller, new ProductOffer
        {
            Product = product2,
            Price = 150, // Different price for offer2
            Stock = 5,
            SellerId = newSeller.Id // Use the registered seller's ID
        });

        // Ensure the customer's cart is empty before adding items for order creation tests
        var currentCartItems = TestContext._cartItemRepository.Where(ci => ci.CartId == _session.Cart.Id);
        foreach (var item in currentCartItems)
        {
            TestContext._cartItemRepository.Delete(item);
        }
        TestContext._cartRepository.Flush();
    }

    [SetUp]
    public void Login()
    {
        _customer= UserManagerTests.Login(_customer, out _);
        _session = _customer.Session;
    }

    [Test, Order(1)]
    public void CreateAnonymously()
    {
        var payment = new Payment() { TransactionId = "21345", Amount = 100, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(_session.Cart, offer);
        //anonymous order.
        var o = TestContext._orderManager.CreateOrder(_session,out var newSession, email:new Faker().Internet.Email(), shippingAddress:new Address(){City = "a", District = "a", Country = "a", Line1 = "a", ZipCode = "a"});
        TestContext._orderRepository.Detach(o);
        _session = newSession;
        var o1 = TestContext._orderManager.GetAnonymousOrder(email:o.Email, id:o.Id);
        Assert.That(o1, Is.Not.Null);
        Assert.That(o.Id, Is.EqualTo(o1.Id));
        Assert.That(o1.Items.Count(), Is.EqualTo(1));
    }

    [Test, Order(2)]
    public void CreateLogged() {
        var payment = new Payment() { TransactionId = "21345", Amount = 100, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(_session.Cart, offer);
        //anonymous order.
        var o = TestContext._orderManager.CreateOrder(_customer.Session,out _, _customer);
        TestContext._userRepository.Detach(_customer);
        TestContext._orderRepository.Detach(o);
        var userWithOrders = TestContext._userRepository.First(u => u.Id == _customer.Id, includes: [[nameof(Customer.Orders)]]);
        var o1 = TestContext._orderRepository.First(or => or.Id == o.Id, includes: [[nameof(Order.Items)]]);
        Assert.That(o1, Is.Not.Null);
        Assert.That(o.Id, Is.EqualTo(o1.Id));
        Assert.That(o1.Items.Count(), Is.EqualTo(1));
        Assert.That(o.User.Id, Is.EqualTo(o1.User.Id));
        Assert.That(userWithOrders.Orders, Contains.Item(o1));
        _customer = userWithOrders;
    }
    [Test, Order(3)]
    public void CancelOrder()
    {
        // First, create an order to cancel
        var payment = new Payment() { TransactionId = "98765", Amount = 50, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(_session.Cart, offer);
        var orderToCancel = TestContext._orderManager.CreateOrder(_session,out _, _customer);
        var cancelledOrder = TestContext._orderManager.CancelOrder(orderToCancel);
        Assert.That(cancelledOrder, Is.Not.Null);
        Assert.That(cancelledOrder.Status, Is.EqualTo(OrderStatus.Cancelled));
        TestContext._orderRepository.Detach(cancelledOrder);
        _customer.Orders.Remove(cancelledOrder); 
        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == cancelledOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.Status, Is.EqualTo(OrderStatus.Cancelled));
    }

    [Test, Order(4)]
    public void CompleteOrder()
    {
        // First, create an order to complete
        var payment = new Payment() { TransactionId = "11223", Amount = 75, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(_session.Cart, offer);
        var orderToComplete = TestContext._orderManager.CreateOrder(_session,out _, _customer);
        TestContext._orderRepository.Flush();
        // Now, complete the order
        TestContext._orderManager.Complete(orderToComplete);
        TestContext._orderRepository.Detach(orderToComplete);
        _customer.Orders.Remove(orderToComplete);
        var completedOrder = TestContext._orderRepository.First(o => o.Id == orderToComplete.Id);
        // Assertions
        Assert.That(completedOrder, Is.Not.Null);
        Assert.That(completedOrder.Status, Is.EqualTo(OrderStatus.Delivered));

        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == completedOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.Status, Is.EqualTo(OrderStatus.Delivered));
    }

    [Test, Order(5)]
    public void UpdateOrder()
    {
        // First, create an order to update
        var payment = new Payment() { TransactionId = "44556", Amount = 120, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(_session.Cart, offer);
        var orderToUpdate = TestContext._orderManager.CreateOrder(_session,out _, _customer);
        // Change a property of the order (e.g., ShippingAddress)
        var originalAddress = orderToUpdate.ShippingAddress;
        orderToUpdate.ShippingAddress = new Address
        {
            City = "NewCity",
            ZipCode = "99999",
            Line1 = "NewStreet",
            District = "NewNeighborhood",
            Country = "NewState"
        };
        TestContext._orderManager.UpdateOrder(orderToUpdate); // Call the manager method
        TestContext._orderRepository.Flush();
        TestContext._orderRepository.Detach(orderToUpdate);
        _customer.Orders.Remove(orderToUpdate);
        var updatedOrder = TestContext._orderRepository.First(o => o.Id == orderToUpdate.Id);
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

    [Test, Order(6)]
    public void TestGetOrderWithItemsAggregates()
    {
        // Clear cart first to ensure a clean state for creating a new order
        var currentCartItems = TestContext._cartItemRepository.Where(ci => ci.CartId == _session.Cart.Id);
        foreach (var item in currentCartItems)
        {
            TestContext._cartItemRepository.Delete(item);
        }
        TestContext._cartRepository.Flush();

        // Add multiple items to the cart for the new order
        uint quantity1 = 2;
        uint quantity2 = 3;

        TestContext._cartManager.Add(_session.Cart, _offer1!, quantity1);
        TestContext._cartManager.Add(_session.Cart, _offer2, quantity2);
        TestContext._cartRepository.Flush();

        // Create the order
        var payment = new Payment() { TransactionId = "ORDER_AGG_TEST", Amount = 100, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var createdOrder = TestContext._orderManager.CreateOrder(_session,out _, _customer);

        // Get the order with aggregates
        var orderWithAggregates = TestContext._orderManager.GetOrderWithItems(createdOrder.Id);

        Assert.That(orderWithAggregates, Is.Not.Null);

        // Calculate expected values
        decimal expectedBasePrice = (_offer1!.Price * quantity1) + (_offer2!.Price * quantity2);
        decimal expectedDiscountedPrice = (_offer1.Price * quantity1 * (decimal)_offer1.Discount) +
                                          (_offer2.Price * quantity2 * (decimal)_offer2.Discount);
        decimal expectedCouponDiscountedPrice = expectedDiscountedPrice; // Assuming no coupons for simplicity
        decimal expectedDiscountAmount = expectedBasePrice - expectedDiscountedPrice;
        decimal expectedCouponDiscountAmount = expectedDiscountedPrice - expectedCouponDiscountedPrice; // Should be 0 if no coupons

        // Assertions
        Assert.That(orderWithAggregates.ItemCount, Is.EqualTo(2)); // Two distinct items
        Assert.That(orderWithAggregates.BasePrice, Is.EqualTo(expectedBasePrice));
        Assert.That(orderWithAggregates.DiscountedPrice, Is.EqualTo(expectedDiscountedPrice));
        Assert.That(orderWithAggregates.CouponDiscountedPrice, Is.EqualTo(expectedCouponDiscountedPrice));
        Assert.That(orderWithAggregates.DiscountAmount, Is.EqualTo(expectedDiscountAmount));
        Assert.That(orderWithAggregates.CouponDiscountAmount, Is.EqualTo(expectedCouponDiscountAmount));
        Assert.That(orderWithAggregates.Items.Count(), Is.EqualTo(2));
    }
}
