using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Shipping;

public class ShippingContext : DbContext
{
    public DbSet<ShippingOffer> ShippingOffers { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<DeliveryInfo > DeliveryInfos { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public static readonly List<Provider> DummyProviders =[
        new Provider{ Id = 1, Name = "Yurtiçi Kargo" },
        new Provider{ Id = 2, Name = "Mng Kargo" },
        new Provider{ Id = 3, Name = "Ptt kargo" }
    ];
    public ShippingContext(DbContextOptions<ShippingContext> options) :base(options){ }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<DeliveryInfo>(d => {
            d.HasKey(d => d.Id);
            d.Property(d => d.Id).ValueGeneratedNever();
            d.ComplexProperty<Address>(d => d.Address).IsRequired();
            d.ComplexProperty<PhoneNumber>(d => d.PhoneNumber).IsRequired();
        });
        modelBuilder.Entity<ShipmentItem>(e => {
            e.HasKey(e => e.Id).IsClustered();
            e.Property(e => e.Id).ValueGeneratedOnAdd();
            e.ComplexProperty<Dimensions>(e => e.Dimensions, c => {
                c.Property(e => e.Depth).HasPrecision(5, 2).IsRequired();
                c.Property(e => e.Height).HasPrecision(5, 2).IsRequired();
                c.Property(e => e.Weight).HasPrecision(5, 2).IsRequired();
                c.Property(e => e.Width).HasPrecision(5, 2).IsRequired();
            });
        });
        modelBuilder.Entity<ShippingOffer>(o => {
            o.HasKey(o => o.Id);
            o.HasMany<ShipmentItem>(o => o.Items).WithMany();
            o.HasOne<Provider>(o => o.Provider).WithMany()
                .HasForeignKey(o => o.ProviderId).HasPrincipalKey(o => o.Id);
            o.Property(o => o.Price).HasPrecision(10, 2);
            o.Property(o => o.Tax).HasPrecision(10, 2);
            o.HasOne<DeliveryInfo>(o => o.Sender).WithMany().HasForeignKey(o => o.SenderId).HasPrincipalKey(o => o.Id)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
            o.HasOne<DeliveryInfo>(o => o.Recipient).WithMany().HasForeignKey(o => o.RecipientId).HasPrincipalKey(o => o.Id)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Provider>(p => {
            p.HasKey(p => p.Id);
            p.Property<string>(p => p.Name).IsRequired();
            p.HasData(DummyProviders);
        });
        modelBuilder.Entity<Shipment>(s => {
            s.HasKey(s => s.Id);
            s.HasMany<ShipmentItem>(s => s.Items).WithMany();
            s.HasOne<Provider>(s => s.Provider).WithMany(s => s.Shipments).HasForeignKey(s => s.ProviderId)
                .HasPrincipalKey(s => s.Id)
                .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            s.HasOne<DeliveryInfo>(o => o.Recipient).WithMany().HasForeignKey(o => o.RecipientId).HasPrincipalKey(o => o.Id)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
            s.HasOne<DeliveryInfo>(o => o.Sender).WithMany().HasForeignKey(o => o.SenderId).HasPrincipalKey(o => o.Id)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
        });
    }
}