using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Ecommerce.Dao.Default;


public class DefaultDbContext : DbContext
{
    public static readonly string DefaultConnectionString = "Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;TrustServerCertificate=True;";
    public DefaultDbContext() : base() { }

    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options) { }
    public DefaultDbContext(DbContextOptions options):base(options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasDefaultSchema("data");
        var userBuilder = modelBuilder.Entity<User>().UseTptMappingStrategy();
        userBuilder.HasKey(u => u.Id);
        userBuilder.Property(u => u.Id).ValueGeneratedOnAdd();
        userBuilder.HasAlternateKey(u => u.NormalizedEmail);
        userBuilder.Property(u => u.PasswordHash).HasMaxLength(24).IsRequired();
        userBuilder.HasOne<Session>(u=>u.Session).WithOne(s=>s.User).HasForeignKey<User>(s=>s.SessionId).HasPrincipalKey<Session>(s=>s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        userBuilder.ComplexProperty(u => u.PhoneNumber);
        userBuilder.ComplexProperty<Address>(u => u.Address, nameof(Customer.Address)).IsRequired();
        var customerBuilder = modelBuilder.Entity<Customer>();
        customerBuilder.HasBaseType<User>();
        customerBuilder.HasMany(c => c.FavoriteSellers).WithMany(s => s.FavoredCustomers);
        customerBuilder.HasMany(u => u.Reviews).WithOne(r => r.Reviewer).HasForeignKey(u => u.ReviewerId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        customerBuilder.HasMany(u => u.ReviewComments).WithOne(rc => rc.User).HasForeignKey(r => r.UserId).HasPrincipalKey(u => u.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        customerBuilder.OwnsMany<Address>(c => c.RegisteredAddresses);
        var sellerBuilder = modelBuilder.Entity<Seller>();
        sellerBuilder.HasBaseType<User>();
        // sellerBuilder.HasKey(s => s.Id);
        sellerBuilder.Property<string>(s => s.ShopName).HasMaxLength(ShopNameMaxLength);
        // sellerBuilder.ComplexProperty<Address>(s => s.ShippingAddress, nameof(Seller.ShippingAddress)).IsRequired();
        sellerBuilder.HasMany(s => s.Offers);
        sellerBuilder.HasMany<Coupon>(s => s.Coupons).WithOne(c => c.Seller).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        var staffBuilder = modelBuilder.Entity<Staff>();
        staffBuilder.HasBaseType<User>();
        staffBuilder.HasMany<PermissionClaim>(s => s.PermissionClaims).WithOne(p => p.Grantee).HasForeignKey(p=>p.GranteeId).HasPrincipalKey(s=>s.Id);
        staffBuilder.HasMany<PermissionClaim>(s => s.PermissionGrants).WithOne(p => p.Granter)
            .HasForeignKey(p => p.GranteeId).HasPrincipalKey(s => s.Id);
        staffBuilder.HasOne<Staff>(s => s.Manager).WithMany(s => s.TeamMembers).HasForeignKey(s => s.ManagerId).HasPrincipalKey(s => s.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        var permissionBuilder = modelBuilder.Entity<Permission>();
        permissionBuilder.HasKey(p => p.Id);
        permissionBuilder.Property(p => p.Id).ValueGeneratedOnAdd();
        permissionBuilder.Property(p => p.Name).HasMaxLength(32).IsRequired();
        var permissionClaimBuilder = modelBuilder.Entity<PermissionClaim>();
        permissionClaimBuilder.HasKey(p => p.Id);
        permissionClaimBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        permissionClaimBuilder.HasOne<Staff>(p => p.Grantee).WithMany(s => s.PermissionClaims)
            .HasForeignKey(p => p.GranteeId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        permissionClaimBuilder.HasOne<Staff>(p => p.Granter).WithMany(s => s.PermissionGrants).HasForeignKey(p => p.GranterId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        permissionClaimBuilder.HasOne<Permission>(p => p.Permission).WithMany().HasForeignKey(p => p.PermissionId)
            .HasPrincipalKey(p => p.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
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
        sessionBuilder.HasOne<User>(s => s.User).WithOne(u => u.Session).HasForeignKey<User>(u => u.SessionId)
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
        orderBuilder.HasOne<Session>(o => o.Session).WithMany().HasForeignKey(o => o.SessionId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<Customer>(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
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
        productBuilder.HasOne<Category>(p => p.Category).WithMany(c=>c.Products).HasForeignKey(p => p.CategoryId)
            .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var categoryBuilder = modelBuilder.Entity<Category>();
        categoryBuilder.HasKey(c => c.Id);
        categoryBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        categoryBuilder.HasOne<Category>(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        categoryBuilder.HasMany<Product>(c => c.Products).WithOne(p=>p.Category).HasForeignKey(p=>p.CategoryId).HasPrincipalKey(c=>c.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
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
        reviewBuilder.HasKey(nameof(ProductReview.ReviewerId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId));
        reviewBuilder.HasMany<ReviewComment>(r => r.Comments).WithOne(c => c.Review).HasForeignKey( r=>r.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        reviewBuilder.HasOne<ProductOffer>(r=>r.Offer).WithMany(o=>o.Reviews).HasForeignKey(nameof(ProductReview.SellerId),nameof(ProductReview.ProductId))
            .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        reviewBuilder.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ProductReview).HasForeignKey(v=>v.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        reviewBuilder.Property<decimal>(r => r.Rating).HasPrecision(3, 2).HasAnnotation(nameof(Annotations.Validation_Positive), true)
            .HasAnnotation(nameof(Annotations.Validation_MaxValue), 5.0m).IsRequired().ValueGeneratedNever();
        reviewBuilder.HasOne<Customer>(r => r.Reviewer).WithMany(u => u.Reviews).HasForeignKey(r=>r.ReviewerId).HasPrincipalKey(u=>u.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        reviewBuilder.HasOne<Session>(r => r.Session).WithMany().HasForeignKey(r => r.SessionId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var commentBuilder = modelBuilder.Entity<ReviewComment>();
        commentBuilder.HasKey(c=>c.Id);
        commentBuilder.HasOne<ProductReview>(r=>r.Review).WithMany(r=>r.Comments).HasForeignKey(c=>c.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        commentBuilder.HasOne<ReviewComment>(r => r.Parent).WithMany(r => r.Replies).HasForeignKey(r => r.ParentId).HasPrincipalKey(r => r.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        commentBuilder.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ReviewComment).HasForeignKey(v=>v.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        commentBuilder.HasOne<Session>(r=>r.Commenter).WithMany().HasForeignKey(c => c.CommenterId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var voteBuilder = modelBuilder.Entity<ReviewVote>();
        voteBuilder.HasKey(v=>v.Id);
        voteBuilder.HasOne<ProductReview>(r=>r.ProductReview).WithMany(r=>r.Votes).HasForeignKey(v=>v.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        voteBuilder.HasOne<ReviewComment>(r=>r.ReviewComment).WithMany(r=>r.Votes).HasForeignKey(v=>v.CommentId)
            .HasPrincipalKey(c=>c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        voteBuilder.HasOne<Session>(v=>v.Voter).WithMany().HasForeignKey(v => v.VoterId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var notificationBuilder = modelBuilder.Entity<Notification>().UseTptMappingStrategy();
        // notificationBuilder.HasDiscriminator(n => n.NotificationType);
        notificationBuilder.HasOne<User>(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId)
            .HasPrincipalKey(u => u.Id)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        notificationBuilder.Property<DateTime>(n=>n.Time).HasDefaultValue(DateTime.UtcNow + TimeSpan.FromHours(3));
        var requestBuilder = modelBuilder.Entity<Request>().UseTphMappingStrategy();
        // requestBuilder.HasDiscriminator(r => r.NotificationType).HasValue("Request");
        requestBuilder.HasBaseType<Notification>().UseTphMappingStrategy();
        requestBuilder.HasDiscriminator(r=>r.RequestType);
        requestBuilder.HasKey(r => r.Id);
        requestBuilder.Property(r => r.Id).ValueGeneratedOnAdd();
        requestBuilder.HasOne<User>(r => r.Requester).WithMany(u => u.Requests).HasForeignKey(u => u.RequesterId).HasPrincipalKey(r => r.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        var refundRequestBuilder = modelBuilder.Entity<RefundRequest>();
        refundRequestBuilder.HasBaseType<Request>().HasDiscriminator(r=>r.RequestType).HasValue("Refund");
        refundRequestBuilder.HasOne<OrderItem>(r => r.Item).WithOne().HasForeignKey<RefundRequest>(r=>new {r.OrderId, r.SellerId, r.ProductId})
            .HasPrincipalKey<OrderItem>(o=>new {o.OrderId, o.SellerId, o.ProductId}).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var permissionRequestBuilder = modelBuilder.Entity<PermissionRequest>();
        permissionRequestBuilder.HasBaseType<Request>().HasDiscriminator(r=>r.RequestType).HasValue("Permission");
        permissionRequestBuilder.HasOne<Permission>(p => p.Permission).WithMany().HasForeignKey(r => r.PermissionId)
            .HasPrincipalKey(p => p.Id)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        var couponNotificationBuilder = modelBuilder.Entity<CouponNotification>();
        couponNotificationBuilder.HasBaseType<Notification>();
        couponNotificationBuilder.HasOne<Coupon>(c => c.Coupon).WithMany().HasForeignKey(c => c.CouponId)
            .HasPrincipalKey(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        couponNotificationBuilder.HasOne<Seller>(c => c.Seller).WithMany().HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var voteNotificationBuilder = modelBuilder.Entity<VoteNotification>();
        voteNotificationBuilder.HasBaseType<Notification>();
        voteNotificationBuilder.HasOne<ProductReview>(n => n.Review).WithMany().HasForeignKey(n => n.ReviewId).HasPrincipalKey(v => v.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        voteNotificationBuilder.HasOne<ReviewComment>(n=>n.Comment).WithMany().HasForeignKey(n=>n.CommentId).HasPrincipalKey(c=>c.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        var reviewNotificationBuilder = modelBuilder.Entity<ReviewNotification>();
        reviewNotificationBuilder.HasBaseType<Notification>();
        reviewNotificationBuilder.HasOne<ProductReview>(n => n.Review).WithOne().HasForeignKey<ReviewNotification>(n => n.ReviewId)
            .HasPrincipalKey<ProductReview>(r => r.Id).IsRequired().OnDelete(DeleteBehavior.ClientSetNull);
        var rcnBuilder = modelBuilder.Entity<ReviewCommentNotification>();
        rcnBuilder.HasBaseType<ReviewCommentNotification>();
        rcnBuilder.HasOne<ReviewComment>(r => r.ReviewComment).WithOne()
            .HasForeignKey<ReviewCommentNotification>(n => n.CommentId)
            .HasPrincipalKey<ReviewComment>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.ClientSetNull);
        var orderNotificationBuilder = modelBuilder.Entity<OrderNotification>();
        orderNotificationBuilder.HasBaseType<Notification>();
        orderNotificationBuilder.HasOne<OrderItem>(n => n.Item).WithOne()
            .HasForeignKey<OrderNotification>(n => new{ n.OrderId, n.UserId, n.ProductId })
            .HasPrincipalKey<OrderItem>(o => new{ o.OrderId, o.SellerId, o.ProductId }).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var discountNotificationBuilder = modelBuilder.Entity<DiscountNotification>();
        discountNotificationBuilder.HasBaseType<Notification>();
        discountNotificationBuilder.HasOne<ProductOffer>(d => d.ProductOffer).WithMany()
            .HasForeignKey(n => new{ n.SellerId, n.ProductId })
            .HasPrincipalKey(o => new{ o.SellerId, o.ProductId }).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var cancellationRequestBuilder = modelBuilder.Entity<CancellationRequest>(); 
        cancellationRequestBuilder.HasBaseType<Request>().HasDiscriminator(c=>c.RequestType).HasValue("Cancellation");
    }

    private const int ShopNameMaxLength = 25;

    public enum Annotations
    {
        Validation_Positive,
        Validation_MaxValue,
        Validation_MinValue,
    }
}
