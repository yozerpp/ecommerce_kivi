using System.Linq.Expressions;
using System.Reflection;
using Ecommerce.Dao.Spi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Ecommerce.Dao.Default;
//TODO async methods need to be rewritten. to use async/await properly
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private DbContext _context;
    private readonly IReadOnlyList<IProperty> _idProperties;
    private readonly IEntityType _entityType;
    public EfRepository(DbContext context)
    {
        this._context = context;
        _entityType = context.Model.FindEntityType(typeof(TEntity));
        _idProperties = _context.Model.FindEntityType(typeof(TEntity)).GetKeys().First(k => k.IsPrimaryKey())
            .Properties;
    }
    public  List<TEntity> All(string[][]? includes = null)
    {
        return doIncludes(_context.Set<TEntity>(),includes).ToList();
    }


    public int Count(Expression<Func<TEntity, bool>> predicate) {
        return _context.Set<TEntity>().Where(predicate).Count();
    }

    public void Clear() {
        _context.ChangeTracker.Clear();
    }

    public int CountProjected<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate) {
        return _context.Set<TEntity>().Select(select).Where(predicate).Count();
    }
    public List<TP> All<TP>(Expression<Func<TEntity, TP>> select, string[][]? includes = null) {
        return doIncludes(_context.Set<TEntity>(),includes).Select(select).ToList();
    }

    public  List<TEntity> Where(Expression<Func<TEntity, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null, string[][]? includes=null)
    {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes),orderBy).Where(predicate).Take(limit).Skip(offset).ToList();
    }

    public List<TP> Where<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate, int offset = 0, int limit = 20, (Expression<Func<TP, object>>, bool)[]? orderBy = null,
        string[][]? includes = null) {
        return doOrderBy(doIncludes(_context.Set<TEntity>(), includes).Select(select).Where(predicate),orderBy).Skip(offset).Take(limit-offset).ToList();
    }

    public TEntity Attach(TEntity entity) {
        return _context.Attach(entity).Entity;
    }

    public List<TP> WhereP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate, int offset = 0, int limit = 20,
        ICollection<(Expression<Func<TEntity, object>>, bool)>? orderBy = null, string[][]? includes = null, bool nonTracking = false) {
        return doOrderBy(doIncludes(nonTracking?_context.Set<TEntity>().AsNoTracking():_context.Set<TEntity>(), includes), orderBy).Where(predicate)
            .Skip(offset).Take(limit - offset)
            .Select(select)
            .ToList();
    }
    public List<TP> WhereProjectGroup<TP, TG>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TG>> groupBy, int offset = 0, int limit = 20,
        (Expression<Func<TEntity, object>>, bool)[]? orderBy = null, string[][]? includes = null) {
        return doOrderBy(_context.Set<TEntity>().Where(predicate),orderBy).Select(select) //trust that EF Core auto includes
            .Skip(offset).Take(limit).ToList();
    }

    public bool TryAdd(TEntity entity) {
        if (_context.Set<TEntity>().Local.Any(e=>e.Equals(entity))){
            return false;
        }
        try{
            _context.Set<TEntity>().Add(entity);
            _context.SaveChanges();
            return true;
        }
        catch (Exception){
            _context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryAddAsync(TEntity entity, CancellationToken cancellation = default) {
        if (_context.Set<TEntity>().Local.Any(e=> cancellation.IsCancellationRequested||e.Equals(entity))){
            return false;
        }
        try{
            await _context.Set<TEntity>().AddAsync(entity, cancellation);
            await _context.SaveChangesAsync(cancellation);
            return true;
        }
        catch (Exception){
            _context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }
    public  TEntity? First(Expression<Func<TEntity, bool>> predicate, string[][]? includes=null, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null, bool nonTracking = false)
    {
        return doOrderBy(doIncludes(nonTracking?_context.Set<TEntity>().AsNoTracking():_context.Set<TEntity>(), includes),orderBy).FirstOrDefault(predicate);
    }

    public TP? FirstP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,
        string[][]? includes = null, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null, bool nonTracking = false) {
        return doOrderBy(doIncludes(nonTracking?_context.Set<TEntity>().AsNoTracking():_context.Set<TEntity>(), includes), orderBy).Where(predicate).Select(select)
            .FirstOrDefault();
    }
    public TP? First<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate,string[][]? includes=null,(Expression<Func<TP, object>>, bool)[]? orderBy=null, bool nonTracking = false) {
            return doOrderBy(doIncludes(nonTracking?_context.Set<TEntity>().AsNoTracking():_context.Set<TEntity>(), includes).Select(select),orderBy).FirstOrDefault(predicate);
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
        // Detach(entity);
        return _context.Set<TEntity>().Add(entity).Entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity, bool flush=true, CancellationToken cancellationToken = default) {
        // await DetachAsync(entity, cancellationToken);
        var ret = await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        if (flush)
            await _context.SaveChangesAsync(cancellationToken);
        return ret.Entity;
    }

    /// <summary>
    /// <warning>Flushed the context</warning>
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public TEntity Save(TEntity entity, bool flush=true) {
        // Detach(entity);
        TEntity ret;
        try{
            ret =  _context.Set<TEntity>().Add(entity).Entity;
            if (flush)
                _context.SaveChanges();
        }
        catch (Exception e){
            if (e is InvalidOperationException io && io.Message.Contains("same") ||
                e is DbUpdateException du &&
                (du.InnerException?.Message?.Contains("duplicate") ?? false)){
                ret = _context.Set<TEntity>().Update(entity).Entity;
                if (flush) _context.SaveChanges();
            }
            else throw;
        }
        return ret;
    }

    public async Task<TEntity> SaveAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default) {
        // throw new NotImplementedException();
            try{
                return await AddAsync(entity, flush, cancellationToken);
            }
            catch (AggregateException e){
                if (e.InnerExceptions.Any(e=>e is InvalidOperationException io && io.Message.Contains("same") ||
                                             e is DbUpdateException du &&
                                             (du.InnerException?.Message.Contains("Conflict") ?? false))){
                    var ret = _context.Set<TEntity>().Update(entity).Entity;
                    if (flush) await _context.SaveChangesAsync(cancellationToken);
                    return ret;
                }
                throw;
            }
    }

    public TEntity UpdateInclude(TEntity entity, params string[] updateProperties) {
        if (updateProperties.Length > 0){
            _context.ChangeTracker.Entries<TEntity>().Where(e=>e.Entity.Equals(entity)).ToList().ForEach(e=>e.State = EntityState.Detached);
            var entry = _context.Entry(entity);
            entry.State= EntityState.Unchanged;
            foreach (var entryComplexProperty in entry.ComplexProperties){
                var b = entryComplexProperty.IsModified = updateProperties.Contains(entryComplexProperty.Metadata.Name);
                entryComplexProperty.EntityEntry.State = b ? EntityState.Modified : EntityState.Unchanged;
            }

            foreach (var member in entry.Members.Where(m => !((m as PropertyEntry)?.Metadata.IsKey() ?? false))){
                member.IsModified = updateProperties.Contains(member.Metadata.Name);
            }

            return entry.Entity;
        }
        return entity;
    }

    public TEntity UpdateIgnore(TEntity entity, bool ignoreNull, params string[] ignoreProperties) {
        var e = _context.Entry(entity);
        e.State = EntityState.Modified;
        
        if(ignoreProperties is{ Length: > 0 })
            foreach (var memberEntry in e.Members) 
                memberEntry.IsModified = !ignoreProperties.Contains(memberEntry.Metadata.Name);
        if (ignoreNull){
            foreach (var memberEntry in e.Members.Where(m=> m.Metadata.ClrType.IsDefaultValue(m.CurrentValue))){
                memberEntry.IsModified = false;
            }
        }
        return e.Entity;
    }

    public TEntity Update(TEntity entity) {
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
            MemberExpression m = propertyAction.Item1.Body as MemberExpression;
            m??= (MemberExpression)((UnaryExpression)propertyAction.Item1.Body).Operand;
            var t= ((PropertyInfo)m.Member).PropertyType;
            var p = m.Expression as ParameterExpression;
            left = Expression.Call(left, 
                typeof(SetPropertyCalls<TEntity>).GetMethods().First(m=>m.Name.Equals("SetProperty")&&
                    m.GetParameters().Length==2 && 
                    m.GetParameters()[1].ParameterType.IsGenericParameter).MakeGenericMethod(t),
                Expression.Lambda(Expression.Property(p, m.Member as PropertyInfo),p), 
                Expression.Constant(propertyAction.Item2, t));
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
    private IQueryable<T> doOrderBy<T>(IQueryable<T> p,ICollection<(Expression<Func<T, object>>, bool)>? orderings) {
        
        if (orderings == null || orderings.Count== 0){
            // return p;
            var param = Expression.Parameter(typeof(T), "t");
            return doOrderBy(p, _idProperties.Select(p =>
                (Expression.Lambda<Func<T, object>>( Expression.Convert(Expression.Property(param,p.PropertyInfo),typeof(object) ), param), true)).ToArray());
        }
        // if (orderings == null) return p;
        bool first = true;
        foreach (var order in orderings){
            string methodName = (first ? "OrderBy" : "ThenBy") + (order.Item2?"" : "Descending");
            var method = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == methodName
                            && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), typeof(object));
            p = (IQueryable<T>)method.Invoke(null,[ p, order.Item1 ])!;
            first = false;
        }
        return p;
    }

    private static readonly Lock _lck = new();
    private static MethodInfo _ıdTupleCreateMethod = null!;
    private IQueryable<IGrouping<object,TEntity>> doGroup(IQueryable<TEntity> query) {
        var idTypes = _idProperties.Select(p => p.ClrType).ToArray();
        lock (_lck){
            _ıdTupleCreateMethod??=typeof(Tuple).GetMethods().First(m => m.IsStatic && m.Name.Equals(nameof(Tuple.Create)) &&
                                                  m.GetGenericArguments().Length == _idProperties.Count)
                .MakeGenericMethod(idTypes);
        }
        var param = Expression.Parameter(typeof(TEntity), "t");
        var props = _idProperties.Select(p => Expression.Property(param, p.PropertyInfo));
        var expr = Expression.Lambda<Func<TEntity, object>>(
           Expression.Convert(
                _idProperties.Count>1?Expression.Call(_ıdTupleCreateMethod, props):props.First(),
                typeof(object)),
                param);
        return query.Distinct().GroupBy(expr);
    }
    private static IQueryable<TEntity> doIncludes(IQueryable<TEntity> query, string[][]? includes) {
        if (includes==null) return query;
        IQueryable<TEntity> ret = query;
        foreach (var include in includes){
            ret = ret.Include(string.Join('.',include));
        }
        return ret;
    }

    public async ValueTask DisposeAsync() {
        await _context.DisposeAsync();
    }
}