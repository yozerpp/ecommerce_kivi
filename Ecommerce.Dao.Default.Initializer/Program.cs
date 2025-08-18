#define DEV
using System.Reflection;
using Ecommerce.Dao.Initializer;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using log4net.Config;
using log4net.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace Ecommerce.Dao.Default.Initializer;
internal static class Initializer
{
    [STAThread]
    static void Main(string[] args) {

        Setup();
        CreateDb();
        using (var ctx = new DefaultDbContext(_dbContextOptions)) {
            SeedCustom(ctx);
        }
        InitDb();
        CreateViews();
    }    
    private static DbContextOptions<DefaultDbContext> _dbContextOptions;
    private const bool Skip = false;

    private static void Setup() {
        _dbContextOptions = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;",
                c=>c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.FullName))
            .EnableSensitiveDataLogging(false).EnableServiceProviderCaching().ConfigureWarnings(w=>w.Ignore(CoreEventId.DetachedLazyLoadingWarning)).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options;
    }
    private static void CreateDb() {
        using var context = new DefaultDbContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        var migrator = context.Database.GetService<IMigrator>();
        foreach (var migration in context.Database.GetMigrations().Where(m=>!m.Contains("View"))){
            migrator.Migrate(migration);
        }
        context.SaveChanges();
    }
    private static void CreateViews() {
        using var context = new DefaultDbContext(_dbContextOptions);
        var migrator = context.Database.GetService<IMigrator>();
        foreach (var migration in context.Database.GetMigrations().Where(m=>m.Contains("View"))){
            migrator.Migrate(migration);
        }
        context.SaveChanges();
    }
    #if DEV
    private const int CustomerCount = 1000;
    private const int SellerCount = 200;
    private const int ProductCount = SellerCount*5;
    private const int ProductOfferCount = SellerCount * ProductCount / 10;
    private const int CouponCount = SellerCount*10;
    private const int ProductReviewCount = ProductCount * CustomerCount / 10;
    private const int ReviewCommentCount = ProductReviewCount*2;
    private const int ReviewVoteCount = ReviewCommentCount + ProductReviewCount;
    private const int SessionCount = 5*(CustomerCount + SellerCount + StaffCount + AnonymousUserCount);
    private const int CartCount = SessionCount;
    private const int OrderItemCount = OrderCount * ProductOfferCount / 3000;
    private const int OrderCount = (CustomerCount + AnonymousUserCount)* 3;
    private const int AnonymousUserCount = CustomerCount * 2;
    private const int CategoryCount = 30;
    private const int StaffCount = 20;
    private const int PermissionCount = 5;
    private const int PermissionClaimCount = PermissionCount * StaffCount / 2;
    private const int ProductFavorCount = ProductCount * CustomerCount / 100;
    private const int SellerFavorCount = SellerCount * CustomerCount / 100;
    private const int CategoryPropertyCount = CategoryCount * 4;
    private const int ProductCategoryPropertyCount = CategoryPropertyCount * ProductCount / 10;
    private const int RefundRequestCount = ProductOfferCount * 6;
#else
    private const int CustomerCount = 1000;
    private const int SellerCount = 200;
    private const int ProductCount = SellerCount*5;
    private const int ProductOfferCount = SellerCount * ProductCount / 10;
    private const int CouponCount = SellerCount*10;
    private const int ProductReviewCount = ProductCount * CustomerCount / 10;
    private const int ReviewCommentCount = ProductReviewCount*2;
    private const int ReviewVoteCount = ReviewCommentCount + ProductReviewCount;
    private const int SessionCount = 5*(CustomerCount + SellerCount + StaffCount);
    private const int CartCount = SessionCount;
    private const int OrderItemCount = OrderCount * ProductOfferCount / 3000;
    private const int OrderCount = (CustomerCount + AnonymousUserCount)* 3;
    private const int AnonymousUserCount = CustomerCount * 2;
    private const int CategoryCount = 30;
    private const int StaffCount = 20;
    private const int PermissionCount = 5;
    private const int PermissionClaimCount = PermissionCount * StaffCount / 2;
    private const int ProductFavorCount = ProductCount * CustomerCount / 100;
    private const int SellerFavorCount = SellerCount * CustomerCount / 100;
    private const int CategoryPropertyCount = CategoryCount * 4;
    private const int ProductCategoryPropertyCount = CategoryPropertyCount * ProductCount / 10;
    private const int RefundRequestCount = ProductOfferCount * 6;
#endif

    private static void InitDb() {
        using var initializer = new DatabaseInitializer(
            typeof(DefaultDbContext),
           _dbContextOptions,
            new Dictionary<Type, int?> {
                {typeof(Customer), CustomerCount },
                {typeof(Seller), SellerCount },
                {typeof(Staff), StaffCount},
                {typeof(Permission), PermissionCount},
                {typeof(PermissionClaim), PermissionClaimCount},
                {typeof(Product), ProductCount },
                {typeof(ProductOffer), ProductOfferCount},
                {typeof(Coupon),CouponCount},
                {typeof(ProductReview), ProductReviewCount},
                {typeof(ReviewComment), ReviewCommentCount},
                {typeof(ReviewVote), ReviewVoteCount},
                {typeof(Cart), CartCount},
                {typeof(CategoryProperty),CategoryPropertyCount},
                {typeof(ProductCategoryProperties),ProductCategoryPropertyCount},
                // {typeof(RefundRequest), RefundRequestCount},
                // { typeof(CartItem), CartItemCount},
                { typeof(Session), SessionCount},
                {typeof(OrderItem), OrderItemCount},
                { typeof(Order), OrderCount},
                {typeof(Category), CategoryCount},
                {typeof(ImageProduct), ProductCount*5},
                {typeof(Payment), OrderCount},
                {typeof(Shipment), OrderItemCount+1000},
                {typeof(AnonymousUser), AnonymousUserCount},
                {typeof(ProductFavor), ProductFavorCount},
                {typeof(SellerFavor), SellerFavorCount},
                {typeof(Image), 100}
            }, defaultCount:0
        );
        initializer.initialize();
    }
    public static void SeedCustom(DefaultDbContext context) {
        // Ensure database is created
        // Seed Category
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<Category>().Any())
            {
                context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [data].[Category] ON");
                var Category = new[]
                {
                    new Category()
                    {
                        Id = 1,
                        Name = "Toys & Games",
                        Description = "Children's toys and gaming products",
                    },
                    new Category()
                    {
                        Id = 2,
                        Name = "Electronics",
                        Description = "Electronic devices and accessories"
                    },
                    new Category()
                    {
                        Id = 3,
                        Name = "Clothing",
                        Description = "Apparel and fashion items"
                    },
                    new Category()
                    {
                        Id = 4,
                        Name = "Books",
                        Description = "Books and educational materials"
                    },
                    new Category()
                    {
                        Id = 5,
                        Name = "Home & Garden",
                        Description = "Home improvement and garden supplies"
                    }
                };
                context.Set<Category>().AddRange(Category);
                context.SaveChanges();
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [data].[Category] OFF");
            }
            transaction.Commit();
        }

        // Seed Category CategoryProperties
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<CategoryProperty>().Any())
            {
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [data].[CategoryProperty] ON");
                var categoryProperties = new[]
                {
                    new CategoryProperty()
                    {
                        Id = 1,
                        PropertyName = "numberProperty",
                        CategoryId = 1,
                        IsNumber = true,
                        IsRequired = true,
                        MaxValue = 100,
                        MinValue = 0,
                    },
                    new CategoryProperty()
                    {
                        Id = 2,
                        PropertyName = "enumProperty",
                        CategoryId = 1,
                        EnumValues = string.Join('|', ["", "opt1", "opt2", "opt3", ""]),
                        IsRequired = true,
                    },
                    new CategoryProperty()
                    {
                        Id = 3,
                        PropertyName = "optionalNumberProperty",
                        CategoryId = 1,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 1000000,
                        MinValue = -100000,
                    },
                    new CategoryProperty()
                    {
                        Id = 4,
                        PropertyName = "stringProperty",
                        CategoryId = 1,
                        IsNumber = false,
                        IsRequired = false,
                    },
                    new CategoryProperty()
                    {
                        Id = 5,
                        PropertyName = "yetAnotherNumberProperty",
                        CategoryId = 2,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 1000000,
                        MinValue = -100000,
                    },
                    new CategoryProperty()
                    {
                        Id = 6,
                        PropertyName = "yetAnotherStringProperty",
                        CategoryId = 2,
                        IsNumber = false,
                        IsRequired = true,
                    },
                    new CategoryProperty()
                    {
                        Id = 7,
                        PropertyName = "yetAnotherEnum",
                        CategoryId = 2,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "good", "very good", "meh", ""])
                    }
                };
                context.Set<CategoryProperty>().AddRange(categoryProperties);
                context.SaveChanges();
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [data].[CategoryProperty] OFF");
            }
            transaction.Commit();
        }

        // Seed Products
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<Product>().Any())
            {
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [data].[Product] ON");
                var products = new[]
                {
                    // Toys & Games products
                    new Product()
                    {
                        Id = 1,
                        CategoryId = 1,
                        Name = "Remote Control Car",
                        Description = "High-speed remote control racing car with LED lights",
                        Dimensions = new Dimensions()
                        {
                            Depth = 25m, Height = 12m, Weight = 0.8m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "6-8 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "Plastic" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "Yes" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "SpeedRacer" }
                        }
                    },
                    new Product()
                    {
                        Id = 2,
                        CategoryId = 1,
                        Name = "Wooden Building Blocks",
                        Description = "Educational wooden blocks for creative building",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 20m, Weight = 1.2m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "3-5 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "Wood" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "EduToys" }
                        }
                    },
                    new Product()
                    {
                        Id = 3,
                        CategoryId = 1,
                        Name = "Plush Teddy Bear",
                        Description = "Soft and cuddly teddy bear for children",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 35m, Weight = 0.5m, Width = 25m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "0-2 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "Fabric" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "CuddleBear" }
                        }
                    },
                    
                    // Electronics products
                    new Product()
                    {
                        Id = 4,
                        CategoryId = 2,
                        Name = "Gaming Laptop",
                        Description = "High performance laptop for gaming and professional work",
                        Dimensions = new Dimensions()
                        {
                            Depth = 25m, Height = 2.5m, Weight = 2.3m, Width = 35m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 5, Value = "15.6" },
                            new ProductCategoryProperties() { CategoryPropertyId = 6, Value = "Windows" },
                            new ProductCategoryProperties() { CategoryPropertyId = 7, Value = "WiFi" },
                            new ProductCategoryProperties() { CategoryPropertyId = 8, Value = "24" }
                        }
                    },
                    new Product()
                    {
                        Id = 5,
                        CategoryId = 2,
                        Name = "Wireless Mouse",
                        Description = "Ergonomic wireless mouse with precision tracking",
                        Dimensions = new Dimensions()
                        {
                            Depth = 12m, Height = 4m, Weight = 0.1m, Width = 7m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 6, Value = "Other" },
                            new ProductCategoryProperties() { CategoryPropertyId = 7, Value = "Wireless" },
                            new ProductCategoryProperties() { CategoryPropertyId = 8, Value = "12" }
                        }
                    },
                    new Product()
                    {
                        Id = 6,
                        CategoryId = 2,
                        Name = "Smartphone",
                        Description = "Latest generation smartphone with advanced camera",
                        Dimensions = new Dimensions()
                        {
                            Depth = 0.8m, Height = 15m, Weight = 0.18m, Width = 7m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 5, Value = "6.1" },
                            new ProductCategoryProperties() { CategoryPropertyId = 6, Value = "Android" },
                            new ProductCategoryProperties() { CategoryPropertyId = 7, Value = "WiFi" },
                            new ProductCategoryProperties() { CategoryPropertyId = 8, Value = "24" }
                        }
                    },
                    
                    // Clothing products
                    new Product()
                    {
                        Id = 7,
                        CategoryId = 3,
                        Name = "Cotton T-Shirt",
                        Description = "Comfortable cotton t-shirt for everyday wear",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 70m, Weight = 0.2m, Width = 50m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 9, Value = "M" },
                            new ProductCategoryProperties() { CategoryPropertyId = 10, Value = "Blue" },
                            new ProductCategoryProperties() { CategoryPropertyId = 11, Value = "Cotton" },
                            new ProductCategoryProperties() { CategoryPropertyId = 12, Value = "Unisex" }
                        }
                    },
                    new Product()
                    {
                        Id = 8,
                        CategoryId = 3,
                        Name = "Denim Jeans",
                        Description = "Classic blue denim jeans with modern fit",
                        Dimensions = new Dimensions()
                        {
                            Depth = 2m, Height = 100m, Weight = 0.6m, Width = 40m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 9, Value = "L" },
                            new ProductCategoryProperties() { CategoryPropertyId = 10, Value = "Dark Blue" },
                            new ProductCategoryProperties() { CategoryPropertyId = 11, Value = "Denim" },
                            new ProductCategoryProperties() { CategoryPropertyId = 12, Value = "Men" }
                        }
                    },
                    
                    // Books products
                    new Product()
                    {
                        Id = 9,
                        CategoryId = 4,
                        Name = "Programming Guide",
                        Description = "Comprehensive guide to modern programming languages",
                        Dimensions = new Dimensions()
                        {
                            Depth = 3m, Height = 24m, Weight = 0.8m, Width = 17m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 13, Value = "Science" },
                            new ProductCategoryProperties() { CategoryPropertyId = 14, Value = "450" },
                            new ProductCategoryProperties() { CategoryPropertyId = 15, Value = "English" },
                            new ProductCategoryProperties() { CategoryPropertyId = 16, Value = "2023" }
                        }
                    },
                    new Product()
                    {
                        Id = 10,
                        CategoryId = 4,
                        Name = "Children's Story Book",
                        Description = "Colorful illustrated story book for young readers",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 21m, Weight = 0.3m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 13, Value = "Children" },
                            new ProductCategoryProperties() { CategoryPropertyId = 14, Value = "32" },
                            new ProductCategoryProperties() { CategoryPropertyId = 15, Value = "Turkish" },
                            new ProductCategoryProperties() { CategoryPropertyId = 16, Value = "2022" }
                        }
                    },
                    
                    // Home & Garden products
                    new Product()
                    {
                        Id = 11,
                        CategoryId = 5,
                        Name = "Garden Hose",
                        Description = "Flexible garden hose for watering plants",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 30m, Weight = 2.5m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 17, Value = "Garden" },
                            new ProductCategoryProperties() { CategoryPropertyId = 18, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = 19, Value = "Manual" },
                            new ProductCategoryProperties() { CategoryPropertyId = 20, Value = "Outdoor" }
                        }
                    },
                    new Product()
                    {
                        Id = 12,
                        CategoryId = 5,
                        Name = "LED Desk Lamp",
                        Description = "Adjustable LED desk lamp with touch controls",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 45m, Weight = 1.2m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 17, Value = "Office" },
                            new ProductCategoryProperties() { CategoryPropertyId = 18, Value = "Yes" },
                            new ProductCategoryProperties() { CategoryPropertyId = 19, Value = "Electric" },
                            new ProductCategoryProperties() { CategoryPropertyId = 20, Value = "Indoor" }
                        }
                    }
                };
                context.Set<Product>().AddRange(products);
                context.SaveChanges();
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [data].[Product] OFF");
            }
            transaction.Commit();
        }

        // Removed the separate seed for ProductCategoryProperties as it's now part of Product seed
        // using (var transaction = context.Database.BeginTransaction())
        // {
        //     if (!context.Set<ProductCategoryProperties>().Any())
        //     {
        //         var productCategoryProperties = new[]
        //         {
        //             new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 1, Value = "51" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 1, Value = "opt2" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 1, Value = "-57" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 1, Value = "strVal" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 2, Value = "49" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 2, Value = "opt3" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 2, Value = "15" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 2, Value = "strstr" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 1, ProductId = 3, Value = "120" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 2, ProductId = 3, Value = "opt1" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 3, ProductId = 3, Value = "80" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 4, ProductId = 3, Value = "asdasd" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 5, ProductId = 4, Value = "124" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 6, ProductId = 4, Value = "some string" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 5, ProductId = 5, Value = "89" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 6, ProductId = 5, Value = "wireless tech" },
        //             new ProductCategoryProperties() { CategoryPropertyId = 7, ProductId = 5, Value = "very good" }
        //         };
        //         context.Set<ProductCategoryProperties>().AddRange(productCategoryProperties);
        //         context.SaveChanges();
        //     }
        //     transaction.Commit();
        // }
    }

    
}
