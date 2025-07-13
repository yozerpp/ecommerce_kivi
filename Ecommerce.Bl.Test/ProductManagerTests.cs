using Ecommerce.Bl.Concrete;
using Ecommerce.Entity;
using Moq;
using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;

namespace Ecommerce.Bl.Test;

public class ProductManagerTests
{
    private ProductManager _productManager;
    private Mock<IRepository<Product>> _mockProductRepository;
    private List<Product> _testProducts;

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
    }

    private void SetupMockRepository()
    {
        _mockProductRepository.Setup(r => r.Where(
            It.IsAny<Expression<Func<Product, bool>>>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<(Expression<Func<Product, object>>,bool)[]>(), 
            It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, bool>> predicate, int offset, int limit, Expression<Func<Product, object>>[] orderBy, Expression<Func<Product, object>>[] includes) =>
                _testProducts.Where(predicate.Compile()).Skip(offset).Take(limit).ToList());
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

    [Test]
    public void Search_InvalidNumberFormat_ThrowsArgumentException()
    {
        // Arrange
        SetupMockRepository();
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "CategoryId", Value = "invalid_number", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _productManager.SearchWithAggregates(predicates, ordering));
    }

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
                It.IsAny<Expression<Func<Product, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<Product, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, bool>> predicate, int offset, int limit, Expression<Func<Product, object>>[] orderBy, Expression<Func<Product, object>>[] includes) =>
                productsWithNull.Where(predicate.Compile()).Skip(offset).Take(limit).ToList());

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
                It.IsAny<Expression<Func<Product, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<Product, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, bool>> predicate, int offset, int limit, Expression<Func<Product, object>>[] orderBy, Expression<Func<Product, object>>[] includes) =>
                productsWithNull.Where(predicate.Compile()).Skip(offset).Take(limit).ToList());

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
                It.IsAny<Expression<Func<Product, bool>>>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<(Expression<Func<Product, object>>,bool)[]>(), 
                It.IsAny<string[][]>()))
            .Returns((Expression<Func<Product, bool>> predicate, int offset, int limit, Expression<Func<Product, object>>[] orderBy, Expression<Func<Product, object>>[] includes) =>
                productsWithNull.Where(predicate.Compile()).Skip(offset).Take(limit).ToList());

        // Act
        var result = _productManager.SearchWithAggregates(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not equal non-null values
    }
}
