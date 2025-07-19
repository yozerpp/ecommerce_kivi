using System.Linq.Expressions;

namespace Ecommerce.Dao.Spi;

public interface IRepository<TEntity> where TEntity : class, new()
{
    public List<TEntity> All(string[][]? includes = null);
    public List<TP> All<TP>(Expression<Func<TEntity, TP>> select,string[][]? includes = null);

    public List<TEntity> Where(Expression<Func<TEntity, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TEntity, object>>, bool)[]? orderBy=null, string[][]? includes=null);
    public List<TP> Where<TP>(Expression<Func<TEntity,TP>> select,Expression<Func<TP, bool>> predicate,int offset=0, int limit = 20, (Expression<Func<TP, object>>, bool)[]? orderBy=null, string[][]? includes=null);
    public TEntity? First(Expression<Func<TEntity, bool>> predicate,string[][]? includes=null,(Expression<Func<TEntity, object>>, bool)[]? orderBy=null);
    public TP? First<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TP, bool>> predicate,string[][]? includes=null,(Expression<Func<TP, object>>, bool)[]? orderBy=null);

    public TP? FirstP<TP>(Expression<Func<TEntity, TP>> select, Expression<Func<TEntity, bool>> predicate,
        string[][]? includes = null, (Expression<Func<TEntity, object>>, bool)[]? orderBy = null);
    public bool Exists(Expression<Func<TEntity, bool>> predicate, string[][]? includes= null);
    public bool Exists<T>(Expression<Func<T, bool>> predicate, Expression<Func<TEntity, T>> select, string[][]? includes = null);
    public TEntity Add(TEntity entity);
    public TEntity Save(TEntity entity, bool flush = true);
    public TEntity Update(TEntity entity);
    public int UpdateExpr((Expression<Func<TEntity,object>>,object)[] memberAccessorsAndValues, Expression<Func<TEntity, bool>> predicate, string[][]? includes = null);
    public TEntity Delete(TEntity entity);
    public int Delete(Expression<Func<TEntity, bool>> predicate,string[][]? includes =null);
    public void Flush();
    public TEntity Detach(TEntity entity);
    public TEntity Merge(TEntity entity);
}