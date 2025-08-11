using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ecommerce.Dao.Spi;

public interface IRepository<TEntity> where TEntity : class
{
    public List<TEntity> All(string[][]? includes = null);
    public List<TP> All<TP>(Expression<Func<TEntity, TP>> select,string[][]? includes = null);
    public int Count(Expression<Func<TEntity, bool>> predicate);
    public int CountProjected<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate);
    public List<TEntity> Where(Expression<Func<TEntity, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null, string[][]? includes=null);
    public List<TP> Where<TP>(Expression<Func<TEntity,TP>> select,Expression<Func<TP, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TP, object>>, bool)[]? orderBy=null, string[][]? includes=null);

    public List<TP> WhereP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,
        int offset = 0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null,
        string[][]? includes = null);
    public TEntity? First(Expression<Func<TEntity, bool>> predicate,string[][]? includes=null,(Expression<Func<TEntity, object>>, bool)[]? orderBy=null);
    public TP? First<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate,string[][]? includes=null,(Expression<Func<TP, object>>, bool)[]? orderBy=null);
    public bool TryAdd(TEntity entity);
    public List<TP> WhereProjectGroup<TP, TG>(Expression<Func<TEntity, TP>> select,
        Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TG>> groupBy, int offset = 0,
        int limit = 20,
        (Expression<Func<TEntity, object>>, bool)[]? orderBy = null, string[][]? includes = null);
    public TP? FirstP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,
        string[][]? includes = null, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null);
    public bool Exists(Expression<Func<TEntity, bool>> predicate, string[][]? includes= null);
    public bool Exists<T>(Expression<Func<T, bool>> predicate, Expression<Func<TEntity, T>> select, string[][]? includes = null);
    public TEntity Add(TEntity entity);
    public Task<TEntity> AddAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default);
    public TEntity Save(TEntity entity, bool flush = true);
    public Task<TEntity> SaveAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default);
    public TEntity Update(TEntity entity, bool ignoreNulls=false);
    public Task<TEntity> UpdateAsync(TEntity entity, bool flush = true, CancellationToken token = default);
    public int UpdateExpr((Expression<Func<TEntity,object>>,object)[] memberAccessorsAndValues, Expression<Func<TEntity, bool>> predicate, string[][]? includes = null);
    public TEntity Delete(TEntity entity);
    public Task<TEntity> DeleteAsync(TEntity entity, bool flush = true, CancellationToken cancellationToken = default);
    public int Delete(Expression<Func<TEntity, bool>> predicate,string[][]? includes =null);
    public void Flush();
    public TEntity Detach(TEntity entity);
    public Task<TEntity> DetachAsync(TEntity entity, CancellationToken cancellationToken = default);
    public TEntity Merge(TEntity entity);
}