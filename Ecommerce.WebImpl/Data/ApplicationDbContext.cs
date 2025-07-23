using Ecommerce.Dao.Default;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Data;

public class ApplicationDbContext : DefaultDbContext
{
    public ApplicationDbContext(){}
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
}