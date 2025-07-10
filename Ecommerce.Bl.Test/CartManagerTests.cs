using Ecommerce.Entity;
using NUnit.Framework.Legacy;

namespace Ecommerce.Bl.Test;

public class CartManagerTests
{
    [Test]
    public void newCartUserless() {
        var c=TestContext._cartRepository.Add(new Cart());
        var s=TestContext._sessionRepository.Add(new Session{ Cart = c, CartId = c.Id });
        ContextHolder.Session = s;
        var news = TestContext._cartManager.newCart();
        ClassicAssert.AreNotEqual(s.Id, news.Id);
    }
    [Test]
    public void newCartUserful() {
        ContextHolder.Session = TestContext._user.Session;
        TestContext._cartManager.newCart();
        ClassicAssert.AreNotEqual(ContextHolder.Session.Id, TestContext._user.SessionId);
    }
    [Test]
    public void AddToCart() {
        ContextHolder.Session = TestContext._user.Session;
        var offer = TestContext._offerRepository.Find(_=>true)!;
        int amount = Random.Shared.Next(3);
        var item = TestContext._cartManager.Add(offer, amount);
        //also assert lazy fetching.
        Assert.That(offer.Product?.Offers, Is.Not.Null);
        //assert that changes are persisted
        var fetchedItem = TestContext._userRepository.Find(u => u.Id == ContextHolder.Session!.UserId).Session.Cart
            .Items.FirstOrDefault(i=>i.CartId==ContextHolder.Session.CartId &&i.ProductId == offer.ProductId && i.SellerId == offer.SellerId);
        Assert.That(fetchedItem, Is.Not.Null);
        Assert.That(fetchedItem, Is.EqualTo(item));
        Assert.That(fetchedItem.Quantity,Is.EqualTo(amount));
        Assert.Equals(offer.ProductId, item.ProductId);
        Assert.Equals(offer.SellerId, item.SellerId);
        increment();
        void increment() {
            TestContext._cartManager.Add(fetchedItem);
            fetchedItem = TestContext._userRepository.Find(u => u.Id == ContextHolder.Session!.UserId).Session.Cart
                .Items.FirstOrDefault(i =>
                    i.CartId == ContextHolder.Session.CartId && i.ProductId == offer.ProductId &&
                    i.SellerId == offer.SellerId);
            Assert.That(fetchedItem, Is.Not.Null);
            Assert.Equals(amount * 2, fetchedItem.Quantity);
        }
    }

    [Test]
    public void Decrement() {
        Assert.That(ContextHolder.Session?.Cart?.Items.Count, Is.GreaterThan(0));
        var item = ContextHolder.Session.Cart.Items.First();
        var oldAmount = item.Quantity;
        var decremented = Random.Shared.Next(item.Quantity - 1);
        TestContext._cartManager.Decrement(item.ProductOffer, decremented);
        var c =TestContext._cartRepository.Find(c => c.Id == ContextHolder.Session.CartId);
        var fetchedItem = c.Items.First(i=>i.ProductId==item.ProductId && i.SellerId==item.SellerId);
        Assert.That(fetchedItem.Quantity, Is.EqualTo(oldAmount - decremented));
    }
}