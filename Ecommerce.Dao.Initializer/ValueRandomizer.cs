using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Bogus;
using Bogus.DataSets;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Address = Ecommerce.Entity.Common.Address;

namespace Ecommerce.Dao.Default.Initializer;

internal class ValueRandomizer
{
    private readonly ILog _logger;
    private readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new();
    private readonly string[] _preGeneratedNames = Enumerable.Range(0, 10000)
        .Select(_ => new Person().FirstName).ToArray();
    private Internet Internet = new();
    private Randomizer Randomizer => new();

    public object Create(IEntityType entityType) {
        var entity = CreateInstance(entityType.ClrType);
        foreach (var prop in entityType.GetProperties().Where(p=> !p.IsShadowProperty()  && !p.IsForeignKey()&&
                                                                  !p.GetViewColumnMappings().Any()&&
            (p.DeclaringType.Equals(entityType) || !entityType.IsAssignableFrom(p.DeclaringType))) )
        {
            var property = prop.PropertyInfo!;
            if (prop.ValueGenerated == ValueGenerated.OnAdd ||
                prop.ValueGenerated == ValueGenerated.OnAddOrUpdate){
                continue;
            }
            var propType = property.PropertyType;
            PropertyCache.SetProperty(entity, property,RandomizeValue(propType, property, prop) );
        }
        foreach (var complexProperty in entityType.GetComplexProperties()){
            var val = RandomizeComplex(complexProperty.ComplexType);
            PropertyCache.SetProperty(entity, complexProperty.PropertyInfo, val);
        }

        foreach (var listProp in entityType.GetProperties().Where(p=> p.ClrType.GetInterfaces().Any(t=>t.IsGenericType&&t.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                     (!t.GetGenericArguments()[0].IsGenericType || t.GetGenericArguments()[0].GetGenericTypeDefinition() != typeof(KeyValuePair<,>))))){
            var type = listProp.ClrType.GenericTypeArguments[0];
            var val = CreateCollectionInstance(listProp.ClrType);
            var c = new Random().Next(1,5);
            for (int i = 0; i <c;  i++){
                val.GetType().GetRuntimeMethod("Add", [type])
                    .Invoke(val, [RandomizeValue(type, listProp.PropertyInfo, listProp)]);
            }
            PropertyCache.SetProperty(entity, listProp.PropertyInfo!, val);
        }
        return entity;

    }

    private object RandomizeComplex(IComplexType type) {
        var propType = type.ClrType;
        object value;
        if ( typeof(PhoneNumber).IsAssignableFrom(propType)) {
            if (typeof(PhoneNumber).IsAssignableFrom(propType)) {
                value = new PhoneNumber{CountryCode = Randomizer.Number(999),Number = new Bogus.Faker().Phone.PhoneNumber()};    
            } else value = new Faker().Phone.PhoneNumber();
        } else if(typeof(Address).IsAssignableFrom(propType)){
            value = new Address{
                City = new Faker().Address.City(), District = new Faker().Address.County(),
                Country = new Faker().Address.State(), Line1 = new Faker().Address.StreetName(),
                ZipCode = new Faker().Address.ZipCode()
            };
        }
        else{
            value = Activator.CreateInstance(type.ClrType);
            foreach (var p in type.GetProperties()){
                var val = RandomizeValue(p.ClrType, p.PropertyInfo, p);
                p.PropertyInfo.SetValue(value, val);
            }
        }

        return value;
    }
    private object? RandomizeValue(Type propType, PropertyInfo property, IPropertyBase prop) {
        object? value= null;
            if (property.GetCustomAttribute<ImageAttribute>()!=null){
                var bytes = FetchImage();
                if (typeof(string).IsAssignableFrom(propType))
                    value = Convert.ToBase64String(bytes);
                else if(typeof(byte[]) .IsAssignableFrom(propType)) value = bytes;
            }
            else if ( typeof(PhoneNumber).IsAssignableFrom(propType)) {
                if (typeof(PhoneNumber).IsAssignableFrom(propType)) {
                    value = new PhoneNumber{CountryCode = Randomizer.Number(999),Number = new Bogus.Faker().Phone.PhoneNumber()};    
                } else value = new Faker().Phone.PhoneNumber();
            } else if(typeof(Address).IsAssignableFrom(propType)){
                value = new Address{
                    City = new Faker().Address.City(), District = new Faker().Address.County(),
                    Country = new Faker().Address.Country(), Line1 = new Faker().Address.StreetAddress(),
                    ZipCode = new Faker().Address.ZipCode(), Line2 = new Faker().Address.SecondaryAddress()
                };
            }
            else if ( propType== typeof(string)) {
                if (property.Name.Contains("Username", StringComparison.OrdinalIgnoreCase))
                    value = Internet.UserName();
                else if (property.Name.Contains("Name")) 
                    value = _preGeneratedNames[Random.Shared.Next(_preGeneratedNames.Length)];
                else if (property.Name.Contains("Email") ||
                    property.GetCustomAttribute<EmailAddressAttribute>() != null){
                    value = Internet.Email();
                    if (((IProperty)prop).IsKey() ){
                        var s = ((string)value).Split('@');
                        value = $"{s[0]}{Randomizer.Number(0, 100000000)}@{s[1]}";
                    }
                } else if (property.GetCustomAttribute<PhoneAttribute>() != null){
                    value = new Faker().Phone.PhoneNumber();
                }
                else{
                    int max =  (prop is IProperty ip?ip.GetMaxLength():(property.GetCustomAttribute<MaxLengthAttribute>()?.Length))??100;
                    int min = property.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;
                    if(prop is IProperty ip1&& ip1.IsKey())
                        value = Randomizer.String2(max, max);
                    else{
                        value = Randomizer.Words(max / 5);
                        value = ((string)value).Remove(Randomizer.Number(min,
                            Math.Min(max, ((string)value).Length)));
                    }
                }
            }
            else if (propType.IsEnum){
                var enums= Enum.GetValues(propType);
                value = enums.GetValue(Randomizer.Number(enums.Length-1)) ?? enums.GetValue(0)!;
            } else if (typeof(DateTime).IsAssignableFrom(propType)){
                if (propType.GetCustomAttribute<BirthDate>() != null)
                    value = new Faker().Date.Past(50);
                else value = new Faker().Date.Recent(60);
            } 
            else{
                var positiveAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(Annotations.Validation_Positive));
                var maxAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(Annotations.Validation_MaxValue));
                var minAnnotation =
                    ((IProperty)prop).FindAnnotation(nameof(Annotations.Validation_MinValue));
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
    private object CreateInstance(Type type) {
        return _constructorCache.GetOrAdd(type, t =>
            Expression.Lambda<Func<object>>(Expression.New(t)).Compile())();
    }

    private object CreateCollectionInstance(Type type) {
        type = GetDefaultImplementationType(type);
        return CreateInstance(type);
    }

    public static Type GetDefaultImplementationType(Type interfaceType) {
        if (interfaceType == null)
            throw new ArgumentNullException(nameof(interfaceType));

        // Process generic interfaces first
        if (interfaceType.IsGenericType){
            var genericDef = interfaceType.GetGenericTypeDefinition();
            Type[] typeArgs = interfaceType.GetGenericArguments();

            if (genericDef == typeof(IEnumerable<>)
                || genericDef == typeof(ICollection<>)
                || genericDef == typeof(IList<>)
                || genericDef == typeof(IReadOnlyCollection<>)
                || genericDef == typeof(IReadOnlyList<>)){
                // Use List<T> as the default implementation.
                return typeof(List<>).MakeGenericType(typeArgs);
            }

            if (genericDef == typeof(ISet<>)){
                // Use HashSet<T> as the default set.
                return typeof(HashSet<>).MakeGenericType(typeArgs);
            }

            if (genericDef == typeof(IDictionary<,>)
                || genericDef == typeof(IReadOnlyDictionary<,>)){
                // Use Dictionary<TKey,TValue> as the default implementation.
                return typeof(Dictionary<,>).MakeGenericType(typeArgs);
            }

            if (genericDef == typeof(IProducerConsumerCollection<>)){
                // Use ConcurrentQueue<T> as a common producer-consumer collection.
                return typeof(ConcurrentQueue<>).MakeGenericType(typeArgs);
            }

            // You can add other mappings as needed.
            throw new NotSupportedException($"The generic interface type '{interfaceType}' is not supported.");
        }
        else{
            // Non-generic interfaces.
            if (interfaceType == typeof(IEnumerable)
                || interfaceType == typeof(ICollection)
                || interfaceType == typeof(IList)){
                // Use ArrayList as the non-generic default.
                return typeof(ArrayList);
            }
            else if (interfaceType == typeof(IDictionary)){
                // Use Hashtable as the default.
                return typeof(Hashtable);
            }

            // Add other non-generic mappings if needed.
            throw new NotSupportedException($"The non-generic interface type '{interfaceType}' is not supported.");
        }
    }
    private static DirectoryInfo ImagesLocation = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Images"));
    private static FileInfo[] ImageFiles = ImagesLocation.GetFiles( "*.jpg", SearchOption.TopDirectoryOnly);
    private static int _imageCount = ImageFiles.Length;
    private const int MaxImageCount = 50;

    private byte[] FetchImage() {
        if (_imageCount >=MaxImageCount){
            return File.ReadAllBytes(Path.Combine(ImagesLocation.FullName, Random.Shared.Next(_imageCount ) + ".jpg"));
        }
        var res = new HttpClient().GetAsync("https://picsum.photos/300/200").Result;
        MemoryStream ms = new MemoryStream();
        res.Content.CopyTo(ms,null, CancellationToken.None);
        File.WriteAllBytes(Path.Combine(ImagesLocation.FullName, _imageCount + ".jpg"),ms.ToArray());
        _imageCount++;
        return ms.ToArray();
    }

}