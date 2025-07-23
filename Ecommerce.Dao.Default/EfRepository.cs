using System.Linq.Expressions;
using Ecommerce.Dao.Spi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Ecommerce.Dao.Default;
//TODO async methods need to be rewritten. to use async/await properly
public class EfRepository<TEntity> : IRepository<TEntity>, IAsyncDisposable where TEntity : class, new()
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
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).Where(predicate).Skip(offset).Take(limit).ToList();
    }

    public List<TP> Where<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate, int offset = 0, int limit = 20, (Expression<Func<TP, object>>, bool)[]? orderBy = null,
        string[][]? includes = null) {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes).Select(select).Where(predicate),orderBy).Skip(offset).Take(limit).ToList();
    }

    public  TEntity? First(Expression<Func<TEntity, bool>> predicate, string[][]? includes=null, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null)
    {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).FirstOrDefault(predicate);
    }

    public TP? FirstP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,
        string[][]? includes = null, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null) {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes), orderBy).Where(predicate).Select(select)
            .FirstOrDefault();
    }
    public TP? First<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate,string[][]? includes=null,(Expression<Func<TP, object>>, bool)[]? orderBy=null) {
            return doOrderBy(doIncludes(_context.Set<TEntity>(), includes).Select(select),orderBy).FirstOrDefault(predicate);

    }

    public bool Exists(Expression<Func<TEntity, bool>> predicate,string[][]? includes=null)
    {
        return doIncludes(_context.Set<TEntity>(),includes).Any(predicate);
    }
    public bool Exists<T>(Expression<Func<T, bool>> predicate, Expression<Func<TEntity, T>> select,string[][]? includes=null)
    {
        return doIncludes(_context.Set<TEntity>(),includes).Select(select).Any(predicate);
    }
    public TEntity Add(TEntity entity) {
        Detach(entity);
        return _context.Set<TEntity>().Add(entity).Entity;
    }

    public Task<TEntity> AddAsync(TEntity entity, bool flush=true, CancellationToken cancellationToken = default) {
        return DetachAsync(entity, cancellationToken).ContinueWith(r => {
            if (cancellationToken.IsCancellationRequested) return null!;
            var t = _context.AddAsync(r.Result, cancellationToken).AsTask();
            if (flush&& !cancellationToken.IsCancellationRequested) {
                t.ContinueWith(_ => _context.SaveChangesAsync(cancellationToken), cancellationToken).Wait(cancellationToken);
            }
            else t.Wait(cancellationToken);
            return cancellationToken.IsCancellationRequested? null!:t.Result.Entity;
        }, cancellationToken);
    }

    /// <summary>
    /// <warning>Flushed the context</warning>
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public TEntity Save(TEntity entity, bool flush=true) {
        Detach(entity);
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

    public Task<TEntity> SaveAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default) {
        return DetachAsync(entity).ContinueWith(r => {
            try{
                var t =AddAsync(r.Result, flush, cancellationToken);
                t.Wait(cancellationToken);
                return t.Result;
            }
            catch (AggregateException e){
                if (e.InnerExceptions.Any(e=>e is InvalidOperationException io && io.Message.Contains("same") ||
                                             e is DbUpdateException du &&
                                             du.Message.Contains("Conflict"))){
                    var ret = _context.Set<TEntity>().Update(r.Result).Entity;
                    if (flush) _context.SaveChangesAsync(cancellationToken).Wait(cancellationToken);
                    return ret;
                }
                throw;
            }
        }, cancellationToken);
        
    }

    public TEntity Update(TEntity entity) {
        Detach(entity);
        return _context.Set<TEntity>().Update(entity).Entity;
    }

    public  Task<TEntity> UpdateAsync(TEntity entity, bool flush = true, CancellationToken token = default) {
        return DetachAsync(entity,token).ContinueWith(r => _context.Set<TEntity>().Update(r.Result).Entity, token)
            .ContinueWith(reT => {
            if(flush)
                _context.SaveChangesAsync(token).Wait(token);
            return reT.IsCanceled? null! : reT.Result;
        }, token);
    }
    public int UpdateExpr((Expression<Func<TEntity, object>>,object)[] memberAccessorAndValues, Expression<Func<TEntity, bool>> predicate, string[][]? includes = null) {
        var q = doIncludes(_context.Set<TEntity>(), includes).Where(predicate);
        var param = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "setters");
        Expression left = param;
        foreach (var propertyAction in memberAccessorAndValues){
            left = Expression.Call(left, typeof(SetPropertyCalls<TEntity>).GetMethods().First(m=>m.Name.Equals("SetProperty")&&m.GetParameters().Length==2 && m.GetParameters()[1].ParameterType.IsGenericParameter).MakeGenericMethod(typeof(object)),
                propertyAction.Item1, Expression.Convert(Expression.Constant(propertyAction.Item2), typeof(object)));
        }
        return q.ExecuteUpdate(Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(left, param));
    }


    public  TEntity Delete(TEntity entity) {
        Detach(entity);
        return _context.Set<TEntity>().Remove(entity).Entity;
    }
    public int Delete(Expression<Func<TEntity, bool>> predicate, string[][]? includes=null)
    {
        return doIncludes(_context.Set<TEntity>(), includes).Where(predicate).ExecuteDelete();
    }

    public Task<TEntity> DeleteAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default) {
        return DetachAsync(entity, cancellationToken).ContinueWith(r => _context.Set<TEntity>().Remove(r.Result).Entity, cancellationToken).ContinueWith(r => {
                _context.SaveChangesAsync(cancellationToken);
                return cancellationToken.IsCancellationRequested ?null!:r.Result;
        },cancellationToken);
    }
    public void Flush() {
        _context.SaveChanges();
    }

    public TEntity Detach(TEntity entity) {
        foreach (var e1 in _context.ChangeTracker.Entries<TEntity>().Where(e => e.Entity.Equals(entity))){
            e1.State = EntityState.Detached;
        }
        return entity;
    }
    public async Task<TEntity> DetachAsync(TEntity entity, CancellationToken cancellationToken = default) {
            foreach (var e1 in _context.ChangeTracker.Entries<TEntity>().Where(e => e.Entity.Equals(entity))){
                if(cancellationToken.IsCancellationRequested) return null!;
                e1.State = EntityState.Detached;
            }
            return entity;
    }
    public TEntity Merge(TEntity entity) {
        throw new NotImplementedException();
    }
    private static IQueryable<T> doOrderBy<T>(IQueryable<T> p,(Expression<Func<T, object>>, bool)[]? orderings) {
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

    public void Dispose() {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync() {
        await _context.DisposeAsync();
    }
}