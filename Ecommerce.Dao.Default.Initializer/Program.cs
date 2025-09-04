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
                {typeof(AnonymousCustomer), AnonymousUserCount},
                {typeof(ProductFavor), ProductFavorCount},
                {typeof(SellerFavor), SellerFavorCount},
                {typeof(Image), 100}
            }, config:new Config(){
                FetchRealAddresses = false,
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
                        Name = "Oyuncak & Oyun",
                        Description = "Çocuk oyuncakları ve oyun ürünleri",
                    },
                    // new Category()
                    // {
                    //     Name = "Elektronik",
                    //     Description = "Elektronik cihazlar ve aksesuarlar"
                    // },
                    // new Category()
                    // {
                    //     Name = "Giyim",
                    //     Description = "Kıyafet ve moda ürünleri"
                    // },
                    // new Category()
                    // {
                    //     Name = "Kitap",
                    //     Description = "Kitaplar ve eğitim materyalleri"
                    // },
                    new Category()
                    {
                        Name = "Ev & Bahçe",
                        Description = "Ev geliştirme ve bahçe malzemeleri"
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
                var toysCategory = context.Set<Category>().First(c => c.Name == "Oyuncak & Oyun");
                // var electronicsCategory = context.Set<Category>().First(c => c.Name == "Elektronik");
                // var clothingCategory = context.Set<Category>().First(c => c.Name == "Giyim");
                // var booksCategory = context.Set<Category>().First(c => c.Name == "Kitap");
                var homeGardenCategory = context.Set<Category>().First(c => c.Name == "Ev & Bahçe");
                
                var categoryProperties = new[]
                {
                    // Oyuncak & Oyun özellikleri
                    new CategoryProperty()
                    {
                        PropertyName = "Yaş Aralığı",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "0-2 yaş", "3-5 yaş", "6-8 yaş", "9-12 yaş", "13+ yaş", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Malzeme",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Plastik", "Ahşap", "Metal", "Kumaş", "Karışık", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Pil Gerekli",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Evet", "Hayır", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Marka",
                        CategoryId = toysCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                    },
                    
                    // Elektronik özellikleri
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Ekran Boyutu",
                    //     CategoryId = electronicsCategory.Id,
                    //     IsNumber = true,
                    //     IsRequired = false,
                    //     MaxValue = 99,
                    //     MinValue = 1,
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "İşletim Sistemi",
                    //     CategoryId = electronicsCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    //     EnumValues = string.Join('|', ["", "Windows", "macOS", "Linux", "Android", "iOS", "Diğer", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Bağlantı",
                    //     CategoryId = electronicsCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = false,
                    //     EnumValues = string.Join('|', ["", "WiFi", "Bluetooth", "USB", "Ethernet", "Kablosuz", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Garanti Süresi",
                    //     CategoryId = electronicsCategory.Id,
                    //     IsNumber = true,
                    //     IsRequired = false,
                    //     MaxValue = 60,
                    //     MinValue = 0,
                    // },
                    //
                    // // Giyim özellikleri
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Beden",
                    //     CategoryId = clothingCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    //     EnumValues = string.Join('|', ["", "XS", "S", "M", "L", "XL", "XXL", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Renk",
                    //     CategoryId = clothingCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Kumaş Türü",
                    //     CategoryId = clothingCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = false,
                    //     EnumValues = string.Join('|', ["", "Pamuk", "Polyester", "Yün", "İpek", "Kot", "Deri", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Cinsiyet",
                    //     CategoryId = clothingCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    //     EnumValues = string.Join('|', ["", "Erkek", "Kadın", "Üniseks", "Çocuk", ""]),
                    // },
                    //
                    // // Kitap özellikleri
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Tür",
                    //     CategoryId = booksCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    //     EnumValues = string.Join('|', ["", "Kurgu", "Kurgu Dışı", "Bilim", "Tarih", "Biyografi", "Çocuk", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Sayfa Sayısı",
                    //     CategoryId = booksCategory.Id,
                    //     IsNumber = true,
                    //     IsRequired = false,
                    //     MaxValue = 2000,
                    //     MinValue = 1,
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Dil",
                    //     CategoryId = booksCategory.Id,
                    //     IsNumber = false,
                    //     IsRequired = true,
                    //     EnumValues = string.Join('|', ["", "Türkçe", "İngilizce", "Almanca", "Fransızca", "İspanyolca", ""]),
                    // },
                    // new CategoryProperty()
                    // {
                    //     PropertyName = "Yayın Yılı",
                    //     CategoryId = booksCategory.Id,
                    //     IsNumber = true,
                    //     IsRequired = false,
                    //     MaxValue = 2024,
                    //     MinValue = 1900,
                    // },
                    
                    // Ev & Bahçe özellikleri
                    new CategoryProperty()
                    {
                        PropertyName = "Oda Tipi",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Oturma Odası", "Yatak Odası", "Mutfak", "Banyo", "Bahçe", "Ofis", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Montaj Gerekli",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "Evet", "Hayır", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "Güç Kaynağı",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = false,
                        EnumValues = string.Join('|', ["", "Elektrikli", "Pilli", "Manuel", "Güneş Enerjili", ""]),
                    },
                    new CategoryProperty()
                    {
                        PropertyName = "İç Mekan/Dış Mekan",
                        CategoryId = homeGardenCategory.Id,
                        IsNumber = false,
                        IsRequired = true,
                        EnumValues = string.Join('|', ["", "İç Mekan", "Dış Mekan", "Her ikisi de", ""]),
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
                var toysCategory = context.Set<Category>().First(c => c.Name == "Oyuncak & Oyun");
                // var electronicsCategory = context.Set<Category>().First(c => c.Name == "Elektronik");
                // var clothingCategory = context.Set<Category>().First(c => c.Name == "Giyim");
                // var booksCategory = context.Set<Category>().First(c => c.Name == "Kitap");
                var homeGardenCategory = context.Set<Category>().First(c => c.Name == "Ev & Bahçe");
                
                // Get category properties for reference
                var ageRangeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Yaş Aralığı");
                var materialProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Malzeme");
                var batteryProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Pil Gerekli");
                var brandProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Marka");
                // var screenSizeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Ekran Boyutu");
                // var osProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "İşletim Sistemi");
                // var connectivityProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Bağlantı");
                // var warrantyProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Garanti Süresi");
                // var sizeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Beden");
                // var colorProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Renk");
                // var fabricProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Kumaş Türü");
                // var genderProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Cinsiyet");
                // var genreProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Tür");
                // var pageCountProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Sayfa Sayısı");
                // var languageProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Dil");
                // var publicationYearProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Yayın Yılı");
                var roomTypeProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Oda Tipi");
                var assemblyProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Montaj Gerekli");
                var powerSourceProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "Güç Kaynağı");
                var indoorOutdoorProp = context.Set<CategoryProperty>().First(cp => cp.PropertyName == "İç Mekan/Dış Mekan");
                
                var products = new[]
                {
                    // Oyuncak & Oyun ürünleri
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Uzaktan Kumandalı Araba",
                        Images = GetImage,
                        Description = "LED ışıklı yüksek hızlı uzaktan kumandalı yarış arabası",
                        Dimensions = new Dimensions()
                        {
                            Depth = 25m, Height = 12m, Weight = 0.8m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "6-8 yaş" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Plastik" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "Evet" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "SpeedRacer" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Ahşap Yapı Blokları",
                        Images = GetImage,
                        Description = "Yaratıcı yapılar için eğitici ahşap bloklar",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 20m, Weight = 1.2m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "3-5 yaş" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Ahşap" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "Hayır" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "EduToys" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = toysCategory.Id,
                        Name = "Peluş Oyuncak Ayı",
                        Images = GetImage,
                        Description = "Çocuklar için yumuşak ve sevimli oyuncak ayı",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 35m, Weight = 0.5m, Width = 25m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = ageRangeProp.Id, Value = "0-2 yaş" },
                            new ProductCategoryProperty() { CategoryPropertyId = materialProp.Id, Value = "Kumaş" },
                            new ProductCategoryProperty() { CategoryPropertyId = batteryProp.Id, Value = "Hayır" },
                            new ProductCategoryProperty() { CategoryPropertyId = brandProp.Id, Value = "CuddleBear" }
                        }
                    },
                    
                    // // Elektronik ürünleri
                    // new Product()
                    // {
                    //     CategoryId = electronicsCategory.Id,
                    //     Name = "Oyuncu Dizüstü Bilgisayarı",
                    //     Images = GetImage,
                    //     Description = "Oyun ve profesyonel işler için yüksek performanslı dizüstü bilgisayar",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 25m, Height = 2.5m, Weight = 2.3m, Width = 35m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = screenSizeProp.Id, Value = "15.6" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Windows" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
                    //     }
                    // },
                    // new Product()
                    // {
                    //     CategoryId = electronicsCategory.Id,
                    //     Name = "Kablosuz Fare",
                    //     Images = GetImage,
                    //     Description = "Hassas izlemeli ergonomik kablosuz fare",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 12m, Height = 4m, Weight = 0.1m, Width = 7m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Diğer" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "Kablosuz" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "12" }
                    //     }
                    // },
                    // new Product()
                    // {
                    //     CategoryId = electronicsCategory.Id,
                    //     Name = "Akıllı Telefon",
                    //     Images = GetImage,
                    //     Description = "Gelişmiş kameralı en yeni nesil akıllı telefon",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 0.8m, Height = 15m, Weight = 0.18m, Width = 7m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = screenSizeProp.Id, Value = "6.1" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = osProp.Id, Value = "Android" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = connectivityProp.Id, Value = "WiFi" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = warrantyProp.Id, Value = "24" }
                    //     }
                    // },
                    
                    // // Giyim ürünleri
                    // new Product()
                    // {
                    //     CategoryId = clothingCategory.Id,
                    //     Name = "Pamuklu Tişört",
                    //     Description = "Günlük kullanım için rahat pamuklu tişört",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 1m, Height = 70m, Weight = 0.2m, Width = 50m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = sizeProp.Id, Value = "M" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = colorProp.Id, Value = "Mavi" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = fabricProp.Id, Value = "Pamuk" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = genderProp.Id, Value = "Üniseks" }
                    //     }
                    // },
                    // new Product()
                    // {
                    //     CategoryId = clothingCategory.Id,
                    //     Name = "Kot Pantolon",
                    //     Description = "Modern kesimli klasik mavi kot pantolon",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 2m, Height = 20m, Weight = 0.6m, Width = 40m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = sizeProp.Id, Value = "L" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = colorProp.Id, Value = "Koyu Mavi" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = fabricProp.Id, Value = "Kot" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = genderProp.Id, Value = "Erkek" }
                    //     }
                    // },
                    
                    // // Kitap ürünleri
                    // new Product()
                    // {
                    //     CategoryId = booksCategory.Id,
                    //     Name = "Programlama Rehberi",
                    //     Description = "Modern programlama dilleri için kapsamlı rehber",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 3m, Height = 24m, Weight = 0.8m, Width = 17m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = genreProp.Id, Value = "Bilim" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = pageCountProp.Id, Value = "450" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = languageProp.Id, Value = "İngilizce" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = publicationYearProp.Id, Value = "2023" }
                    //     }
                    // },
                    // new Product()
                    // {
                    //     CategoryId = booksCategory.Id,
                    //     Name = "Çocuk Hikaye Kitabı",
                    //     Description = "Genç okuyucular için renkli resimli hikaye kitabı",
                    //     Dimensions = new Dimensions()
                    //     {
                    //         Depth = 1m, Height = 21m, Weight = 0.3m, Width = 15m
                    //     },
                    //     CategoryProperties = new List<ProductCategoryProperty>()
                    //     {
                    //         new ProductCategoryProperty() { CategoryPropertyId = genreProp.Id, Value = "Çocuk" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = pageCountProp.Id, Value = "32" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = languageProp.Id, Value = "Türkçe" },
                    //         new ProductCategoryProperty() { CategoryPropertyId = publicationYearProp.Id, Value = "2022" }
                    //     }
                    // },
                    //
                    // Ev & Bahçe ürünleri
                    new Product()
                    {
                        CategoryId = homeGardenCategory.Id,
                        Name = "Bahçe Hortumu",
                        Images = GetImage,
                        Description = "Bitkileri sulamak için esnek bahçe hortumu",
                        Dimensions = new Dimensions()
                        {
                            Depth = 30m, Height = 30m, Weight = 2.5m, Width = 30m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = roomTypeProp.Id, Value = "Bahçe" },
                            new ProductCategoryProperty() { CategoryPropertyId = assemblyProp.Id, Value = "Hayır" },
                            new ProductCategoryProperty() { CategoryPropertyId = powerSourceProp.Id, Value = "Manuel" },
                            new ProductCategoryProperty() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "Dış Mekan" }
                        }
                    },
                    new Product()
                    {
                        CategoryId = homeGardenCategory.Id,
                        Name = "LED Masa Lambası",
                        Images = GetImage,
                        Description = "Dokunmatik kontrollü ayarlanabilir LED masa lambası",
                        Dimensions = new Dimensions()
                        {
                            Depth = 20m, Height = 45m, Weight = 1.2m, Width = 15m
                        },
                        CategoryProperties = new List<ProductCategoryProperty>()
                        {
                            new ProductCategoryProperty() { CategoryPropertyId = roomTypeProp.Id, Value = "Ofis" },
                            new ProductCategoryProperty() { CategoryPropertyId = assemblyProp.Id, Value = "Evet" },
                            new ProductCategoryProperty() { CategoryPropertyId = powerSourceProp.Id, Value = "Elektrikli" },
                            new ProductCategoryProperty() { CategoryPropertyId = indoorOutdoorProp.Id, Value = "İç Mekan" }
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
                        FirstName = "Can",
                        LastName = "Yılmaz",
                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "Oyuncak Dünyası Mağazası",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551234567" },
                        Address = new Address()
                        {
                            Line1 = "123 Oyuncak Caddesi",
                            Line2 = "Daire 100",
                            District = "Kadıköy",
                            City = "İstanbul",
                            Country = "Türkiye",
                            ZipCode = "34710"
                        }
                    },
                    new Seller()
                    {
                        Email = "techgadgets@example.com",
                        NormalizedEmail = "TECHGADGETS@EXAMPLE.COM",
                        PasswordHash = "hashedpassword456",
                        FirstName = "Sare",
                        LastName = "Yıldırım",                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "Teknoloji Aletleri Pro",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5559876543" },
                        Address = new Address()
                        {
                            Line1 = "456 Elektronik Bulvarı",
                            District = "Beşiktaş",
                            City = "İstanbul",
                            Country = "Türkiye",
                            ZipCode = "34349"
                        }
                    },
                    new Seller()
                    {
                        Email = "fashionhub@example.com",
                        NormalizedEmail = "FASHIONHUB@EXAMPLE.COM",
                        PasswordHash = "hashedpassword789",
                        FirstName = "Mert",
                        LastName = "Kahraman",                        Session = new Session(){
                            Cart = new Cart()
                        },
                        ShopName = "Moda Merkezi",
                        PhoneNumber = new PhoneNumber() { CountryCode = 90, Number = "5551122334" },
                        Address = new Address()
                        {
                            Line1 = "789 Moda Bulvarı",
                            District = "Şişli",
                            City = "İstanbul",
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
            if (!context.Set<ProductOption>().Any())
            {
                var productOffers = context.Set<ProductOffer>().ToList();
                var productCategoryProperties = context.Set<ProductCategoryProperty>()
                    .Include(pcp => pcp.CategoryProperty)
                    .ToList();
                
                var productOptions = new List<ProductOption>();
                
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
                                        case "marka":
                                            options.AddRange(new[] { "Jenerik", "Premium", "Lüks" });
                                            break;
                                        case "renk":
                                            options.AddRange(new[] { "Siyah", "Beyaz", "Gri" });
                                            break;
                                        default:
                                            // Generic string variations
                                            options.AddRange(new[] { $"{currentValue} Plus", $"{currentValue} Pro", $"Standart {currentValue}" });
                                            break;
                                    }
                                }
                            }
                        }

                        // Create individual ProductOption instances for each option value
                        if (options.Count > 1)
                        {
                            foreach (var option in options.Distinct())
                            {
                                productOptions.Add(new ProductOption()
                                {
                                    ProductId = offer.ProductId,
                                    SellerId = offer.SellerId,
                                    CategoryPropertyId = categoryProperty.CategoryPropertyId,
                                    Value = option
                                });
                            }
                        }
                    }
                }
                
                context.Set<ProductOption>().AddRange(productOptions);
                context.SaveChanges();
            }
            transaction.Commit();
        }
    }

    private static IList<ImageProduct> GetImage => new List<ImageProduct>([
        new ImageProduct(){
            IsPrimary = true, Image = new Image(){
                Data =
                    Convert.ToBase64String(ValueRandomizer.FetchImage())
            }
        }
    ]);


}
