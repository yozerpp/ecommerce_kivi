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
            ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            ctx.ChangeTracker.AutoDetectChangesEnabled = true;
            SeedCustom(ctx);
        }
        InitDb();
        CreateViews();
    }    
    private static DbContextOptions<DefaultDbContext> _dbContextOptions;

    private static void Setup() {
        _dbContextOptions = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;",
                c=>c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.FullName).CommandTimeout(600))
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
                {typeof(CardPayment), OrderCount + 1000},
                {typeof(ReviewVote), ReviewVoteCount},
                {typeof(Cart), CartCount},
                // {typeof(CategoryProperty),CategoryPropertyCount},
                // {typeof(ProductCategoryProperty),ProductCategoryPropertyCount},
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
            }, config:new Config(){
                FetchRealAddresses = true,
                AddressFetcherApiKey = "d4907aae36c14038bc231576e0b5e8ca"
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
                        MaxValue = 99,
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "6-8 years" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Plastic" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "Yes" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "SpeedRacer" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "3-5 years" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Wood" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "No" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "EduToys" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "0-2 years" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Fabric" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "No" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "CuddleBear" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = screenSizeProp.Id, Value = "15.6" },
                            new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Windows" },
                            new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                            new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Other" },
                            new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "Wireless" },
                            new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "12" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = screenSizeProp.Id, Value = "6.1" },
                            new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Android" },
                            new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                            new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = sizeProp.Id, Value = "M" },
                            new ProductCategoryProperty() { CategoryPropertyId = colorProp.Id, Value = "Blue" },
                            new ProductCategoryProperty() { CategoryPropertyId = fabricProp.Id, Value = "Cotton" },
                            new ProductCategoryProperty() { CategoryPropertyId = genderProp.Id, Value = "Unisex" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = clothingCategory.Id,
                        Name = "Denim Jeans",
                        Description = "Classic blue denim jeans with modern fit",
                        Dimensions = new Dimensions()
                        {
                            Depth = 2m, Height = 20m, Weight = 0.6m, Width = 40m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = sizeProp.Id, Value = "L" },
                            new ProductCategoryProperty() { CategoryPropertyId = colorProp.Id, Value = "Dark Blue" },
                            new ProductCategoryProperty() { CategoryPropertyId = fabricProp.Id, Value = "Denim" },
                            new ProductCategoryProperty() { CategoryPropertyId = genderProp.Id, Value = "Men" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = genreProp.Id, Value = "Science" },
                            new ProductCategoryProperty() { CategoryPropertyId = pageCountProp.Id, Value = "450" },
                            new ProductCategoryProperty() { CategoryPropertyId = languageProp.Id, Value = "English" },
                            new ProductCategoryProperty() { CategoryPropertyId = publicationYearProp.Id, Value = "2023" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = genreProp.Id, Value = "Children" },
                            new ProductCategoryProperty() { CategoryPropertyId = pageCountProp.Id, Value = "32" },
                            new ProductCategoryProperty() { CategoryPropertyId = languageProp.Id, Value = "Turkish" },
                            new ProductCategoryProperty() { CategoryPropertyId = publicationYearProp.Id, Value = "2022" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = roomTypeProp.Id, Value = "Garden" },
                            new ProductCategoryProperty() { CategoryPropertyId = assemblyProp.Id, Value = "No" },
                            new ProductCategoryProperty() { CategoryPropertyId = powerSourceProp.Id, Value = "Manual" },
                            new ProductCategoryProperty() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "Outdoor" }
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
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = roomTypeProp.Id, Value = "Office" },
                            new ProductCategoryProperty() { CategoryPropertyId = assemblyProp.Id, Value = "Yes" },
                            new ProductCategoryProperty() { CategoryPropertyId = powerSourceProp.Id, Value = "Electric" },
                            new ProductCategoryProperty() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "Indoor" }
                        }
                    }
                };
                context.Set<Product>().AddRange(products);
                context.SaveChanges();
            }
            transaction.Commit();
        }

        // Seed Sellers
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<Seller>().Any())
            {
                var sellers = new[]
                {
                    new Seller()
                    {
                        Email = "toystore@example.com",
                        NormalizedEmail = "TOYSTORE@EXAMPLE.COM",
                        PasswordHash = "hashedpassword123",
                        FirstName = "John",
                        LastName = "Smith",
                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "ToyWorld Store",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551234567" },
                        Address = new Address()
                        {
                            Line1 = "123 Toy Street",
                            Line2 = "Suite 100",
                            District = "Kadikoy",
                            City = "Istanbul",
                            Country = "Türkiye",
                            ZipCode = "34710"
                        }
                    },
                    new Seller()
                    {
                        Email = "techgadgets@example.com",
                        NormalizedEmail = "TECHGADGETS@EXAMPLE.COM",
                        PasswordHash = "hashedpassword456",
                        FirstName = "Sarah",
                        LastName = "Johnson",                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "Tech Gadgets Pro",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5559876543" },
                        Address = new Address()
                        {
                            Line1 = "456 Electronics Ave",
                            District = "Besiktas",
                            City = "Istanbul",
                            Country = "Türkiye",
                            ZipCode = "34349"
                        }
                    },
                    new Seller()
                    {
                        Email = "fashionhub@example.com",
                        NormalizedEmail = "FASHIONHUB@EXAMPLE.COM",
                        PasswordHash = "hashedpassword789",
                        FirstName = "Michael",
                        LastName = "Brown",                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "Fashion Hub",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551122334" },
                        Address = new Address()
                        {
                            Line1 = "789 Fashion Blvd",
                            District = "Sisli",
                            City = "Istanbul",
                            Country = "Türkiye",
                            ZipCode = "34394"
                        }
                    }
                };
                context.Set<Seller>().AddRange(sellers);
                context.SaveChanges();
            }
            transaction.Commit();
        }

        // Seed ProductOffers
        using (var transaction = context.Database.BeginTransaction())
        {
            if (!context.Set<ProductOffer>().Any())
            {
                var products = context.Set<Product>().ToList();
                var sellers = context.Set<Seller>().ToList();
                
                var productOffers = new List<ProductOffer>();
                
                // Create offers for each product from different sellers
                foreach (var product in products)
                {
                    foreach (var seller in sellers)
                    {
                        var basePrice = 50m + (product.Id * 10m) + (seller.Id * 5m);
                        
                        productOffers.Add(new ProductOffer()
                        {
                            ProductId = product.Id,
                            SellerId = seller.Id,
                            Price = basePrice,
                            Discount = 0.85m + (seller.Id * 0.05m), // 85%, 90%, 95%
                            Stock = (uint)(100 + (seller.Id * 20)) // 120, 140, 160
                        });
                    }
                }
                
                context.Set<ProductOffer>().AddRange(productOffers);
                context.SaveChanges();
            }
            transaction.Commit();
        }

        // Seed ProductOptions
        using (var transaction = context.Database.BeginTransaction())
        {
            if (context.Set<ProductOffer>().All(o=>o.Options.Count==0))
            {
                var productOffers = context.Set<ProductOffer>().ToList();
                var productCategoryProperties = context.Set<ProductCategoryProperty>()
                    .Include(pcp => pcp.CategoryProperty)
                    .ToList();
                
                
                foreach (var offer in productOffers)
                {
                    // Find category properties for this product
                    var relevantProperties = productCategoryProperties
                        .Where(pcp => pcp.ProductId == offer.ProductId)
                        .ToList();
                    
                    foreach (var categoryProperty in relevantProperties)
                    {
                        var options = new List<string>();
                        var currentValue = categoryProperty.Value;
                        
                        // Check if this is an enum property
                        if (!string.IsNullOrEmpty(categoryProperty.CategoryProperty.EnumValues) && 
                            categoryProperty.CategoryProperty.EnumValues.Contains("|"))
                        {
                            // For enum properties: create subset of enum values with current value first
                            var enumValues = categoryProperty.CategoryProperty.EnumValues
                                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                                .Where(v => !string.IsNullOrWhiteSpace(v))
                                .ToList();
                            
                            if (enumValues.Count > 1)
                            {
                                // Add current value first
                                if (!string.IsNullOrEmpty(currentValue) && enumValues.Contains(currentValue))
                                {
                                    options.Add(currentValue);
                                }
                                
                                // Add a subset of other enum values (not all)
                                var otherValues = enumValues.Where(v => v != currentValue).Take(2).ToList();
                                options.AddRange(otherValues);
                            }
                        }
                        else
                        {
                            // For non-enum properties: create variations based on the current value
                            if (!string.IsNullOrEmpty(currentValue))
                            {
                                // Add current value first
                                options.Add(currentValue);
                                
                                if (categoryProperty.CategoryProperty.IsNumber)
                                {
                                    // For number properties: create numeric variations
                                    if (decimal.TryParse(currentValue, out var numValue))
                                    {
                                        // Add some variations around the current number
                                        var variation1 = (numValue * 0.8m).ToString("0.##");
                                        var variation2 = (numValue * 1.2m).ToString("0.##");
                                        var variation3 = (numValue + 10).ToString("0.##");
                                        
                                        options.Add(variation1);
                                        options.Add(variation2);
                                        options.Add(variation3);
                                    }
                                }
                                else
                                {
                                    // For string properties: create string variations
                                    switch (categoryProperty.CategoryProperty.PropertyName.ToLower())
                                    {
                                        case "brand":
                                            options.AddRange(new[] { "Generic", "Premium", "Deluxe" });
                                            break;
                                        case "color":
                                            options.AddRange(new[] { "Black", "White", "Gray" });
                                            break;
                                        default:
                                            // Generic string variations
                                            options.AddRange(new[] { $"{currentValue} Plus", $"{currentValue} Pro", $"Standard {currentValue}" });
                                            break;
                                    }
                                }
                            }
                        }

                        var productOptions = offer.Options = new List<ProductOption>();
                        // Only create ProductOptions if we have multiple options
                        if (options.Count > 1)
                        {
                            productOptions.Add(new ProductOption()
                            {
                                ProductId = offer.ProductId,
                                SellerId = offer.SellerId,
                                CategoryPropertyId = categoryProperty.CategoryPropertyId,
                                Options = options.Distinct().ToList()
                            });
                        }
                    }
                }
                
                context.SaveChanges();
            }
            transaction.Commit();
        }
    }

    
}
