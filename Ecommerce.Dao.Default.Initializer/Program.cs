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
                var categories = new[]
                {
                    new Category()
                    {
                        Name = "Toys & Games",
                        Description = "Children's toys and gaming products",
                    },
                    new Category()
                    {
                        Name = "Electronics",
                        Description = "Electronic devices and accessories"
                    },
                    new Category()
                    {
                        Name = "Clothing",
                        Description = "Apparel and fashion items"
                    },
                    new Category()
                    {
                        Name = "Books",
                        Description = "Books and educational materials"
                    },
                    new Category()
                    {
                        Name = "Home & Garden",
                        Description = "Home improvement and garden supplies"
                    }
                };
                context.Set<Category>().AddRange(categories);
                context.SaveChanges();
            }
            transaction.Commit();
        }

        // Seed Category CategoryProperties
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<CategoryProperty>().Any())
            {
                var toysCategory = context.Set<Category>().First(c => c.Name == "Toys & Games");
                var electronicsCategory = context.Set<Category>().First(c => c.Name == "Electronics");
                var clothingCategory = context.Set<Category>().First(c => c.Name == "Clothing");
                var booksCategory = context.Set<Category>().First(c => c.Name == "Books");
                var homeGardenCategory = context.Set<Category>().First(c => c.Name == "Home & Garden");
                
                var categoryProperties = new[]
                {
                    // Toys & Games properties
                    new CategoryProperty()
                    {
                        PropertyName = "Age Range",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "0-2 years", "3-5 years", "6-8 years", "9-12 years", "13+ years", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Material",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Plastic", "Wood", "Metal", "Fabric", "Mixed", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Battery Required",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Yes", "No", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Brand",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                    },
                    
                    // Electronics properties
                    new CategoryProperty()
                    {
                        PropertyName = "Screen Size",
                        CategoryId = electronicsCategory.Id,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 100,
                        MinValue = 1,
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Operating System",
                        CategoryId = electronicsCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Windows", "macOS", "Linux", "Android", "iOS", "Other", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Connectivity",
                        CategoryId = electronicsCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "WiFi", "Bluetooth", "USB", "Ethernet", "Wireless", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Warranty Period",
                        CategoryId = electronicsCategory.Id,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 60,
                        MinValue = 0,
                    },
                    
                    // Clothing properties
                    new CategoryProperty()
                    {
                        PropertyName = "Size",
                        CategoryId = clothingCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "XS", "S", "M", "L", "XL", "XXL", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Color",
                        CategoryId = clothingCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Fabric Type",
                        CategoryId = clothingCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Cotton", "Polyester", "Wool", "Silk", "Denim", "Leather", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Gender",
                        CategoryId = clothingCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Men", "Women", "Unisex", "Kids", ""]),
                    },
                    
                    // Books properties
                    new CategoryProperty()
                    {
                        PropertyName = "Genre",
                        CategoryId = booksCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Fiction", "Non-Fiction", "Science", "History", "Biography", "Children", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Page Count",
                        CategoryId = booksCategory.Id,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 2000,
                        MinValue = 1,
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Language",
                        CategoryId = booksCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Turkish", "English", "German", "French", "Spanish", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Publication Year",
                        CategoryId = booksCategory.Id,
                        IsNumber = true,
                        IsRequired = false,
                        MaxValue = 2024,
                        MinValue = 1900,
                    },
                    
                    // Home & Garden properties
                    new CategoryProperty()
                    {
                        PropertyName = "Room Type",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Living Room", "Bedroom", "Kitchen", "Bathroom", "Garden", "Office", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Assembly Required",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Yes", "No", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Power Source",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Electric", "Battery", "Manual", "Solar", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Indoor/Outdoor",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Indoor", "Outdoor", "Both", ""]),
                    }
                };
                context.Set<CategoryProperty>().AddRange(categoryProperties);
                context.SaveChanges();
            }
            transaction.Commit();
        }

        // Seed Products
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<Product>().Any())
            {
                var toysCategory = context.Set<Category>().First(c => c.Name == "Toys & Games");
                var electronicsCategory = context.Set<Category>().First(c => c.Name == "Electronics");
                var clothingCategory = context.Set<Category>().First(c => c.Name == "Clothing");
                var booksCategory = context.Set<Category>().First(c => c.Name == "Books");
                var homeGardenCategory = context.Set<Category>().First(c => c.Name == "Home & Garden");
                
                // Get category properties for reference
                var ageRangeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Age Range");
                var materialProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Material");
                var batteryProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Battery Required");
                var brandProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Brand");
                var screenSizeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Screen Size");
                var osProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Operating System");
                var connectivityProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Connectivity");
                var warrantyProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Warranty Period");
                var sizeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Size");
                var colorProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Color");
                var fabricProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Fabric Type");
                var genderProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Gender");
                var genreProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Genre");
                var pageCountProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Page Count");
                var languageProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Language");
                var publicationYearProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Publication Year");
                var roomTypeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Room Type");
                var assemblyProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Assembly Required");
                var powerSourceProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Power Source");
                var indoorOutdoorProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Indoor/Outdoor");
                
                var products = new[]
                {
                    // Toys & Games products
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Remote Control Car",
                        Description = "High-speed remote control racing car with LED lights",
                        Dimensions = new Dimensions()
                        {
                            Depth = 25m, Height = 12m, Weight = 0.8m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = ageRangeProp.Id, Value = "6-8 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = materialProp.Id, Value = "Plastic" },
                            new ProductCategoryProperties() { CategoryPropertyId = batteryProp.Id, Value = "Yes" },
                            new ProductCategoryProperties() { CategoryPropertyId = brandProp.Id, Value = "SpeedRacer" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Wooden Building Blocks",
                        Description = "Educational wooden blocks for creative building",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 20m, Weight = 1.2m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = ageRangeProp.Id, Value = "3-5 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = materialProp.Id, Value = "Wood" },
                            new ProductCategoryProperties() { CategoryPropertyId = batteryProp.Id, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = brandProp.Id, Value = "EduToys" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Plush Teddy Bear",
                        Description = "Soft and cuddly teddy bear for children",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 35m, Weight = 0.5m, Width = 25m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = ageRangeProp.Id, Value = "0-2 years" },
                            new ProductCategoryProperties() { CategoryPropertyId = materialProp.Id, Value = "Fabric" },
                            new ProductCategoryProperties() { CategoryPropertyId = batteryProp.Id, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = brandProp.Id, Value = "CuddleBear" }
                        }
                    },
                    
                    // Electronics products
                    new Product()
                    {
                        CategoryId = electronicsCategory.Id,
                        Name = "Gaming Laptop",
                        Description = "High performance laptop for gaming and professional work",
                        Dimensions = new Dimensions()
                        {
                            Depth = 25m, Height = 2.5m, Weight = 2.3m, Width = 35m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = screenSizeProp.Id, Value = "15.6" },
                            new ProductCategoryProperties() { CategoryPropertyId = osProp.Id, Value = "Windows" },
                            new ProductCategoryProperties() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                            new ProductCategoryProperties() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = electronicsCategory.Id,
                        Name = "Wireless Mouse",
                        Description = "Ergonomic wireless mouse with precision tracking",
                        Dimensions = new Dimensions()
                        {
                            Depth = 12m, Height = 4m, Weight = 0.1m, Width = 7m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = osProp.Id, Value = "Other" },
                            new ProductCategoryProperties() { CategoryPropertyId = connectivityProp.Id, Value = "Wireless" },
                            new ProductCategoryProperties() { CategoryPropertyId = warrantyProp.Id, Value = "12" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = electronicsCategory.Id,
                        Name = "Smartphone",
                        Description = "Latest generation smartphone with advanced camera",
                        Dimensions = new Dimensions()
                        {
                            Depth = 0.8m, Height = 15m, Weight = 0.18m, Width = 7m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = screenSizeProp.Id, Value = "6.1" },
                            new ProductCategoryProperties() { CategoryPropertyId = osProp.Id, Value = "Android" },
                            new ProductCategoryProperties() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                            new ProductCategoryProperties() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
                        }
                    },
                    
                    // Clothing products
                    new Product()
                    {
                        CategoryId = clothingCategory.Id,
                        Name = "Cotton T-Shirt",
                        Description = "Comfortable cotton t-shirt for everyday wear",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 70m, Weight = 0.2m, Width = 50m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = sizeProp.Id, Value = "M" },
                            new ProductCategoryProperties() { CategoryPropertyId = colorProp.Id, Value = "Blue" },
                            new ProductCategoryProperties() { CategoryPropertyId = fabricProp.Id, Value = "Cotton" },
                            new ProductCategoryProperties() { CategoryPropertyId = genderProp.Id, Value = "Unisex" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = clothingCategory.Id,
                        Name = "Denim Jeans",
                        Description = "Classic blue denim jeans with modern fit",
                        Dimensions = new Dimensions()
                        {
                            Depth = 2m, Height = 100m, Weight = 0.6m, Width = 40m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = sizeProp.Id, Value = "L" },
                            new ProductCategoryProperties() { CategoryPropertyId = colorProp.Id, Value = "Dark Blue" },
                            new ProductCategoryProperties() { CategoryPropertyId = fabricProp.Id, Value = "Denim" },
                            new ProductCategoryProperties() { CategoryPropertyId = genderProp.Id, Value = "Men" }
                        }
                    },
                    
                    // Books products
                    new Product()
                    {
                        CategoryId = booksCategory.Id,
                        Name = "Programming Guide",
                        Description = "Comprehensive guide to modern programming languages",
                        Dimensions = new Dimensions()
                        {
                            Depth = 3m, Height = 24m, Weight = 0.8m, Width = 17m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = genreProp.Id, Value = "Science" },
                            new ProductCategoryProperties() { CategoryPropertyId = pageCountProp.Id, Value = "450" },
                            new ProductCategoryProperties() { CategoryPropertyId = languageProp.Id, Value = "English" },
                            new ProductCategoryProperties() { CategoryPropertyId = publicationYearProp.Id, Value = "2023" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = booksCategory.Id,
                        Name = "Children's Story Book",
                        Description = "Colorful illustrated story book for young readers",
                        Dimensions = new Dimensions()
                        {
                            Depth = 1m, Height = 21m, Weight = 0.3m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = genreProp.Id, Value = "Children" },
                            new ProductCategoryProperties() { CategoryPropertyId = pageCountProp.Id, Value = "32" },
                            new ProductCategoryProperties() { CategoryPropertyId = languageProp.Id, Value = "Turkish" },
                            new ProductCategoryProperties() { CategoryPropertyId = publicationYearProp.Id, Value = "2022" }
                        }
                    },
                    
                    // Home & Garden products
                    new Product()
                    {
                        CategoryId = homeGardenCategory.Id,
                        Name = "Garden Hose",
                        Description = "Flexible garden hose for watering plants",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 30m, Weight = 2.5m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = roomTypeProp.Id, Value = "Garden" },
                            new ProductCategoryProperties() { CategoryPropertyId = assemblyProp.Id, Value = "No" },
                            new ProductCategoryProperties() { CategoryPropertyId = powerSourceProp.Id, Value = "Manual" },
                            new ProductCategoryProperties() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "Outdoor" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = homeGardenCategory.Id,
                        Name = "LED Desk Lamp",
                        Description = "Adjustable LED desk lamp with touch controls",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 45m, Weight = 1.2m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperties>()
                        {
                            new ProductCategoryProperties() { CategoryPropertyId = roomTypeProp.Id, Value = "Office" },
                            new ProductCategoryProperties() { CategoryPropertyId = assemblyProp.Id, Value = "Yes" },
                            new ProductCategoryProperties() { CategoryPropertyId = powerSourceProp.Id, Value = "Electric" },
                            new ProductCategoryProperties() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "Indoor" }
                        }
                    }
                };
                context.Set<Product>().AddRange(products);
                context.SaveChanges();
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
