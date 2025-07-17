using Ecommerce.Entity;
using NUnit.Framework.Legacy;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Test;

public class CartManagerTests
{
    private static User _user;
    [OneTimeSetUp]
    public void Register() {
        _user = UserManagerTests.Register();
    }

    [SetUp]
    public void Login() {
        UserManagerTests.Login(_user,out _);
    }
    [Test]
    public void newCartUserful() {
        var oldSession = ContextHolder.Session;
        TestContext._cartManager.newCart();
        TestContext._cartRepository.Flush();
        Assert.That(ContextHolder.Session, Is.EqualTo(oldSession));
    }
    [Test]
    public void TestAdd() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var amount = (uint) Random.Shared.Next(1,3);
        var item = TestContext._cartManager.Add(offer, amount);
        TestContext._cartRepository.Flush();
        var items = TestContext._cartRepository.First(c=>c.Id==ContextHolder.Session.Cart.Id,includes:[[nameof(Cart.Items)]]).Items;        
        Assert.That(items, Contains.Item(item));
        Assert.That(item.CartId, Is.EqualTo(ContextHolder.Session.Cart.Id));
        Assert.That(item.ProductId, Is.EqualTo(offer.ProductId));
        Assert.That(item.SellerId, Is.EqualTo(offer.SellerId));
    }
    [Test]
    public void TestIncrement() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var old = TestContext._cartItemRepository.First(i=>i.CartId == ContextHolder.Session.Cart.Id&&
                                                       i.ProductId==offer.ProductId && i.SellerId == offer.SellerId);
        uint oldAmount;
        if (old != null){
            old = TestContext._cartItemRepository.Detach(old);
            oldAmount = old.Quantity;
        }
        else oldAmount = 0;
        var i = TestContext._cartManager.Add(offer);
        TestContext._cartRepository.Flush();
        Assert.That(i.Quantity, Is.EqualTo(oldAmount +1));
    }
    [Test]
    public void Decrement() {
        var offer = TestContext._offerRepository.First(_=>true)!;
        var i = TestContext._cartManager.Add(offer);
        TestContext._cartItemRepository.Flush();
        var oldAmount = i.Quantity;
        var decremented = Random.Shared.Next((int)i.Quantity - 1);
        TestContext._cartManager.Decrement(offer, (uint)decremented);
        TestContext._cartRepository.Flush();
        var refetched = TestContext._cartItemRepository.First(i=>i.CartId == ContextHolder.Session.Cart.Id && i.ProductId == offer.ProductId && i.SellerId==offer.SellerId);
        Assert.That(refetched.Quantity, Is.EqualTo(oldAmount - decremented));
    }

    [Test]
    public void TestGetWithAggregates()
    {
        // Clear cart first to ensure a clean state for testing aggregates
        TestContext._cartManager.newCart();
        var currentCartItems = TestContext._cartItemRepository.Where(ci => ci.CartId == ContextHolder.Session.Cart.Id);
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

        TestContext._cartManager.Add(offer1!, quantity1);
        TestContext._cartManager.Add(offer2!, quantity2);
        TestContext._cartRepository.Flush();

        // Get cart with aggregates
        var cartWithAggregates = TestContext._cartManager.Get(true, true) as CartWithAggregates;

        Assert.That(cartWithAggregates, Is.Not.Null);

        // Calculate expected values
        decimal expectedTotalPrice = (offer1!.Price * quantity1) + (offer2!.Price * quantity2);
        decimal expectedDiscountedPrice = (offer1.Price * quantity1 * (decimal)offer1.Discount) +
                                          (offer2.Price * quantity2 * (decimal)offer2.Discount);
        decimal expectedCouponDiscountedPrice = expectedDiscountedPrice; // Assuming no coupons for simplicity in this test
        decimal expectedDiscountAmount = expectedTotalPrice - expectedDiscountedPrice;
        decimal expectedCouponDiscountAmount = expectedDiscountedPrice - expectedCouponDiscountedPrice; // Should be 0 if no coupons

        // Assertions
        Assert.That(cartWithAggregates.ItemCount, Is.EqualTo(2));
        Assert.That(cartWithAggregates.TotalPrice, Is.EqualTo(expectedTotalPrice));
        Assert.That(cartWithAggregates.DiscountedPrice, Is.EqualTo(expectedDiscountedPrice));
        Assert.That(cartWithAggregates.CouponDiscountedPrice, Is.EqualTo(expectedCouponDiscountedPrice));
        Assert.That(cartWithAggregates.DiscountAmount, Is.EqualTo(expectedDiscountAmount));
        Assert.That(cartWithAggregates.CouponDiscountAmount, Is.EqualTo(expectedCouponDiscountAmount));
        Assert.That(cartWithAggregates.Items.Sum(i=>i.Quantity), Is.EqualTo(5)); // Should have 2 distinct items
    }
}
