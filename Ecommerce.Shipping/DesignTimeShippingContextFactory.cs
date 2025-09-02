using Ecommerce.Shipping.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ecommerce.Shipping;

public class DesignTimeShippingContextFactory :IDesignTimeDbContextFactory<ShippingContext>
{
    public ShippingContext CreateDbContext(string[] args) {
        return new ShippingContext(new DbContextOptionsBuilder<ShippingContext>().UseSqlServer(
                "Server=localhost;Database=Shipping;User Id=sa;Password=12345;TrustServerCertificate=True;Encrypt=True;",
                c => {
                    c.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    c.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    c.MigrationsAssembly(typeof(ShippingContext).Assembly.GetName().Name);
                }).EnableDetailedErrors(false)
            .EnableServiceProviderCaching().Options);
    }
}