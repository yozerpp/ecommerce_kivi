using System.Diagnostics;
using System.Reflection;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using NUnit.Framework.Legacy;

namespace Ecommerce.Dao.Tests;

public class RepositoryProxyTests
{
    private IRepository<Cart> _repo1, _repo2;

    [OneTimeSetUp]
    public void Init() {
        _repo1 = MockRepository.Create<Cart>();
        _repo2 = MockRepository.Create<Cart>();
    }
    [Test]
    public void TestTransactions() {
        var watch = Stopwatch.StartNew();
        //these need to be serialzied.
        var t1 = new Thread(() => {
            Thread.Sleep(50);
            _repo1.Add(new Cart{ });
        });
        var t2= new Thread(()=> _repo2.Add(new Cart()));
        t1.Start();
        t2.Start();
        t2.Join();
        ClassicAssert.GreaterOrEqual(watch.ElapsedMilliseconds, TimeSpan.FromMilliseconds(2000).Milliseconds);
        t1.Join();
    }
    public class MockRepository : DispatchProxy 
    {
        public static IRepository<T> Create<T>() where T: class, new() {
            return DispatchProxy.Create<IRepository<T>, MockRepository>();
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
            Thread.Sleep(1000);
            return null;
        }
    }
}
