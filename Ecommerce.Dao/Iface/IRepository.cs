using System.Configuration;
using System.Linq.Expressions;

namespace Ecommerce.Dao.Iface;

public interface IRepository<Entity> where Entity : class, new()
{
    public List<Entity> All(string[][]? includes = null);
    public List<TP> All<TP>(Expression<Func<Entity, TP>> select,string[][]? includes = null);

    public List<Entity> Where(Expression<Func<Entity, bool>> predicate,int offset=0, int limit = 20, Expression<Func<Entity, object>>[]? orderBy=null, string[][]? includes=null);
    public List<TP> Where<TP>(Expression<Func<Entity,TP>> select,Expression<Func<Entity, bool>> predicate,int offset=0, int limit = 20, Expression<Func<Entity, object>>[]? orderBy=null, string[][]? includes=null);

    public Entity? First(Expression<Func<Entity, bool>> predicate,string[][]? includes=null,Expression<Func<Entity, object>>[]? orderBy=null);
    public TP? First<TP>(Expression<Func<Entity, TP>> select, Expression<Func<Entity, bool>> predicate,string[][]? includes=null,Expression<Func<Entity, object>>[]? orderBy=null);
    public bool Exists(Expression<Func<Entity, bool>> predicate, string[][]? includes= null);
    public Entity Add(Entity entity);
    public Entity Update(Entity entity);
    public void Update(Action<Entity> updateAction, Expression<Func<Entity, bool>> predicate, string[][]? includes = null);
    public Entity Delete(Entity entity);
    public void Delete(Expression<Func<Entity, bool>> predicate,string[][]? includes =null);
    public void Flush();
    public Entity Detach(Entity entity);
    public Entity Merge(Entity entity);
}