using System.Linq.Expressions;
using Ecommerce.Dao.Spi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Ecommerce.Dao.Default;

internal class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class, new()
{
    private DbContext _context;
    public EfRepository(DbContext context)
    {
        this._context = context;
    }
    public  List<TEntity> All(string[][]? includes = null)
    {
        return doIncludes(_context.Set<TEntity>(),includes).ToList();
    }

    public List<TP> All<TP>(Expression<Func<TEntity, TP>> select, string[][]? includes = null) {
        return doIncludes(_context.Set<TEntity>(),includes).Select(select).ToList();
    }

    public  List<TEntity> Where(Expression<Func<TEntity, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null, string[][]? includes=null)
    {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).Skip(offset).Take(limit).ToList();
    }

    public List<TP> Where<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate, int offset = 0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null,
        string[][]? includes = null) {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).Skip(offset).Take(limit).Select(select).ToList();
    }

    public  TEntity? First(Expression<Func<TEntity, bool>> predicate, string[][]? includes=null, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null)
    {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).FirstOrDefault(predicate);
    }

    public TP? First<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,string[][]? includes=null,(Expression<Func<TEntity, object>>, bool)[]? orderBy=null) {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).Where(predicate).Select(select).FirstOrDefault();
    }

    public bool Exists(Expression<Func<TEntity, bool>> predicate,string[][]? includes=null)
    {
        return doIncludes(_context.Set<TEntity>(),includes).Any(predicate);
    }

    public TEntity Add(TEntity entity)
    { 
        return _context.Set<TEntity>().Add(entity).Entity;
    }
    /// <summary>
    /// <warning>Flushed the context</warning>
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public TEntity Save(TEntity entity, bool flush=true) {
        TEntity ret;
        try{
            ret =  _context.Set<TEntity>().Add(entity).Entity;
            if (flush)
                _context.SaveChanges();
        }
        catch (Exception e){
            if (e is InvalidOperationException io && io.Message.Contains("same") ||
                e is DbUpdateException du &&
                du.Message.Contains("Conflict")){
                ret = _context.Set<TEntity>().Update(entity).Entity;
                if (flush) _context.SaveChanges();
            }
            else throw;
        }
        return ret;
    }
    public TEntity Update(TEntity entity)
    {
        var e = _context.Entry(entity);
        e.State = EntityState.Modified;
        return e.Entity;
    }

    public int Update((Expression<Func<TEntity, object>>,object)[] memberAccessorAndValues, Expression<Func<TEntity, bool>> predicate, string[][]? includes = null) {
        var q = doIncludes(_context.Set<TEntity>(), includes).Where(predicate);
        var param = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "setters");
        Expression left = param;
        foreach (var propertyAction in memberAccessorAndValues){
            left = Expression.Call(left, typeof(SetPropertyCalls<TEntity>).GetMethod("SetProperty"),
                propertyAction.Item1, Expression.Constant(propertyAction.Item2));
        }
        return q.ExecuteUpdate(Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(left, param));
    }


    public  TEntity Delete(TEntity entity)
    {
        return _context.Set<TEntity>().Remove(entity).Entity;
    }
    public int Delete(Expression<Func<TEntity, bool>> predicate, string[][]? includes=null)
    {
        return doIncludes(_context.Set<TEntity>(), includes).Where(predicate).ExecuteDelete();
    }

    public void Flush() {
        _context.SaveChanges();
    }

    public TEntity Detach(TEntity entity) {
        var e = _context.Entry(entity);
        e.State = EntityState.Detached;
        return e.Entity;
    }

    public TEntity Merge(TEntity entity) {
        throw new NotImplementedException();
    }
    private static IQueryable<TEntity> doOrderBy(IQueryable<TEntity> p,(Expression<Func<TEntity, object>>, bool)[]? orderings) {
        if (orderings == null) return p;
        foreach (var order in orderings) p = order.Item2 ? p.OrderBy(order.Item1) : p.OrderByDescending(order.Item1);
        return p;
    }
    private static IQueryable<TEntity> doIncludes(IQueryable<TEntity> query, string[][]? includes) {
        if (includes==null) return query;
        IQueryable<TEntity> ret = query;
        foreach (var include in includes){
            ret = ret.Include(string.Join('.',include));
        }
        return ret;
    }
}