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
                    new Product()
                    {
                        Id = 1,
                        CategoryId = 1,
                        Name = "Car",
                        Description = "Whoof",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "51" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "opt2" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "-57" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "strVal" }
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
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "49" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "opt3" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "15" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "strstr" }
                        }
                    },
                    new Product()
                    {
                        Id = 3,
                        CategoryId = 1,
                        Name = "toy car",
                        Description = ":)",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 2m, Weight = 5m, Width = 1m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 1, Value = "120" },
                            new ProductCategoryProperties() { CategoryPropertyId = 2, Value = "opt1" },
                            new ProductCategoryProperties() { CategoryPropertyId = 3, Value = "80" },
                            new ProductCategoryProperties() { CategoryPropertyId = 4, Value = "asdasd" }
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
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 5, Value = "124" },
                            new ProductCategoryProperties() { CategoryPropertyId = 6, Value = "some string" }
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
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = 5, Value = "89" },
                            new ProductCategoryProperties() { CategoryPropertyId = 6, Value = "wireless tech" },
                            new ProductCategoryProperties() { CategoryPropertyId = 7, Value = "very good" }
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
