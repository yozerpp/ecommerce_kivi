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
    private readonly Dictionary<IKey, IEnumerator<ISet<object>>> _keyEnumerators = new();
    private readonly Dictionary<IForeignKey, IEnumerator<object>> _foreignKeyEnumerators = new();
    private readonly Dictionary<IAnnotatable, Lock> _enumeratorLocks = new();
    internal RelationRandomizer(ICollection<IEntityType> types) {
        foreach (var entityType in types){
            var compositeKeys = entityType.GetProperties() //properties that participate in a composite key
                .SelectMany(p => p.GetContainingKeys().Where(k => k.Properties.Count > 1)).ToHashSet();
            foreach (var compositeKey in compositeKeys.Where(k1 => !compositeKeys.Any(k2 =>
                             !k1.Equals(k2) && k1.Properties.All(p1 => k2.Properties.Contains(p1)) //Only get the most inclusive key.
                     ))){
                var navs = compositeKey.Properties.SelectMany(p=>p.GetContainingForeignKeys()).Select(fk=>fk.GetNavigation(true)).Where(n=>n!=null).ToHashSet();
                CompositeKeys[compositeKey] = navs!;
                _enumeratorLocks[compositeKey] = new Lock();
            }
            foreach (var keyValuePair in entityType.GetForeignKeys()
                         .Where(f => CompositeKeys.Where(kv => kv.Key.DeclaringEntityType == f.DeclaringEntityType)
                             .Select(kv => kv.Key)
                             .All(key => !f.Properties.All(fp => key.Properties.Contains(fp))) && f.GetNavigation(true)!=null //
                             )
                         .Select(f => (f,f.GetNavigation(true)))){
                ForeignKeys[keyValuePair.f] =  keyValuePair.Item2;
                _enumeratorLocks[keyValuePair.f] = new Lock();
            }
        }
    }

    public void SetGlobalStore(EntityCache globalStore) {
        _globalStore = globalStore;
    }
    public IEnumerable<(INavigation, object)> GetKeyValues(IEntityType type,IKey key) {
        var navigations = CompositeKeys[key];
        _enumeratorLocks[key].Enter();
        try{
            if (!_keyEnumerators.TryGetValue( key, out var enumerator)){
                enumerator = _keyEnumerators[ key] =
                    CartesianLive(navigations.Select(n => _globalStore.GetAll(n.TargetEntityType,key)).ToArray()).GetEnumerator();
            }
            if (!enumerator.MoveNext()) throw new IndexOutOfRangeException();
            var ret = enumerator.Current.Select(k =>
                (navigations.First(n => n.TargetEntityType.ClrType.IsInstanceOfType(k)), k));
            return ret;
        }
        finally{
            _enumeratorLocks[key].Exit();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="foreignKey"></param>
    /// <param name="from">passed when the fk is self-referencing and there needs to be no cycles</param>
    /// <returns></returns>
    public (INavigation, object) GetForeignKeyValue(IForeignKey foreignKey, object from) {
        var nav = ForeignKeys[foreignKey];
        bool selfRef = foreignKey.IsSelfReferencing(); //don't allow cycles in self-ref foreign keys by default
        if (!foreignKey.IsUnique){
            object ret;
            do{
                ret = RetrieveRandom(nav.TargetEntityType);
            } while (selfRef && ContainsInHierarchy(nav.PropertyInfo, ret, from));
            return (nav,ret);
        }
        var lck = _enumeratorLocks[foreignKey];
        lck.Enter();
        try{
            if (!_foreignKeyEnumerators.TryGetValue(foreignKey, out var enumerator)){
                enumerator = _foreignKeyEnumerators[foreignKey] = UniqueEnumerable(_globalStore.GetAll(foreignKey.PrincipalEntityType,foreignKey)).GetEnumerator();
            }
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
    object lck = new();
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
                lock (lck)
                {
                    allItems[index].Add(item);
                    Monitor.PulseAll(lck);
                }
            }
        }, cancellationToken);
    }
    List<string> processedIndices = new();
    var addingCompleted = false;
    while (!cancellationToken.IsCancellationRequested)
    {
        lock (lck)
        {
            // Wait for at least one item in each collection
            if (allItems.Any(list => list.Count == 0))
            {
                Monitor.Wait(lck, 1000);
                continue;
            }
            int totalCombinations;
            while (true){
                totalCombinations = allItems.Aggregate(1, (acc, list) => acc * list.Count);
                if(totalCombinations > lastCombinationCount) break;
                if (addingCompleted) yield break;
                Monitor.Wait(lck, 1000);
            }   
            // Only generate new combinations
            for (int i = lastCombinationCount; i < totalCombinations; i++)
            {
                var indices = GenerateNthCombinationIndices(allItems, i);
                if (processedIndices.Any(ids=>ids.Equals(string.Join(",",indices))))
                    continue;
                processedIndices.Add(string.Join(",",indices));
                yield return indices.Select((idx, ii) => allItems[ii][idx]).ToHashSet();
            }
            
            lastCombinationCount = totalCombinations;
        }

        if (readerTasks.All(t => t.IsCompleted))
            addingCompleted = true;
    }
    }
    private static int[] GenerateNthCombinationIndices(List<object>[] arrays, int n)
    {
        var result = new int[arrays.Length];
        for (int i = arrays.Length - 1, idx = n; i >= 0; i--){
            result[i] = idx % arrays[i].Count;
            idx /= arrays[i].Count;
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
    public void DisposeEnumerators(IEntityType type) {
        foreach (var enumerator in type.GetForeignKeys().Select(f=>_foreignKeyEnumerators.GetValueOrDefault(f)).Where(f=>f!=null)){
            enumerator.Dispose();
        }
        foreach (var enumerator in type.GetKeys().Select(k=>_keyEnumerators.GetValueOrDefault(k)).Where(k=>k!=null)){
            enumerator.Dispose();
        }
    }
    private bool ContainsInHierarchy(PropertyInfo propertyInfo, object searched, object looked) {
        do{
            if (searched == looked) return true;
            searched = PropertyCache.GetProperty(searched, propertyInfo)!;
        } while (searched!=null!);
        return false;
    }
    private object RetrieveRandom(IEntityType type) {
        return _globalStore.Get(type);
    }
    private static IEnumerable<object> UniqueEnumerable(BlockingCollection<object> objects) {
        while (!objects.IsCompleted){
            if (!objects.TryTake(out var item, TimeSpan.FromMinutes(3))){
                Console.WriteLine("BlockingCollection timed out while trying to take an item.");
                yield break;
            }
            yield return item;
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