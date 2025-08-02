using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.Dao.Default.Initializer;

public class DatabaseInitializer: IDisposable
{
    private readonly DbContext _defaultContext;
    private readonly EntityCache _saved;
    private readonly Dictionary<Type, int?> _typeCounts;
    private readonly SetterCache _setterCache;
    private readonly int _defaultCount;
    private readonly ICollection<IEntityType> _entityTypes;
    private readonly RelationRandomizer _relationRandomizer;
    private readonly ValueRandomizer _valueRandomizer ;
    private readonly Type _contextType;
    private readonly DbContextOptions _dbContextOptions;
    private readonly ICollection<(DbContext, IEntityType)> _lanes;
    public DatabaseInitializer(Type contextType,DbContextOptions options, Dictionary<Type, int?> typeCounts, int defaultCount = 100) {
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _dbContextOptions = options;
        _contextType = contextType;
        _valueRandomizer = new ValueRandomizer(_setterCache = new SetterCache());
        
        _defaultContext = (DbContext) contextType.GetConstructor([typeof(DbContextOptions<>).MakeGenericType(contextType)])!.Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().Where(t=>!t.IsOwned() && !typeof(Dictionary<string,object>).IsAssignableFrom(t.ClrType)).ToArray();
        var nonZeroTypes = _entityTypes.Where(t=>_typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0).ToArray();
        _entityTypes = _entityTypes.Where(t=>
            defaultCount > 0 || nonZeroTypes.Any(t.IsAssignableFrom)|| _typeCounts.ContainsKey(t.ClrType) && _typeCounts[t.ClrType] > 0).ToArray();
        _lanes = _entityTypes.Select(t=>((DbContext)_contextType.GetConstructor([_dbContextOptions.GetType()]).Invoke([_dbContextOptions]), t)).ToList();
        _relationRandomizer = new RelationRandomizer(_entityTypes);
        _saved = new EntityCache(_entityTypes, _relationRandomizer);
        _relationRandomizer.SetGlobalStore(_saved);
    }
    public void initialize() {
        CreateEntities();
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
    private void CreateAll(IEntityType entityType) {
            while (true){
                // try{
                if(IsFull(entityType))break;
                var entity = Randomize(entityType);
                Save(entityType, entity);
                // }
                // catch (Exception e){
                // if (e.InnerException is SqlException sqlException && sqlException.Number == 2627)
                // _contextThreadLocal.Value.ChangeTracker.Clear();
                // else throw;
                // 
                // Debug.WriteLine(e);
                // return;
                // }
            }
            _relationRandomizer.Dispose(entityType);
            _saved.Complete(entityType);
    }
    private void CreateEntities() {
        Task.WaitAll(_lanes.Select(async l => {
            var entityType = l.Item2;
            await Task.Run(() => CreateAll(entityType));
        }));
        _defaultContext.SaveChanges();
    }
    private void Save(IEntityType entityType, object entity) {
        lock (_defaultContext){
            _defaultContext.Add(entity);
        }
        _saved.Add(entityType, entity);
    }
    private void PopulateNonRequiredRelations()
    {
        foreach (var (entityType, set) in _saved.NonUniqueCache){
            var skipped = new HashSet<IForeignKey>();
            foreach (var entity in set)
            {
                foreach (var fk in entityType.GetForeignKeys().Where(fk=>!fk.IsRequired && !skipped.Contains(fk))){
                    try{
                        AssignNavigation(entity, _relationRandomizer.GetForeignKeyValue(entityType,fk, entity));
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
            
            AssignNavigation(entity , _relationRandomizer.GetKeyValues(enttiyType,key));
        }
        foreach (var foreignKey in enttiyType.GetForeignKeys().Where(fk=>fk.IsRequired&&!fk.Properties.All(p=>p.IsKey()))){ //TODO no action if foreign key is both self-referencing and required.
            
            AssignNavigation(entity,_relationRandomizer.GetForeignKeyValue(enttiyType,foreignKey));
        }

        return entity;
    }


    private void AssignNavigation( object dependent, params IEnumerable<(INavigation, object)> keyValues) {
        foreach (var (nav, principal) in keyValues){
            _setterCache.SetProperty(dependent, nav.PropertyInfo, principal);
            // var propsInPrincipal = nav.ForeignKey.PrincipalKey.Properties;
            // var dependentProps = nav.ForeignKey.Properties;
            // for (var i = 0; i < dependentProps.Count; i++){
                // _setterCache.SetProperty(dependent, dependentProps[i].PropertyInfo,propsInPrincipal[i].PropertyInfo.GetValue(principal));
            // }
        }
    }
    private bool IsFull(IEntityType entityType1) {
        var ret = _saved.Count(entityType1) >= (_typeCounts.GetValueOrDefault(entityType1.ClrType) ?? _defaultCount);
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
    private readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> _setterCache = new ();
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

internal class EntityCache
{
    private readonly Dictionary<IEntityType, Dictionary<(IEntityType, IAnnotatable), BlockingCollection<object>>> _cache = new();
    public readonly Dictionary<IEntityType, List<object>> NonUniqueCache  =  new();
    private readonly Dictionary<IEntityType, object>_conditionVarialbes = new();
    private readonly Dictionary<IEntityType, bool> _isCompleteMap = new();
    public EntityCache(ICollection<IEntityType> entityTypes, RelationRandomizer relationRandomizer) {
        foreach (var entityType in entityTypes){
            _cache[entityType] = new();
            NonUniqueCache[entityType] = new();
            _conditionVarialbes[entityType] = new();
            _isCompleteMap[entityType] = false;
        }

        foreach (var entityType in entityTypes){
            foreach (var principalType in relationRandomizer.ForeignKeys.Where(kv=>kv.Key.DeclaringEntityType.IsAssignableFrom(entityType))
                         .Select(kv=>kv)){
                if (!_cache[principalType.Value.TargetEntityType].ContainsKey((entityType, principalType.Key)))
                    _cache[principalType.Value.TargetEntityType].Add((entityType, principalType.Key), new ());
            }
            foreach (var keyAndNavs in relationRandomizer.CompositeKeys.Where(k=>k.Key.DeclaringEntityType.IsAssignableFrom(entityType))){
                foreach (var keyNav in keyAndNavs.Value){
                    var d  =_cache[keyNav.TargetEntityType];
                    if (!d.ContainsKey((entityType, keyAndNavs.Key))){
                        d.Add((entityType, keyAndNavs.Key),new  ());
                    }    
                }
                
            }
        }
    }
    public void Complete(IEntityType type, bool force = false) {
        if(type.IsAbstract()&&!force) return;
        if(type.BaseType?.GetDerivedTypes().All(t=>_isCompleteMap[t])??false)
            Complete(type, true);    
        foreach (var blockingCollection in _cache[type].Values){
            blockingCollection.CompleteAdding();
        }
        _isCompleteMap[type] = true;
        Console.WriteLine("Completed " + type.Name);
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
    public BlockingCollection<object> GetAll(IEntityType from, IEntityType to, IAnnotatable key) {
        
        return _cache[from][(to,key)];
    }
    public object Get(IEntityType from, IEntityType? to = null, IAnnotatable? key = null) {
        if (to == null || key == null){
            return GetRandom(from);
        }
        
        return _cache[from][(to,key)].Take();
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