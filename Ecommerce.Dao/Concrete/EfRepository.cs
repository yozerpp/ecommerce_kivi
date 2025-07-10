using Ecommerce.Dao.Iface;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Concrete;

class EfRepository<Entity> : IRepository<Entity> where Entity : class, new()
{
    private DbContext _context;

    public EfRepository(DbContext context)
    {
        this._context = context;
    }
    public  List<Entity> All()
    {
        return _context.Set<Entity>().ToList();
    }
    public  List<Entity> Where(Func<Entity, bool> predicate,int offset=0, int limit = 20, params Func<Entity, object>[] orderBy)
    {
        var p = _context.Set<Entity>().Where(predicate);
        foreach (var order in orderBy)
            p = p.OrderBy(order);
        return p.Skip(offset).Take(limit).ToList();
    }
    
    public  Entity? Find(Func<Entity, bool> predicate)
    {
        return _context.Set<Entity>().Where(predicate).SingleOrDefault();
    }

    public bool Exists(Func<Entity, bool> predicate)
    {
        return _context.Set<Entity>().Any(predicate);
    }

    public Entity Add(Entity entity)
    { 
        return _context.Set<Entity>().Add(entity).Entity;
    }
    public Entity Update(Entity entity)
    {
        var e = _context.Entry(entity);
        e.State = EntityState.Modified;
        return e.Entity;
    }

    public  Entity Delete(Entity entity)
    {
        return _context.Set<Entity>().Remove(entity).Entity;
    }

    public void Delete(Func<Entity, bool> predicate)
    {
        _context.Set<Entity>().RemoveRange(_context.Set<Entity>().Where(predicate));
    }

    public void Flush() {
        _context.SaveChanges();
    }
}