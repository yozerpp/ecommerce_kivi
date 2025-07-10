using Ecommerce.Bl.Concrete;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Test;

public class ProductManagerTests
{
    private ProductManager<Product> _productManager;
    private Mock<IRepository<Product>> _mockProductRepository;
    private List<Product> _testProducts;

    [SetUp]
    public void Setup()
    {
        _mockProductRepository = new Mock<IRepository<Product>>();
        _productManager = new ProductManager<Product>(_mockProductRepository.Object);
        // Create test products with various properties
        _testProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Apple iPhone", Price = 999.99m, Description ="Smartphone"},
            new Product { Id = 2, Name = "Samsung Galaxy", Price = 799.99m, Description = "Android phone" },
            new Product { Id = 3, Name = "Apple iPad", Price = 599.99m, Description = "Tablet device" },
            new Product { Id = 4, Name = "Dell Laptop", Price = 1299.99m, Description = "Gaming laptop" },
            new Product { Id = 5, Name = "HP Printer", Price = 199.99m, Description = "Inkjet printer" }
        };
    }

    [Test]
    public void Search_EqualsOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple iPhone", Operator = SearchPredicate.OperatorType.Equals }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Apple iPhone"));
    }

    [Test]
    public void Search_LikeOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple", Operator = SearchPredicate.OperatorType.Like }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Apple iPhone and Apple iPad
    }

    [Test]
    public void Search_GreaterThanOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "800", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // iPhone and Dell Laptop
    }

    [Test]
    public void Search_GreaterThanOrEqualOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "799.99", Operator = SearchPredicate.OperatorType.GreaterThanOrEqual }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3)); // Samsung, iPhone, Dell Laptop
    }

    [Test]
    public void Search_LessThanOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "600", Operator = SearchPredicate.OperatorType.LessThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1)); // HP Printer
    }

    [Test]
    public void Search_LessThanOrEqualOperator_ReturnsMatchingProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "599.99", Operator = SearchPredicate.OperatorType.LessThanOrEqual }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // iPad and HP Printer
    }

    [Test]
    public void Search_InvalidNumberFormat_ThrowsArgumentException()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "invalid_number", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
            {
                // This will trigger the exception when the predicate is evaluated
                return _testProducts.Where(predicate).ToList();
            });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _productManager.Search(predicates, ordering));
    }

    [Test]
    public void Search_NullPropertyValue_ReturnsFalseForNumericComparisons()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = "Test Product", Price = null, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Price", Value = "100", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                productsWithNull.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not match numeric comparisons
    }

    [Test]
    public void Search_MultiplePredicates_ReturnsProductsMatchingAllConditions()
    {
        // Arrange
        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Apple", Operator = SearchPredicate.OperatorType.Like },
            new SearchPredicate { PropName = "Price", Value = "700", Operator = SearchPredicate.OperatorType.GreaterThan }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1)); // Only Apple iPhone matches both conditions
        Assert.That(result[0].Name, Is.EqualTo("Apple iPhone"));
    }

    [Test]
    public void Search_WithOrdering_FiltersInvalidOrderingProperties()
    {
        // Arrange
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>
        {
            new SearchOrder { PropName = "Name", Ascending = true },
            new SearchOrder { PropName = "InvalidProperty", Ascending = false }, // This should be filtered out
            new SearchOrder { PropName = "Price", Ascending = false }
        };

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(5)); // All products should be returned
        // The invalid ordering property should be filtered out (tested by not throwing exception)
    }

    [Test]
    public void Search_WithPagination_ReturnsCorrectPageAndSize()
    {
        // Arrange
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>();
        int page = 1;
        int pageSize = 2;

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), page * pageSize, (page + 1) * pageSize, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Skip(offset).Take(limit - offset).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering, page, pageSize);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Should return 2 products for page 1 with pageSize 2
    }

    [Test]
    public void Search_EmptyPredicatesAndOrdering_ReturnsAllProducts()
    {
        // Arrange
        var predicates = new List<SearchPredicate>();
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                _testProducts.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(5)); // All test products should be returned
    }

    [Test]
    public void Search_LikeOperatorWithNullValue_HandlesGracefully()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = null, Price = 100m, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Test", Operator = SearchPredicate.OperatorType.Like }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                productsWithNull.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not match Like operator
    }

    [Test]
    public void Search_EqualsOperatorWithNullValue_HandlesGracefully()
    {
        // Arrange
        var productsWithNull = new List<Product>
        {
            new Product { Id = 1, Name = null, Price = 100m, Description = "Test" }
        };

        var predicates = new List<SearchPredicate>
        {
            new SearchPredicate { PropName = "Name", Value = "Test", Operator = SearchPredicate.OperatorType.Equals }
        };
        var ordering = new List<SearchOrder>();

        _mockProductRepository.Setup(r => r.Where(It.IsAny<System.Func<Product, bool>>(), 0, 20, It.IsAny<System.Func<Product, object>>()))
            .Returns((System.Func<Product, bool> predicate, int offset, int limit, System.Func<Product, object> orderBy) =>
                productsWithNull.Where(predicate).ToList());

        // Act
        var result = _productManager.Search(predicates, ordering);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0)); // Null values should not equal non-null values
    }
}
