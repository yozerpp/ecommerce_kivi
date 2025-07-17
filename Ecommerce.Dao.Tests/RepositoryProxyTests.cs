using System.Diagnostics;
using System.Reflection;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using NUnit.Framework.Legacy;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

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

    [Test]
    public void TestValidation()
    {
        var mockValidator = new MockValidator<Cart>();
        var validatedRepo = RepositoryProxy<Cart>.Create(new MockRepositoryInternal<Cart>(), new IValidator<Cart>[] { mockValidator });

        var cart = new Cart(); // The actual content of the cart doesn't matter for this test

        // Expect a ValidationException because the mockValidator always returns an error
        var ex = Assert.Throws<ValidationException>(() => validatedRepo.Add(cart));
        Assert.That(ex.Message, Is.EqualTo("Validation failed: Mock validation error"));
    }

    private class MockRepository : DispatchProxy 
    {
        public static IRepository<T> Create<T>() where T: class, new() {
            return DispatchProxy.Create<IRepository<T>, MockRepository>();
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
            Thread.Sleep(1000);
            return null;
        }
    }

    // Internal mock repository for TestValidation to avoid Thread.Sleep
    private class MockRepositoryInternal<T> : IRepository<T> where T : class, new()
    {
        public List<T> All(string[][]? includes = null) => new List<T>();
        public List<TP> All<TP>(Expression<Func<T, TP>> select, string[][]? includes = null) => new List<TP>();

        public List<T> Where(Expression<Func<T, bool>> predicate, int offset = 0, int limit = 20, (Expression<Func<T, object>>, bool)[]? orderBy = null,
            string[][]? includes = null) {
            return[];
        }

        public List<TP> Where<TP>(Expression<Func<T, TP>> select, Expression<Func<TP, bool>> predicate, int offset = 0, int limit = 20,
            (Expression<Func<TP, object>>, bool)[]? orderBy = null, string[][]? includes = null) {
            return[];
        }


        public T? First(Expression<Func<T, bool>> predicate, string[][]? includes = null, (Expression<Func<T, object>>, bool)[]? orderBy = null) => null;

        public TP? First<TP>(Expression<Func<T, TP>> select, Expression<Func<TP, bool>> predicate, string[][]? includes = null,
            (Expression<Func<TP, object>>, bool)[]? orderBy = null) {
            return default(TP);
        }

        public bool Exists(Expression<Func<T, bool>> predicate, string[][]? includes = null) => false;
        public bool Exists<T1>(Expression<Func<T1, bool>> predicate, Expression<Func<T, T1>> select, string[][]? includes = null) {
            return true;
        }

        public T Add(T entity) => entity;
        public T Save(T entity, bool flush = true) => entity;
        public T Update(T entity) => entity;
        public int UpdateExpr((Expression<Func<T, object>>, object)[] memberAccessorsAndValues, Expression<Func<T, bool>> predicate, string[][]? includes = null) => 0;
        public T Delete(T entity) => entity;
        public int Delete(Expression<Func<T, bool>> predicate, string[][]? includes = null) => 0;
        public void Flush() { }
        public T Detach(T entity) => entity;
        public T Merge(T entity) => entity;
    }

    private class MockValidator<T> : IValidator<T>
    {
        public ValidationResult Validate(T entity)
        {
            return new ValidationResult("Mock validation error");
        }
    }
}
