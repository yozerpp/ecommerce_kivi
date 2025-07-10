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
    private readonly Dictionary<IEntityType, Dictionary<INavigation, ISet<object> >> _uniqueStore = new();
    private readonly Dictionary<IEntityType, Dictionary<EqualityComparableSet<INavigation>,Dictionary<object, EqualityComparableSet<object>>>> _compositeKeyStore = new();
    private readonly Dictionary<Type, Int32?> _typeCounts = new();
    private readonly Int32 _defaultCount;
    private readonly Dictionary<IEntityType,ISet<INavigation>> _nonRequiredNavigations = new();
    private readonly Dictionary<IEntityType,ISet<INavigation>> _requiredNavigations = new();
    private readonly Dictionary<IEntityType, ISet<EqualityComparableSet<INavigation>>> _compositeKeys = new();
    private ICollection<IEntityType> _entityTypes;
    //
    public DatabaseInitializer(DbContext context, Dictionary<Type, Int32?> typeCounts, Int32 defaultCount = 100) {
        this._context = context;
        this._defaultCount = defaultCount;
        this._typeCounts = typeCounts;
        _entityTypes = context.Model.GetEntityTypes().ToArray();
    }
    public void initialize() {
        _context.Database.EnsureCreated();
        InitContainers();
        _entityTypes = Sort();
        CreateEntities();
        PopulateNonRequiredRelations();
    }

    private ICollection<IEntityType> Sort() {
        var stack = new Stack<IEntityType>(_entityTypes.Count());
        var visited = new HashSet<string>(_entityTypes.Count());
        foreach (var entityType in _entityTypes){
            if (visited.Contains(entityType.Name)) continue;
            SortRecursive(entityType);
        }
        return stack.Reverse().ToArray();
        void SortRecursive(IEntityType entityType) {
            visited.Add(entityType.Name);
            foreach (var navigation in _requiredNavigations[entityType]){
                SortRecursive(navigation.TargetEntityType);
            }
            stack.Push(entityType);
        }
    }
    
    private void CreateEntities() {
        foreach (var type in _entityTypes){
            while( !IsFull(type))
            {
                RandomizeAndSave(type, null, null);
            }
        }
    }
    private void PopulateNonRequiredRelations()
    {
        foreach (var kv in _saved)
        {
            var entityType = kv.Key;
            foreach (var entity in kv.Value)
            {
                WireNonRequiredRelation(entity, entityType);
            }
        }
    }
    private void WireNonRequiredRelation(object entity, IEntityType entityType)
    {
        foreach (var navigation in _nonRequiredNavigations[entityType]){
            var targetType = navigation.TargetEntityType;
            if (navigation.PropertyInfo.GetCustomAttribute<SelfReferencingProperty>()?.BreakCycle??false)
            {
                navigation.PropertyInfo.SetValue(entity, RetrieveSelfReferencing(targetType, entity, navigation.PropertyInfo));
            } else if (navigation.ForeignKey.IsUnique){
                navigation.PropertyInfo.SetValue(entity, RetrieveUnique(targetType, navigation));
            }
            else navigation.PropertyInfo.SetValue(entity, RetrieveRandom(targetType));
        }
        try{
            _context.Update(entity);
        }
        catch (Exception e){
            Console.WriteLine(e);
        }
    }

    private object? RetrieveKey(IEntityType entityType, EqualityComparableSet<INavigation> key, IEntityType targetType, ICollection<ISet<object>> prevTried, params object[] others) {
         return _saved[targetType].FirstOrDefault(t => {
            var s = new EqualityComparableSet<object>(others);
            s.Add(t);
            return !_compositeKeyStore[entityType][key].ContainsValue(s) &&
                   !prevTried.Any(s1=>s1.Contains(t));
        });
    }
    private object RandomizeAndSave(IEntityType enttiyType, EqualityComparableSet<INavigation>? kkey = null, INavigation? nav = null, object? from =null)
    {
        if (kkey==null) // do not retrieve composite Keys from here, they are retrieved with RetrieveKey.
        lock (_dictLock)
        {
            if ( IsFull(enttiyType))
            {
                if (from != null)
                {
                    return RetrieveSelfReferencing(enttiyType, from, nav!.PropertyInfo);
                } else if (nav?.ForeignKey.IsUnique ?? false)
                {
                    return RetrieveUnique(enttiyType, nav);
                }
                else return RetrieveRandom(enttiyType);
            }
        }
        var entity = RandomizeValueProperties(enttiyType);
        foreach (var keyNavs in _compositeKeys[enttiyType]){
            EqualityComparableSet<object> keyValues = new();
            ICollection<ISet<object>> tried = new List<ISet<object>>();
            var enumerator = keyNavs.GetEnumerator();
            enumerator.MoveNext();
            while(true){
                var targetType = enumerator.Current.TargetEntityType;
                //keyProperty is the id property.
                var navProperty = enumerator.Current;
                object? val;
                if (IsFull(targetType)){
                    val = RetrieveKey(enttiyType, keyNavs, targetType, tried, keyValues.ToArray());
                    if (val == null){
                        tried.Add(keyValues);
                        enumerator = keyNavs.GetEnumerator();
                        enumerator.MoveNext();
                        continue;
                    }
                }
                else val = RandomizeAndSave(targetType, keyNavs);
                keyValues.Add(val);
                navProperty.PropertyInfo.SetValue(entity, val);
                if (!enumerator.MoveNext()) break;
            }
            _compositeKeyStore[enttiyType][keyNavs][entity] = keyValues;
        }
        foreach (var navigation in _requiredNavigations[enttiyType])
        {
            from = null;
            if (navigation.PropertyInfo.GetCustomAttribute<SelfReferencingProperty>()?.BreakCycle??false)
            {
                from = entity;
            }
            navigation.PropertyInfo.SetValue(entity,RandomizeAndSave(navigation.TargetEntityType,null,navigation, from));
        }

        try{
            entity = _context.Add(entity).Entity;
            _context.SaveChanges();
        }
        catch (Exception e){
            Console.WriteLine(e);
        }
        lock (_dictLock)
        {
            _saved[enttiyType].Add(entity);
        }
        return entity;
    }
    private void InitContainers() {

        foreach (var entityType in _entityTypes){
            _requiredNavigations[entityType] = new HashSet<INavigation>();
            _nonRequiredNavigations[entityType] = new HashSet<INavigation>();
            _saved[entityType] = new HashSet<object>();
            _uniqueStore[entityType] = new Dictionary<INavigation, ISet<object>>();
            _compositeKeys[entityType] = new HashSet<EqualityComparableSet<INavigation>>();
            _compositeKeyStore[entityType] = new Dictionary<EqualityComparableSet<INavigation>, Dictionary<object, EqualityComparableSet<object>>>();
        }
        foreach (var entityType in _entityTypes){
            //this also gets the properties that reference to a single object. such as CartItem->ProductOffer.
            foreach (var key in entityType.GetKeys().Where(k=>k.Properties.Count > 1)){
                //don't allow composite keys that are subsets of other composite keys.
                //navigations associated with this composite key
                var navs = key.Properties.Select(p => p.GetContainingForeignKeys().ElementAt(0).GetNavigation(true))
                    .ToHashSet();
                if (_compositeKeys[entityType].Any(n=>navs.All(n.Contains)))
                    continue;                    
                var keyNavs = new EqualityComparableSet<INavigation>(navs);
                _compositeKeys[entityType].Add(keyNavs);
                _compositeKeyStore[entityType][keyNavs] = new Dictionary<object, EqualityComparableSet<object>>();
            }
            foreach (var navigation in entityType.GetNavigationsInHierarchy().Where(n=>n.IsOnDependent&&n.DeclaringEntityType.IsAssignableFrom(entityType) &&
                         !_compositeKeys[entityType].Any(n1=>n1.Contains(n)))){
                _uniqueStore[entityType][navigation] = new HashSet<object>();
                if (navigation.ForeignKey.IsRequired) 
                    _requiredNavigations[entityType].Add(navigation);
                else _nonRequiredNavigations[entityType].Add(navigation);
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

    private object RetrieveUnique(IEntityType targetType, INavigation nav) {
        lock (_dictLock){
            var s = (_saved.GetValueOrDefault(targetType) ?? (_saved[targetType] = new HashSet<object>())).First(s=>!(_uniqueStore[targetType].GetValueOrDefault(nav)??
                (_uniqueStore[targetType][nav] = new HashSet<object>())).Contains(s));
            _uniqueStore[targetType][nav].Add(s);
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

    public static object RandomizeValueProperties(IEntityType entityType) {
        var entity = entityType.ClrType.GetConstructor([])!.Invoke(null);
        foreach (var prop in entityType.GetPropertiesInHierarchy().Where(p=> !p.IsShadowProperty() && !p.IsPrimaryKey() && !p.IsForeignKey()&&
            (p.DeclaringType.Equals(entityType) || !entityType.IsAssignableFrom(p.DeclaringType))))
        {
            var property = prop.PropertyInfo!;
            object? value = null;
            var propType = property.PropertyType;
            property.SetValue(entity, RandomizeValue(propType, property, value, prop));
        }

        foreach (var complexProperty in entityType.GetComplexProperties()){
            var prop = complexProperty.PropertyInfo!;
            object? value = null;
            var propType = prop.PropertyType;
            prop.SetValue(entity, RandomizeValue(propType,prop, value, complexProperty));
        }
        return entity;
        object? RandomizeValue(Type propType, PropertyInfo property, object? value, IPropertyBase prop) {
            if ( propType== typeof(string)) {
                if (property.Name.Contains("username"))
                    value = new Bogus.DataSets.Internet().UserName();
                else if (property.Name.Contains("name")) 
                    value = new Bogus.Person().FirstName;
                else if (property.GetCustomAttribute<EmailAddressAttribute>() != null)
                    value = new Bogus.Person().Email;
                else{
                    int max =  (prop is IProperty ip?ip.GetMaxLength():(property.GetCustomAttribute<MaxLengthAttribute>()?.Length))??100;
                    int min = property.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;
                    value = new Bogus.Randomizer().Words(max / 5).Remove(new Bogus.Randomizer().Number(min,max));
                }
            } else if (property.GetCustomAttribute<ImageAttribute>()!=null){
                var bytes = FetchImage();
                if (typeof(string).IsAssignableFrom(property.PropertyType))
                    value = Convert.ToBase64String(bytes);
                else if(typeof(byte[]) .IsAssignableFrom(property.PropertyType)) value = bytes;
            }
            else if (property.GetCustomAttribute<PhoneAttribute>() != null || typeof(PhoneNumber).IsAssignableFrom(propType)) {
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
    public static string ImagesLocation = Path.Combine(AppContext.BaseDirectory, "Images");
    public static string[] ImageFiles = Directory.GetFiles(ImagesLocation, "*.jpg", SearchOption.TopDirectoryOnly);
    public static int _imageCount = ImageFiles.Length;
    public static readonly int _maxImageCount = 50;
    public static byte[] FetchImage() {
        if (_imageCount >=_maxImageCount){
            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Images", Random.Shared.Next(_imageCount ).ToString(), ".jpg"));
        }
        var res = new HttpClient().GetAsync("https://picsum.photos/200/300").Result;
        MemoryStream ms = new MemoryStream();
        res.Content.CopyTo(ms,null, CancellationToken.None);
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory,"Images", _imageCount + ".jpg"),ms.ToArray());
        _imageCount++;
        return ms.ToArray();
    }
    bool IsFull(IEntityType entityType1) {
        lock (_dictLock){
            return (_saved.GetValueOrDefault(entityType1) ?? (_saved[entityType1] = new HashSet<object>())).Count >=
                   (_typeCounts.GetValueOrDefault(entityType1.ClrType) ?? _defaultCount);
        }
    }
    private class EqualityComparableSet<T>: HashSet<T>, IEquatable<IEnumerable<T>>
    {
        public EqualityComparableSet(): base(){}
        public EqualityComparableSet(ICollection<T> collection) : base(collection) {
        }
        public bool Equals(IEnumerable<T>? other) {
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
}