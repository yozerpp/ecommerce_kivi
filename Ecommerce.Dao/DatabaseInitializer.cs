using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Bogus;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Address = Ecommerce.Entity.Common.Address;
using Enum = System.Enum;

namespace Ecommerce.Dao;

public class DatabaseInitializer
{
    private readonly DbContext _context;
    private readonly Lock _dictLock = new();
    private readonly Dictionary<IEntityType, ISet<object>> _saved = new ();
    private readonly Dictionary<IEntityType, Dictionary<INavigation, ISet<object>>> _uniqueStore = new();
    private readonly Dictionary<Type, Int32?> _typeCounts = new();
    private readonly Int32 _defaultCount;
    private readonly ICollection<INavigation> _nonRequiredNavigations = new List<INavigation>();
    private readonly ICollection<INavigation> _requiredNavigations = new List<INavigation>();
    private readonly IEnumerable<IEntityType> _entityTypes;
    //
    public DatabaseInitializer(DbContext context, Dictionary<Type, Int32?> typeCounts, Int32 defaultCount = 100) {
        this._context = context;
        this._defaultCount = defaultCount;
        this._typeCounts = typeCounts;
        _entityTypes = context.Model.GetEntityTypes();
    }
    public void initialize() {
        _context.Database.EnsureCreated();
        Init();
        CreateEntities();
        RandomizeNavigationProperties(true);
        PersistAll();
        _context.SaveChanges();
        RandomizeNavigationProperties(false);
        UpdateAll();
        _context.SaveChanges();
    }

    private void CreateEntities() {
        foreach (var type in _entityTypes){
            for (int i = 0; i <(_typeCounts.GetValueOrDefault(type.ClrType) ?? _defaultCount); i++){
                if (IsFull(type)) continue;
                var e = RandomizeValueProperties(type);
                SaveToDict(type,e);
            }
        }
    }
    private void Init() {
        foreach (var entityType in _entityTypes){
            _saved[entityType] = new HashSet<object>();
            _uniqueStore[entityType] = new Dictionary<INavigation, ISet<object>>();
            foreach (var navigation in entityType.GetNavigationsInHierarchy().Where(n=>n.IsOnDependent&&n.DeclaringEntityType!=entityType&&entityType.IsAssignableFrom(n.DeclaringEntityType))){
                _uniqueStore[entityType][navigation] = new HashSet<object>();
                if (navigation.ForeignKey.IsRequired)
                    _requiredNavigations.Add(navigation);
                else 
                    _nonRequiredNavigations.Add(navigation);
            }
        }
    }

    private void SaveToDict(IEntityType entityType, object entity) {
        lock (_dictLock){
            (_saved.GetValueOrDefault(entityType)?? (_saved[entityType] = new HashSet<object>())).Add(entity);
        }
    }
    private object RetrieveRandom(IEntityType type) {
        lock (_dictLock){
            var s = _saved.GetValueOrDefault(type)?? (_saved[type] = new HashSet<object>());
            return s.ElementAt(new Randomizer().Number(s.Count - 1));
        }
    }
    //don't allow circular reference.
    private object? RetrieveSelfReferencing(IEntityType type, object current, PropertyInfo property) {
        lock (_dictLock){
            var s = (_saved.GetValueOrDefault(type) ?? (_saved[type] = new HashSet<object>())).Where(o =>
                !property.GetValue(o)?.Equals(current) ?? false);
            return s.ElementAtOrDefault(new Randomizer().Number(s.Count() - 1));
        }
    }

    private object RetrieveUnique(IEntityType type, INavigation nav) {
        lock (_dictLock){
            var s = (_saved.GetValueOrDefault(nav.TargetEntityType) ?? (_saved[nav.TargetEntityType] = new HashSet<object>())).First(s=>!(_uniqueStore[type].GetValueOrDefault(nav)??
                (_uniqueStore[type][nav] = new HashSet<object>())).Contains(s));
            _uniqueStore[type][nav].Add(s);
            return s;
        }
    }
    private void RandomizeNavigationProperties(bool required) {
        foreach (var typeAndEntity in _saved){
            var entityType = typeAndEntity.Key;
            var navs = entityType.GetNavigationsInHierarchy().Where(n =>
                n.IsOnDependent && (n.DeclaringEntityType == entityType ||
                                    n.DeclaringEntityType.IsAssignableFrom(entityType)) && n.ForeignKey.IsRequired == required).ToArray();
            if (navs.Length==0) continue;
            foreach (var entity in typeAndEntity.Value){
                foreach (var nav in navs){
                    object target;
                    if (nav.PropertyInfo.GetCustomAttribute<SelfReferencingProperty>()?.BreakCycle ?? false){
                        target = RetrieveSelfReferencing(entityType, entity, nav.PropertyInfo)!;
                    }
                    else if (nav.ForeignKey.IsUnique){
                        target = RetrieveUnique(entityType, nav);
                    }
                    else target = RetrieveRandom(nav.TargetEntityType);
                    nav.PropertyInfo.SetValue(entity, target);
                }
            }
        }
    }
    private void populateIds(IEntityType entityType, object original, object populated) {
        foreach (var key in entityType.GetKeys()){
            foreach (var keyProperty in key.Properties){
                keyProperty.PropertyInfo.SetValue(original, keyProperty.PropertyInfo.GetValue(populated));
            }
        }
    }
    private void PersistAll() {
        foreach (var kv in _saved){
            var entityType = kv.Key;
            var navs = entityType.GetNavigationsInHierarchy()
                .Where(n => n.IsOnDependent && n.DeclaringEntityType.IsAssignableFrom(entityType));
            foreach (var entity in kv.Value){
                _context.Add(entity);
            }
        }
    }

    private void Persist(IEntityType entityType, object entity) {
        
    }
    private void UpdateAll() {
        foreach (var savedValue in _saved.Values){
            foreach (var entity in savedValue){
                _context.Update(entity);
            }
        }
    }
    private object RandomizeValueProperties(IEntityType entityType) {
        var entity = entityType.ClrType.GetConstructor([])!.Invoke(null);
        foreach (var prop in entityType.GetPropertiesInHierarchy().Where(p=> !p.IsShadowProperty() && !p.IsPrimaryKey() && !p.IsForeignKey()&&
            (p.DeclaringType.Equals(entityType) || !entityType.IsAssignableFrom(p.DeclaringType))))
        {
            var property = prop.PropertyInfo!;
            object? value = null;
            var propType = property.PropertyType;
            property.SetValue(entity, RandomizeValue(propType, property, value, prop));
        }
        return entity;
        object? RandomizeValue(Type propType, PropertyInfo property, object? value, IProperty prop) {
            if ( propType== typeof(string)) {
                if (property.Name.Contains("username"))
                    value = new Bogus.DataSets.Internet().UserName();
                else if (property.Name.Contains("name")) 
                    value = new Bogus.Person().FirstName;
                else if (property.GetCustomAttribute<EmailAddressAttribute>() != null)
                    value = new Bogus.Person().Email;
                else{
                    int max = prop.GetMaxLength()?? 100;
                    int min = property.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;
                    value = new Bogus.Randomizer().Words(max / 5).Remove(new Bogus.Randomizer().Number(min,max));
                }
            }else if (property.GetCustomAttribute<PhoneAttribute>() != null) {
                if (typeof(PhoneNumber).IsAssignableFrom(propType)) {
                    value = new PhoneNumber{CountryCode = new Bogus.Randomizer().Number(999),Number = new Bogus.Faker().Phone.PhoneNumber()};    
                } else value = new Faker().Phone.PhoneNumber();
            } else if(typeof(Address).IsAssignableFrom(propType)){
                value = new Address{
                    City = new Faker().Address.City(), neighborhood = new Faker().Address.County(),
                    state = new Faker().Address.State(), Street = new Faker().Address.StreetName(),
                    ZipCode = new Faker().Address.ZipCode()
                };
            } else if (propType.IsEnum){
                var enums= Enum.GetValues(propType);
                value = enums.GetValue(new Randomizer().Number(enums.Length-1)) ?? enums.GetValue(0)!;
            } else if (typeof(DateTime).IsAssignableFrom(propType)){
                if (propType.GetCustomAttribute<BirthDate>() != null)
                    value = new Faker().Date.Past(50);
                else value = new Faker().Date.Recent(60);
            }else if (propType.IsPrimitive){
                var range = property.GetCustomAttribute<RangeAttribute>();
                switch (Type.GetTypeCode(propType)){
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Int16:
                        value = new Randomizer().Number(range?.Minimum as int? ?? Int16.MinValue,
                            range?.Maximum as int? ?? Int16.MaxValue);
                        break;
                    case TypeCode.Double:
                        value = new Randomizer().Double(range?.Minimum as double? ?? Double.MinValue,
                            range?.Maximum as double? ?? Double.MaxValue);
                        break;
                    case TypeCode.Boolean:
                        value = new Randomizer().Bool();
                        break;
                    case TypeCode.Decimal:
                        value = new Randomizer().Decimal(range?.Minimum as decimal? ?? Decimal.MinValue,
                            range?.Maximum as decimal? ?? Decimal.MaxValue);
                        break;
                    case TypeCode.Char:
                        value = new Randomizer().Char();
                        break;
                    case TypeCode.Byte:
                        value = new Randomizer().Byte();
                        break;
                    case TypeCode.SByte:
                        value = new Randomizer().SByte();
                        break;
                }
            }
            else value = null;

            return value;
        }
    }
    bool IsFull(IEntityType entityType1) {
        lock (_dictLock){
            return (_saved.GetValueOrDefault(entityType1) ?? (_saved[entityType1] = new HashSet<object>())).Count >=
                   (_typeCounts.GetValueOrDefault(entityType1.ClrType) ?? _defaultCount);
        }
    }
}