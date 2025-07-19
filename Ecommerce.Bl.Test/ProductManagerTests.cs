using Ecommerce.Bl.Concrete;
using Ecommerce.Entity;
using Moq;
using System.Linq.Expressions;
using Bogus;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework.Legacy;
using Product = Ecommerce.Entity.Product;

namespace Ecommerce.Bl.Test;

public class ProductManagerTests
{
    private ProductManager _productManager;
    private Mock<IRepository<Product>> _mockProductRepository;
    private List<Product> _testProducts;

    // Test data for aggregates
    private Seller _testSeller;
    private Product _productWithAggregates;
    private ProductOffer _offer1;
    private ProductOffer _offer2;
    private User _buyerUser;
    private User _reviewerUser;

    [OneTimeSetUp]
    public void OneTimeSetupAggregates()
    {
        // 1. Register and Login a Seller
        _testSeller = new Seller
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Agg",
            LastName = "Seller",
            ShopName = "AggTestShop",
            ShopEmail = new Faker().Internet.Email(),
            ShopPhoneNumber = new PhoneNumber{CountryCode = 90,Number = "345345345"},
            ShopAddress = new Address{City = "c",Neighborhood = "n",State = "s",Street = "st",ZipCode = "z"},
            ShippingAddress = new Address{City = "c",Neighborhood = "n",State = "s",Street = "st",ZipCode = "z"},
            PhoneNumber = new PhoneNumber{CountryCode = 90,Number = "345345345"}
        };
        _testSeller = (Seller)TestContext._userManager.Register(_testSeller);
        TestContext._sellerRepository.Flush();

        // Login as the seller to list products
        TestContext._userManager.LoginSeller(_testSeller.Email, _testSeller.PasswordHash, out SecurityToken sellerToken);
        TestContext._jwtmanager.UnwrapToken(sellerToken, out var sellerUser, out var sellerSession);

        // 2. Create a Product and multiple Offers for it
        var category = TestContext._categoryRepository.First(_ => true);
        _productWithAggregates = new Product { Name = "Aggregated Product", Description = "For aggregate testing", CategoryId = category.Id };

        _offer1 = new ProductOffer
        {
            Product = _productWithAggregates,
            Price = 100m,
            Stock = 10,
            SellerId = _testSeller.Id,
            Discount = 0.1m // 10% discount
        };
        _offer1 = TestContext._sellerManager.ListProduct(_offer1); // This will add the product too
        ContextHolder.Session = null;
        var secondSeller = TestContext._userManager.Register(new Seller{
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Agg",
            LastName = "Seller",
            ShopName = "AggTestShop",
            ShopEmail = new Faker().Internet.Email(),
            ShopPhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "345345345" },
            ShopAddress = new Address{ City = "c", Neighborhood = "n", State = "s", Street = "st", ZipCode = "z" },
            ShippingAddress = new Address{ City = "c", Neighborhood = "n", State = "s", Street = "st", ZipCode = "z" },
            PhoneNumber = new PhoneNumber{ CountryCode = 90, Number = "345345345" }
        });
        UserManagerTests.Login(secondSeller,out _);
        _offer2 = new ProductOffer
        {
            ProductId = _productWithAggregates.Id, // Link to the same product
            Price = 120m,
            Stock = 5,
            SellerId = secondSeller.Id,
            Discount = 0.0m // No discount
        };
        _offer2 = TestContext._sellerManager.ListProduct(_offer2);

        // 3. Register Buyer User
        _buyerUser = new User
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "buyerpass",
            FirstName = "Agg",
            LastName = "Buyer",
            ShippingAddress = new Address{City = "c",Neighborhood = "n",State = "s",Street = "st",ZipCode = "z"},
            PhoneNumber = new PhoneNumber{CountryCode = 90,Number = "345345345"}
        };
        ContextHolder.Session = null;
        _buyerUser = TestContext._userManager.Register(_buyerUser);

        // 4. Simulate Sales (Orders)
        // Login as buyer
        TestContext._userManager.LoginUser(_buyerUser.Email, _buyerUser.PasswordHash, out SecurityToken buyerToken);
        TestContext._jwtmanager.UnwrapToken(buyerToken, out var buyerUser, out var buyerSession);

        // Purchase from offer1
        TestContext._cartManager.newCart(_buyerUser);
        TestContext._cartManager.Add(_offer1, 2); // Buy 2 units
        TestContext._cartRepository.Flush();
        var payment1 = new Payment { TransactionId = "AGG_SALE_1", Amount = _offer1.Price * 2, PaymentMethod = PaymentMethod.CARD };
        payment1 = TestContext._paymentRepository.Add(payment1);
        TestContext._orderManager.CreateOrder();
        TestContext._orderRepository.Flush();

        // Purchase from offer2
        TestContext._cartManager.newCart(_buyerUser);
        TestContext._cartManager.Add(_offer2, 1); // Buy 1 unit
        TestContext._cartRepository.Flush();
        var payment2 = new Payment { TransactionId = "AGG_SALE_2", Amount = _offer2.Price * 1, PaymentMethod = PaymentMethod.CARD };
        payment2 = TestContext._paymentRepository.Add(payment2);
        TestContext._orderManager.CreateOrder();
        TestContext._orderRepository.Flush();

        // 5. Register Reviewer User
        _reviewerUser = new User
        {
            Email =new Faker().Internet.Email(),
            PasswordHash = "reviewerpass",
            FirstName = "Agg",
            LastName = "Reviewer",
            ShippingAddress = new Address{City = "c",Neighborhood = "n",State = "s",Street = "st",ZipCode = "z"},
            PhoneNumber = new PhoneNumber{CountryCode = 90,Number = "345345345"}
        };
        ContextHolder.Session = null;
        _reviewerUser = TestContext._userManager.Register(_reviewerUser);
        TestContext._userRepository.Flush();

        // 6. Simulate Reviews
        // Login as reviewer
        TestContext._userManager.LoginUser(_reviewerUser.Email, _reviewerUser.PasswordHash, out SecurityToken reviewerToken);
        TestContext._jwtmanager.UnwrapToken(reviewerToken, out var rUser, out var rSession);

        // Review for offer1
        var review1 = new ProductReview
        {
            ProductId = _offer1.ProductId,
            SellerId = _offer1.SellerId,
            SessionId = _reviewerUser.Id,
            Rating = 5,
            Comment = "Excellent product!"
        };
        TestContext._reviewManager.LeaveReview(review1);
        TestContext._reviewRepository.Flush();

        // Review for offer2
        var review2 = new ProductReview
        {
            ProductId = _offer2.ProductId,
            SellerId = _offer2.SellerId,
            SessionId = _reviewerUser.Id,
            Rating = 3,
            Comment = "It's okay."
        };
        TestContext._reviewManager.LeaveReview(review2);
        TestContext._reviewRepository.Flush();
        
    }


    [SetUp]
    public void Setup()
    {
        _mockProductRepository = new Mock<IRepository<Product>>();
        _productManager = new ProductManager(_mockProductRepository.Object);
        // Create test products with various properties
        _testProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Apple iPhone", CategoryId = 1, Description ="Smartphone"},
            new Product { Id = 2, Name = "Samsung Galaxy", CategoryId = 1, Description = "Android phone" },
            new Product { Id = 3, Name = "Apple iPad", CategoryId = 2, Description = "Tablet device" },
            new Product { Id = 4, Name = "Dell Laptop", CategoryId = 3, Description = "Gaming laptop" },
            new Product { Id = 5, Name = "HP Printer", CategoryId = 4, Description = "Inkjet printer" }
        };
        UserManagerTests.Login(_buyerUser, out _);
    }

    private void SetupMockRepository()
    {
        _mockProductRepository.Setup(r => r.Where(
            It.IsAny<Expression<Func<Product, ProductWithAggregates>>>(), // Changed to ProductWithAggregates
            It.IsAny<Expression<Func<ProductWithAggregates, bool>>>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<(Expression<Func<ProductWithAggregates, object>>,bool)[]>(), 
            It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, ProductWithAggregates>> select, Expression<Func<Product, bool>> predicate, int offset, int limit, (Expression<Func<Product, object>>, bool)[] orderBy, string[][] includes) =>
                _testProducts.Where(predicate.Compile()).Select(select.Compile()).Skip(offset).Take(limit).ToList());

        _mockProductRepository.Setup(r => r.First(
                It.IsAny<Expression<Func<Product, ProductWithAggregates>>>(), // Changed to ProductWithAggregates
                It.IsAny<Expression<Func<ProductWithAggregates, bool>>>(),
                It.IsAny<string[][]>(),
                It.IsAny<(Expression<Func<ProductWithAggregates, object>>,bool)[]>()))
            .Returns((Expression<Func<Product, ProductWithAggregates>> select, Expression<Func<Product, bool>> predicate, string[][] includes, (Expression<Func<Product, object>>,bool)[] order) =>
                _testProducts.Where(predicate.Compile()).Select(select.Compile()).FirstOrDefault());

        _mockProductRepository.Setup(r => r.First(
                It.IsAny<Expression<Func<Product, Product>>>(), // For GetById
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<string[][]>(),
                It.IsAny<(Expression<Func<Product, object>>,bool)[]>()))
            .Returns((Expression<Func<Product, Product>> select, Expression<Func<Product, bool>> predicate, string[][] includes,(Expression<Func<Product, object>>,bool)[] orders) =>
                _testProducts.Where(predicate.Compile()).Select(select.Compile()).FirstOrDefault());
    }

    [Test]
    public void Search_EqualsOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple iPhone", Operator = SearchPredicate.OperatorType.Equals }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Apple iPhone"));
    }

    [Test]
    public void Search_LikeOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple", Operator = SearchPredicate.OperatorType.Like }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Apple iPhone and Apple iPad
    }

    [Test]
    public void Search_GreaterThanOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "2", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Dell Laptop and HP Printer (CategoryId 3 and 4)
    }

    [Test]
    public void Search_GreaterThanOrEqualOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "2", Operator = SearchPredicate.OperatorType.GreaterThanOrEqual }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3)); // Apple iPad, Dell Laptop, HP Printer (CategoryId 2, 3, 4)
    }

    [Test]
    public void Search_LessThanOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "3", Operator = SearchPredicate.OperatorType.LessThan }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3)); // Apple iPhone, Samsung Galaxy, Apple iPad (CategoryId 1, 1, 2)
    }

    [Test]
    public void Search_LessThanOrEqualOperator_ReturnsMatchingProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "2", Operator = SearchPredicate.OperatorType.LessThanOrEqual }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3)); // Apple iPhone, Samsung Galaxy, Apple iPad (CategoryId 1, 1, 2)
    }

    // [Test]
    // public void Search_InvalidNumberFormat_ThrowsArgumentException()
    // {
    //     // Arrange
    //     SetupMockRepository();
    //     var predicates = new List<SearchPredicate>
    //     {
    //         new SearchPredicate { PropName = "CategoryId", Value = "invalid_number", Operator = SearchPredicate.OperatorType.GreaterThan }
    //     };
    //     var ordering = new List<SearchOrder>();
    //
    //     // Act & Assert
    //     Assert.Throws<ArgumentException>(() => _productManager.SearchWithAggregates(predicates, ordering));
    // }

    [Test]
    public void Search_NullPropertyValue_ReturnsFalseForNumericComparisons()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = "Test Product", CategoryId = 0, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "100", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(
                It.IsAny<Expression<Func<Product, ProductWithAggregates>>>(), // Changed to ProductWithAggregates
                It.IsAny<Expression<Func<ProductWithAggregates, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<ProductWithAggregates, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, ProductWithAggregates>> select, Expression<Func<Product, bool>> predicate, int offset, int limit, (Expression<Func<Product, object>>, bool)[] orderBy, string[][] includes) =>
                productsWithNull.Where(predicate.Compile()).Select(select.Compile()).Skip(offset).Take(limit).ToList());

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // CategoryId 0 is not greater than 100
    }

    [Test]
    public void Search_MultiplePredicates_ReturnsProductsMatchingAllConditions()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple", Operator = SearchPredicate.OperatorType.Like },
            new SearchPredicate { PropName = "CategoryId", Value = "1", Operator = SearchPredicate.OperatorType.GreaterThanOrEqual }
        };
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Apple iPhone and Apple iPad match both conditions
        Assert.That(result.All(p => p.Name.Contains("Apple")), Is.True);
    }

    [Test]
    public void Search_WithOrdering_FiltersInvalidOrderingProperties()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>
        {
            new SearchOrder { PropName = "Name", Ascending = true },
            new SearchOrder { PropName = "InvalidProperty", Ascending = false }, // This should be filtered out
            new SearchOrder { PropName = "CategoryId", Ascending = false }
        };

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(5)); // All products should be returned
        // The invalid ordering property should be filtered out (tested by not throwing exception)
    }

    [Test]
    public void Search_WithPagination_ReturnsCorrectPageAndSize()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>();
        int page = 1;
        int pageSize = 2;

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering, page:page,pageSize: pageSize);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Should return 2 products for page 1 with pageSize 2
    }

    [Test]
    public void Search_EmptyPredicatesAndOrdering_ReturnsAllProducts()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>();

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(5)); // All test products should be returned
    }

    [Test]
    public void Search_LikeOperatorWithNullValue_HandlesGracefully()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = null, CategoryId = 1, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Test", Operator = SearchPredicate.OperatorType.Like }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(
                It.IsAny<Expression<Func<Product, ProductWithAggregates>>>(), // Changed to ProductWithAggregates
                It.IsAny<Expression<Func<ProductWithAggregates, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<ProductWithAggregates, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, ProductWithAggregates>> select, Expression<Func<Product, bool>> predicate, int offset, int limit, (Expression<Func<Product, object>>, bool)[] orderBy, string[][] includes) =>
                productsWithNull.Where(predicate.Compile()).Select(select.Compile()).Skip(offset).Take(limit).ToList());

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not match Like operator
    }

    [Test]
    public void Search_EqualsOperatorWithNullValue_HandlesGracefully()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = null, CategoryId = 1, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Test", Operator = SearchPredicate.OperatorType.Equals }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(
                It.IsAny<Expression<Func<Product, ProductWithAggregates>>>(), // Changed to ProductWithAggregates
                It.IsAny<Expression<Func<ProductWithAggregates, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<ProductWithAggregates, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, ProductWithAggregates>> select, Expression<Func<Product, bool>> predicate, int offset, int limit, (Expression<Func<Product, object>>, bool)[] orderBy, string[][] includes) =>
                productsWithNull.Where(predicate.Compile()).Select(select.Compile()).Skip(offset).Take(limit).ToList());

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not equal non-null values
    }
    //TODO: FİX THİS
    [Test]
    public void GetByIdWithAggregates_CalculatesCorrectAggregates()
    {
        // Arrange
        // The _productWithAggregates and its related data are set up in OneTimeSetupAggregates
        // We need to mock the repository to return this specific product with its loaded offers, reviews, and bought items
        _productManager = new ProductManager(TestContext._productRepository);
        var result = _productManager.GetByIdWithAggregates(_productWithAggregates.Id, fetchReviews: true, fetchImage: false);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(_productWithAggregates.Id));

        // SaleCount: 2 from offer1 + 1 from offer2 = 3
        Assert.That(result.SaleCount, Is.EqualTo(3));

        // ReviewCount: 1 from offer1 + 1 from offer2 = 2
        Assert.That(result.ReviewCount, Is.EqualTo(2));

        // ReviewAverage: (5 + 3) / 2 = 4.0
        Assert.That(result.ReviewAverage, Is.EqualTo(4.0f));

        // MinPrice: Should be the minimum price among offers (100m)
        Assert.That(result.MinPrice, Is.EqualTo(100m));

        // MaxPrice: Should be the maximum price among offers (120m)
        Assert.That(result.MaxPrice, Is.EqualTo(120m));
    }
}
