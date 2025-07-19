using System.Linq.Expressions;
using Bogus;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Test;

public class OrderManagerTests
{
    private static User _user;
    private static ProductOffer _offer1, _offer2;
    [OneTimeSetUp]
    public void SetupUsersAndProducts()
    {

        var _testSeller = new Seller
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop",
            ShopEmail = new Faker().Internet.Email(),
            ShopPhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"},
            ShopAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        ContextHolder.Session = null;
        _testSeller = (Seller)TestContext._userManager.Register(_testSeller);
        // Login as the seller to list the product offer
        TestContext._userManager.LoginSeller(_testSeller.Email, _testSeller.PasswordHash, out SecurityToken sellerToken);
        // Create a product and offer for testing reviews
        var category = TestContext._categoryRepository.First(_ => true);
        var product = new Product { Name = "Review Test Product", Description = "Description", CategoryId = category.Id };
        _offer1 = new ProductOffer
        {
            Product = product,
            Price = 100,
            Stock = 10,
            SellerId = _testSeller.Id // Use the registered seller's ID
        };
        _offer1 = TestContext._sellerManager.ListProduct(_offer1);
        // Simulate a purchase by _reviewerUser for _testOffer
        // First, login as the reviewer user
        ContextHolder.Session = null;
        var newSeller = new Seller
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop",
            ShopEmail = new Faker().Internet.Email(),
            ShopPhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"},
            ShopAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };;
        TestContext._userManager.Register(newSeller);
        TestContext._userManager.LoginSeller(newSeller.Email,newSeller.PasswordHash, out _);
        product = TestContext._productRepository.Detach(product);
        product.Offers.Clear();
        _offer2 = TestContext._sellerManager.ListProduct(new ProductOffer{
            Product = product,
            Price = 100,
            Stock = 10,
            SellerId = newSeller.Id // Use the registered seller's ID
        });
        TestContext._cartManager.Add(_offer1, 1);
        var payment = new Payment { TransactionId = "REVIEW_PURCHASE_" + Guid.NewGuid().ToString(), Amount = _offer1.Price, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder();
        ContextHolder.Session = null;
        _user = TestContext._userManager.Register(new User{
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShippingAddress = new Address()
                { City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd" },
            PhoneNumber = new PhoneNumber(){ CountryCode = 90, Number = "5551234567" }
        });
    }
    [SetUp]
    public void Login() {
        UserManagerTests.Login(_user,out _);
    }
    [Test]
    public void CreateOrder() {
        var payment = new Payment(){ TransactionId = "21345", Amount = 100,  PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var o = TestContext._orderManager.CreateOrder();
        TestContext._orderRepository.Flush();
        TestContext._userRepository.Detach(_user);
        var userWithOrders = TestContext._userRepository.First(u=>u.Id == _user.Id, includes:[[nameof(User.Orders)]]);
        TestContext._orderRepository.Detach(o);
        var o1=TestContext._orderRepository.First(or => or.Id == o.Id, includes:[[nameof(Order.Items)]]);
        Assert.That(o1, Is.Not.Null);
        Assert.That(o.Id, Is.EqualTo(o1.Id));
        Assert.That(o1.Items.Count(), Is.EqualTo(1));
        Assert.That(o.User.Id, Is.EqualTo(o1.User.Id));
        Assert.That(userWithOrders.Orders, Contains.Item(o1));
        _user = userWithOrders;
    }

    [Test]
    public void CancelOrder()
    {
        // First, create an order to cancel
        var payment = new Payment() { TransactionId = "98765", Amount = 50, Id = 0, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var orderToCancel = TestContext._orderManager.CreateOrder();
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
        var orderToComplete = TestContext._orderManager.CreateOrder();
        TestContext._orderRepository.Flush();
        // Now, complete the order
        TestContext._orderManager.complete(orderToComplete);
        TestContext._orderRepository.Detach(orderToComplete);
        var completedOrder = TestContext._orderRepository.First(o=>o.Id ==orderToComplete.Id);
        // Assertions
        Assert.That(completedOrder, Is.Not.Null);
        Assert.That(completedOrder.Status, Is.EqualTo(OrderStatus.DELIVERED));

        var fetchedOrder = TestContext._orderRepository.First(o => o.Id == completedOrder.Id);
        Assert.That(fetchedOrder, Is.Not.Null);
        Assert.That(fetchedOrder.Status, Is.EqualTo(OrderStatus.DELIVERED));
    }

    [Test]
    public void UpdateOrder()
    {
        // First, create an order to update
        var payment = new Payment() { TransactionId = "44556", Amount = 120, PaymentMethod = PaymentMethod.CASH };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.First(f => true);
        TestContext._cartManager.Add(offer);
        var orderToUpdate = TestContext._orderManager.CreateOrder();
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
        TestContext._orderRepository.Detach(orderToUpdate);
        var updatedOrder = TestContext._orderRepository.First(o=>o.Id ==orderToUpdate.Id);
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

    [Test]
    public void TestGetOrderWithItemsAggregates()
    {
        // Clear cart first to ensure a clean state for creating a new order
        var currentCartItems = TestContext._cartItemRepository.Where(ci => ci.CartId == ContextHolder.Session.Cart.Id);
        foreach (var item in currentCartItems)
        {
            TestContext._cartItemRepository.Delete(item);
        }
        TestContext._cartRepository.Flush();

        // Add multiple items to the cart for the new order


        uint quantity1 = 2;
        uint quantity2 = 3;

        TestContext._cartManager.Add(_offer1!, quantity1);
        TestContext._cartManager.Add(_offer2, quantity2);
        TestContext._cartRepository.Flush();

        // Create the order
        var payment = new Payment() { TransactionId = "ORDER_AGG_TEST", Amount = 100, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var createdOrder = TestContext._orderManager.CreateOrder();

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
