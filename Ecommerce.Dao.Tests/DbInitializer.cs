using System.Diagnostics;
using Ecommerce.Dao.Concrete;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using NUnit.Framework.Legacy;

namespace Ecommerce.Dao.Tests;

public class Tests
{
    private DefaultDbContext _dbContext;
    private IRepository<Cart> _repo1, _repo2;
    [SetUp]
    public void Setup() {
        _dbContext = new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer("Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;")
            .EnableSensitiveDataLogging().Options);
        _repo1 = LockingMockRepository<Cart>.Create();
        _repo2 = LockingMockRepository<Cart>.Create();
    }

    [Test]
    public void TestTransactions() {
        var watch = Stopwatch.StartNew();
        //these need to be serialzied.
        var t1 =new Thread(() => _repo1.Add(new Cart{ }));
        var t2= new Thread(()=> _repo2.Add(new Cart()));
        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();
        ClassicAssert.GreaterOrEqual(watch.ElapsedMilliseconds, TimeSpan.FromMilliseconds(2000).Milliseconds);
    }
    [Test]
    public void initDb() {
        if (false){
            return;
        }
        DatabaseInitializer initializer = new DatabaseInitializer(
           _dbContext,
            new Dictionary<Type, int?> {
                { typeof(User), 100 },
                { typeof(Seller), 20 },
                { typeof(Product), 100 },
                { typeof(ProductOffer), 200 },
                { typeof(Cart), 100 },
                { typeof(CartItem), 100 },
                { typeof(Session), 100 },
                { typeof(Order), 30 },
                { typeof(Payment), 30 }
            }
        );
        initializer.initialize();
    }
    [TearDown]
    public void TearDown() {
        _dbContext.Dispose();
    }
}