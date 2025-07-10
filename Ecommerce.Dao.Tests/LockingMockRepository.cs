using System.Reflection;
using Ecommerce.Dao.Concrete;
using Ecommerce.Dao.Iface;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Tests;

public class LockingMockRepository <TE> : IRepository<TE> where TE : class, new()
{
    public static IRepository<TE> Create() {
        return RepositoryFactory.RepositoryProxy<TE>.Create(new LockingMockRepository<TE>());
    }
    public List<TE> All() {
        Thread.Sleep(1000);
        return[];
    }

    public List<TE> Where(Func<TE, bool> predicate, int offset = 0, int limit = 20, params Func<TE, object>[] orderBy) {
        Thread.Sleep(1000);
        return[]; }

    public TE? Find(Func<TE, bool> predicate) {
        Thread.Sleep(1000);
        return null;
        
    }

    public bool Exists(Func<TE, bool> predicate) {
        Thread.Sleep(1000);
        return true;
        
    }

    public TE Add(TE entity) {
        Thread.Sleep(1000);
        return new TE();
    }

    public TE Update(TE entity) {
        Thread.Sleep(1000);
        return new TE();
    }

    public TE Delete(TE entity) {
        Thread.Sleep(1000);
        return new TE();
    }

    public void Delete(Func<TE, bool> predicate) {
        Thread.Sleep(1000);
    }

    public void Flush() {
        Thread.Sleep(1000);
    }
}