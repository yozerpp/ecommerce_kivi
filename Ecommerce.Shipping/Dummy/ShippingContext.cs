using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Entity;
using Microsoft.EntityFrameworkCore;
namespace Ecommerce.Shipping.Dummy;

public class ShippingContext : DbContext
{
    public static readonly List<Provider> DummyProviders =[
        new Provider{ Id = 1, Name = "Yurtiçi Kargo" },
        new Provider{ Id = 2, Name = "Mng Kargo" },
        new Provider{ Id = 3, Name = "Ptt kargo" }
    ];
    public static readonly string DefaultConntectionString = "Server=localhost;Database=Shipping;User Id=sa;Password=12345;TrustServerCertificate=True";
    public ShippingContext(DbContextOptions<ShippingContext> options) :base(options){ }
    public ShippingContext() : base(new DbContextOptionsBuilder<ShippingContext>().UseSqlServer(DefaultConntectionString).Options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        
        modelBuilder.Entity<ShippingOffer>(o => {
            o.HasKey(o => o.Id);
            o.HasOne<Provider>(o => o.Provider).WithMany()
                .HasForeignKey(o => o.ProviderId).HasPrincipalKey(o => o.Id);
            o.Property(o => o.Amount).HasPrecision(10, 2);
            o.Property(o => o.AmountTax).HasPrecision(10, 2);
        });
        modelBuilder.Entity<Provider>(p => {
            p.HasKey(p => p.Id);
            p.Property<string>(p => p.Name).IsRequired();
            p.HasData(DummyProviders);
        });
        modelBuilder.Entity<Shipment>(s => {
            s.HasKey(s => s.Id);
            s.HasOne<ShippingOffer>(s => s.ShippingOffer).WithOne().HasForeignKey<Shipment>(s => s.OfferId)
                .HasPrincipalKey<ShippingOffer>(s => s.Id);
            s.OwnsMany<ShipmentItem>(s => s.Items,i=>i.WithOwner(i=>i.Shipment).HasForeignKey(i=>i.ShipmentId).HasPrincipalKey(i=>i.Id));
            s.HasOne<Provider>(s => s.Provider).WithMany(s => s.Shipments).HasForeignKey(s => s.ProviderId)
                .HasPrincipalKey(s => s.Id)
                .IsRequired().OnDelete(DeleteBehavior.ClientCascade);
            s.ComplexProperty<Address>(s => s.CurrentAddress).IsRequired();
            s.ComplexProperty<Address>(s => s.ShippingAddress).IsRequired();
            s.ComplexProperty<Address>(s => s.DeliveryAddress).IsRequired();
            s.ComplexProperty<PhoneNumber>(s => s.RecepientPhoneNumber).IsRequired();
            s.ComplexProperty<PhoneNumber>(s => s.SenderPhoneNumber).IsRequired();
        });
    }
}