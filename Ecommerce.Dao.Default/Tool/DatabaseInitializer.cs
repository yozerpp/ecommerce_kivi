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
    private readonly Dictionary<IEntityType, Lock> _dictLocks;
    private readonly Dictionary<IEntityType, ISet<object>> _saved;
    private readonly Dictionary<Type, Int32?> _typeCounts;
    private readonly int _defaultCount;
    private readonly ICollection<IEntityType> _entityTypes;
    private readonly RelationRandomizer _relationRandomizer;
    private readonly ICollection<(TC,ICollection<IEntityType>)> _lanes;
    //
    public DatabaseInitializer(DbContextOptions<TC> options, Dictionary<Type, int?> typeCounts, int defaultCount = 100) {
        _defaultCount = defaultCount;
        _typeCounts = typeCounts;
        _defaultContext = (TC) typeof(TC).GetConstructor([typeof(DbContextOptions<TC>)]).Invoke([options]);
        _entityTypes=_defaultContext.Model.GetEntityTypes().ToArray();
        _saved = _entityTypes.ToDictionary(e=>e, e=>(ISet<object>)new HashSet<object>());
        _dictLocks = _entityTypes.ToDictionary(e=>e, e=>new Lock());
        _relationRandomizer = new RelationRandomizer(_saved, _dictLocks);
        _lanes = Sort(options);
    }
    public void initialize() {
        CreateEntities();
        Console.WriteLine("-----Finished Creating Entities. Wiring Non-Required Relations...-----");
        PopulateNonRequiredRelations();
    }
    private ICollection<(TC, ICollection<IEntityType>)> Sort(DbContextOptions<TC> options) {
        var visited = new HashSet<string>(_entityTypes.Count);
        var lanes = new List<Stack<IEntityType>>();
        foreach (var entityType in _entityTypes){
            if (visited.Contains(entityType.Name)) continue;
            var lane = new Stack<IEntityType>();
            SortRecursive(entityType, lane);
            lanes.Add(lane);
        }
        return lanes.Select(l=>((TC)typeof(TC).GetConstructor([typeof(DbContextOptions<TC>)]).Invoke([options]),(ICollection<IEntityType>)l.Reverse().ToArray() )).ToArray();
        void SortRecursive(IEntityType entityType, Stack<IEntityType> lane) {
            visited.Add(entityType.Name);
            foreach (var navigation in entityType.GetNavigations().Where(n =>n.IsOnDependent&& n.ForeignKey.IsRequired).ToHashSet()){
                SortRecursive(navigation.TargetEntityType, lane);
            }
            lane.Push(entityType);
        }
    }

    private readonly ThreadLocal<TC> _contextThreadLocal = new();
    private void CreateEntities() {
        foreach(var l in _lanes){
            _contextThreadLocal.Value = l.Item1;
            foreach (var entityType in l.Item2){
                while (!IsFull(entityType)){
                    _dictLocks[entityType].Enter();
                    try{
                        RandomizeAndSave(entityType);
                    }
                    finally{
                        _dictLocks[entityType].Exit();
                    }
                }
            }
        }
        _defaultContext.SaveChanges();
    }
    private void PopulateNonRequiredRelations()
    {
        foreach (var (entityType, set) in _saved)
        {
            foreach (var entity in set)
            {
                foreach (var fk in entityType.GetForeignKeys().Where(fk=>!fk.IsRequired)){
                    AssignForeignKeys(entity, _relationRandomizer.GetForeignKeyValue(fk, entity));
                }
                _defaultContext.Update(entity);
            }
        }
        _defaultContext.SaveChanges();
    }

    private void RandomizeAndSave(IEntityType enttiyType)
    {
        var entity = CreateAndPopulatePrimitives(enttiyType);
        foreach (var key in enttiyType.GetKeys().Where(k=>k.Properties.Count > 1)){
            AssignForeignKeys(entity , _relationRandomizer.GetKeyValues(key));
        }
        foreach (var foreignKey in enttiyType.GetForeignKeys().Where(fk=>fk.IsRequired&&!fk.Properties.All(p=>p.IsKey()))){ //TODO no action if foreign key is both self-referencing and required.
            AssignForeignKeys(entity,_relationRandomizer.GetForeignKeyValue(foreignKey));
        }
        entity = _defaultContext.Add(entity).Entity;
        _saved[enttiyType].Add(entity);
    }


    private void AssignForeignKeys( object dependent, params IEnumerable<(INavigation, object)> keyValues) {
        foreach (var (nav, principal) in keyValues){
            nav.PropertyInfo.SetValue(dependent, principal);
        }
    }


    //don't allow circular reference.
    private object? RetrieveSelfReferencing(IEntityType type, object current, PropertyInfo property) {
        var s = _saved[type].Where(o =>
                !property.GetValue(o)?.Equals(current) ?? false);
            var ret = s.ElementAtOrDefault(new Randomizer().Number(s.Count() - 1));
        return ret;
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