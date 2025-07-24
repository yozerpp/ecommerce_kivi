using Ecommerce.Entity;
using NUnit.Framework.Legacy;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Test;

public class CartManagerTests
{
    private static Customer _customer;
    private static Session _session;
    [OneTimeSetUp]
    public void Register() {
        _customer = UserManagerTests.Register();
    }

    [SetUp]
    public void Login() {
        _customer = UserManagerTests.Login(_customer,out _);
        _session = _customer.Session;
    }
    [Test]
    public void newCartUserful() {
        var oldSession = _session;
        _session = TestContext._cartManager.newSession(_customer, true);
        Assert.That(_session, Is.Not.EqualTo(oldSession));
    }
    [Test]
    public void TestAdd() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var amount = (uint) Random.Shared.Next(1,3);
        var item = TestContext._cartManager.Add(_session.Cart, offer, amount);
        TestContext._cartRepository.Flush();
        var items = TestContext._cartRepository.First(c=>c.Id==_session.Cart.Id,includes:[[nameof(Cart.Items)]]).Items;        
        Assert.That(items, Contains.Item(item));
        Assert.That(item.CartId, Is.EqualTo(_session.Cart.Id));
        Assert.That(item.ProductId, Is.EqualTo(offer.ProductId));
        Assert.That(item.SellerId, Is.EqualTo(offer.SellerId));
    }
    [Test]
    public void TestIncrement() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var old = TestContext._cartItemRepository.First(i=>i.CartId == _session.Cart.Id&&
                                                       i.ProductId==offer.ProductId && i.SellerId == offer.SellerId);
        uint oldAmount;
        if (old != null){
            old = TestContext._cartItemRepository.Detach(old);
            oldAmount = old.Quantity;
        }
        else oldAmount = 0;
        var i = TestContext._cartManager.Add(_session.Cart, offer);
        TestContext._cartRepository.Flush();
        Assert.That(i.Quantity, Is.EqualTo(oldAmount +1));
    }
    [Test]
    public void Decrement() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var i = TestContext._cartManager.Add(_session.Cart, offer);
        TestContext._cartItemRepository.Flush();
        var oldAmount = i.Quantity;
        var decremented = Random.Shared.Next((int)i.Quantity - 1);
        TestContext._cartManager.Decrement(_session.Cart, offer, (uint)decremented);
        TestContext._cartRepository.Flush();
        var refetched = TestContext._cartItemRepository.First(i=>i.CartId == _session.Cart.Id && i.ProductId == offer.ProductId && i.SellerId==offer.SellerId);
        Assert.That(refetched.Quantity, Is.EqualTo(oldAmount - decremented));
    }

    [Test]
    public void TestGetWithAggregates()
    {
        // Clear cart first to ensure a clean state for testing aggregates
        _session = TestContext._cartManager.newSession(_customer);
        var currentCartItems = TestContext._cartItemRepository.Where(ci => ci.CartId == _session.Cart.Id);
        foreach (var item in currentCartItems)
        {
            TestContext._cartItemRepository.Delete(item);
        }
        TestContext._cartRepository.Flush();

        // Add multiple items to the cart
        var offer1 = TestContext._offerRepository.First(_ => true);
        var offer2 = TestContext._offerRepository.Where(_ => true).Skip(1).FirstOrDefault();

        if (offer1 == null || offer2 == null || offer2.Equals(offer1))
        {
            Assert.Inconclusive("Not enough distinct offers available for testing aggregates.");
        }

        uint quantity1 = 2;
        uint quantity2 = 3;

        TestContext._cartManager.Add(_session.Cart, offer1!, quantity1);
        TestContext._cartManager.Add(_session.Cart, offer2!, quantity2);
        TestContext._cartRepository.Flush();

        // Get cart with aggregates
        var cartWithAggregates = TestContext._cartManager.Get(_session, true, true) as CartWithAggregates;

        Assert.That(cartWithAggregates, Is.Not.Null);

        // Calculate expected values
        decimal expectedTotalPrice = (offer1!.Price * quantity1) + (offer2!.Price * quantity2);
        decimal expectedDiscountedPrice = (offer1.Price * quantity1 * (decimal)offer1.Discount) +
                                          (offer2.Price * quantity2 * (decimal)offer2.Discount);
        decimal expectedCouponDiscountedPrice = expectedDiscountedPrice; // Assuming no coupons for simplicity in this test
        decimal expectedDiscountAmount = expectedTotalPrice - expectedDiscountedPrice;
        decimal expectedCouponDiscountAmount = expectedDiscountedPrice - expectedCouponDiscountedPrice; // Should be 0 if no coupons

        // Assertions
        Assert.That(cartWithAggregates.ItemCount, Is.EqualTo(quantity1 + quantity2));
        Assert.That(cartWithAggregates.TotalPrice, Is.EqualTo(expectedTotalPrice));
        Assert.That(cartWithAggregates.DiscountedPrice, Is.EqualTo(expectedDiscountedPrice));
        Assert.That(cartWithAggregates.TotalDiscountedPrice, Is.EqualTo(expectedCouponDiscountedPrice));
        Assert.That(cartWithAggregates.DiscountAmount, Is.EqualTo(expectedDiscountAmount));
        Assert.That(cartWithAggregates.CouponDiscountAmount, Is.EqualTo(expectedCouponDiscountAmount));
        Assert.That(cartWithAggregates.Items.Sum(i=>i.Quantity), Is.EqualTo(5)); // Should have 2 distinct items
    }
}
