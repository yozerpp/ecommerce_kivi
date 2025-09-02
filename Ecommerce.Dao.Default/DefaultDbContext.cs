using System.Text.Json;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Ecommerce.Dao.Default
{
public class DefaultDbContext : DbContext
{
    public const string DefaultConnectionString = "Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;TrustServerCertificate=True;";
    public const string DefaultSchema = "data";
    public DefaultDbContext() : base() { }
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.Owned<Address>();
        var userBuilder = modelBuilder.Entity<User>().UseTphMappingStrategy();
        userBuilder.HasDiscriminator<User.UserRole>(nameof(User.Role))
            .HasValue<Customer>(User.UserRole.Customer).HasValue<Seller>(User.UserRole.Seller)
            .HasValue<Staff>(User.UserRole.Staff).IsComplete();
        // userBuilder.Property(u => u.UserName).IsRequired();
        // userBuilder.Property(u=>u.UserName)
        // userBuilder.HasAlternateKey(u => u.UserName);
        userBuilder.HasKey(u => u.Id);
        userBuilder.Property(u => u.Id).ValueGeneratedOnAdd();
        userBuilder.HasAlternateKey(u => u.NormalizedEmail);
        userBuilder.Property(u => u.PasswordHash).HasMaxLength(24).IsRequired(false);
        userBuilder.HasOne<Session>(u=>u.Session).WithOne(s=>s.User).HasForeignKey<User>(s=>s.SessionId).HasPrincipalKey<Session>(s=>s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict).Metadata.DependentToPrincipal!.SetIsEagerLoaded(true);
        userBuilder.HasOne<Image>(u => u.ProfilePicture).WithOne()
            .HasForeignKey<User>(u => u.ProfilePictureId).HasPrincipalKey<Image>(p => p.Id).IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict).Metadata.IsUnique=false;
        userBuilder.ComplexProperty<PhoneNumber>(u => u.PhoneNumber, c => {
            c.IsRequired();
            c.Property(p => p.CountryCode).IsRequired();
            c.Property(p => p.Number).HasMaxLength(30).IsRequired();
        });
        userBuilder.HasMany(u => u.ReviewComments).WithOne(rc => rc.User).HasForeignKey(r => r.UserId).HasPrincipalKey(u => u.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        // userBuilder.HasMany<Notification>(u=>u.Notifications).WithOne(n=>n.User).HasForeignKey(n=>n.UserId).HasPrincipalKey(u=>u.Id)
        // .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        var customerBuilder = modelBuilder.Entity<Customer>();
        customerBuilder.HasBaseType<User>();
        customerBuilder.HasMany(c => c.FavoriteSellers).WithMany(s => s.FavoredCustomers);
        customerBuilder.HasMany(u => u.Reviews).WithOne(r => r.Reviewer).HasForeignKey(u => u.ReviewerId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);

        customerBuilder.HasMany<Order>(c=>c.Orders).WithOne(o=>o.User).HasForeignKey(o=>o.UserId).HasPrincipalKey(u=>u.Id)
            .IsRequired().OnDelete(DeleteBehavior.Restrict).Metadata.GetNavigation(false);
        customerBuilder.HasMany<Product>(c => c.FavoriteProducts).WithMany(p => p.FavoredCustomers)
            .UsingEntity<ProductFavor>(
                l=>
                    l.HasOne<Product>(f => f.Product).WithMany().HasForeignKey(f => f.ProductId)
                        .HasPrincipalKey(c => c.Id).IsRequired().OnDelete(DeleteBehavior.ClientCascade),
                r => 
                    r.HasOne<Customer>(f => f.Customer).WithMany().HasForeignKey(c => c.CustomerId)
                        .HasPrincipalKey(c => c.Id)
                        .IsRequired().OnDelete(DeleteBehavior.ClientCascade),
                e=>e.HasKey(e=>new {e.CustomerId, e.ProductId})
            );
        customerBuilder.Ignore(c => c.PrimaryAddress);
        var opt = new JsonSerializerOptions(){ WriteIndented = false, };
        customerBuilder.Property<IList<Address>>(c => c.Addresses).HasConversion<string>(
            a => JsonSerializer.Serialize(a, opt), s => JsonSerializer.Deserialize<List<Address>>(s, opt)).IsUnicode();
        customerBuilder.HasMany<RefundRequest>(c => c.RefundRequests).WithOne();
        customerBuilder.HasMany<CancellationRequest>(c => c.CancellationRequests).WithOne(c => c.Customer)
            .HasForeignKey(c => c.RequesterId).HasPrincipalKey(c => c.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        customerBuilder.HasMany<Seller>(c => c.FavoriteSellers).WithMany(s => s.FavoredCustomers)
            .UsingEntity<SellerFavor>(t => 
                    t.HasOne<Seller>(t => t.Seller).WithMany().HasForeignKey(t => t.SellerId).HasPrincipalKey(s => s.Id)
                        .IsRequired().OnDelete(DeleteBehavior.ClientCascade),
                l=>
                    l.HasOne<Customer>(t => t.Customer).WithMany().HasForeignKey(s => s.CustomerId)
                        .HasPrincipalKey(s => s.Id)
                        .IsRequired().OnDelete(DeleteBehavior.ClientCascade),
                e=>e.HasKey(t=>new {t.CustomerId, t.SellerId})
            );
        customerBuilder.OwnsOne<CustomerStats>(c => c.Stats, cs => {
            cs.HasKey(s => s.CustomerId);
            cs.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
            cs.WithOwner().HasForeignKey(s => s.CustomerId).HasPrincipalKey(c => c.Id).Metadata.IsUnique = true;
            cs.ToView($"{nameof(CustomerStats)}", DefaultSchema, v => {
                v.Property(s => s.CustomerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                v.Property(s => s.TotalOrders).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.TotalReviews).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.TotalComments).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.ReviewVotes).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.CommentVotes).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.TotalSpent).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.TotalDiscountUsed).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(s => s.TotalKarma).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            });
        });
        var sellerBuilder = modelBuilder.Entity<Seller>();
        sellerBuilder.HasBaseType<User>();
        sellerBuilder.ComplexProperty<Address>(s => s.Address, a => {
            a.Property(a => a.City).IsRequired();
            a.Property(a => a.Country).IsRequired().HasDefaultValue("Türkiye");
            a.Property(a => a.District).IsRequired();
            a.Property(a => a.Line1).IsRequired();
            a.Property(a => a.ZipCode).IsRequired();
            a.Property(a => a.Line2).IsRequired(false);
            a.Property(a => a.ApiId).IsRequired(false);
        });
        // sellerBuilder.HasKey(s => s.Id);
        sellerBuilder.Property<string>(s => s.ShopName).HasMaxLength(ShopNameMaxLength).IsRequired();
        // sellerBuilder.ComplexProperty<Address>(s => s.ShippingAddress, nameof(Seller.ShippingAddress)).IsRequired();
        sellerBuilder.HasMany(s => s.Offers).WithOne(o=>o.Seller).HasForeignKey(o=>o.SellerId).HasPrincipalKey(s=>s.Id)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        sellerBuilder.HasMany<Coupon>(s => s.Coupons).WithOne(c => c.Seller).HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        sellerBuilder.OwnsOne<SellerOrderStats>(s => s.OrderStats, os => {
            var nav = os.WithOwner().HasForeignKey(s => s.SellerId).HasPrincipalKey(s => s.Id).Metadata;
            nav.IsUnique = true;
            nav.GetNavigation(false).SetIsEagerLoaded(false);
            os.ToView($"{nameof(SellerOrderStats)}", DefaultSchema, vb => {
                vb.Property(s => s.SellerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                vb.Property(s => s.TotalComplete).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.CountComplete).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.TotalCanceled).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.CountCanceled).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.TotalInProgress).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.CountInProgress).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            });
        });
        sellerBuilder.OwnsOne<SellerStats>(s => s.Stats, ss => {
            ss.HasKey(s => s.SellerId);
            ss.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
            ss.WithOwner().HasForeignKey(s => s.SellerId).HasPrincipalKey(s => s.Id).Metadata.IsUnique = true;
            ss.ToView($"{nameof(SellerStats)}", DefaultSchema, vb => {
                vb.Property(s => s.SellerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                vb.Property(s => s.OfferCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.ReviewCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.ReviewAverage).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.RatingTotal).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.SaleCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.TotalSold).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                vb.Property(s => s.RefundCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            });
        });
        sellerBuilder.HasMany<Product>(s => s.Products).WithMany(p => p.Sellers).UsingEntity<ProductOffer>(r =>
                r.HasOne<Product>(o => o.Product).WithMany(p => p.Offers).HasForeignKey(o => o.ProductId)
                    .HasPrincipalKey(o => o.Id)
                    .IsRequired().OnDelete(DeleteBehavior.Cascade),
            l => l.HasOne<Seller>(o => o.Seller).WithMany(o => o.Offers).HasForeignKey(o => o.SellerId)
                .HasPrincipalKey(s => s.Id)
                .IsRequired().OnDelete(DeleteBehavior.Cascade),
            entity => {
                entity.HasKey(o => new{ o.SellerId, o.ProductId }).IsClustered(false);
                entity.HasMany<ProductOption>(e => e.Options);
                entity.HasMany<ProductReview>(o => o.Reviews).WithOne(r => r.Offer).HasForeignKey(nameof(ProductReview.SellerId),nameof(ProductReview.ProductId))
                    .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasMany<OrderItem>(p=>p.BoughtItems).WithOne(o=>o.ProductOffer).HasForeignKey(nameof(OrderItem.SellerId),nameof(OrderItem.ProductId))
                    .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
                entity.Property<decimal>(p => p.Discount).HasPrecision(3, 2).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 1m).IsRequired().HasDefaultValue(1m).ValueGeneratedNever();
                entity.Property<decimal>(p => p.Price).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 10000m).IsRequired().ValueGeneratedNever();
                entity.Property<uint>(p => p.Stock).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 10000).IsRequired().ValueGeneratedNever();
                entity.HasIndex(o => o.ProductId).IsClustered();
                entity.HasIndex(o => o.SellerId).IsClustered(false);
                entity.Ignore(o => o.DiscountedPrice);
                entity.OwnsOne<OfferStats>(o => o.Stats, e => {
                    e.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                    e.WithOwner().HasForeignKey(os => new{ os.SellerId, os.ProductId })
                        .HasPrincipalKey(o => new{ o.SellerId, o.ProductId }).Metadata.IsUnique = true;
                    e.ToView($"{nameof(OfferStats)}", DefaultSchema, vb => {
                        vb.Property(os => os.SellerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                        vb.Property(os => os.ProductId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                        vb.Property(os => os.ReviewCount).Overrides.Property.ValueGenerated =
                            ValueGenerated.OnAddOrUpdate;
                        vb.Property(os=>os.ReviewAverage).Overrides.Property.ValueGenerated =
                            ValueGenerated.OnAddOrUpdate;
                        vb.Property(os => os.RatingTotal).Overrides.Property.ValueGenerated =
                            ValueGenerated.OnAddOrUpdate;
                        vb.Property(os => os.RefundCount).Overrides.Property.ValueGenerated =
                            ValueGenerated.OnAddOrUpdate;
                    });
                });
        });
        var staffBuilder = modelBuilder.Entity<Staff>();
        staffBuilder.HasBaseType<User>();
        staffBuilder.HasMany<PermissionClaim>(s => s.PermissionClaims).WithOne(p => p.Grantee).HasForeignKey(p=>p.GranteeId).HasPrincipalKey(s=>s.Id);
        staffBuilder.HasMany<PermissionClaim>(s => s.PermissionGrants).WithOne(p => p.Granter)
            .HasForeignKey(p => p.GranteeId).HasPrincipalKey(s => s.Id);
        staffBuilder.HasOne<Staff>(s => s.Manager).WithMany(s => s.TeamMembers).HasForeignKey(s => s.ManagerId)
            .HasPrincipalKey(s => s.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull).Metadata.IsUnique = false;
        staffBuilder.HasMany<PermissionRequest>(s => s.SentPermissionRequests).WithOne(s => s.Requester)
            .HasForeignKey(s => s.RequesterId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        staffBuilder.HasMany<PermissionRequest>(s => s.ReceivedPermissionRequests)
            .WithOne(s => s.Requestee)
            .HasForeignKey(s => s.UserId).HasPrincipalKey(s => s.Id).IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
        staffBuilder.HasMany<CancellationRequest>(s => s.CancellationRequests).WithOne(c => c.Staff)
            .HasForeignKey(c => c.UserId).HasPrincipalKey(c => c.Id).IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
        var permissionBuilder = modelBuilder.Entity<Permission>();
        permissionBuilder.HasKey(p => p.Id);
        permissionBuilder.Property(p => p.Id);
        var permissionClaimBuilder = modelBuilder.Entity<PermissionClaim>();
        permissionClaimBuilder.HasKey(p => p.Id);
        permissionClaimBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        permissionClaimBuilder.Ignore(p => p.NotExpired);
        permissionClaimBuilder.HasOne<Staff>(p => p.Grantee).WithMany(s => s.PermissionClaims)
            .HasForeignKey(p => p.GranteeId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        permissionClaimBuilder.HasOne<Staff>(p => p.Granter).WithMany(s => s.PermissionGrants).HasForeignKey(p => p.GranterId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        permissionClaimBuilder.HasOne<Permission>(p => p.Permission).WithMany().HasForeignKey(p => p.PermissionId)
            .HasPrincipalKey(p => p.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var cartItemBuilder = modelBuilder.Entity<CartItem>(entity => {
            entity.HasKey(e => new{ e.SellerId, e.ProductId, e.CartId }).IsClustered(false);
            entity.HasOne<ProductOffer>(ci => ci.ProductOffer).WithMany()
                .HasForeignKey(nameof(CartItem.SellerId),nameof(CartItem.ProductId)).HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Cart>(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci=>ci.CartId).HasPrincipalKey(c=>c.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Coupon>(ci => ci.Coupon).WithMany().HasForeignKey(ci => ci.CouponId).HasPrincipalKey(c => c.Id)
                .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.Property<uint>(ci => ci.Quantity).HasAnnotation(nameof(Annotations.Validation_Positive), true)
                .HasAnnotation(nameof(Annotations.Validation_MaxValue), 100).IsRequired().ValueGeneratedNever();
            entity.HasIndex(e => new { e.CartId })
                .IsClustered(true);
            entity.OwnsOne<CartItemAggregates>(o => o.Aggregates, e => {
                e.HasKey(a => new{ a.SellerId, a.ProductId,a.CartId });
                e.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                e.WithOwner().HasForeignKey(a => new{ a.SellerId, a.ProductId,a.CartId })
                    .HasPrincipalKey(o => new{ o.SellerId, o.ProductId,o.CartId }).Metadata.IsUnique = true;
                e.ToView(nameof(CartItemAggregates), DefaultSchema, v => {
                    v.Property(a => a.CartId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.SellerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.ProductId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.BasePrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.DiscountedPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.CouponDiscountedPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.TotalDiscountPercentage).Overrides.Property.ValueGenerated =
                        ValueGenerated.OnAddOrUpdate;
                });
            });
            entity.HasMany<ProductOption>(c=>c.SelectedOptions).WithMany().LeftNavigation.SetIsEagerLoaded(false);
        });
        var sessionBuilder = modelBuilder.Entity<Session>();
        sessionBuilder.HasKey(s => s.Id);
        sessionBuilder.Property(s => s.Id).ValueGeneratedOnAdd();
        sessionBuilder.HasOne<User>(s => s.User).WithOne(u => u.Session).HasForeignKey<User>(u => u.SessionId)
            .HasPrincipalKey<Session>(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        sessionBuilder.HasOne(s => s.Cart).WithOne(c => c.Session).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict).Metadata.DependentToPrincipal.SetIsEagerLoaded(false);
        sessionBuilder.HasMany<Category>(s=>s.VisitedCategories).WithMany().UsingEntity<SessionVisitedCategory>(
                r=>r.HasOne<Category>(v=>v.Category).WithMany().HasForeignKey(v=>v.CategoryId).HasPrincipalKey(c=>c.Id),
                l=>l.HasOne<Session>(v=>v.Session).WithMany().HasForeignKey(c=>c.SessionId).HasPrincipalKey(c=>c.Id),
                t=>t.HasKey(t=>new { t.SessionId, t.CategoryId}));
        var cartBuilder = modelBuilder.Entity<Cart>();
        cartBuilder.HasKey(c => c.Id);
        cartBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        cartBuilder.HasOne<Session>(c => c.Session).WithOne(s => s.Cart).HasForeignKey<Session>(s => s.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        cartBuilder.OwnsOne<CartAggregates>(o => o.Aggregates, c => {
            c.HasKey(v => v.CartId);
            c.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
            c.WithOwner().HasForeignKey(v => v.CartId).HasPrincipalKey(o => o.Id).Metadata.IsUnique = true;
            c.ToView($"{nameof(CartAggregates)}_{nameof(CartAggregates)}", DefaultSchema, v => {
                v.Property(os => os.CartId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                v.Property(os => os.ItemCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.BasePrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.CouponDiscountedPrice).Overrides.Property.ValueGenerated =
                    v.Property(os => os.DiscountedPrice).Overrides.Property.ValueGenerated =
                        ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.CouponDiscountAmount).Overrides.Property.ValueGenerated =
                    ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.TotalDiscountPercentage).Overrides.Property.ValueGenerated =
                    ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.DiscountAmount).Overrides.Property.ValueGenerated =
                    ValueGenerated.OnAddOrUpdate;
            });
        });
        var anonymousUserBuilder = modelBuilder.Entity<AnonymousCustomer>();
        anonymousUserBuilder.HasKey(a => a.Email);
        anonymousUserBuilder.HasMany<Order>(a => a.Orders).WithOne(o => o.AnonymousUser).HasForeignKey(o => o.Email)
            .HasPrincipalKey(a => a.Email);
        var shipmentBuilder = modelBuilder.Entity<Shipment>();
        shipmentBuilder.HasKey(s => s.Id);
        shipmentBuilder.Property(s => s.Id).ValueGeneratedOnAdd();
        shipmentBuilder.HasMany<OrderItem>(s => s.OrderItems).WithOne(o => o.SentShipment)
            .HasForeignKey(o => o.ShipmentId).HasPrincipalKey(s => s.Id).IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        shipmentBuilder.ComplexProperty<Address>(s => s.RecepientAddress).IsRequired();
        shipmentBuilder.ComplexProperty<Address>(s => s.SenderAddress).IsRequired();
        var orderBuilder = modelBuilder.Entity<Order>();
        orderBuilder.HasKey(o => o.Id);
        orderBuilder.HasOne<AnonymousCustomer>(o => o.AnonymousUser).WithMany(u => u.Orders);
        orderBuilder.Property(o=>o.Id).ValueGeneratedOnAdd();
        orderBuilder.Property(o => o.Status).HasDefaultValue(OrderStatus.WaitingConfirmation);
        orderBuilder.HasMany<OrderItem>(o => o.Items).WithOne(oi=>oi.Order).HasForeignKey(oi=>oi.OrderId)
            .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.OwnsOne<OrderAggregates>(o => o.Aggregates, c => {
            c.HasKey(v => v.OrderId);
            c.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
            c.WithOwner().HasForeignKey(v => v.OrderId).HasPrincipalKey(o => o.Id).Metadata.IsUnique = true;
            c.ToView($"{nameof(OrderAggregates)}_{nameof(OrderAggregates)}", DefaultSchema, v => {
                v.Property(os => os.OrderId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                v.Property(os => os.ItemCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.BasePrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.DiscountedPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.CouponDiscountedPrice).Overrides.Property.ValueGenerated =
                    ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.DiscountAmount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.CouponDiscountAmount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.TotalDiscountAmount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                v.Property(os => os.TotalDiscountPercentage).Overrides.Property.ValueGenerated =
                    ValueGenerated.OnAddOrUpdate;
            });
        });
        orderBuilder.HasOne<Session>(o => o.Session).WithMany().HasForeignKey(o => o.SessionId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<Customer>(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        orderBuilder.HasOne<Payment>(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired(true).OnDelete(DeleteBehavior.Restrict);
        orderBuilder.ComplexProperty(o => o.ShippingAddress).IsRequired();
        orderBuilder.Property(o => o.Date).IsRequired();
        modelBuilder.Entity<Payment>(paymentBuilder => {
            paymentBuilder.UseTphMappingStrategy();
            paymentBuilder.HasKey(p => p.Id).IsClustered(true);
            paymentBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
            paymentBuilder.HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Order>(o => o.PaymentId)
                .HasPrincipalKey<Payment>(p => p.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            paymentBuilder.HasDiscriminator<PaymentMethod>(p=>p.Method).HasValue<CardPayment>(PaymentMethod.Card).HasValue<CashPayment>(PaymentMethod.Cash).IsComplete();
        });
        modelBuilder.Entity<CardPayment>(cardPaymentBuilder => {
            cardPaymentBuilder.HasBaseType<Payment>();
            cardPaymentBuilder.Property(c => c.ApiId).IsRequired(true);
        });
        modelBuilder.Entity<CashPayment>(cashPaymentBuilder => {
            cashPaymentBuilder.HasBaseType<Payment>();
        });
        var productCategoryPropertyBuilder = modelBuilder.Entity<ProductCategoryProperty>(b => {
            b.HasKey(c => new{ c.CategoryPropertyId, c.ProductId }).IsClustered(false);
            // b.WithOwner(p => p.Product).HasForeignKey(p => p.ProductId).HasPrincipalKey(p => p.Id)
            //     .Metadata.GetNavigation(false).SetIsEagerLoaded(false);
            b.HasIndex(c => c.ProductId).IsClustered();
            b.HasOne<CategoryProperty>(p => p.CategoryProperty).WithMany()
                .HasForeignKey(p => p.CategoryPropertyId).HasPrincipalKey(p => p.Id).IsRequired(true).OnDelete(DeleteBehavior.Restrict)
                .Metadata.GetNavigation(true).SetIsEagerLoaded(true);
        });
        var productOptionBuilder = modelBuilder.Entity<ProductOption>(c => {
            c.HasKey(o => new{ o.Id });
            c.Property(o => o.Id).ValueGeneratedOnAdd();
            c.HasOne<ProductOffer>(o => o.ProductOffer).WithMany(o=>o.Options).HasForeignKey(o => new{ o.SellerId, o.ProductId })
                .HasPrincipalKey(o => new{ o.SellerId, o.ProductId }).IsRequired(true).OnDelete(DeleteBehavior.ClientCascade);
            c.HasOne<ProductCategoryProperty>(o => o.Property).WithMany()
                .HasForeignKey(o => new{ o.CategoryPropertyId, o.ProductId })
                .HasPrincipalKey(c => new{ c.CategoryPropertyId, c.ProductId }).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
        var productBuilder = modelBuilder.Entity<Product>(entity => {
            entity.HasKey(p => p.Id);
            entity.Ignore(p => p.MainImage);
            entity.OwnsOne<ProductStats>(p => p.Stats, e => {
                e.WithOwner().HasForeignKey(p => p.ProductId)
                    .HasPrincipalKey(p => p.Id).Metadata.IsUnique=true;
                e.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                e.ToView($"{nameof(ProductStats)}",DefaultSchema, b => {
                    b.Property(p => p.ProductId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    b.Property(p => p.MinPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p => p.MaxPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p => p.ReviewCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p=>p.RatingTotal).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.RatingAverage).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p =>p.OrderCount ).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p => p.SaleCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p => p.FavorCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(p => p.RefundCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                });
            });
            entity.OwnsOne<ProductRatingStats>(s => s.RatingStats, b => {
                b.WithOwner().HasForeignKey(s => s.ProductId).HasPrincipalKey(s => s.Id).Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                b.ToView($"{nameof(ProductRatingStats)}", DefaultSchema, b => {
                    b.Property(s => s.ProductId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    b.Property(s=>s.ReviewCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.FiveStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.FourStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.ThreeStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.TwoStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.OneStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    b.Property(s => s.ZeroStarCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                });
                });
            entity.Property(p=>p.Id).ValueGeneratedOnAdd();
            entity.HasMany(p => p.Offers).WithOne(o => o.Product).HasForeignKey(o => o.ProductId)
                .HasPrincipalKey(p => p.Id).OnDelete(DeleteBehavior.Cascade);
            entity.ComplexProperty<Dimensions>(p => p.Dimensions, c => {
                c.Property(d => d.Height).HasAnnotation(nameof(Annotations.Validation_Positive),true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 20m)
                    .HasPrecision(4,2).IsRequired();
                c.Property(d => d.Depth).HasPrecision(4,2).HasAnnotation(nameof(Annotations.Validation_Positive),true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 20m).IsRequired();
                c.Property(d=>d.Width).HasPrecision(4,2).HasAnnotation(nameof(Annotations.Validation_Positive),true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 20m).IsRequired();
                c.Property(d => d.Weight).HasPrecision(4,2).HasAnnotation(nameof(Annotations.Validation_Positive),true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 20m).IsRequired();
            });
            entity.HasOne<Category>(p => p.Category).WithMany(c=>c.Products).HasForeignKey(p => p.CategoryId)
                .HasPrincipalKey(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
            entity.HasMany<Customer>(p => p.FavoredCustomers).WithMany(c => c.FavoriteProducts);
            entity.HasMany<ProductCategoryProperty>(p => p.CategoryProperties).WithOne(p=>p.Product).HasForeignKey(p=>p.ProductId).HasPrincipalKey(p=>p.Id).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            entity.Property(p => p.Active).HasDefaultValue(true);
            entity.HasMany<ImageProduct>(e => e.Images).WithOne(i => i.Product).HasForeignKey(i => i.ProductId)
                .HasPrincipalKey(i => i.Id).OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
        var imageProductBuilder = modelBuilder.Entity<ImageProduct>(c => {
            c.HasKey(i => new{ i.ImageId, i.ProductId }).IsClustered(false);
            c.HasOne<Product>(i=>i.Product).WithMany(p=>p.Images).HasForeignKey(i=>i.ProductId)
                .HasPrincipalKey(p=>p.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            c.HasOne<Image>(i => i.Image).WithMany().HasForeignKey(i => i.ImageId)
                .HasPrincipalKey(i => i.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            c.Property(i => i.ImageId).ValueGeneratedNever();
            c.Property(i => i.ProductId).ValueGeneratedNever();
            c.HasIndex(i => i.ProductId).IsClustered();
        });
        var imageBuilder = modelBuilder.Entity<Image>();
        imageBuilder.HasKey(i => i.Id);
        imageBuilder.Property(i => i.Id).ValueGeneratedOnAdd();
        var categoryPropertyBuilder = modelBuilder.Entity<CategoryProperty>(p => {
            p.HasKey(c => c.Id).IsClustered(true);
            p.Property(c => c.Id).ValueGeneratedOnAdd();
            var prop = p.HasOne<Category>(c => c.Category).WithMany(c => c.CategoryProperties)
                .HasForeignKey(c => c.CategoryId).HasPrincipalKey(c => c.Id)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull)
                .Metadata;
            prop.GetNavigation(true).SetIsEagerLoaded(false);
            prop.GetNavigation(false).SetIsEagerLoaded(false);
            p.HasIndex(p => p.CategoryId).IsClustered(false);
        });
        var categoryBuilder = modelBuilder.Entity<Category>(categoryBuilder => {
            categoryBuilder.HasKey(c => c.Id);
            categoryBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
            categoryBuilder.HasOne<Category>(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
                .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull).Metadata.IsUnique=false;
            categoryBuilder.HasMany<Product>(c => c.Products).WithOne(p=>p.Category).HasForeignKey(p=>p.CategoryId).HasPrincipalKey(c=>c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        });
        var couponBuilder = modelBuilder.Entity<Coupon>(entity => {
            entity.HasKey(c => c.Id);
            entity.HasIndex(e => e.SellerId)
                .IsClustered(false)
                .IncludeProperties(c=>new {c.DiscountRate, c.ExpirationDate});
            entity.HasOne<Seller>(c => c.Seller).WithMany(s => s.Coupons).HasForeignKey(c => c.SellerId)
                .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Property<string>(c => c.Id).HasMaxLength(28).ValueGeneratedNever(); //extra space for discount amount
            entity.Property(c => c.DiscountRate).HasPrecision(2, 2).HasAnnotation(nameof(Annotations.Validation_Positive),true).IsRequired().ValueGeneratedNever();
        });
        var orderItemBuilder = modelBuilder.Entity<OrderItem>(entity => {
            entity.HasKey(o=>new {o.OrderId, o.SellerId, o.ProductId}).IsClustered(false);
            entity.HasOne<Order>(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(o => o.OrderId)
                .HasPrincipalKey(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Shipment>(o => o.SentShipment).WithMany(s=>s.OrderItems).HasForeignKey(o => o.ShipmentId)
                .HasPrincipalKey(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Shipment>(o => o.RefundShipment).WithOne()
                .HasForeignKey<OrderItem>(o => o.RefundShipmentId).HasPrincipalKey<Shipment>(s => s.Id).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProductOffer>(o=>o.ProductOffer).WithMany(of=>of.BoughtItems).HasForeignKey(nameof(OrderItem.SellerId),nameof(OrderItem.ProductId))
                .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Restrict);
            entity.Property<uint>(o => o.Quantity).HasAnnotation(nameof(Annotations.Validation_Positive), true).HasAnnotation(nameof(Annotations.Validation_MaxValue), 100).IsRequired().ValueGeneratedNever();
            entity.HasIndex(e => e.ProductId)
                .IsClustered(false)
                .IncludeProperties(e => e.Quantity);
            entity.HasIndex(e => e.SellerId)
                .IsClustered(false);
            entity.HasIndex(e => e.OrderId)
                .IsClustered();
            entity.OwnsOne<OrderItemAggregates>(o => o.Aggregates, e => {
                e.HasKey(a => new{ a.OrderId, a.SellerId, a.ProductId });
                e.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                e.WithOwner().HasForeignKey(a => new{ a.OrderId, a.SellerId, a.ProductId })
                    .HasPrincipalKey(o => new{ o.OrderId, o.SellerId, o.ProductId }).Metadata.IsUnique = true;
                e.ToView(nameof(OrderItemAggregates), DefaultSchema, v => {
                    v.Property(a => a.OrderId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.SellerId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.ProductId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    v.Property(a => a.BasePrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.DiscountedPrice).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.CouponDiscountedPrice).Overrides.Property.ValueGenerated =
                        ValueGenerated.OnAddOrUpdate;
                    v.Property(a => a.TotalDiscountPercentage).Overrides.Property.ValueGenerated =
                        ValueGenerated.OnAddOrUpdate;
                });
            });
            entity.HasMany<ProductOption>(o => o.SelectedOptions).WithMany().LeftNavigation.SetIsEagerLoaded(false);
        });
        var reviewBuilder = modelBuilder.Entity<ProductReview>(entity => {
            entity.HasKey(r => r.Id).IsClustered(false);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.HasAlternateKey(nameof(ProductReview.SessionId),nameof(ProductReview.SellerId),nameof(ProductReview.ProductId));
            entity.HasMany<ReviewComment>(r => r.Comments).WithOne(c => c.Review).HasForeignKey( r=>r.ReviewId)
                .HasPrincipalKey(r=>r.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ProductOffer>(r=>r.Offer).WithMany(o=>o.Reviews).HasForeignKey(nameof(ProductReview.SellerId),nameof(ProductReview.ProductId))
                .HasPrincipalKey(nameof(ProductOffer.SellerId),nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            entity.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ProductReview).HasForeignKey(v=>v.ReviewId)
                .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
            entity.Property<decimal>(r => r.Rating).HasPrecision(3, 2).HasAnnotation(nameof(Annotations.Validation_Positive), true)
                .HasAnnotation(nameof(Annotations.Validation_MaxValue), 5.0m).IsRequired().ValueGeneratedNever();
            entity.HasOne<Customer>(r => r.Reviewer).WithMany(u => u.Reviews).HasForeignKey(r=>r.ReviewerId).HasPrincipalKey(u=>u.Id)
                .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<Session>(r => r.Session).WithMany().HasForeignKey(r => r.SessionId)
                .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Property(r => r.Created).HasDefaultValueSql("DATEADD(HOUR, 3, GETUTCDATE())");
            entity.HasIndex(e => e.ProductId)
                .IsClustered(true);
            entity.HasIndex(e => new { e.ProductId, e.SellerId })
                .IsClustered(false)
                .IncludeProperties(e => e.Rating);
            entity.HasIndex(e => e.SellerId)
                .IsClustered(false)
                .IncludeProperties(e => e.Rating);
            entity.HasIndex(e => e.Created).IsClustered(false).IsDescending(true);
            entity.HasIndex(e => e.Rating).IsClustered(false).IsDescending(true);
            entity.OwnsOne<ReviewStats>(e => e.Stats, c => {
                c.HasKey(s => s.ReviewId);
                c.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                c.WithOwner().HasForeignKey(s => s.ReviewId).HasPrincipalKey(r => r.Id).Metadata.IsUnique = true;
                c.ToView($"{nameof(ReviewStats)}", DefaultSchema, vb => {
                    vb.Property(s => s.ReviewId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    vb.Property(s => s.CommentCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    vb.Property(s=>s.Votes).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    vb.Property(s=>s.VoteCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                });
            });
        });
        
        var commentBuilder = modelBuilder.Entity<ReviewComment>(entity => {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.HasIndex(e => e.ReviewId)
                .IsClustered(true);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.HasOne<ProductReview>(r=>r.Review).WithMany(r=>r.Comments).HasForeignKey(c=>c.ReviewId)
                .HasPrincipalKey(r=>r.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ReviewComment>(r => r.Parent).WithMany(r => r.Replies).HasForeignKey(r => r.ParentId).HasPrincipalKey(r => r.Id)
                .IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
            entity.HasMany<ReviewVote>(r=>r.Votes).WithOne(r=>r.ReviewComment).HasForeignKey(v=>v.ReviewId)
                .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
            entity.HasOne<Session>(r=>r.Commenter).WithMany().HasForeignKey(c => c.CommenterId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>(c => c.User).WithMany(u => u.ReviewComments).HasForeignKey(c => c.UserId)
                .HasPrincipalKey(c => c.Id)
                .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
            entity.Property(c => c.Created).HasDefaultValueSql("DATEADD(HOUR, 3, GETUTCDATE())");
            entity.OwnsOne<ReviewCommentStats>(e => e.Stats, c => {
                c.HasKey(s => s.CommentId);
                c.Metadata.GetNavigation(false).SetIsEagerLoaded(false);
                c.WithOwner().HasForeignKey(s => s.CommentId).HasPrincipalKey(c => c.Id).Metadata.IsUnique = true;
                c.ToView($"{nameof(ReviewCommentStats)}", DefaultSchema, vb => {
                    vb.Property(s => s.CommentId).Overrides.Property.ValueGenerated = ValueGenerated.OnAdd;
                    vb.Property(s => s.ReplyCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    vb.Property(s=>s.Votes).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    vb.Property(s => s.VoteCount).Overrides.Property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                });
            });
        });
        var voteBuilder = modelBuilder.Entity<ReviewVote>();
        voteBuilder.HasKey(v=>v.Id);
        voteBuilder.Property(v => v.Id).ValueGeneratedOnAdd();
        voteBuilder.HasOne<ProductReview>(r=>r.ProductReview).WithMany(r=>r.Votes).HasForeignKey(v=>v.ReviewId)
            .HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        voteBuilder.HasOne<ReviewComment>(r=>r.ReviewComment).WithMany(r=>r.Votes).HasForeignKey(v=>v.CommentId)
            .HasPrincipalKey(c=>c.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        voteBuilder.HasOne<Session>(v=>v.Voter).WithMany().HasForeignKey(v => v.VoterId).HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        //notficatinos
        var notificationBuilder = modelBuilder.Entity<Notification>(e => {
            e.HasKey(n => n.Id).IsClustered(false);
            e.UseTphMappingStrategy();
            e.HasDiscriminator<Notification.NotificationType>(n => n.Type)
                .HasValue<CouponNotification>(Notification.NotificationType.Coupon)
                .HasValue<ReviewNotification>(Notification.NotificationType.Review)
                .HasValue<VoteNotification>(Notification.NotificationType.Vote)
                .HasValue<DiscountNotification>(Notification.NotificationType.Discount)
                .HasValue<OrderNotification>(Notification.NotificationType.Order)
                .HasValue<ReviewCommentNotification>(Notification.NotificationType.ReviewComment)
                .HasValue<OrderCompletionNotification>(Notification.NotificationType.OrderCompletion)
                .HasValue<RefundRequest>(Notification.NotificationType.RefundRequest)
                .HasValue<PermissionRequest>(Notification.NotificationType.PermissionRequest)
                .HasValue<CancellationRequest>(Notification.NotificationType.CancellationRequest)
                .IsComplete();
            e.Property(n => n.Id).ValueGeneratedOnAdd();
            e.HasOne<User>(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId)
                .HasPrincipalKey(u => u.Id)
                .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            e.Property<DateTime>(n=>n.Time).HasDefaultValueSql("DATEADD(HOUR, 3, GETUTCDATE())");
        });
        var requestBuilder = modelBuilder.Entity<Request>();
        requestBuilder.HasBaseType<Notification>();
        requestBuilder.HasOne<User>(r => r.Requester).WithMany().HasForeignKey(u => u.RequesterId).HasPrincipalKey(r => r.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        var refundRequestBuilder = modelBuilder.Entity<RefundRequest>(entity => {
            entity.HasBaseType<Request>();
            entity.HasOne<OrderItem>(r => r.Item).WithOne(o=>o.RefundRequest)
                .HasForeignKey<RefundRequest>(r => new{ r.OrderId, r.UserId, r.ProductId }).HasPrincipalKey<OrderItem>(o=>new {o.OrderId, o.SellerId, o.ProductId})
                .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            entity.HasOne<Customer>(e => e.Customer).WithMany(c => c.RefundRequests).HasForeignKey(r=>r.RequesterId).HasPrincipalKey(r=>r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
            entity.HasOne<Seller>(r => r.Seller).WithMany(r => r.RefundRequests).HasForeignKey(r => r.UserId)
                .HasPrincipalKey(r => r.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
            entity.HasIndex(e => new { e.ProductId, e.UserId })
                .IsClustered(false)
                .IncludeProperties(e => e.IsApproved);
        });
        var permissionRequestBuilder = modelBuilder.Entity<PermissionRequest>();
        permissionRequestBuilder.HasBaseType<Request>();
        permissionRequestBuilder.HasOne<Permission>(p => p.Permission).WithMany().HasForeignKey(r => r.PermissionId)
            .HasPrincipalKey(p => p.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        permissionRequestBuilder.HasOne<Staff>(p => p.Requester).WithMany(p => p.SentPermissionRequests)
            .HasForeignKey(p => p.RequesterId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        permissionRequestBuilder.HasOne<Staff>(p => p.Requestee).WithMany(p => p.ReceivedPermissionRequests)
            .HasForeignKey(p => p.UserId).HasPrincipalKey(s => s.Id)
            .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        var couponNotificationBuilder = modelBuilder.Entity<CouponNotification>();
        couponNotificationBuilder.HasBaseType<Notification>();
        couponNotificationBuilder.HasOne<Coupon>(c => c.Coupon).WithMany().HasForeignKey(c => c.CouponId)
            .HasPrincipalKey(c => c.Id).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        couponNotificationBuilder.HasOne<Seller>(c => c.Seller).WithMany().HasForeignKey(c => c.SellerId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        couponNotificationBuilder.HasOne<Customer>(c => c.Customer).WithMany(c => c.CouponNotifications)
            .HasForeignKey(c => c.UserId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientCascade);
        var voteNotificationBuilder = modelBuilder.Entity<VoteNotification>();
        voteNotificationBuilder.HasBaseType<Notification>();
        voteNotificationBuilder.HasOne<ProductReview>(n => n.Review).WithMany().HasForeignKey(n => n.ReviewId).HasPrincipalKey(v => v.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        voteNotificationBuilder.HasOne<ReviewComment>(n=>n.Comment).WithMany().HasForeignKey(n=>n.CommentId).HasPrincipalKey(c=>c.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        voteNotificationBuilder.HasOne<Customer>(v => v.Customer).WithMany(v => v.VoteNotifications)
            .HasForeignKey(s => s.UserId).HasPrincipalKey(s => s.Id).IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
        var reviewNotificationBuilder = modelBuilder.Entity<ReviewNotification>();
        reviewNotificationBuilder.HasBaseType<Notification>();
        reviewNotificationBuilder.HasOne<Seller>(r => r.Seller).WithMany(s => s.ReviewNotifications)
            .HasForeignKey(s => s.UserId).HasPrincipalKey(s => s.Id).IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
        reviewNotificationBuilder.HasOne<ProductReview>(n => n.Review).WithOne().HasForeignKey<ReviewNotification>(n => n.ReviewId)
            .HasPrincipalKey<ProductReview>(r => r.Id).IsRequired().OnDelete(DeleteBehavior.ClientSetNull);
        var rcnBuilder = modelBuilder.Entity<ReviewCommentNotification>();
        rcnBuilder.HasBaseType<Notification>();
        rcnBuilder.HasOne<ReviewComment>(r => r.ReviewComment).WithOne()
            .HasForeignKey<ReviewCommentNotification>(n => n.CommentId)
            .HasPrincipalKey<ReviewComment>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.ClientSetNull);
        var orderNotificationBuilder = modelBuilder.Entity<OrderNotification>();
        orderNotificationBuilder.HasBaseType<Notification>();
        orderNotificationBuilder.HasOne<Seller>(o => o.Seller).WithMany().HasForeignKey(o => o.UserId)
            .HasPrincipalKey(s => s.Id).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        orderNotificationBuilder.HasOne<OrderItem>(n => n.Item).WithOne()
            .HasForeignKey<OrderNotification>(n => new{ n.OrderId, n.UserId, n.ProductId })
            .HasPrincipalKey<OrderItem>(o => new{ o.OrderId, o.SellerId, o.ProductId }).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        var discountNotificationBuilder = modelBuilder.Entity<DiscountNotification>();
        discountNotificationBuilder.HasBaseType<Notification>();
        discountNotificationBuilder.Property(d => d.DiscountAmount).HasPrecision(10, 2);
        discountNotificationBuilder.Property(d => d.DiscountRate).HasPrecision(2, 2);
        discountNotificationBuilder.HasOne<ProductOffer>(d => d.ProductOffer).WithMany()
            .HasForeignKey(n => new{ n.SellerId, n.ProductId })
            .HasPrincipalKey(o => new{ o.SellerId, o.ProductId }).IsRequired().OnDelete(DeleteBehavior.ClientCascade);
        var orderCompletionNotificationBuilder = modelBuilder.Entity<OrderCompletionNotification>();
        orderCompletionNotificationBuilder.HasBaseType<OrderNotification>();
        
        var cancellationRequestBuilder = modelBuilder.Entity<CancellationRequest>();
        cancellationRequestBuilder.HasBaseType<Request>();
        cancellationRequestBuilder.HasOne<Order>(c => c.Order).WithOne(o => o.CancellationRequest)
            .HasForeignKey<CancellationRequest>(c => c.OrderId).HasPrincipalKey<Order>(o => o.Id).IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
    }



    private void AddPhoneNumber<T>(EntityTypeBuilder<T> builder) where T : User {
        builder.ComplexProperty<PhoneNumber>(u => u.PhoneNumber, c=> {
            c.IsRequired();
            c.Property(p => p.CountryCode).HasMaxLength(5).IsRequired();
            c.Property(p => p.Number).HasMaxLength(20).IsRequired();
        });
    }
    private const int ShopNameMaxLength = 50;
}
}
