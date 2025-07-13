using Ecommerce.Entity;
using NUnit.Framework.Legacy;

namespace Ecommerce.Bl.Test;

public class CartManagerTests
{

    [OneTimeSetUp]
    public void Register() {
        UserManagerTests.TestRegister();
    }

    [SetUp]
    public void Login() {
        UserManagerTests.TestLogin();
    }
    [Test, Order(1)]
    public void newCartUserful() {
        var oldSession = ContextHolder.Session;
        TestContext._cartManager.newCart();
        TestContext._cartRepository.Flush();
        ClassicAssert.AreNotEqual(oldSession, TestContext._user.Session);
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
        var old = TestContext._itemRepository.First(i=>i.CartId == ContextHolder.Session.Cart.Id&&
                                                       i.ProductId==offer.ProductId && i.SellerId == offer.SellerId);
        uint oldAmount;
        if (old != null){
            old = TestContext._itemRepository.Detach(old);
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
        TestContext._itemRepository.Flush();
        var oldAmount = i.Quantity;
        var decremented = Random.Shared.Next((int)i.Quantity - 1);
        TestContext._cartManager.Decrement(offer, (uint)decremented);
        TestContext._cartRepository.Flush();
        var refetched = TestContext._itemRepository.First(i=>i.CartId == ContextHolder.Session.Cart.Id && i.ProductId == offer.ProductId && i.SellerId==offer.SellerId);
        Assert.That(refetched.Quantity, Is.EqualTo(oldAmount - decremented));
    }
}