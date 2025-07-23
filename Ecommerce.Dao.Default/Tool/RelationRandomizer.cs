using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.Dao.Default.Tool;

public class RelationRandomizer
{
    private readonly  Dictionary<IEntityType, ISet<object>> _globalStore;
    private readonly Dictionary<IKey, ICollection<INavigation>> _compositeKeys =new () ;
    private readonly Dictionary<IForeignKey, INavigation> _foreignKeys = new();
    private readonly Dictionary<IAnnotatable, (Lock, Lock)> _locks = new();
    private readonly Dictionary<IKey, IEnumerator<ISet<object>>> _keyEnumerators = new();
    private readonly Dictionary<IForeignKey, IEnumerator<object>> _foreignKeyEnumerators = new();
    public RelationRandomizer(Dictionary<IEntityType, ISet<object>> globalStore, Dictionary<IEntityType,Lock> lck) {
        _globalStore = globalStore;
        foreach (var entityType in _globalStore.Keys){
            var compositeKeys = entityType.GetProperties() //properties that participate in a composite key
                .SelectMany(p => p.GetContainingKeys().Where(k => k.Properties.Count > 1)).ToHashSet();
            foreach (var compositeKey in compositeKeys.Where(k1 => !compositeKeys.Any(k2 =>
                             !k1.Equals(k2) && k1.Properties.All(p1 => k2.Properties.Contains(p1)) //Only get the most inclusive key.
                     ))){
                var navs = compositeKey.Properties.SelectMany(p=>p.GetContainingForeignKeys()).Select(fk=>fk.GetNavigation(true)).Where(n=>n!=null).ToHashSet();
                _compositeKeys[compositeKey] = navs!;
                _locks[compositeKey] = (new Lock(), new Lock());
            }
            foreach (var keyValuePair in entityType.GetForeignKeys()
                         .Where(f => _compositeKeys.Where(kv => kv.Key.DeclaringEntityType == f.DeclaringEntityType)
                             .Select(kv => kv.Key)
                             .All(key => !f.Properties.All(fp => key.Properties.Contains(fp))))
                         .Select(f => (f,f.GetNavigation(true)))){
                _foreignKeys[keyValuePair.f] =  keyValuePair.Item2;
            }
            
        }
    }

    public IEnumerable<(INavigation, object)> GetKeyValues(IKey key) {
        var navigations = _compositeKeys[key];
        if (!_keyEnumerators.TryGetValue(key, out var enumerator)){
            enumerator = _keyEnumerators[key] =
                CartesianEnumerable(navigations.Select(n => _globalStore[n.TargetEntityType]).ToArray()).GetEnumerator();
        }
        if (!enumerator.MoveNext()) return[];
        return enumerator.Current.Select(k =>
            (navigations.First(n => n.TargetEntityType.ClrType.IsInstanceOfType(k)), k));
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="foreignKey"></param>
    /// <param name="from">passed when the fk is self-referencing and there needs to be no cycles</param>
    /// <returns></returns>
    public (INavigation, object) GetForeignKeyValue(IForeignKey foreignKey, object? from=null) {
        var nav = _foreignKeys[foreignKey];
        if (!foreignKey.IsUnique) return (nav, RetrieveRandom(nav.TargetEntityType));
        if (!_foreignKeyEnumerators.TryGetValue(foreignKey, out var enumerator)){
            enumerator = _foreignKeyEnumerators[foreignKey] = UniqueEnumerable(_globalStore[foreignKey.PrincipalEntityType]).GetEnumerator();
        }

        bool selfRef = foreignKey.IsSelfReferencing();
        do{
            if (!enumerator.MoveNext()) 
                return(nav, null);    
        } while (selfRef && enumerator.Current.Equals(from)); 
        return (nav, enumerator.Current);
    }

    private object RetrieveRandom(IEntityType type) {
        var s = _globalStore[type];
        var ret = s.ElementAt(new Randomizer().Number(s.Count - 1));
        return ret;
    }
    private static IEnumerable<object> UniqueEnumerable(ICollection<object> objects) {
        foreach (var o in objects){
            yield return o;
        }
    }
    private static IEnumerable<EqualityComparableSet<object>> CartesianEnumerable(ICollection<ICollection<object>> sets)
    {
        if (!sets.Any())
        {
            yield return new EqualityComparableSet<object>();
            yield break;
        }

        var setsArray = sets.ToArray();
        var indices = new int[setsArray.Length];
        var setsSizes = setsArray.Select(s => s.Count).ToArray();

        if (setsSizes.Any(size => size == 0))
            yield break;
        
        do
        {
            var result = new EqualityComparableSet<object>();
            for (int i = 0; i < setsArray.Length; i++)
            {
                result.Add(setsArray[i].ElementAt(indices[i]));
            }
            yield return result;
        }
        while (IncrementIndices(indices, setsSizes));
    }
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