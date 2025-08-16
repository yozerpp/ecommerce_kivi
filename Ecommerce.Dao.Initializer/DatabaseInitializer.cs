// #define USE_FOREIGN_KEY_STRATEGY

using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Bogus;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.Dao.Default.Initializer;
public class DatabaseInitializer: IDisposable
{
    private readonly DbContext _defaultContext;
    private EntityCache _saved;
    private readonly Dictionary<Type, int?> _typeCounts;
    private readonly int _defaultCount;
    private readonly ICollection<IEntityType> _entityTypes;
    private RelationRandomizer _relationRandomizer;
    private readonly ValueRandomizer _valueRandomizer ;
    private readonly Type _contextType;
    private readonly DbContextOptions _dbContextOptions;
    private readonly ICollection<(DbContext, IEntityType)> _lanes;
    private readonly Dictionary<IEntityType, bool> _isCompleteMap = new();
    public DatabaseInitializer(Type contextType,DbContextOptions options, Dictionary<Type, int?> typeCounts, int defaultCount = 100) {
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _dbContextOptions = options;
        _contextType = contextType;
        _valueRandomizer = new ValueRandomizer();
        _defaultContext = (DbContext) contextType.GetConstructor([typeof(DbContextOptions<>).MakeGenericType(contextType)])!.Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().Where(t=>!t.IsOwned() && !typeof(Dictionary<string,object>).IsAssignableFrom(t.ClrType)).ToArray();
        var nonZeroTypes = _entityTypes.Where(t=>_typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0).ToArray();
        _entityTypes = _entityTypes.Where(t=>!t.GetViewMappings().Any() && (
            defaultCount > 0 || nonZeroTypes.Any(t.IsAssignableFrom) ||
            _typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0)).ToArray();
        _isCompleteMap = _entityTypes.ToDictionary(t => t, _ => false);
        _lanes = _entityTypes.Where(t=>!t.IsAbstract()).Select(t=>((DbContext)_contextType.GetConstructor([_dbContextOptions.GetType()]).Invoke([_dbContextOptions]), t)).ToList();
        _relationRandomizer = new RelationRandomizer(_entityTypes);
        _saved = new EntityCache(_entityTypes, _relationRandomizer);
        _relationRandomizer.SetGlobalStore(_saved);
    }
    public void initialize() {
        CreateEntities();
        Console.WriteLine("Completed Saving entities.");
        ReinitializeCollections();
        Console.WriteLine("Populating non-required relations...");
        PopulateNonRequiredRelations();
    }

    private void ReinitializeCollections() {
        _saved = new EntityCache(_saved);
        _relationRandomizer = new RelationRandomizer(_entityTypes);
        _relationRandomizer.SetGlobalStore(_saved);
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

    private Task CreateAll(IEntityType entityType, DbContext dbContext) {
        int counter = 1;
        const int batchCount = 2000;
        var batch = new List<object>();
        var lastSaveTask = Task.CompletedTask;
        while (!IsFull(entityType, counter-1)){
            var entity = Randomize(entityType);
            lastSaveTask = lastSaveTask.ContinueWith(_ => batch.Add(entity));
            if (counter++ % batchCount == 0){
               lastSaveTask = lastSaveTask.ContinueWith(_=>SaveBatch(entityType,batch, dbContext));
            }
        }
        if(counter%batchCount!=1) //TODO: ub if only one entity was requested
            lastSaveTask=lastSaveTask.ContinueWith(_=>SaveBatch(entityType,batch, dbContext));
        return lastSaveTask.ContinueWith(_=>Complete(entityType));
    }


    private void CreateEntities() {
        #if USE_FOREIGN_KEY_STRATEGY
        #else
        _defaultContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _defaultContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        #endif
        Task.WaitAll(_lanes.Select( l => Task.Run(async () => {
                var entityType = l.Item2;
                DbContext ctx;
                #if USE_FOREIGN_KEY_STRATEGY
                ctx=l.Item1;
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                #else
                ctx = _defaultContext;
                #endif
                await CreateAll(entityType,ctx);
            })
        ));
        #if USE_FOREIGN_KEY_STRATEGY
        #else
        _defaultContext.SaveChanges();
        // _defaultContext.BulkSaveChanges(new BulkConfig(){
            // IncludeGraph = false,
            // OnSaveChangesSetFK = true,
            // SetOutputIdentity= true,
        // });
        #endif
    }
    private void SaveBatch(IEntityType type, List<object> batch, DbContext dbContext) {
        #if USE_FOREIGN_KEY_STRATEGY
        dbContext.BulkInsertDynamic(type, batch,new BulkConfig(){
            ConflictOption=ConflictOption.None,
            IncludeGraph = false,
            OnSaveChangesSetFK=true,
            SetOutputIdentity = true,
        } );
        dbContext.ChangeTracker.Clear();
        #else
        lock (dbContext){
            dbContext.AddRange(batch);
        }
        #endif
        foreach (var e in batch){
            _saved.Add(type, e);
        }
        batch.Clear();
    }
    private void PopulateNonRequiredRelations() {
        _defaultContext.ChangeTracker.AutoDetectChangesEnabled = true;
        _defaultContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        _saved.NonUniqueCache.AsParallel().ForAll(kv => {
            try{
                var (t, set) = kv;
                var skipped = new HashSet<IForeignKey>();
                foreach (var entity in set){
                    foreach (var fk in t.GetForeignKeys().Where(fk => !fk.IsRequired && !(fk.IsOwnership || fk.PrincipalEntityType.IsOwned()) && !skipped.Contains(fk))){
                        var c = Random.Shared.Next() % 100;
                        if(c < 15) continue; // leave null
                        try{
                            AssignForeignKeyValues(entity, _relationRandomizer.GetForeignKeyValue(fk, entity));
                        }
                        catch (IndexOutOfRangeException){
                            Console.WriteLine("Skipping foreign key " + fk.Properties.Select(p=>p.Name).Aggregate((a, b) => a + ", " + b) + " for entity " + t.ClrType.Name + ". There is not enough principal created, consider creating more of the principal entity type.");
                            skipped.Add(fk);
                        }
                    }
                }
                lock (_defaultContext){
                    _defaultContext.UpdateRange(set);
                }
                Console.WriteLine("Populated " + t.Name);
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }
            });
        _defaultContext.SaveChanges();
    }
    private object Randomize(IEntityType enttiyType)
    {
        var entity = _valueRandomizer.Create(enttiyType);
        foreach (var key in enttiyType.GetKeys().Where(k=>k.Properties.Count > 1)){
            
            AssignForeignKeyValues(entity , _relationRandomizer.GetKeyValues(enttiyType,key));
        }
        foreach (var foreignKey in enttiyType.GetForeignKeys().Where(fk=>fk.IsRequired&&!fk.Properties.All(p=>p.IsKey()))){ //TODO no action if foreign key is both self-referencing and required.
            
            AssignForeignKeyValues(entity,_relationRandomizer.GetForeignKeyValue(foreignKey, entity));
        }

        return entity;
    }


    private void AssignForeignKeyValues( object dependent, params IEnumerable<(INavigation, object)> keyValues) {
        foreach (var (nav, principal) in keyValues){
            #if USE_FOREIGN_KEY_STRATEGY
            var propsInPrincipal = nav.ForeignKey.PrincipalKey.Properties;
            var dependentProps = nav.ForeignKey.Properties;
            for (var i = 0; i < dependentProps.Count; i++){
                _setterCache.SetProperty(dependent, dependentProps[i].PropertyInfo,propsInPrincipal[i].PropertyInfo.GetValue(principal));
            }
            #else
            PropertyCache.SetProperty(dependent, nav.PropertyInfo, principal);
            #endif
        }
    }
    private bool IsFull(IEntityType entityType1, int counter) {
        var limit = (_typeCounts.GetValueOrDefault(entityType1.ClrType) ?? _defaultCount);
        return _saved.Count(entityType1) >= limit || counter >= limit;
    }

    private void Complete(IEntityType type, bool force = false) {
        if(type.IsAbstract()&&!force) return;
        _isCompleteMap[type] = true;
        if (type.BaseType?.GetDerivedTypes().All(t => _isCompleteMap[t]) ?? false){
            Complete(type.BaseType, true);
            _relationRandomizer.DisposeEnumerators(type);
        }
        _saved.Complete(type);
        Console.WriteLine("Completed " + type.Name);
    }
    public void Dispose() {
        GC.SuppressFinalize(this);
        _defaultContext.Dispose();
        foreach (var l in _lanes){
            l.Item1.Dispose();
        }
    }
}

internal static class PropertyCache
{
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> SetterCache = new ();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> GetterCache = new ();
    public static object? GetProperty(object obj, PropertyInfo propertyInfo) {
        var getter = GetterCache.GetOrAdd(propertyInfo, p => {
            var param =Expression.Parameter(typeof(object), "e");
            return Expression.Lambda<Func<object, object?>>(
                Expression.Convert(Expression.Property(
                    Expression.Convert(param, p.DeclaringType), propertyInfo), propertyInfo.PropertyType), param).Compile();
        });
        return getter(obj);
    }
    public static void SetProperty(object obj, PropertyInfo prop, object? value) {
        var setter = SetterCache.GetOrAdd(prop, p => {
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

internal class EntityCache
{
    private readonly Dictionary<IEntityType, Dictionary<IAnnotatable, BlockingCollection<object>>> _cache = new();
    public readonly Dictionary<IEntityType, List<object>> NonUniqueCache  =  new();
    private readonly Dictionary<IEntityType, object>_conditionVarialbes = new();

    public EntityCache(EntityCache other) {
        NonUniqueCache = other.NonUniqueCache;
        _conditionVarialbes = NonUniqueCache.ToDictionary(kv => kv.Key, _=>new object());
        _cache = other._cache.ToDictionary(kv => kv.Key,
            kv => kv.Value.ToDictionary(kv => kv.Key,_=> new BlockingCollection<object>()));
        foreach (var kv in other.NonUniqueCache){
            foreach (var prop in kv.Value){
                foreach (var blockingCollection in _cache[kv.Key].Values){
                    blockingCollection.Add(prop);
                }
            }
        }
    }
    public EntityCache(ICollection<IEntityType> entityTypes, RelationRandomizer relationRandomizer) {
        foreach (var entityType in entityTypes){
            _cache[entityType] = new();
            NonUniqueCache[entityType] = new();
            _conditionVarialbes[entityType] = new();
        }
        foreach (var entityType in entityTypes){
            foreach (var keyAndNav in relationRandomizer.ForeignKeys.Where(kv=>kv.Key.DeclaringEntityType.IsAssignableFrom(entityType))
                         .Select(kv=>kv)){
                InitToAllInHierarchy(keyAndNav.Key.PrincipalEntityType, keyAndNav.Key);
            }
            foreach (var keyAndNavs in relationRandomizer.CompositeKeys.Where(k=>k.Key.DeclaringEntityType.IsAssignableFrom(entityType))){
                foreach (var keyNav in keyAndNavs.Value){
                    InitToAllInHierarchy(keyNav.TargetEntityType, keyAndNavs.Key);
                }
                
            }
        }
    }
    //Other need to be discarded

    public void Complete(IEntityType type, bool force = false) {
        foreach (var blockingCollection in _cache[type].Values){
            blockingCollection.CompleteAdding();
        }
    }
    private void InitToAllInHierarchy(IEntityType entityType, IAnnotatable key) {
        IEntityType? parent = entityType;
        do{
            if (!_cache[parent].ContainsKey(key))
                _cache[parent][key] = new();
            parent = parent.BaseType;
        } while (parent!=null);

    }

    public void Add(IEntityType type,  object entity) {
        foreach (var collection in GetQueuesInHierarchy(type)){
            collection.Add(entity);
        }
        AddToRandom(type, entity);
    }
    private IEnumerable<BlockingCollection<object>> GetQueuesInHierarchy(IEntityType entityType) {
        IEntityType? parent = entityType;
        do{
            foreach (var blockingCollection in _cache[parent].Values){
                yield return blockingCollection;
            }
            parent = parent.BaseType;
        } while (parent!=null);
    }
    private object GetRandom(IEntityType type) {
        var l = _conditionVarialbes[type];
        
        List<object> list;
        lock (l){
            list = NonUniqueCache[type];
            if (list.Count == 0){
                Monitor.Wait(l);
            }
        }
        return list[Random.Shared.Next(list.Count)];
    }
    private void AddToRandom(IEntityType type, object entity) {
        IEntityType? parent = type;
        do{
            var l = _conditionVarialbes[parent];
            lock (l){
                var d = NonUniqueCache[parent];
                d.Add(entity);
                if (d.Count == 1){
                    Monitor.PulseAll(l);
                }
            }
            parent = parent.BaseType;
        } while (parent!=null);
    }
    public int Count(IEntityType type) {
        lock (_conditionVarialbes[type]){
            return NonUniqueCache[type].Count;
        }
    }
    
    public BlockingCollection<object> GetAll(IEntityType from, IAnnotatable to) {
            return _cache[from][to];
    }
    public object Get(IEntityType from, IAnnotatable? to = null) {
        if (to == null){
            return GetRandom(from);
        }
        
        return _cache[from][to].Take();
    }
}
