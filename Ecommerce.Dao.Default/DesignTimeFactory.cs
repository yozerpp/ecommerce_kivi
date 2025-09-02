using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ecommerce.Dao.Default;

public class DesignTimeFactory : IDesignTimeDbContextFactory<DefaultDbContext>
{
    public DefaultDbContext CreateDbContext(string[] args) {
        return new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>().UseSqlServer(
                DefaultDbContext.DefaultConnectionString,
                c => {
                    c.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    c.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.GetName().Name);
                }).EnableDetailedErrors(false)
            .EnableServiceProviderCaching().Options);
    }
}