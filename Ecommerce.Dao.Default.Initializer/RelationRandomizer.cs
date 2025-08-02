using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.Dao.Default.Initializer;

internal class RelationRandomizer
{
    private EntityCache _globalStore;
    public readonly Dictionary<IKey, ICollection<INavigation>> CompositeKeys =new () ;
    public readonly Dictionary<IForeignKey, INavigation> ForeignKeys = new();
    private readonly Dictionary<(IEntityType,IKey), IEnumerator<ISet<object>>> _keyEnumerators = new();
    private readonly Dictionary<(IEntityType,IForeignKey), IEnumerator<object>> _foreignKeyEnumerators = new();
    private readonly Dictionary<(IEntityType,IAnnotatable), Lock> _enumeratorLocks = new();
    internal RelationRandomizer(ICollection<IEntityType> types) {
        foreach (var entityType in types){
            var compositeKeys = entityType.GetProperties() //properties that participate in a composite key
                .SelectMany(p => p.GetContainingKeys().Where(k => k.Properties.Count > 1)).ToHashSet();
            foreach (var compositeKey in compositeKeys.Where(k1 => !compositeKeys.Any(k2 =>
                             !k1.Equals(k2) && k1.Properties.All(p1 => k2.Properties.Contains(p1)) //Only get the most inclusive key.
                     ))){
                var navs = compositeKey.Properties.SelectMany(p=>p.GetContainingForeignKeys()).Select(fk=>fk.GetNavigation(true)).Where(n=>n!=null).ToHashSet();
                CompositeKeys[compositeKey] = navs!;
                _enumeratorLocks[(entityType,compositeKey)] = new Lock();

            }
            foreach (var keyValuePair in entityType.GetForeignKeys()
                         .Where(f => CompositeKeys.Where(kv => kv.Key.DeclaringEntityType == f.DeclaringEntityType)
                             .Select(kv => kv.Key)
                             .All(key => !f.Properties.All(fp => key.Properties.Contains(fp))) && f.GetNavigation(true)!=null //
                             )
                         .Select(f => (f,f.GetNavigation(true)))){
                ForeignKeys[keyValuePair.f] =  keyValuePair.Item2;
                _enumeratorLocks[(entityType,keyValuePair.f)] = new Lock();
            }
        }
    }

    public void SetGlobalStore(EntityCache globalStore) {
        _globalStore = globalStore;
    }
    public IEnumerable<(INavigation, object)> GetKeyValues(IEntityType type,IKey key) {
        var navigations = CompositeKeys[key];
        _enumeratorLocks[(type,key)].Enter();
        try{
            if (!_keyEnumerators.TryGetValue((type, key), out var enumerator)){
                enumerator = _keyEnumerators[(type, key)] =
                    CartesianLive(navigations.Select(n => _globalStore.GetAll(n.TargetEntityType,type, key)).ToArray()).GetEnumerator();
            }
            if (!enumerator.MoveNext()) return[];
            var ret = enumerator.Current.Select(k =>
                (navigations.First(n => n.TargetEntityType.ClrType.IsInstanceOfType(k)), k));
            return ret;
        }
        finally{
            _enumeratorLocks[(type,key)].Exit();
        }

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="foreignKey"></param>
    /// <param name="from">passed when the fk is self-referencing and there needs to be no cycles</param>
    /// <returns></returns>
    public (INavigation, object) GetForeignKeyValue(IEntityType entityType,IForeignKey foreignKey, object? from=null) {
        var nav = ForeignKeys[foreignKey];
        if (!foreignKey.IsUnique) return (nav, RetrieveRandom(nav.TargetEntityType));
        var lck = _enumeratorLocks[(entityType,foreignKey)];
        lck.Enter();
        try{
            if (!_foreignKeyEnumerators.TryGetValue((entityType,foreignKey), out var enumerator)){
                enumerator = _foreignKeyEnumerators[(entityType,foreignKey)] = UniqueEnumerable(_globalStore.GetAll(foreignKey.PrincipalEntityType,entityType,foreignKey)).GetEnumerator();
            }
            bool selfRef = foreignKey.IsSelfReferencing();
            do{
                if (!enumerator.MoveNext())
                    throw new IndexOutOfRangeException();
            } while (selfRef && ContainsInHierarchy(nav.PropertyInfo!,enumerator.Current, from));
            return (nav,enumerator.Current);
        }
        finally{
            lck.Exit();
        }
    }

    private static IEnumerable<ISet<object>> CartesianLive(
    BlockingCollection<object>[] sources,
    CancellationToken cancellationToken = default){
    var allItems = new List<object>[sources.Length];
    for (int i = 0; i < sources.Length; i++)
        allItems[i] = new List<object>();

    var lastCombinationCount = 0;
    var readerTasks = new Task[sources.Length];

    for (int i = 0; i < sources.Length; i++)
    {
        int index = i; // Capture for closure
        readerTasks[i] = Task.Run(() =>
        {
            // Don't consume - just copy items as they arrive
            foreach (var item in sources[index].GetConsumingEnumerable(cancellationToken))
            {
                lock (allItems)
                {
                    allItems[index].Add(item);
                    Monitor.PulseAll(allItems);
                }
            }
        }, cancellationToken);
    }

    while (!cancellationToken.IsCancellationRequested)
    {
        lock (allItems)
        {
            // Wait for at least one item in each collection
            if (allItems.Any(list => list.Count == 0))
            {
                Monitor.Wait(allItems, 1000);
                continue;
            }

            var totalCombinations = allItems.Aggregate(1, (acc, list) => acc * list.Count);
            
            // Only generate new combinations
            for (int i = lastCombinationCount; i < totalCombinations; i++)
            {
                var combination = GenerateNthCombination(allItems, i);
                yield return new HashSet<object>(combination);
            }
            
            lastCombinationCount = totalCombinations;
        }

        if (readerTasks.All(t => t.IsCompleted))
            break;

        Thread.Sleep(10);
    }
}

private static object[] GenerateNthCombination(List<object>[] arrays, int n)
{
    var result = new object[arrays.Length];
    var temp = n;
    
    for (int i = arrays.Length - 1; i >= 0; i--)
    {
        result[i] = arrays[i][temp % arrays[i].Count];
        temp /= arrays[i].Count;
    }
    
    return result;
}
    public class CollectionContext(BlockingCollection<object> queue, List<object> snapshot)
    {
        public int Index { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public BlockingCollection<object> Queue { get; set; } = queue;
        public List<object> Snapshot { get; set; } = snapshot;
    }
    private bool MoveNext(CollectionContext[] collectionContexts) {
        while (collectionContexts.Any(c=>!c.IsCompleted)){
            for (int i = collectionContexts.Length - 1; i >= 0; i--) {
                collectionContexts[i].Index++;
                if (collectionContexts[i].Index < collectionContexts[i].Snapshot.Count) return true;
                collectionContexts[i].IsCompleted = collectionContexts[i].Queue.IsCompleted;
            }
        }
        return false;
    }
    public void Dispose(IEntityType type) {
        foreach (var enumerator in type.GetForeignKeys().Select(f=>_foreignKeyEnumerators.GetValueOrDefault((type, f))).Where(f=>f!=null)){
            enumerator.Dispose();
        }
        foreach (var enumerator in type.GetKeys().Select(k=>_keyEnumerators.GetValueOrDefault((type,k))).Where(k=>k!=null)){
            enumerator.Dispose();
        }
    }
    private bool ContainsInHierarchy(PropertyInfo propertyInfo, object searched, object? looked) {
        if (looked == null!) return false;
        object? gotten;
        do{
            gotten = propertyInfo.GetValue(searched);
            if (gotten == looked) return true;
        } while (gotten!=null);
        return false;
    }
    private object RetrieveRandom(IEntityType type) {
        return _globalStore.Get(type);
    }
    private static IEnumerable<object> UniqueEnumerable(BlockingCollection<object> objects) {
        while (!objects.IsCompleted){
            yield return objects.Take();
        }
    }
    
    // private static IEnumerable<EqualityComparableSet<object>> CartesianEnumerable(BlockingCollection<object>[] sets)
    // {
    //     if (!sets.Any())
    //     {
    //         yield return new EqualityComparableSet<object>();
    //         yield break;
    //     }
    //
    //     var setsArray = sets.ToArray();
    //     var indices = new int[setsArray.Length];
    //     var setsSizes = setsArray.Select(s => s.Count).ToArray();
    //
    //     if (setsSizes.Any(size => size == 0))
    //         yield break;
    //     
    //     do
    //     {
    //         var result = new EqualityComparableSet<object>();
    //         for (int i = 0; i < setsArray.Length; i++)
    //         {
    //             result.Add(setsArray[i].ElementAt(indices[i]));
    //         }
    //         yield return result;
    //     }
    //     while (IncrementIndices(indices, setsSizes));
    // }
    private static bool IncrementIndices(int[] indices, int[] maxValues)
    {
        for (int i = indices.Length - 1; i >= 0; i--)
        {
            indices[i]++;
            if (indices[i] < maxValues[i])
                return true;
            indices[i] = 0;
        }
        return false;
    }
    // private static Stack<EqualityComparableSet<object>> Cartesian(ICollection<ICollection<object>> sets)
    // {
    //     Stack<EqualityComparableSet<object>> temp =new Stack<EqualityComparableSet<object>>([[]]);
    //     for (int i = 0; i < sets.Count; i++)
    //     {
    //         Stack<EqualityComparableSet<object>> newTemp =[];
    //         foreach (EqualityComparableSet<object> product in temp)
    //         {
    //             foreach (var element in sets.ElementAt(i))
    //             {
    //                 var tempCopy = new EqualityComparableSet<object>(product){ element };
    //                 newTemp.Push(tempCopy);
    //             }
    //         }
    //         temp = newTemp;
    //     }
    //
    //     return temp;
    // }
}