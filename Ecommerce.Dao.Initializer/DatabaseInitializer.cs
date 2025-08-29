// #define USE_FOREIGN_KEY_STRATEGY

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Bogus;
using EFCore.BulkExtensions;
using log4net;
using log4net.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Internal;

namespace Ecommerce.Dao.Initializer;
public class DatabaseInitializer: IDisposable
{

    private readonly DbContext _defaultContext;
    private EntityCache _saved;
    private readonly Dictionary<Type, int?> _typeCounts;
    private readonly int _defaultCount;
    private readonly ICollection<IEntityType> _entityTypes;
    private readonly Dictionary<IEntityType, List<IForeignKey>> _requiredForeignKeys;
    private readonly Dictionary<IEntityType, List<IKey>> _compositeKeys;
    private RelationRandomizer _relationRandomizer;
    private readonly ValueRandomizer _valueRandomizer ;
    private readonly Type _contextType;
    private readonly DbContextOptions _dbContextOptions;
    private readonly ICollection<(DbContext, IEntityType)> _lanes;
    private readonly Dictionary<IEntityType, bool?> _isCompleteMap = new();
    private readonly ILog _logger = LogManager.GetLogger(typeof(DatabaseInitializer));
    private readonly Config _config;
    public DatabaseInitializer(Type contextType,DbContextOptions options, Dictionary<Type, int?> typeCounts, Config? config = null, int defaultCount = 100) {
        _config =config ?? new Config();
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _dbContextOptions = options;
        _contextType = contextType;
        _valueRandomizer = new ValueRandomizer(_config);
        _defaultContext = (DbContext) contextType.GetConstructor([typeof(DbContextOptions<>).MakeGenericType(contextType)])!.Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().Where(t=>!(t.IsOwned() && t.GetViewMappings().Any()) && !typeof(Dictionary<string,object>).IsAssignableFrom(t.ClrType)).ToArray();
        var nonZeroTypes = _entityTypes.Where(t=>_typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0).ToArray();
        _entityTypes = _entityTypes.Where(t=>!t.GetViewMappings().Any() && (
            defaultCount > 0 || nonZeroTypes.Any(t.IsAssignableFrom) ||
            _typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0)).ToArray();
        _isCompleteMap = _entityTypes.ToDictionary(t => t, _ => (bool?)false);
        _lanes = _entityTypes.Where(t=>!t.IsAbstract()).Select(t=>((DbContext)_contextType.GetConstructor([_dbContextOptions.GetType()]).Invoke([_dbContextOptions]), t)).ToList();
        _relationRandomizer = new RelationRandomizer(_entityTypes, _config);
        _saved = new EntityCache(_entityTypes, _relationRandomizer);
        _relationRandomizer.SetGlobalStore(_saved);
        _requiredForeignKeys = _entityTypes.ToDictionary(e =>e, e=>_relationRandomizer.ForeignKeys.Keys.Where(f=>f.IsRequired&&f.DeclaringEntityType.IsAssignableFrom(e)).ToList());
        _compositeKeys = _entityTypes.ToDictionary(e => e,
            e => _relationRandomizer.CompositeKeys.Keys.Where(f => f.DeclaringEntityType.IsAssignableFrom(e)).ToList());
    }

    public void initialize() {
        string dbName = _defaultContext.Database.GetDbConnection().Database;
        _logger.Info($"Initializing database {dbName} with {_entityTypes.Count} entities. Total number of entities to be created: {_entityTypes.Sum(t => _typeCounts.GetValueOrDefault(t.ClrType) ?? _defaultCount)}.");
        var start = DateTime.Now;
        CreateEntities().Wait();
        _logger.Info("Finished saving entities with required relationships in " + (DateTime.Now - start).TotalSeconds +
                    " seconds. Wiring up non-required relations.");
        ReinitializeCollections();
        PopulateNonRequiredRelations().Wait();
        _logger.Info($"Finished Initializing database {dbName} in" + (DateTime.Now - start).ToString("g") + ".");
    }
    private void ReinitializeCollections() {
        _saved = new EntityCache(_saved);
        _relationRandomizer = new RelationRandomizer(_entityTypes, _config);
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
        int counter = 0;
        const int batchCount = 2000;
        var batch = new List<object>();
        var lastSaveTask = Task.CompletedTask;
        while (!IsFull(entityType, counter)){
            var entity = Randomize(entityType);
            lastSaveTask = lastSaveTask.ContinueWith(_ => batch.Add(entity));
            if ((++counter) % batchCount == 0){
               lastSaveTask = lastSaveTask.ContinueWith(_=>SaveBatch(entityType,batch, dbContext));
            }
        }
        if(counter%batchCount!=0) //TODO: ub if only one entity was requested
            lastSaveTask=lastSaveTask.ContinueWith(_=>SaveBatch(entityType,batch, dbContext));
        return lastSaveTask.ContinueWith(_=>Complete(entityType));
    }


    private async Task  CreateEntities() {
        #if USE_FOREIGN_KEY_STRATEGY
        #else
        _defaultContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _defaultContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        #endif
        ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
        ThreadPool.SetMaxThreads(workerThreads + _lanes.Count * 2, completionPortThreads + _lanes.Count);
        if(Debugger.IsAttached)Debugger.Break();
        Task.WaitAll(_lanes.Select( l => Task.Run(() => {
                var entityType = l.Item2;
                DbContext ctx;
                _logger.Debug("Creating entities for " + entityType.Name);
                #if USE_FOREIGN_KEY_STRATEGY
                ctx=l.Item1;
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                #else
                ctx = _defaultContext;
                #endif
                try{
                    CreateAll(entityType,ctx).Wait();
                }
                catch (Exception e){
                    _logger.Error(e);
                    throw;
                }
            })
        ).ToArray());
        #if USE_FOREIGN_KEY_STRATEGY
        #else
        _logger.Info("Created Entities, saving changes.");
        await _defaultContext.SaveChangesAsync();
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
    private async Task PopulateNonRequiredRelations() {
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
                            _logger.Warn("Skipping foreign key " + fk.Properties.Select(p=>p.Name).Aggregate((a, b) => a + ", " + b) + " for entity " + t.ClrType.Name + ". There is not enough principal created, consider creating more of the principal entity type.");
                            skipped.Add(fk);
                        }
                    }
                }
                lock (_defaultContext){
                    _defaultContext.UpdateRange(set);
                }
                _logger.Info("Populated " + t.Name);
            }
            catch (Exception e){
                _logger.Error(e);
                throw;
            }
            });
        _logger.Info("Populated non-required relations. Saving changes."); 
        await _defaultContext.SaveChangesAsync();
    }
    private object Randomize(IEntityType enttiyType)
    {
        var entity = _valueRandomizer.Create(enttiyType);
        foreach (var key in _compositeKeys.GetValueOrDefault(enttiyType)??[]){
            
            AssignForeignKeyValues(entity , _relationRandomizer.GetKeyValues(enttiyType,key));
        }
        foreach (var foreignKey in _requiredForeignKeys.GetValueOrDefault(enttiyType)??[]){ //TODO no action if foreign key is both self-referencing and required.
            
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
        return counter >= limit || _saved.Count(entityType1) >= limit ;
    }

    private void Complete(IEntityType type, bool force = false) {
        if(type.IsAbstract()&&!force) return;
        _isCompleteMap[type] = true;
        if (type.BaseType?.GetDerivedTypes().All(t => _isCompleteMap.GetValueOrDefault(t)?? true) ?? false){
            Complete(type.BaseType, true);
            _relationRandomizer.DisposeEnumerators(type);
        }
        _saved.Complete(type);
        _logger.Info("Completed " + type.Name);
    }
    public void Dispose() {
        GC.SuppressFinalize(this);
        _defaultContext.Dispose();
        foreach (var l in _lanes){
            l.Item1.Dispose();
        }
    }

}
public class Config
{
    public uint UniqueAddressCount { get; set; } = 500u;
    public string? AddressFetcherApiKey { get; set; }
    public bool FetchRealAddresses { get; set; } = true;
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
            foreach (var blockingCollection in _cache[kv.Key].Values){
                blockingCollection.CompleteAdding();
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

    public void Complete(IEntityType type) {
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
