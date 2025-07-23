using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Default.Tool;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework.Legacy;

namespace Ecommerce.Dao.Tests;

public class Tests
{
    private DbContextOptions<DefaultDbContext> _dbContextOptions;
    private const bool Skip = false;
    [OneTimeSetUp]
    public void Setup() {
        _dbContextOptions = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;")
            .EnableSensitiveDataLogging().Options;
    }
    [Test, Order(1)]
    public void CreateDb() {
        if (Skip){
            return;
        }
        using var context = new DefaultDbContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.SaveChanges();
    }
    private const int UserCount = 1000;
    private const int SellerCount = 200;
    private const int ProductCount = SellerCount*5;
    private const int ProductOfferCount = SellerCount * ProductCount / 100;
    private const int CouponCount = SellerCount*10;
    private const int ProductReviewCount = ProductCount * UserCount / 100;
    private const int ReviewCommentCount = ProductOfferCount*10;
    private const int ReviewVoteCount = ReviewCommentCount*2;
    private const int CartCount = UserCount * 2;
    private const int CartItemCount = CartCount;
    private const int SessionCount = UserCount * 2;
    private const int OrderItemCount = CartItemCount;
    private const int OrderCount = UserCount * 2;
    private const int CategoryCount = 10;
    private const int StaffCount = 10;
    private const int PermissionCount = 5;
    private const int PermissionClaimCount = PermissionCount * StaffCount / 2;
    [Test, Order(2)]
    public void InitDb() {
        if (Skip){
            return;
        }
        using var initializer = new DatabaseInitializer<DefaultDbContext>(
           _dbContextOptions,
            new Dictionary<Type, int?> {
                { typeof(Customer), UserCount },
                { typeof(Seller), SellerCount },
                {typeof(Staff), StaffCount},
                {typeof(Permission), PermissionCount},
                {typeof(PermissionClaim), PermissionClaimCount},
                { typeof(Product), ProductCount },
                { typeof(ProductOffer), ProductOfferCount},
                {typeof(Coupon),CouponCount},
                {typeof(ProductReview), ProductReviewCount},
                {typeof(ReviewComment), ReviewCommentCount},
                {typeof(ReviewVote), ReviewVoteCount},
                { typeof(Cart), CartCount},
                { typeof(CartItem), CartItemCount },
                { typeof(Session), SessionCount},
                {typeof(OrderItem), OrderItemCount},
                { typeof(Order), OrderCount},
                {typeof(Category), CategoryCount}
            }, defaultCount:0
        );
        initializer.initialize();
    }

}

