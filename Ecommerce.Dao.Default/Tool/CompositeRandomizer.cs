using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.Dao.Default.Tool;

public class CompositeRandomizer
{
    private readonly  Dictionary<IEntityType, ISet<object>> _globalStore;
    private readonly Dictionary<IEntityType, IDictionary<IKey, ICollection<INavigation>>> _keys =new () ;
    private readonly Dictionary<IKey, ICollection<EqualityComparableSet<object>>> _savedKeys=new ();
    private readonly Dictionary<IEntityType, Lock> _globalLocks;
    private readonly Dictionary<IKey, (Lock, Lock)> _locks = new();
    private readonly Dictionary<IKey, ICollection<EqualityComparableSet<object>>> _computedCombinations = new();
    public CompositeRandomizer(Dictionary<IEntityType, ISet<object>> globalStore, Dictionary<IEntityType,Lock> lck) {
        _globalStore = globalStore;
        _globalLocks = lck;
        InitMaps();
    }

    public List<INavigation> GetNavigations(IEntityType entityType) {
        var ret = new List<INavigation>();
        foreach (var entries in _keys[entityType]){
            ret.AddRange(entries.Value);
        }
        return ret;
    }
    private void InitMaps() {
        foreach (var entityType in _globalStore.Keys){
            _keys[entityType] = new Dictionary<IKey, ICollection<INavigation>>(); //need to filter out the keys that are included in other keys.
            var compositeKeys = entityType.GetProperties() //properties that participate in a composite key
                .SelectMany(p => p.GetContainingKeys().Where(k => k.Properties.Count > 1)).ToHashSet();
            foreach (var compositeKey in compositeKeys.Where(k1 => !compositeKeys.Any(k2 =>
                             !k1.Equals(k2) && k1.Properties.All(p1 => k2.Properties.Contains(p1)) //Only get the most inclusive key.
                     ))){
                var navs = compositeKey.Properties.SelectMany(p=>p.GetContainingForeignKeys()).Select(fk=>fk.GetNavigation(true)).ToHashSet();
                _keys[entityType][compositeKey] = navs!;
                _savedKeys[compositeKey]=[];
                _locks[compositeKey] = (new Lock(), new Lock());
            }
        }
    }
    public IEnumerable<(IForeignKey, object)> GetCompositeKeys(IEntityType type) {
        return _keys[type].Count == 0 ? [] : _keys[type].SelectMany(pair => FindNonExistentCombination(pair.Key, pair.Value));
    }
    
    private IEnumerable<(IForeignKey, object)> FindNonExistentCombination(IKey key, ICollection<INavigation> navigations) {
        _locks[key].Item1.Enter();
        if (!_computedCombinations.TryGetValue(key, out var combinations)){
            var lcks = _globalLocks.Where(p => navigations.Any(n => n.TargetEntityType.IsAssignableFrom(p.Key))).ToArray();
            foreach(var lck in lcks)
                lck.Value.Enter();
            combinations = Cartesian(navigations.Select(n => _globalStore[n.TargetEntityType]).ToArray());
            foreach (var keyValuePair in lcks){
                keyValuePair.Value.Exit();
            }
            _computedCombinations[key] = combinations;
        }
        _locks[key].Item1.Exit();
        foreach (var keyCandidate in combinations){
            _locks[key].Item2.Enter();
            if (!_savedKeys[key].Contains(keyCandidate)){
                _savedKeys[key].Add(keyCandidate);
                var ret = keyCandidate.Select(k =>
                    (navigations.First(n => n.TargetEntityType.ClrType.IsInstanceOfType(k)).ForeignKey, k)); //TODO potential issue here if both navigations target different entities in the same hierarchy.
                _locks[key].Item2.Exit();
                return ret;
            }
            _locks[key].Item2.Exit();
        }
        throw new InvalidOperationException("Cannot find a non-existent combination for key: " + key.Properties.Select(p=>p.Name).Aggregate((a, b) => a + ", " + b));
    }
    private static List<EqualityComparableSet<object>> Cartesian(ICollection<ICollection<object>> sets)
    {
        List<EqualityComparableSet<object>> temp =[new()];
        for (int i = 0; i < sets.Count; i++)
        {
            List<EqualityComparableSet<object>> newTemp =[];
            foreach (EqualityComparableSet<object> product in temp)
            {
                foreach (var element in sets.ElementAt(i))
                {
                    var tempCopy = new EqualityComparableSet<object>(product){ element };
                    newTemp.Add(tempCopy);
                }
            }
            temp = newTemp;
        }

        return temp;
    }
}