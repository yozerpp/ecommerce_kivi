using Bogus;
using Ecommerce.Dao;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Test;

public class SellerManagerTests
{
    private static Seller _seller;

    [OneTimeSetUp]
    public static void CreateSeller() {
        _seller = new Seller(){
            ShopEmail = new Faker().Internet.Email(), ShopName = "ShopName",
            ShopPhoneNumber = new PhoneNumber(){ CountryCode = 90, Number = "1234567890" },
            ShopAddress = new Address()
                { City = "city", Neighborhood = "neighborhood", Street = "street", ZipCode = "12345",State = "state"},
            Email = new Faker().Internet.Email(), PasswordHash = "pass", 
            BillingAddress = new Address{
                City = "İzmir", ZipCode = "35410", Street = "Atatürk Cad.", Neighborhood = "Gazi", State = "Gaziemir"
            },ShippingAddress = new Address{
                City = "Trabzon", ZipCode = "35450", Street = "SFSD", Neighborhood = "Other", State = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }
        };
        _seller =(Seller) TestContext._userManager.Register(_seller);
        TestContext._sellerRepository.Flush();
        ContextHolder.Session!.User = _seller;
    }
    [SetUp]
    public void Login(){
        TestContext._userManager.LoginUser(_seller.Email, _seller.PasswordHash, out SecurityToken token);
    }
    [Test,Order(2)]
    public void LoginSeller() {
        TestContext._userManager.LoginSeller(_seller.Email, _seller.PasswordHash, out _);
        Assert.That(ContextHolder.Session?.User, Is.Not.Null);
        Assert.That(ContextHolder.Session.User, Is.InstanceOf<Seller>());
    }
    [Test,Order(3)]
    public void ListOfferOnExistingProduct() {
        var product = TestContext._productRepository.First(_=>true);
        var offer = TestContext._sellerManager.ListProduct(new ProductOffer(){
            Price = 100,ProductId = product.Id, Stock = 10
        });
        TestContext._offerRepository.Flush();
        TestContext._productRepository.Detach(product);
        product = TestContext._productRepository.First(p => p.Id == product.Id,includes:[[nameof(Product.Offers)]]);
        Assert.That(product.Offers, Contains.Item(offer));
    }
    [Test,Order(4)]
    public void ListOfferOnNonExistingProduct() {
        var cat = TestContext._categoryRepository.First(_ => true);
        var product = new Product{ Description = "desc", Name = "Name", Image = null, CategoryId = cat.Id};
        var listed = TestContext._sellerManager.ListProduct(new ProductOffer(){
            ProductId = 100, Stock = 10, Product = product, SellerId = ContextHolder.Session!.User!.Id
        });
        TestContext._productRepository.Flush();
        var retrieved = TestContext._productRepository.First(p => p.Id == listed.ProductId);
        Assert.That( product.Description, Is.EqualTo(retrieved?.Description));
        Assert.That(product.Name, Is.EqualTo(retrieved?.Name));
        var retrievedOffer = TestContext._offerRepository.First(o => o.ProductId == retrieved!.Id && o.SellerId==ContextHolder.Session.User.Id);
        Assert.That(retrievedOffer, Is.Not.Null);
        Assert.That(retrievedOffer.ProductId, Is.EqualTo(listed.ProductId));
        _offer = retrievedOffer;
    }

    private ProductOffer _offer;
    [Test,Order(5)]
    public void UpdateOffer() {
        var oldStock = _offer.Stock;
        _offer.Stock += 10;
        TestContext._sellerManager.updateOffer(_offer, _offer.ProductId);
        TestContext._offerRepository.Flush();
        TestContext._offerRepository.Detach(_offer);
        var newOffer = TestContext._offerRepository.First(o=>o.ProductId==_offer.ProductId && o.SellerId==_offer.SellerId);
        Assert.That(newOffer, Is.Not.Null);
        Assert.That(newOffer.Stock,Is.EqualTo(oldStock +10));
        _offer = newOffer;
    }

    [Test,Order(6)]
    public void DeleteOffer() {
        TestContext._sellerManager.UnlistOffer(_offer);
        TestContext._offerRepository.Flush();
        var exists = TestContext._offerRepository.Exists(o => o.ProductId == _offer.ProductId && o.SellerId == _offer.SellerId);
        Assert.That(exists, Is.False);
    }
}