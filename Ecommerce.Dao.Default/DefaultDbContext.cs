using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Default;

public class DefaultDbContext : DbContext
{
    public DefaultDbContext() : base() { }
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options) { }
    public DefaultDbContext(DbContextOptions options):base(options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasDefaultSchema("data");
        var userBuilder = modelBuilder.Entity<User>().UseTphMappingStrategy();
        userBuilder.HasKey(u => u.Id);
        userBuilder.Property(u => u.Id).ValueGeneratedOnAdd();
        userBuilder.HasAlternateKey(u => u.Email);
        userBuilder.HasOne<Session>(u=>u.Session).WithOne(s=>s.User).HasForeignKey<User>(s=>s.SessionId).HasPrincipalKey<Session>(s=>s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        userBuilder.ComplexProperty(u => u.PhoneNumber);
        userBuilder.ComplexProperty<Address>(u => u.ShippingAddress, nameof(User.ShippingAddress)).IsRequired();
        userBuilder.HasMany<ProductReview>(u => u.Reviews).WithOne(r => r.Reviewer).HasForeignKey(u => u.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        userBuilder.HasMany<ReviewComment>(u => u.ReviewComments).WithOne(rc => rc.User).HasForeignKey(r => r.UserId).HasPrincipalKey(u => u.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var sellerBuilder = modelBuilder.Entity<Seller>();
        sellerBuilder.HasBaseType<User>();
        // sellerBuilder.HasKey(s => s.Id);
        sellerBuilder.Property(s => s.ShopEmail).IsRequired();
        sellerBuilder.HasIndex(s => s.ShopEmail).IsUnique();
        sellerBuilder.Property<string>(s => s.ShopName).HasMaxLength(ShopNameMaxLength);
        // sellerBuilder.HasOne<User>().WithOne().HasForeignKey<Seller>(s => s.Id).HasPrincipalKey<User>(u=>u.Id).OnDelete(DeleteBehavior.Cascade);
        sellerBuilder.ComplexProperty<Address>(s => s.ShopAddress, nameof(Seller.ShopAddress)).IsRequired();
        sellerBuilder.ComplexProperty<PhoneNumber>(s => s.ShopPhoneNumber).IsRequired();
        sellerBuilder.HasMany(s => s.Offers);
        sellerBuilder.HasMany<Coupon>(s => s.Coupons).WithOne(c => c.Seller).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var productOfferBuilder = modelBuilder.Entity<ProductOffer>();
        productOfferBuilder.HasKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId));
        productOfferBuilder.HasOne(o => o.Seller).WithMany(s => s.Offers).HasForeignKey(o => o.SellerId)
            .HasPrincipalKey(s => s.Id).OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasOne(o => o.Product).WithMany(p => p.Offers).HasForeignKey(o => o.ProductId).HasPrincipalKey(p=>p.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasMany<ProductReview>(o => o.Reviews).WithOne(r => r.Offer).HasForeignKey(nameof(ProductReview.SellerId),nameof(ProductReview.ProductId))
            .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasMany<OrderItem>(p=>p.BoughtItems).WithOne(o=>o.ProductOffer).HasForeignKey(nameof(OrderItem.SellerId),nameof(OrderItem.ProductId))
            .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
        productOfferBuilder.Property<decimal>(p => p.Discount).HasPrecision(2, 2).HasAnnotation(nameof(Annotations.Validation_Positive), true).IsRequired().HasDefaultValue(1f).ValueGeneratedNever();
        productOfferBuilder.Property<decimal>(p => p.Price).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 10000m).IsRequired().ValueGeneratedNever();
        productOfferBuilder.Property<uint>(p => p.Stock).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 10000).IsRequired().ValueGeneratedNever();
        var cartItemBuilder = modelBuilder.Entity<CartItem>();
        cartItemBuilder.HasKey(nameof(CartItem.SellerId), nameof(CartItem.ProductId), nameof(CartItem.CartId));
        cartItemBuilder.HasOne<ProductOffer>(ci => ci.ProductOffer).WithMany()
            .HasForeignKey(nameof(CartItem.SellerId),nameof(CartItem.ProductId)).HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        cartItemBuilder.HasOne<Cart>(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci=>ci.CartId).HasPrincipalKey(c=>c.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        cartItemBuilder.HasOne<Coupon>(ci => ci.Coupon).WithMany().HasForeignKey(ci => ci.CouponId).HasPrincipalKey(c => c.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        cartItemBuilder.Property<uint>(ci => ci.Quantity).HasAnnotation(nameof(Annotations.Validation_Positive), true).IsRequired().ValueGeneratedNever();
        var sessionBuilder = modelBuilder.Entity<Session>();
        sessionBuilder.HasKey(s => s.Id);
        sessionBuilder.Property(s => s.Id).ValueGeneratedOnAdd();
        sessionBuilder.HasOne(s => s.User).WithOne(u => u.Session).HasForeignKey<User>(u => u.SessionId)
            .HasPrincipalKey<Session>(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        sessionBuilder.HasOne(s => s.Cart).WithOne(c => c.Session).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        sessionBuilder.HasMany<Order>(s => s.Orders);
        var cartBuilder = modelBuilder.Entity<Cart>();
        cartBuilder.HasKey(c => c.Id);
        cartBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        cartBuilder.HasOne<Session>(c => c.Session).WithOne(s => s.Cart).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var orderBuilder = modelBuilder.Entity<Order>();
        orderBuilder.HasKey(o => o.Id);
        orderBuilder.Property(o=>o.Id).ValueGeneratedOnAdd();
        orderBuilder.HasOne<Session>(o => o.Session).WithMany(s => s.Orders).HasForeignKey(o => o.SessionId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<User>(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        orderBuilder.HasMany<OrderItem>(o => o.Items).WithOne(oi=>oi.Order).HasForeignKey(oi=>oi.OrderId)
            .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<Payment>(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        orderBuilder.ComplexProperty(o => o.ShippingAddress).IsRequired();
        orderBuilder.Property(o => o.Date).IsRequired();
        var paymentBuilder = modelBuilder.Entity<Payment>();
        paymentBuilder.HasKey(p => p.Id);
        paymentBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        paymentBuilder.HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var productBuilder = modelBuilder.Entity<Product>();
        productBuilder.HasKey(p => p.Id);
        productBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        productBuilder.HasMany(p => p.Offers).WithOne(o => o.Product).HasForeignKey(o => o.ProductId)
            .HasPrincipalKey(p => p.Id).OnDelete(DeleteBehavior.Cascade);
        productBuilder.HasOne<Category>(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId)
            .HasPrincipalKey(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var categoryBuilder = modelBuilder.Entity<Category>();
        categoryBuilder.HasKey(c => c.Id);
        categoryBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        categoryBuilder.HasOne<Category>(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        categoryBuilder.HasMany(c => c.Products).WithOne(p => p.Category);
        var couponBuilder = modelBuilder.Entity<Coupon>();
        couponBuilder.HasKey(c => c.Id);
        couponBuilder.HasOne<Seller>(c => c.Seller).WithMany(s => s.Coupons).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        couponBuilder.Property<string>(c => c.Id).HasMaxLength(ShopNameMaxLength + 3).ValueGeneratedNever(); //extra space for discount amount
        couponBuilder.Property(c => c.DiscountRate).HasPrecision(2, 2).HasAnnotation(nameof(Annotations.Validation_Positive),true).IsRequired().ValueGeneratedNever();
        var orderItemBuilder = modelBuilder.Entity<OrderItem>();
        orderItemBuilder.HasKey( nameof(OrderItem.OrderId), nameof(OrderItem.SellerId),nameof(OrderItem.ProductId));
        orderItemBuilder.HasOne<Order>(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(o => o.OrderId)
            .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        //This prevents delisting OrderItems.
        orderItemBuilder.HasOne<ProductOffer>(o=>o.ProductOffer).WithMany(of=>of.BoughtItems).HasForeignKey(nameof(OrderItem.SellerId),nameof(OrderItem.ProductId))
            .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderItemBuilder.Property<uint>(o => o.Quantity).HasAnnotation(nameof(Annotations.Validation_Positive), true).IsRequired().ValueGeneratedNever();
        var reviewBuilder = modelBuilder.Entity<ProductReview>();
        reviewBuilder.HasOne<Session>(r => r.Session).WithMany().HasForeignKey(r => r.SessionId) .HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        reviewBuilder.HasKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId));
        reviewBuilder.HasOne<ProductOffer>(r=>r.Offer).WithMany(o=>o.Reviews).HasForeignKey(nameof(ProductReview.SellerId),nameof(ProductReview.ProductId))
            .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        reviewBuilder.HasMany<ReviewComment>(r => r.Comments).WithOne(c => c.Review).HasForeignKey( nameof(ReviewComment.ReviewSessionId),nameof(ReviewComment.SellerId),nameof(ReviewComment.ProductId))
            .HasPrincipalKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId) ).IsRequired().OnDelete(DeleteBehavior.Cascade);
        reviewBuilder.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ProductReview).HasForeignKey(nameof(ReviewVote.ReviewSessionId),nameof(ReviewVote.SellerId),nameof(ReviewVote.ProductId))
            .HasPrincipalKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        reviewBuilder.Property<decimal>(r => r.Rating).HasPrecision(3, 2).HasAnnotation(nameof(Annotations.Validation_Positive), true)
            .HasAnnotation(nameof(Annotations.Validation_MaxValue), 5.0m).IsRequired().ValueGeneratedNever();
        reviewBuilder.HasOne<User>(r => r.Reviewer).WithMany(u => u.Reviews).HasForeignKey(r=>r.UserId).HasPrincipalKey(u=>u.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        reviewBuilder.HasOne<Session>(r => r.Session).WithMany().HasForeignKey(r => r.SessionId)
            .HasPrincipalKey(u => u.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var commentBuilder = modelBuilder.Entity<ReviewComment>();
        commentBuilder.HasKey(nameof(ReviewComment.SessionId), nameof(ReviewComment.ReviewSessionId),nameof(ReviewComment.SellerId),nameof(ReviewComment.ProductId));
        commentBuilder.HasOne<ProductReview>(r=>r.Review).WithMany(r=>r.Comments).HasForeignKey(nameof(ReviewComment.ReviewSessionId),nameof(ReviewComment.SellerId),nameof(ReviewComment.ProductId))
            .HasPrincipalKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        commentBuilder.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ReviewComment).HasForeignKey(nameof(ReviewVote.CommenterId),nameof(ReviewVote.ReviewSessionId),nameof(ReviewVote.SellerId),nameof(ReviewVote.ProductId))
            .HasPrincipalKey(nameof(ReviewComment.SessionId),nameof(ReviewComment.ReviewSessionId),nameof(ReviewComment.SellerId),nameof(ReviewComment.ProductId)).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        commentBuilder.HasOne<Session>(r=>r.Session).WithMany().HasForeignKey(c => c.SessionId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        commentBuilder.HasOne<User>(c=>c.User).WithMany(u=>u.ReviewComments).HasForeignKey(c=>c.UserId).HasPrincipalKey(u=>u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        var voteBuilder = modelBuilder.Entity<ReviewVote>();
        voteBuilder.HasKey(nameof(ReviewVote.VoterId),nameof(ReviewVote.ReviewSessionId),nameof(ReviewVote.SellerId),nameof(ReviewVote.ProductId));
        voteBuilder.HasOne<ProductReview>(r=>r.ProductReview).WithMany(r=>r.Votes).HasForeignKey(nameof(ReviewVote.ReviewSessionId),nameof(ReviewVote.SellerId),nameof(ReviewVote.ProductId))
            .HasPrincipalKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId)).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
        voteBuilder.HasOne<ReviewComment>(r=>r.ReviewComment).WithMany(r=>r.Votes).HasForeignKey(nameof(ReviewVote.CommenterId),nameof(ReviewVote.ReviewSessionId),nameof(ReviewVote.SellerId),nameof(ReviewVote.ProductId))
            .HasPrincipalKey(nameof(ReviewComment.SessionId),nameof(ReviewComment.ReviewSessionId),nameof(ReviewComment.SellerId),nameof(ReviewComment.ProductId)).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        voteBuilder.HasOne<Session>(v=>v.Voter).WithMany().HasForeignKey(v => v.VoterId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        voteBuilder.HasOne<User>(v => v.VoterUser).WithMany(u => u.ReviewVotes).HasForeignKey(u => u.VoterUserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
    private const int ShopNameMaxLength = 25;
    public enum Annotations
    {
        Validation_Positive,
        Validation_MaxValue,
        Validation_MinValue,
    }
}
