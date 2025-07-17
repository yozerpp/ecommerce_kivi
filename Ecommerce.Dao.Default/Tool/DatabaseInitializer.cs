using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Bogus;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Address = Ecommerce.Entity.Common.Address;
using Enum = System.Enum;

namespace Ecommerce.Dao.Default.Tool;

public class DatabaseInitializer<TC> : IDisposable where TC: DbContext, new()
{
    private readonly DbContext _defaultContext;
    private readonly Dictionary<IEntityType, Lock> _dictLocks = new();
    private readonly Dictionary<IEntityType, ISet<object>> _saved = new();
    private readonly Dictionary<IEntityType, Dictionary<INavigation, ISet<object> >> _uniqueStore = new();
    private readonly Dictionary<Type, Int32?> _typeCounts;
    private readonly int _defaultCount;
    private readonly Dictionary<IEntityType,ISet<INavigation>> _nonRequiredNavigations = new();
    private readonly Dictionary<IEntityType,ISet<INavigation>> _requiredNavigations = new();
    private readonly Dictionary<IEntityType, ISet<EqualityComparableSet<INavigation>>> _compositeKeys = new();
    private readonly ICollection<IEntityType> _entityTypes;
    private readonly CompositeRandomizer _compositeRandomizer;
    private readonly ICollection<(TC,ICollection<IEntityType>)> _lanes;
    //
    public DatabaseInitializer(DbContextOptions<TC> options, Dictionary<Type, int?> typeCounts, int defaultCount = 100) {
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _defaultContext = (TC) typeof(TC).GetConstructor([typeof(DbContextOptions<TC>)]).Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().ToArray();
        InitContainers();
        _compositeRandomizer = new CompositeRandomizer(_saved, _dictLocks);
        _lanes = Sort(options);
    }
    public void initialize() {
        CreateEntities();
        PopulateNonRequiredRelations();
    }
    private ICollection<(TC, ICollection<IEntityType>)> Sort(DbContextOptions<TC> options) {
        var visited = new HashSet<string>(_entityTypes.Count());
        var lanes = new List<Stack<IEntityType>>();
        foreach (var entityType in _entityTypes){
            if (visited.Contains(entityType.Name)) continue;
            var lane = new Stack<IEntityType>();
            SortRecursive(entityType, lane);
            lanes.Add(lane);
        }
        return lanes.Select(l=>((TC)typeof(TC).GetConstructor([typeof(DbContextOptions<TC>)]).Invoke([options]),(ICollection<IEntityType>)l.Reverse().ToArray() )).ToArray();
        void SortRecursive(IEntityType entityType, Stack<IEntityType> lane) {
            var keys = _compositeRandomizer.GetNavigations(entityType);
            keys.AddRange(RequiredNavsInHierarchy(entityType));
            visited.Add(entityType.Name);
            foreach (var navigation in keys){
                SortRecursive(navigation.TargetEntityType, lane);
            }
            lane.Push(entityType);
        }
    }

    private readonly ThreadLocal<TC> _contextThreadLocal = new();
    private void CreateEntities() {
        _lanes.AsParallel().ForAll(l => {
            _contextThreadLocal.Value = l.Item1;
            foreach (var entityType in l.Item2){
                while (!IsFull(entityType)){
                    _dictLocks[entityType].Enter();
                    try{
                        RandomizeAndSave(entityType);
                    }
                    catch (Exception e){
                        Debug.WriteLine(e);
                    }
                    finally{
                        _dictLocks[entityType].Exit();
                    }
                }
            }
        });
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
            _defaultContext.Update(entity);
        }
        catch (Exception e){
            Debug.WriteLine(e);
        }
    }

    private object RandomizeAndSave(IEntityType enttiyType)
    {
        var entity = CreateAndPopulatePrimitives(enttiyType);
        foreach (var compositeKey in _compositeRandomizer.GetCompositeKeys(enttiyType)){
            AssignForeignKeys(compositeKey.Item1, entity, compositeKey.Item2);
        }
        var navsInHierarchy = RequiredNavsInHierarchy(enttiyType);
        foreach (var navigation in navsInHierarchy)
        {
            _dictLocks[navigation.TargetEntityType].Enter();
            try{
                object val;
                if ((val = GetSavedIfFull(navigation.TargetEntityType, navigation, 
                        navigation.PropertyInfo.GetCustomAttribute<SelfReferencingProperty>()?.BreakCycle??false?entity:null
                        )) == null)
                    val = RandomizeAndSave(navigation.TargetEntityType);
                AssignForeignKeys(navigation.ForeignKey, entity, val);
                _uniqueStore[navigation.TargetEntityType][navigation].Add(val);
            }
            finally{
                _dictLocks[navigation.TargetEntityType].Exit();
            }
        }
        var context = _contextThreadLocal.Value!;
        entity = context.Add(entity).Entity;
        context.SaveChanges();
        _saved[enttiyType].Add(entity);
        return entity;
    }

    private object GetSavedIfFull(IEntityType enttiyType, INavigation? nav, object? from) {
        if ( IsFull(enttiyType)){
            object ret;
            if (from != null) ret = RetrieveSelfReferencing(enttiyType, from, nav!.PropertyInfo);
            if (nav?.ForeignKey.IsUnique ?? false) ret = RetrieveUnique(enttiyType, nav);
            else ret= RetrieveRandom(enttiyType);
            return ret;
        }

        return null;
    }

    private void AssignForeignKeys(IForeignKey fk, object dependent, object principal) {
        var propsInPrincipal = fk.PrincipalKey.Properties;
        var propsIndependent = fk.Properties;
        for (int i = 0; i < propsIndependent.Count; i++){
            propsIndependent[i].PropertyInfo.SetValue(dependent, propsInPrincipal[i].PropertyInfo.GetValue(principal));
        }
    }
    private ISet<INavigation> RequiredNavsInHierarchy(IEntityType enttiyType) {
        ISet<INavigation> navsInHierarchy = new HashSet<INavigation>();
        IEntityType? tp=enttiyType;
        while (tp!=null){
            foreach (var nav1 in _requiredNavigations[tp]){
                navsInHierarchy.Add(nav1);
                
            }

            tp = tp.BaseType;
        }

        return navsInHierarchy;
    }

    private void InitContainers() {
        
        foreach (var entityType in _entityTypes){
            _requiredNavigations[entityType] = new HashSet<INavigation>();
            _nonRequiredNavigations[entityType] = new HashSet<INavigation>();
            _saved[entityType] = new HashSet<object>();
            _uniqueStore[entityType] = new Dictionary<INavigation, ISet<object>>();
            _compositeKeys[entityType] = new HashSet<EqualityComparableSet<INavigation>>();
            _dictLocks[entityType] = new Lock();
        }
        foreach (var entityType in _entityTypes){
            //this also gets the properties that reference to a single object. such as CartItem->ProductOffer.
            foreach (var key in entityType.GetKeys().Where(k=>k.Properties.Count > 1)){
                //don't allow composite keys that are subsets of other composite keys.
                //navigations associated with this composite key
                var navs = key.Properties.Select(p => p.GetContainingForeignKeys().ElementAt(0).GetNavigation(true))
                    .Where(n=>n!=null) .ToHashSet();
                if (_compositeKeys[entityType].Any(n=>navs.All(n.Contains)))
                    continue;                    
                var keyNavs = new EqualityComparableSet<INavigation>(navs);
                _compositeKeys[entityType].Add(keyNavs);
            }
            foreach (var navigation in entityType.GetNavigations().Where(n=>n.IsOnDependent&&n.DeclaringEntityType.IsAssignableFrom(entityType) &&
                         !_compositeKeys[entityType].Any(n1=>n1.Contains(n)))){
                _uniqueStore[navigation.TargetEntityType][navigation] = new HashSet<object>();
                if (navigation.ForeignKey.IsRequired) 
                    _requiredNavigations[entityType].Add(navigation);
                else _nonRequiredNavigations[entityType].Add(navigation);
            }
        }
    }

    private object RetrieveRandom(IEntityType type) {
        var s = _saved[type];
        var ret = s.ElementAt(new Randomizer().Number(s.Count - 1));
        return ret;
    }
    //don't allow circular reference.
    private object? RetrieveSelfReferencing(IEntityType type, object current, PropertyInfo property) {
        var s = _saved[type].Where(o =>
                !property.GetValue(o)?.Equals(current) ?? false);
            var ret = s.ElementAtOrDefault(new Randomizer().Number(s.Count() - 1));
        return ret;
    }

    private object RetrieveUnique(IEntityType targetType, INavigation nav) { 
        var s = _saved[targetType]
            .First(s=>!(_uniqueStore[targetType].GetValueOrDefault(nav)?.Contains(s) ?? false));
        return s;
    }

    private static object CreateAndPopulatePrimitives(IEntityType entityType) {
        var entity = entityType.ClrType.GetConstructor([])!.Invoke(null);
        foreach (var prop in entityType.GetProperties().Where(p=> !p.IsShadowProperty()  && !p.IsForeignKey()&&
            (p.DeclaringType.Equals(entityType) || !entityType.IsAssignableFrom(p.DeclaringType))) )
        {
            var property = prop.PropertyInfo!;
            if (prop.ValueGenerated == ValueGenerated.OnAdd ||
                prop.ValueGenerated == ValueGenerated.OnAddOrUpdate){
                continue;
            }
            object? value = null;
            var propType = property.PropertyType;
            property.SetValue(entity, RandomizeValue(propType, property, value, prop));
        }

        foreach (var complexProperty in entityType.GetComplexProperties()){
            var prop = complexProperty.PropertyInfo!;
            var propType = prop.PropertyType;
            object? value = null;
            prop.SetValue(entity, RandomizeValue(propType,prop, value, complexProperty));
        }
        return entity;
        object? RandomizeValue(Type propType, PropertyInfo property, object? value, IPropertyBase prop) {
            if (property.GetCustomAttribute<ImageAttribute>()!=null){
                var bytes = FetchImage();
                if (typeof(string).IsAssignableFrom(property.PropertyType))
                    value = Convert.ToBase64String(bytes);
                else if(typeof(byte[]) .IsAssignableFrom(property.PropertyType)) value = bytes;
            }
            else if ( propType== typeof(string)) {
                if (property.Name.Contains("Username", StringComparison.OrdinalIgnoreCase))
                    value = new Bogus.DataSets.Internet().UserName();
                else if (property.Name.Contains("Name")) 
                    value = new Person().FirstName;
                else if (property.Name.Contains("Email")||property.GetCustomAttribute<EmailAddressAttribute>() != null)
                    value = new Person().Email;
                else{
                    int max =  (prop is IProperty ip?ip.GetMaxLength():(property.GetCustomAttribute<MaxLengthAttribute>()?.Length))??100;
                    int min = property.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;
                    if(prop is IProperty ip1&& ip1.IsKey())
                        value = new Randomizer().String2(max, max);
                    else{
                        value = new Randomizer().Words(max / 5);
                        value = ((string)value).Remove(new Randomizer().Number(min,
                            Math.Min(max, ((string)value).Length)));
                    }
                }
            }
            else if (property.GetCustomAttribute<PhoneAttribute>() != null || typeof(PhoneNumber).IsAssignableFrom(propType)) {
                if (typeof(PhoneNumber).IsAssignableFrom(propType)) {
                    value = new PhoneNumber{CountryCode = new Bogus.Randomizer().Number(999),Number = new Bogus.Faker().Phone.PhoneNumber()};    
                } else value = new Faker().Phone.PhoneNumber();
            } else if(typeof(Address).IsAssignableFrom(propType)){
                value = new Address{
                    City = new Faker().Address.City(), Neighborhood = new Faker().Address.County(),
                    State = new Faker().Address.State(), Street = new Faker().Address.StreetName(),
                    ZipCode = new Faker().Address.ZipCode()
                };
            } else if (propType.IsEnum){
                var enums= Enum.GetValues(propType);
                value = enums.GetValue(new Randomizer().Number(enums.Length-1)) ?? enums.GetValue(0)!;
            } else if (typeof(DateTime).IsAssignableFrom(propType)){
                if (propType.GetCustomAttribute<BirthDate>() != null)
                    value = new Faker().Date.Past(50);
                else value = new Faker().Date.Recent(60);
            } 
            else{
                var positiveAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(DefaultDbContext.Annotations.Validation_Positive));
                var maxAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(DefaultDbContext.Annotations.Validation_MaxValue));
                var minAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(DefaultDbContext.Annotations.Validation_MinValue));
                if (maxAnnotation == null || maxAnnotation.Value is not int intMax)
                    intMax = positiveAnnotation?.Value is false ? 0 : Int16.MaxValue;
                var pres = ((IProperty)prop).GetPrecision();
                var scale = ((IProperty)prop).GetScale();
                if (maxAnnotation == null ||  maxAnnotation.Value is not decimal decimalMax){
                    if (pres != null && scale != null && pres >= scale)
                        decimalMax = (decimal)Math.Pow(10, (int)(pres - scale)) -
                                     (decimal)Math.Pow(10, (int)(pres - scale - 1));
                    else decimalMax = 9999999999999999.99m; //SQL Server creates decimal(18,2) clumns by default.
                }
                if(minAnnotation == null ||minAnnotation.Value is not int intMin)
                    intMin = Type.GetTypeCode(propType) is TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 || positiveAnnotation?.Value is true ? 0 : Int16.MinValue;
                if(minAnnotation == null || minAnnotation.Value is not decimal decimalMin)
                    decimalMin = intMin==0?0:(-decimalMax + 1000);
                value = Type.GetTypeCode(propType) switch{
                    TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 => new Randomizer().Number(
                        intMin as int? ?? Int16.MinValue, intMax as int? ?? Int16.MaxValue),
                    TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => new Randomizer().UInt((ushort)intMin,
                        intMax as ushort? ?? UInt16.MaxValue),
                    TypeCode.Double => new Randomizer().Double((double)decimalMin, (double)decimalMax),
                    TypeCode.Boolean => new Randomizer().Bool(),
                    TypeCode.Single => new Randomizer().Float((float)decimalMin, (float)decimalMax),
                    TypeCode.Decimal => Decimal.Round(new Randomizer().Decimal(decimalMin, decimalMax), scale ?? 2),
                    TypeCode.Char => new Randomizer().Char(),
                    TypeCode.Byte => new Randomizer().Byte(),
                    TypeCode.SByte => new Randomizer().SByte(),
                    _ => null
                };
            }
            return value;
        }
    }

    private static DirectoryInfo ImagesLocation = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Images"));
    private static FileInfo[] ImageFiles = ImagesLocation.GetFiles( "*.jpg", SearchOption.TopDirectoryOnly);
    private static int _imageCount = ImageFiles.Length;
    private static readonly int _maxImageCount = 50;

    private static byte[] FetchImage() {
        if (_imageCount >=_maxImageCount){
            return File.ReadAllBytes(Path.Combine(ImagesLocation.FullName, Random.Shared.Next(_imageCount ) + ".jpg"));
        }
        var res = new HttpClient().GetAsync("https://picsum.photos/200/300").Result;
        MemoryStream ms = new MemoryStream();
        res.Content.CopyTo(ms,null, CancellationToken.None);
        File.WriteAllBytes(Path.Combine(ImagesLocation.FullName, _imageCount + ".jpg"),ms.ToArray());
        _imageCount++;
        return ms.ToArray();
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

public class EqualityComparableSet<T>: HashSet<T>, IEquatable<IEnumerable<T>>
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