using Ecommerce.Dao;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;

namespace Ecommerce.Bl.Test;

public class SellerManagerTests
{
    private Seller _seller;

    [Test]
    public void CreateSeller() {
        ContextHolder.Session = TestContext._user.Session;
        _seller = new Seller(){
            SellerEmail = "email@b.com", ShopName = "ShopName",
            SellerPhoneNumber = new PhoneNumber(){ CountryCode = 90, Number = "1234567890" },
            Address = new Address()
                { City = "city", neighborhood = "neighborhood", Street = "street", ZipCode = "12345" }
        };
        _seller = TestContext._sellerManager.CreateSeller(_seller);
        foreach (var property in typeof(User).GetProperties()){
            Assert.That(
                property.GetValue(_seller),Is.EqualTo(
                property.GetValue(TestContext._user))
            );
        }
        ContextHolder.Session!.User = _seller;
    }

    [Test]
    public void LoginSeller() {
        TestContext._userManager.Login(_seller.Username, _seller.PasswordHash, out _);
        TestContext._sellerManager.Login();
        Assert.That(ContextHolder.Session?.User, Is.Not.Null);
        Assert.That(ContextHolder.Session.User, Is.InstanceOf<Seller>());
    }
    [Test]
    public void ListOfferOnExistingProduct() {
        var product = TestContext._productManager.Search([], [], 0, 1).ElementAt(0);
        TestContext._sellerManager.ListProduct(new ProductOffer(){
            Price = 100,ProductId = product.Id, Stock = 10
        });
    }

    [Test]
    public void ListOfferOnNonExistingProduct() {
        var product = new Product{ Description = "desc", Name = "Name", Image = null};
        var listed = TestContext._sellerManager.ListProduct(new ProductOffer(){
            ProductId = 100, Stock = 10, Product = product, SellerId = ContextHolder.Session!.User!.Id
        });
        var retrieved = TestContext._productRepository.Find(p => p.Id == listed.ProductId);
        Assert.That( product.Description, Is.EqualTo(retrieved?.Description));
        Assert.That(product.Name, Is.EqualTo(retrieved?.Name));
        var retrievedOffer = TestContext._offerRepository.Find(o => o.ProductId == retrieved!.Id && o.SellerId==ContextHolder.Session.User.Id);
        Assert.That(retrievedOffer, Is.Not.Null);
        Assert.That(retrievedOffer.ProductId, Is.EqualTo(listed.ProductId));
        _offer = retrievedOffer;
    }

    private ProductOffer _offer;
    [Test]
    public void UpdateOffer() {
        var oldStock = _offer.Stock;
        _offer.Stock += 10;
        TestContext._sellerManager.updateOffer(_offer, _offer.ProductId);
        var newOffer = TestContext._offerRepository.Find(o=>o.ProductId==_offer.ProductId && o.SellerId==_offer.SellerId);
        Assert.That(newOffer, Is.Not.Null);
    }

    [Test]
    public void DeleteOffer() {
        TestContext._sellerManager.UnlistOffer(_offer);
        var exists = TestContext._offerRepository.Exists(o => o.ProductId == _offer.ProductId && o.SellerId == _offer.SellerId);
        Assert.That(exists, Is.False);
    }
}