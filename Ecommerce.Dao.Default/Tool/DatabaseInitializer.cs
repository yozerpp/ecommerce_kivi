using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Address = Ecommerce.Entity.Common.Address;
using Enum = System.Enum;

namespace Ecommerce.Dao.Default.Tool;

public class DatabaseInitializer: IDisposable
{
    private readonly DbContext _defaultContext;
    private readonly Dictionary<IEntityType, Lock> _dictLocks;
    private readonly Dictionary<IEntityType, ISet<object>> _saved;
    private readonly Dictionary<Type, int?> _typeCounts;
    private readonly SetterCache _setterCache;
    private readonly int _defaultCount;
    private readonly ICollection<IEntityType> _entityTypes;
    private readonly RelationRandomizer _relationRandomizer;
    private readonly ICollection<(DbContext,ICollection<IEntityType>)> _lanes;
    private readonly ValueRandomizer _valueRandomizer ;
    private readonly ILog _logger;
    public DatabaseInitializer(Type contextType,DbContextOptions options, Dictionary<Type, int?> typeCounts, int defaultCount = 100) {
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _valueRandomizer = new ValueRandomizer(_setterCache = new SetterCache());
        GlobalContext.Properties["LogFileName"] = Environment.CurrentDirectory + Path.DirectorySeparatorChar+ $"DatabaseInitializer_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        Console.WriteLine("Logging to: " + GlobalContext.Properties["LogFileName"]);
        _logger = LogManager.GetLogger(typeof(DatabaseInitializer));
        _defaultContext = (DbContext) contextType.GetConstructor([typeof(DbContextOptions<>).MakeGenericType(contextType)])!.Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().Where(t=>!t.IsOwned() && !typeof(Dictionary<string,object>).IsAssignableFrom(t.ClrType)).ToArray();
        _saved = _entityTypes.ToDictionary(e=>e, e=>(ISet<object>)new HashSet<object>());
        _dictLocks = _entityTypes.ToDictionary(e=>e, e=>new Lock());
        _relationRandomizer = new RelationRandomizer(_saved, _dictLocks);
        _lanes = Sort(contextType,options);
    }
    public void initialize() {
        CreateEntities();
        Console.WriteLine("-----Finished Creating Entities. Wiring Non-Required Relations...-----");
        PopulateNonRequiredRelations();
    }
    private ICollection<(DbContext, ICollection<IEntityType>)> Sort(Type contextType,DbContextOptions options) {
        var visited = new HashSet<string>(_entityTypes.Count);
        var lanes = new List<Stack<IEntityType>>();
        foreach (var entityType in _entityTypes){
            if (visited.Contains(entityType.Name)) continue;
            var lane = new Stack<IEntityType>();
            SortRecursive(entityType, lane);
            lanes.Add(lane);
        }
        return lanes.Select(l=>((DbContext) contextType.GetConstructor([typeof(DbContextOptions<>).MakeGenericType(contextType)]).Invoke([options]),(ICollection<IEntityType>)l.Reverse().ToArray() )).ToArray();
        void SortRecursive(IEntityType entityType, Stack<IEntityType> lane) {
            visited.Add(entityType.Name);
            foreach (var navigation in entityType.GetNavigations().Where(n =>n.IsOnDependent&& n.ForeignKey.IsRequired).ToHashSet()){
                SortRecursive(navigation.TargetEntityType, lane);
            }
            lane.Push(entityType);
        }
    }

    private readonly ThreadLocal<DbContext> _contextThreadLocal = new();
    private void CreateEntities() {
        _lanes.AsParallel().ForAll(l => {
            _contextThreadLocal.Value = l.Item1;
            var batch = new List<object>();
            const int BatchSize = 2000;
            int batchCounter = 0;
            foreach (var entityType in l.Item2){
                while (true){
                    _dictLocks[entityType].Enter();
                    if(IsFull(entityType))break;
                    object entity;
                    // try{
                        entity = Randomize(entityType);

                    // }
                    // catch (Exception e){
                        // if (e.InnerException is SqlException sqlException && sqlException.Number == 2627)
                            // _contextThreadLocal.Value.ChangeTracker.Clear();
                        // else throw;
                        // _logger.Error(e.Message);
                        // Debug.WriteLine(e);
                        // return;
                    // }
                    batch.Add(entity);
                    _saved[entityType].Add(entity);
                    _dictLocks[entityType].Exit();
                    if(batchCounter++>=BatchSize)
                        PersistBatch();
                }
                PersistBatch();
            }
            void PersistBatch() {
                var ctx = _contextThreadLocal.Value;
                foreach (var entity in batch){
                    ctx!.Add(entity);
                }
                ctx.SaveChanges(); 
                ctx.ChangeTracker.Clear();
                batch.Clear();
            }
        });
    }
    private void PopulateNonRequiredRelations()
    {
        foreach (var (entityType, set) in _saved){
            var skipped = new HashSet<IForeignKey>();
            foreach (var entity in set)
            {
                foreach (var fk in entityType.GetForeignKeys().Where(fk=>!fk.IsRequired && !skipped.Contains(fk))){
                    try{
                        AssignForeignKeys(entity, _relationRandomizer.GetForeignKeyValue(fk, entity));
                    }
                    catch (IndexOutOfRangeException){
                        skipped.Add(fk);
                    }
                }
                _defaultContext.Update(entity);
            }
        }
        _defaultContext.SaveChanges();
    }

    private object Randomize(IEntityType enttiyType)
    {
        var entity = _valueRandomizer.Create(enttiyType);
        foreach (var key in enttiyType.GetKeys().Where(k=>k.Properties.Count > 1)){
            AssignForeignKeys(entity , _relationRandomizer.GetKeyValues(key));
        }
        foreach (var foreignKey in enttiyType.GetForeignKeys().Where(fk=>fk.IsRequired&&!fk.Properties.All(p=>p.IsKey()))){ //TODO no action if foreign key is both self-referencing and required.
            AssignForeignKeys(entity,_relationRandomizer.GetForeignKeyValue(foreignKey));
        }

        return entity;
    }


    private void AssignForeignKeys( object dependent, params IEnumerable<(INavigation, object)> keyValues) {
        foreach (var (nav, principal) in keyValues){
            var propsInPrincipal = nav.ForeignKey.PrincipalKey.Properties;
            var dependentProps = nav.ForeignKey.Properties;
            for (var i = 0; i < dependentProps.Count; i++){
                _setterCache.SetProperty(dependent, dependentProps[i].PropertyInfo,propsInPrincipal[i].PropertyInfo.GetValue(principal));
            }
        }
    }
    private bool IsFull(IEntityType entityType1) {
        var ret = _saved[entityType1].Count >= (_typeCounts.GetValueOrDefault(entityType1.ClrType) ?? _defaultCount);
        return ret;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _defaultContext.Dispose();
        foreach (var dbContext in _lanes){
            dbContext.Item1.Dispose();
        }
        _contextThreadLocal.Dispose();
    }
}

internal class SetterCache
{
    private readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> _setterCache;
    
    public SetterCache()
    {
        _setterCache = new ConcurrentDictionary<PropertyInfo, Action<object, object?>>();
    }
    public void SetProperty(object obj, PropertyInfo prop, object? value) {
        var setter = _setterCache.GetOrAdd(prop, p => {
            var target = Expression.Parameter(typeof(object));
            var val = Expression.Parameter(typeof(object));
            var convert = Expression.Convert(target, p.DeclaringType);
            var assign = Expression.Assign(
                Expression.Property(convert, p),
                Expression.Convert(val, p.PropertyType));
            return Expression.Lambda<Action<object, object?>>(assign, target, val).Compile();
        });
        setter(obj, value);
    }
}
internal class EqualityComparableSet<T>: HashSet<T>, IEquatable<IEnumerable<T>>
{
    public EqualityComparableSet(): base(){}
    public EqualityComparableSet(ICollection<T> collection) : base(collection) {
    }
    public bool Equals(IEnumerable<T>? other) {
        if (other is null) return false;
        return this.SequenceEqual(other);
    }
    public override bool Equals(object? obj) {
        if (obj is IEnumerable<T> enumerable){
            return Equals(enumerable);
        }
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return this.Aggregate(0, (current, item) => current ^ item?.GetHashCode() ?? 0);
    }

}
