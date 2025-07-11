using System.Linq.Expressions;
using Ecommerce.Dao.Iface;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Dao.Concrete;

class EfRepository<Entity> : IRepository<Entity> where Entity : class, new()
{
    private DbContext _context;
    public EfRepository(DbContext context)
    {
        this._context = context;
    }
    public  List<Entity> All(string[][]? includes = null)
    {
        return doIncludes(_context.Set<Entity>(),includes).ToList();
    }

    public List<TP> All<TP>(Expression<Func<Entity, TP>> select, string[][]? includes = null) {
        return doIncludes(_context.Set<Entity>(),includes).Select(select).ToList();
    }

    public  List<Entity> Where(Expression<Func<Entity, bool>> predicate,int offset=0, int limit = 20, Expression<Func<Entity, object>>[]? orderBy=null, string[][]? includes=null)
    {
        return doOrderBy(doIncludes(_context.Set<Entity>(), includes),orderBy).Skip(offset).Take(limit).ToList();
    }

    public List<TP> Where<TP>(Expression<Func<Entity, TP>> select, Expression<Func<Entity, bool>> predicate, int offset = 0, int limit = 20, Expression<Func<Entity, object>>[]? orderBy = null,
        string[][]? includes = null) {
        return doOrderBy(doIncludes(_context.Set<Entity>(), includes),orderBy).Skip(offset).Take(limit).Select(select).ToList();
    }

    public  Entity? First(Expression<Func<Entity, bool>> predicate, string[][]? includes=null, Expression<Func<Entity, object>>[]? orderBy=null)
    {
        return doOrderBy(doIncludes(_context.Set<Entity>(), includes),orderBy).FirstOrDefault(predicate);
    }

    public TP? First<TP>(Expression<Func<Entity, TP>> select, Expression<Func<Entity, bool>> predicate,string[][]? includes=null,Expression<Func<Entity, object>>[]? orderBy=null) {
        return doOrderBy(doIncludes(_context.Set<Entity>(), includes),orderBy).Where(predicate).Select(select).FirstOrDefault();
    }

    public bool Exists(Expression<Func<Entity, bool>> predicate,string[][]? includes=null)
    {
        return doIncludes(_context.Set<Entity>(),includes).Any(predicate);
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

    public void Update(Action<Entity> updateAction, Expression<Func<Entity, bool>> predicate, string[][]? includes = null) {
        var it = doIncludes(_context.Set<Entity>(),includes).Where(predicate);
        foreach (var entity in it){
            updateAction.Invoke(entity);
            _context.Update(entity);
        }
    }


    public  Entity Delete(Entity entity)
    {
        return _context.Set<Entity>().Remove(entity).Entity;
    }
    public void Delete(Expression<Func<Entity, bool>> predicate, string[][]? includes=null)
    {
        _context.Set<Entity>().RemoveRange(doIncludes(_context.Set<Entity>(), includes).Where(predicate));
    }

    public void Flush() {
        _context.SaveChanges();
    }

    public Entity Detach(Entity entity) {
        var e = _context.Entry(entity);
        e.State = EntityState.Detached;
        return e.Entity;
    }

    public Entity Merge(Entity entity) {
        throw new NotImplementedException();
    }
    private static IQueryable<Entity> doOrderBy(IQueryable<Entity> p,Expression<Func<Entity, object>>[]? orderings) {
        if (orderings == null) return p;
        foreach (var order in orderings)
            p = p.OrderBy(order);
        return p;
    }
    private static IQueryable<Entity> doIncludes(IQueryable<Entity> query, string[][]? includes) {
        if (includes==null) return query;
        IQueryable<Entity> ret = query;
        foreach (var include in includes){
            ret = ret.Include(string.Join('.',include));
        }
        return ret;
    }
}