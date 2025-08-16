using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Default;

public class Seed
{
    public static void Main(string[] args)
    {
        using var context = new DefaultDbContext();
        SeedData(context);
    }

    public static void SeedData(DefaultDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();
        // Seed Categories
        if (!context.Set<Category>().Any())
        {
            var categories = new[]
            {
                new Category()
                {
                    Id = 1,
                    Name = "some category",
                    Description = "desc",
                },
                new Category()
                {
                    Id = 2,
                    Name = "other one",
                    Description = "d"
                }
            };
            context.Set<Category>().AddRange(categories);
            context.SaveChanges();
        }

        // Seed Category Properties
        if (!context.Set<Category.CategoryProperty>().Any())
        {
            var categoryProperties = new[]
            {
                new Category.CategoryProperty()
                {
                    Id = 1,
                    PropertyName = "numberProperty",
                    CategoryId = 1,
                    IsNumber = true,
                    IsRequired = true,
                    MaxValue = 100,
                    MinValue = 0,
                },
                new Category.CategoryProperty()
                {
                    Id = 2,
                    PropertyName = "enumProperty",
                    CategoryId = 1,
                    EnumValues = string.Join(',', ["", "opt1", "opt2", "opt3", ""]),
                    IsRequired = true,
                },
                new Category.CategoryProperty()
                {
                    Id = 3,
                    PropertyName = "optionalNumberProperty",
                    CategoryId = 1,
                    IsNumber = true,
                    IsRequired = false,
                    MaxValue = 1000000,
                    MinValue = -100000,
                },
                new Category.CategoryProperty()
                {
                    Id = 4,
                    PropertyName = "stringProperty",
                    CategoryId = 1,
                    IsNumber = false,
                    IsRequired = false,
                },
                new Category.CategoryProperty()
                {
                    Id = 5,
                    PropertyName = "yetAnotherNumberProperty",
                    CategoryId = 2,
                    IsNumber = true,
                    IsRequired = false,
                    MaxValue = 1000000,
                    MinValue = -100000,
                },
                new Category.CategoryProperty()
                {
                    Id = 6,
                    PropertyName = "yetAnotherStringProperty",
                    CategoryId = 2,
                    IsNumber = false,
                    IsRequired = true,
                },
                new Category.CategoryProperty()
                {
                    Id = 7,
                    PropertyName = "yetAnotherEnum",
                    CategoryId = 2,
                    IsNumber = false,
                    IsRequired = false,
                    EnumValues = string.Join(',', ["", "good", "very good", "meh", ""])
                }
            };
            context.Set<Category.CategoryProperty>().AddRange(categoryProperties);
            context.SaveChanges();
        }

        // Seed Products
        if (!context.Set<Product>().Any())
        {
            var products = new[]
            {
                new Product()
                {
                    Id = 1,
                    CategoryId = 1,
                    Name = "Car",
                    Description = "Whoof",
                    Dimensions = new Dimensions()
                    {
                        Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                    }
                },
                new Product()
                {
                    Id = 2,
                    CategoryId = 1,
                    Name = "Toy",
                    Description = "...",
                    Dimensions = new Dimensions()
                    {
                        Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                    }
                },
                new Product()
                {
                    Id = 3,
                    Name = "toy car",
                    Description = ":)",
                    Dimensions = new Dimensions()
                    {
                        Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                    }
                },
                new Product()
                {
                    Id = 4,
                    CategoryId = 2,
                    Name = "Gaming Laptop",
                    Description = "High performance laptop for gaming",
                    Dimensions = new Dimensions()
                    {
                        Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                    }
                },
                new Product()
                {
                    Id = 5,
                    CategoryId = 2,
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse",
                    Dimensions = new Dimensions()
                    {
                        Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                    }
                }
            };
            context.Set<Product>().AddRange(products);
            context.SaveChanges();
        }

        // Seed Product Category Properties
        if (!context.Set<ProductCategoryProperties>().Any())
        {
            var productCategoryProperties = new[]
            {
                new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 1, Value = "51" },
                new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 1, Value = "opt2" },
                new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 1, Value = "-57" },
                new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 1, Value = "strVal" },
                new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 2, Value = "49" },
                new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 2, Value = "opt3" },
                new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 2, Value = "15" },
                new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 2, Value = "strstr" },
                new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 3, Value = "120" },
                new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 3, Value = "opt1" },
                new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 3, Value = "80" },
                new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 3, Value = "asdasd" },
                new ProductCategoryProperties() { CategoryPropertyId = 5, ProductId = 4, Value = "124" },
                new ProductCategoryProperties() { CategoryPropertyId = 6, ProductId = 4, Value = "some string" },
                new ProductCategoryProperties() { CategoryPropertyId = 5, ProductId = 5, Value = "89" },
                new ProductCategoryProperties() { CategoryPropertyId = 6, ProductId = 5, Value = "wireless tech" },
                new ProductCategoryProperties() { CategoryPropertyId = 7, ProductId = 5, Value = "very good" }
            };
            context.Set<ProductCategoryProperties>().AddRange(productCategoryProperties);
            context.SaveChanges();
        }
    }
}
