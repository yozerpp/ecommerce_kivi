using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Default;
using Ecommerce.Dao.Spi;
public static class RepositoryFactory
{
    public static IRepository<TE> Create<TE>(DbContext defaultDbContext,params IValidator<TE>[]? validators)where TE : class, new() {
        return RepositoryProxy<TE>.Create(new EfRepository<TE>(defaultDbContext), validators);
    }
}