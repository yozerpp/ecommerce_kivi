using System.Configuration;

namespace Ecommerce.Dao.Iface;

public interface IRepository<Entity> where Entity : class, new()
{
    public  List<Entity> All();

    public  List<Entity> Where(Func<Entity, bool> predicate, int offset=0, int limit = 20, params Func<Entity, object>[] orderBy);

    public  Entity? Find(Func<Entity, bool> predicate);
    public bool Exists(Func<Entity, bool> predicate);
    public  Entity Add(Entity entity);

    public Entity Update(Entity entity);

    public  Entity Delete(Entity entity);
    public void Delete(Func<Entity, bool> predicate);
    public void Flush();
}