using Bogus;
using Ecommerce.Dao;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.IdentityModel.Tokens;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Test;

public class SellerManagerTests
{
    private static Seller _seller;
    private static ProductOffer _offer; // Moved to class level for reuse
    private static Customer _user;
    [OneTimeSetUp]
    public static void CreateSeller() {
        var e = new Faker().Internet.Email();
        _seller = new Seller(){
            ShopName = "ShopName",
            FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),
            NormalizedEmail = e.ToUpper(),
            Email = e,
            PasswordHash = "pass",Address = new Address{
                City = "Trabzon", ZipCode = "35450", Line1 = "SFSD", District = "Other", Country = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }
        };
        _seller = TestContext._userManager.Register(_seller);
        e = new Faker().Internet.Email();
        _user = new Customer(){
            Email =e, NormalizedEmail = e.ToUpper(), FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(), PasswordHash = "pass", Address = new Address{
                City = "Trabzon", ZipCode = "35450", Line1 = "SFSD", District = "Other", Country = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }

        };
        _user = TestContext._userManager.Register(_user);
    }
    [SetUp]
    public void Login(){
        _seller = TestContext._userManager.LoginSeller(_seller.NormalizedEmail, _seller.PasswordHash, out SecurityToken token);
        Assert.That(_seller, Is.Not.Null);
        Assert.That(_seller, Is.InstanceOf<Seller>());
    }

    [Test,Order(3)]
    public void ListOfferOnExistingProduct() {
        var product = TestContext._productRepository.First(_=>true);
        var offer = TestContext._sellerManager.ListOffer(_seller,new ProductOffer(){
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
        var product = new Product{ Description = "desc", Name = "Name",  CategoryId = cat.Id};
        var listed = TestContext._sellerManager.ListOffer(_seller,new ProductOffer(){
            ProductId = 100, Stock = 10, Product = product, SellerId = _seller.Id
        });
        TestContext._productRepository.Flush();
        var retrieved = TestContext._productRepository.First(p => p.Id == listed.ProductId);
        Assert.That( product.Description, Is.EqualTo(retrieved?.Description));
        Assert.That(product.Name, Is.EqualTo(retrieved?.Name));
        var retrievedOffer = TestContext._offerRepository.First(o => o.ProductId == retrieved!.Id && o.SellerId==_seller.Id);
        Assert.That(retrievedOffer, Is.Not.Null);
        Assert.That(retrievedOffer.ProductId, Is.EqualTo(listed.ProductId));
        _offer = retrievedOffer;
    }

    [Test,Order(5)]
    public void UpdateOffer() {
        var oldStock = _offer.Stock;
        _offer.Stock += 10;
        _offer.Product = null;
        TestContext._sellerManager.updateOffer(_seller,_offer, _offer.ProductId);
        TestContext._offerRepository.Flush();
        TestContext._offerRepository.Detach(_offer);
        var newOffer = TestContext._offerRepository.First(o=>o.ProductId==_offer.ProductId && o.SellerId==_offer.SellerId);
        Assert.That(newOffer, Is.Not.Null);
        Assert.That(newOffer.Stock,Is.EqualTo(oldStock +10));
        _offer = newOffer;
    }

    [Test,Order(6)]
    public void DeleteOffer() {
        TestContext._sellerManager.UnlistOffer(_seller,_offer);
        TestContext._offerRepository.Flush();
        var exists = TestContext._offerRepository.Exists(o => o.ProductId == _offer.ProductId && o.SellerId == _offer.SellerId);
        Assert.That(exists, Is.False);
    }

    [Test, Order(7)]
    public void TestSellerAggregatesAfterProductListing()
    {
        // Ensure seller is logged in
        Login();

        // Get initial seller aggregates
        var initialSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, true, false);
        uint initialProductCount = initialSellerAggregates?.OfferCount ?? 0;

        // List a new product
        var cat = TestContext._categoryRepository.First(_ => true);
        var product = new Product { Description = "New Product Desc", Name = "New Product Name",  CategoryId = cat.Id };
        var newOffer = TestContext._sellerManager.ListOffer(_seller,new ProductOffer
        {
            Price = 50,
            Stock = 5,
            Product = product,
            SellerId = _seller.Id
        });
        TestContext._productRepository.Flush();

        // Get updated seller aggregates
        var updatedSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, true, false);

        // Assert that OfferCount increased by 1
        Assert.That(updatedSellerAggregates, Is.Not.Null);
        Assert.That(updatedSellerAggregates.OfferCount, Is.EqualTo(initialProductCount + 1));
    }

    [Test, Order(8)]
    public void TestSellerAggregatesAfterSale()
    {
        // List a product if not already listed for this test
        var offerForSale = TestContext._offerRepository.First(o => o.SellerId == _seller.Id);
        if (offerForSale == null)
        {
            var cat = TestContext._categoryRepository.First(_ => true);
            var product = new Product { Description = "Sale Product Desc", Name = "Sale Product Name",  CategoryId = cat.Id };
            offerForSale = TestContext._sellerManager.ListOffer(_seller,new ProductOffer
            {
                Price = 25,
                Stock = 10,
                Product = product,
                SellerId = _seller.Id
            });
            TestContext._productRepository.Flush();
        }

        // Get initial seller aggregates
        var initialSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, true, false);
        uint initialSaleCount = initialSellerAggregates?.SaleCount ?? 0;

        // Simulate a sale: Add item to cart and create an order
        TestContext._cartManager.newSession(_user); // Ensure a fresh cart for the user
        TestContext._cartManager.Add(_user.Session.Cart,offerForSale, 2); // Add 2 units of the product
        TestContext._cartRepository.Flush();

        var payment = new Payment { TransactionId = "SALE_TEST_" + Guid.NewGuid().ToString(), Amount = offerForSale.Price * 2, PaymentMethod = PaymentMethod.Card };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder(_user.Session, out _, _user);
        TestContext._orderRepository.Flush();

        // Get updated seller aggregates
        var updatedSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, true, false);

        // Assert that SaleCount increased by the quantity sold
        Assert.That(updatedSellerAggregates, Is.Not.Null);
        Assert.That(updatedSellerAggregates.SaleCount, Is.EqualTo(initialSaleCount + 2));
    }

    [Test, Order(9)]
    public void TestSellerAggregatesAfterReview()
    {
        // Ensure seller is logged in
        Login();
        
        // List a product if not already listed for this test
        var offerForReview = TestContext._offerRepository.First(o => o.SellerId == _seller.Id);
        if (offerForReview == null)
        {
            var cat = TestContext._categoryRepository.First(_ => true);
            var product = new Product { Description = "Review Product Desc", Name = "Review Product Name",  CategoryId = cat.Id };
            offerForReview = TestContext._sellerManager.ListOffer(_seller,new ProductOffer
            {
                Price = 15,
                Stock = 1,
                Product = product,
                SellerId = _seller.Id
            });
            TestContext._productRepository.Flush();
        }

        // Get initial seller aggregates
        var initialSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, false, true);
        uint initialReviewCount = initialSellerAggregates?.ReviewCount ?? 0;
        double initialReviewAverage = initialSellerAggregates?.ReviewAverage ?? 0.0;
        var reviewer = _user;
        // Simulate a review
        var review = new ProductReview
        {
            ProductId = offerForReview.ProductId,
            SellerId = offerForReview.SellerId,
            ReviewerId = reviewer.Id, // Assuming TestContext.User is the current logged-in user
            SessionId = reviewer.Session?.Id??reviewer.SessionId,
            Rating = 4,
            Comment = "Great product!"
        };
        TestContext._reviewRepository.Add(review);
        TestContext._reviewRepository.Flush();

        // Get updated seller aggregates
        var updatedSellerAggregates = TestContext._sellerManager.GetSellerWithAggregates(_seller.Id, false, true);

        // Assert that ReviewCount increased by 1
        Assert.That(updatedSellerAggregates, Is.Not.Null);
        Assert.That(updatedSellerAggregates.ReviewCount, Is.EqualTo(initialReviewCount + 1));

        // Assert that ReviewAverage is updated (simple average for one new review)
        // If initialReviewCount was 0, new average should be 4.0
        // If initialReviewCount was > 0, calculate the new average
        double expectedReviewAverage;
        if (initialReviewCount == 0)
        {
            expectedReviewAverage = 4.0;
        }
        else
        {
            expectedReviewAverage = ((initialReviewAverage * initialReviewCount) + (double)review.Rating) / (initialReviewCount + 1);
        }
        Assert.That(updatedSellerAggregates.ReviewAverage, Is.EqualTo(expectedReviewAverage).Within(0.001)); // Use Within for double comparison
    }
}
