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

    [OneTimeSetUp]
    public static void CreateSeller() {
        _seller = new Seller(){
            ShopEmail = new Faker().Internet.Email(), ShopName = "ShopName",
            ShopPhoneNumber = new PhoneNumber(){ CountryCode = 90, Number = "1234567890" },
            FirstName = new Faker().Name.FirstName(),
            LastName = new Faker().Name.LastName(),
            ShopAddress = new Address()
                { City = "city", Neighborhood = "neighborhood", Street = "street", ZipCode = "12345",State = "state"},
            Email = new Faker().Internet.Email(), PasswordHash = "pass",ShippingAddress = new Address{
                City = "Trabzon", ZipCode = "35450", Street = "SFSD", Neighborhood = "Other", State = "Gaziemir"
            },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "5551234567" }
        };
        _seller =(Seller) TestContext._userManager.Register(_seller);
        TestContext._sellerRepository.Flush();
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

    [Test,Order(5)]
    public void UpdateOffer() {
        var oldStock = _offer.Stock;
        _offer.Stock += 10;
        _offer.Product = null;
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
        var product = new Product { Description = "New Product Desc", Name = "New Product Name", Image = null, CategoryId = cat.Id };
        var newOffer = TestContext._sellerManager.ListProduct(new ProductOffer
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
        // Ensure seller is logged in
        Login();

        // List a product if not already listed for this test
        var offerForSale = TestContext._offerRepository.First(o => o.SellerId == _seller.Id);
        if (offerForSale == null)
        {
            var cat = TestContext._categoryRepository.First(_ => true);
            var product = new Product { Description = "Sale Product Desc", Name = "Sale Product Name", Image = null, CategoryId = cat.Id };
            offerForSale = TestContext._sellerManager.ListProduct(new ProductOffer
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
        TestContext._cartManager.newCart(); // Ensure a fresh cart for the user
        TestContext._cartManager.Add(offerForSale, 2); // Add 2 units of the product
        TestContext._cartRepository.Flush();

        var payment = new Payment { TransactionId = "SALE_TEST_" + Guid.NewGuid().ToString(), Amount = offerForSale.Price * 2, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder();
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
            var product = new Product { Description = "Review Product Desc", Name = "Review Product Name", Image = null, CategoryId = cat.Id };
            offerForReview = TestContext._sellerManager.ListProduct(new ProductOffer
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
        var reviewer = _seller;
        // Simulate a review
        var review = new ProductReview
        {
            ProductId = offerForReview.ProductId,
            SellerId = offerForReview.SellerId,
            ReviewerId = reviewer.Id, // Assuming TestContext.User is the current logged-in user
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
