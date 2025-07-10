using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Dao.Default;


public class DefaultDbContext : DbContext
{
    public DefaultDbContext() : base() { }
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasDefaultSchema("data");
        var userBuilder = modelBuilder.Entity<User>().UseTptMappingStrategy();
        userBuilder.HasKey(u => u.Id);
        userBuilder.Property(u => u.Id).ValueGeneratedOnAdd();
        userBuilder.HasAlternateKey(u => u.Email);
        userBuilder.HasOne<Session>(u=>u.Session).WithOne(s=>s.User).HasForeignKey<User>(s=>s.SessionId).HasPrincipalKey<Session>(s=>s.Id)
            .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
        userBuilder.ComplexProperty(u => u.PhoneNumber).IsRequired();
        userBuilder.ComplexProperty(u => u.BillingAddress).IsRequired();
        userBuilder.ComplexProperty(u => u.ShippingAddress).IsRequired();
        var sellerBuilder = modelBuilder.Entity<Seller>();
        sellerBuilder.HasBaseType<User>();
        // sellerBuilder.HasKey(s => s.Id);
        sellerBuilder.Property(s => s.SellerEmail).IsRequired();
        sellerBuilder.HasIndex(s => s.SellerEmail).IsUnique();
        sellerBuilder.HasOne<User>().WithOne().HasForeignKey<Seller>(s => s.Id).HasPrincipalKey<User>(u=>u.Id).OnDelete(DeleteBehavior.Cascade);
        sellerBuilder.ComplexProperty(s => s.Address).IsRequired();
        sellerBuilder.ComplexProperty(s => s.SellerPhoneNumber).IsRequired();
        sellerBuilder.HasMany(s => s.Offers);
        var productOfferBuilder = modelBuilder.Entity<ProductOffer>();
        productOfferBuilder.HasKey(nameof(ProductOffer.SellerId), nameof(ProductOffer.ProductId));
        productOfferBuilder.HasOne(o => o.Product).WithMany(p => p.Offers).HasForeignKey(o => o.ProductId).HasPrincipalKey(p=>p.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        productOfferBuilder.HasOne(o => o.Seller).WithMany(s => s.Offers).HasForeignKey(o => o.SellerId)
            .HasPrincipalKey(s => s.Id).OnDelete(DeleteBehavior.Cascade);
        var cartItemBuilder = modelBuilder.Entity<CartItem>();
        cartItemBuilder.HasKey(nameof(CartItem.SellerId), nameof(CartItem.ProductId), nameof(CartItem.CartId));
        cartItemBuilder.HasOne<Cart>(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci=>ci.CartId).HasPrincipalKey(c=>c.Id).IsRequired().OnDelete(DeleteBehavior.Cascade);
        cartItemBuilder.HasOne<ProductOffer>(ci => ci.ProductOffer).WithMany()
            .HasForeignKey(nameof(CartItem.ProductId), nameof(CartItem.SellerId)).HasPrincipalKey(nameof(ProductOffer.SellerId), nameof(ProductOffer.ProductId)).IsRequired().OnDelete(DeleteBehavior.Cascade);
        var sessionBuilder = modelBuilder.Entity<Session>();
        sessionBuilder.HasKey(s => s.Id);
        sessionBuilder.Property(s => s.Id).ValueGeneratedOnAdd();
        sessionBuilder.HasOne(s => s.User).WithOne(u => u.Session).HasForeignKey<User>(u => u.SessionId)
            .HasPrincipalKey<Session>(s => s.Id).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
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
        orderBuilder.HasOne<Cart>(o => o.Cart).WithOne().HasForeignKey<Order>(o => o.CartId)
            .HasPrincipalKey<Cart>(c => c.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.HasOne<User>(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        orderBuilder.HasOne<Payment>(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Order>(o => o.PaymentId)
            .HasPrincipalKey<Payment>(p => p.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        orderBuilder.ComplexProperty(o => o.ShippingAddress).IsRequired();
        orderBuilder.Property(o => o.Date).IsRequired();
        var paymentBuilder = modelBuilder.Entity<Payment>();
        paymentBuilder.HasKey(p => p.Id);
        paymentBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        paymentBuilder.HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Payment>(o => o.OrderId)
            .HasPrincipalKey<Order>(o => o.Id).IsRequired().OnDelete(DeleteBehavior.Restrict);
        var productBuilder = modelBuilder.Entity<Product>();
        productBuilder.HasKey(p => p.Id);
        productBuilder.Property(p=>p.Id).ValueGeneratedOnAdd();
        productBuilder.HasMany(p => p.Offers).WithOne(o => o.Product).HasForeignKey(o => o.ProductId)
            .HasPrincipalKey(p => p.Id).OnDelete(DeleteBehavior.Cascade);
        productBuilder.HasOne<Category>(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId)
            .HasPrincipalKey(c => c.Id).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        var categoryBuilder = modelBuilder.Entity<Category>();
        categoryBuilder.HasKey(c => c.Id);
        categoryBuilder.Property(c => c.Id).ValueGeneratedOnAdd();
        categoryBuilder.HasOne<Category>(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .HasPrincipalKey(c => c.Id).OnDelete(DeleteBehavior.ClientSetNull).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        
    }
}