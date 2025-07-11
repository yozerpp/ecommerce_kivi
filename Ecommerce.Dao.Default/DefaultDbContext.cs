using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Ecommerce.Dao.Default;


public class DefaultDbContext : DbContext
{
    public DefaultDbContext() : base() { }
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        // optionsBuilder.UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasDefaultSchema("data");
        var userBuilder = modelBuilder.Entity<User>().UseTptMappingStrategy();
        userBuilder.HasKey(u => u.Id);
        userBuilder.Property(u => u.Id).ValueGeneratedOnAdd();
        userBuilder.HasAlternateKey(u => u.Email);
        userBuilder.HasOne<Session>(u=>u.Session).WithOne(s=>s.User).HasForeignKey<User>(s=>s.SessionId).HasPrincipalKey<Session>(s=>s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        userBuilder.ComplexProperty(u => u.PhoneNumber);
        userBuilder.ComplexProperty<Address>(u=>u.BillingAddress, nameof(User.BillingAddress)).IsRequired();
        userBuilder.ComplexProperty<Address>(u => u.ShippingAddress, nameof(User.ShippingAddress)).IsRequired();
        var sellerBuilder = modelBuilder.Entity<Seller>();
        sellerBuilder.HasBaseType<User>();
        // sellerBuilder.HasKey(s => s.Id);
        sellerBuilder.Property(s => s.ShopEmail).IsRequired();
        sellerBuilder.HasIndex(s => s.ShopEmail).IsUnique();
        sellerBuilder.HasOne<User>().WithOne().HasForeignKey<Seller>(s => s.Id).HasPrincipalKey<User>(u=>u.Id).OnDelete(DeleteBehavior.Cascade);
        sellerBuilder.ComplexProperty<Address>(s => s.ShopAddress, nameof(Seller.ShopAddress)).IsRequired();
        sellerBuilder.ComplexProperty<PhoneNumber>(s => s.ShopPhoneNumber).IsRequired();
        sellerBuilder.HasMany(s => s.Offers);
        sellerBuilder.HasMany<Coupon>(s => s.Coupons).WithOne(c => c.Seller).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var productOfferBuilder = modelBuilder.Entity<ProductOffer>();
        productOfferBuilder.HasKey(nameof(ProductOffer.SellerId), nameof(ProductOffer.ProductId));
        productOfferBuilder.HasOne(o => o.Product).WithMany(p => p.Offers).HasForeignKey(o => o.ProductId).HasPrincipalKey(p=>p.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasOne(o => o.Seller).WithMany(s => s.Offers).HasForeignKey(o => o.SellerId)
            .HasPrincipalKey(s => s.Id).OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasMany<ProductReview>(o => o.Reviews).WithOne(r => r.Offer).HasForeignKey(nameof(ProductReview.ProductId), nameof(ProductReview.SellerId))
            .HasPrincipalKey(nameof(ProductOffer.ProductId), nameof(ProductOffer.SellerId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasMany<OrderItem>(p=>p.BoughtItems).WithOne(o=>o.ProductOffer).HasForeignKey(nameof(OrderItem.ProductId), nameof(OrderItem.SellerId))
            .HasPrincipalKey(nameof(ProductOffer.ProductId), nameof(ProductOffer.SellerId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var cartItemBuilder = modelBuilder.Entity<CartItem>();
        cartItemBuilder.HasKey(nameof(CartItem.SellerId), nameof(CartItem.ProductId), nameof(CartItem.CartId));
        cartItemBuilder.HasOne<Cart>(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci=>ci.CartId).HasPrincipalKey(c=>c.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        cartItemBuilder.HasOne<ProductOffer>(ci => ci.ProductOffer).WithMany()
            .HasForeignKey(nameof(CartItem.ProductId), nameof(CartItem.SellerId)).HasPrincipalKey(nameof(ProductOffer.SellerId), nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        cartItemBuilder.HasOne<Coupon>(ci => ci.Coupon).WithMany().HasForeignKey(ci => ci.CouponId).HasPrincipalKey(c => c.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        var sessionBuilder = modelBuilder.Entity<Session>();
        sessionBuilder.HasKey(s => s.Id);
        sessionBuilder.Property(s => s.Id).ValueGeneratedOnAdd();
        sessionBuilder.HasOne(s => s.User).WithOne(u => u.Session).HasForeignKey<User>(u => u.SessionId)
            .HasPrincipalKey<Session>(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        sessionBuilder.HasOne(s => s.Cart).WithOne(c => c.Session).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict); ;
        var cartBuilder = modelBuilder.Entity<Cart>();
        cartBuilder.HasKey(c => c.Id);
        cartBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        cartBuilder.HasOne<Session>(c => c.Session).WithOne(s => s.Cart).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var orderBuilder = modelBuilder.Entity<Order>();
        orderBuilder.HasKey(o => o.Id);
        orderBuilder.Property(o=>o.Id).ValueGeneratedOnAdd();
        orderBuilder.HasMany<OrderItem>(o => o.Items).WithOne(oi=>oi.Order).HasForeignKey(oi=>oi.OrderId)
            .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<User>(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<Payment>(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.ComplexProperty(o => o.ShippingAddress).IsRequired();
        orderBuilder.Property(o => o.Date).IsRequired();
        var paymentBuilder = modelBuilder.Entity<Payment>();
        paymentBuilder.HasKey(p => p.Id);
        paymentBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        paymentBuilder.HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired(false);
        var productBuilder = modelBuilder.Entity<Product>();
        productBuilder.HasKey(p => p.Id);
        productBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        productBuilder.HasMany(p => p.Offers).WithOne(o => o.Product).HasForeignKey(o => o.ProductId)
            .HasPrincipalKey(p => p.Id).OnDelete(DeleteBehavior.Cascade);
        productBuilder.HasOne<Category>(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId)
            .HasPrincipalKey(c => c.Id).IsRequired(true).OnDelete(DeleteBehavior.Restrict);
        var categoryBuilder = modelBuilder.Entity<Category>();
        categoryBuilder.HasKey(c => c.Id);
        categoryBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        categoryBuilder.HasOne<Category>(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        var reviewBuilder = modelBuilder.Entity<ProductReview>();
        reviewBuilder.HasKey(nameof(ProductReview.ProductId), nameof(ProductReview.SellerId), nameof(ProductReview.ReviewerId));
        reviewBuilder.HasMany<ReviewComment>(r => r.Comments).WithOne(c => c.Review).HasForeignKey(nameof(ReviewComment.ProductId), nameof(ReviewComment.SellerId), nameof(ReviewComment.RaterId))
            .HasPrincipalKey(nameof(ProductReview.ProductId), nameof(ProductReview.SellerId), nameof(ProductReview.ReviewerId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        reviewBuilder.HasOne<ProductOffer>(r=>r.Offer).WithMany(o=>o.Reviews).HasForeignKey(nameof(ProductReview.ProductId), nameof(ProductReview.SellerId))
            .HasPrincipalKey(nameof(ProductOffer.ProductId), nameof(ProductOffer.SellerId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var commentBuilder = modelBuilder.Entity<ReviewComment>();
        commentBuilder.HasKey(nameof(ReviewComment.ProductId), nameof(ReviewComment.SellerId), nameof(ReviewComment.RaterId), nameof(ReviewComment.CommenterId));
        commentBuilder.HasOne<ProductReview>(r=>r.Review).WithMany(r=>r.Comments).HasForeignKey(nameof(ReviewComment.ProductId), nameof(ReviewComment.SellerId), nameof(ReviewComment.RaterId))
            .HasPrincipalKey(nameof(ProductReview.ProductId), nameof(ProductReview.SellerId), nameof(ProductReview.ReviewerId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var couponBuilder = modelBuilder.Entity<Coupon>();
        couponBuilder.HasKey(c => c.Id);
        couponBuilder.HasOne<Seller>(c => c.Seller).WithMany(s => s.Coupons).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var orderItemBuilder = modelBuilder.Entity<OrderItem>();
        orderItemBuilder.HasKey(nameof(OrderItem.ProductId), nameof(OrderItem.SellerId), nameof(OrderItem.OrderId));
        orderItemBuilder.HasOne<Order>(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(o => o.OrderId)
            .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        //This prevents delisting OrderItems.
        orderItemBuilder.HasOne<ProductOffer>(o=>o.ProductOffer).WithMany(of=>of.BoughtItems).HasForeignKey(nameof(OrderItem.ProductId), nameof(OrderItem.SellerId))
            .HasPrincipalKey(nameof(ProductOffer.ProductId), nameof(ProductOffer.SellerId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}
