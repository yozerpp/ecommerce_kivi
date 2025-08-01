using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Bogus;
using Bogus.DataSets;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Common.Meta;
using log4net;
using Microsoft.EntityFrameworkCore.Metadata;
using Address = Ecommerce.Entity.Common.Address;

namespace Ecommerce.Dao.Default.Tool;

internal class ValueRandomizer
{
    private readonly ILog _logger;
    private readonly SetterCache _setterCache;
    private readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new();
    private readonly string[] _preGeneratedNames = Enumerable.Range(0, 10000)
        .Select(_ => new Person().FirstName).ToArray();
    private Internet Internet = new();
    private Randomizer Randomizer = new();
    public ValueRandomizer(SetterCache setterCache) {
        _setterCache = setterCache;
    }
    public object Create(IEntityType entityType) {
        var entity = CreateInstance(entityType.ClrType);
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
            _setterCache.SetProperty(entity, property,RandomizeValue(propType, property, value, prop) );
        }
        foreach (var complexProperty in entityType.GetComplexProperties()){
            var prop = complexProperty.PropertyInfo!;
            var propType = prop.PropertyType;
            _setterCache.SetProperty(entity,prop,  RandomizeValue(propType,prop, null, complexProperty));
        }

        foreach (var listProp in entityType.GetProperties().Where(p=> p.ClrType.GetInterfaces().Any(t=>t.IsGenericType&&t.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                     (!t.GetGenericArguments()[0].IsGenericType || t.GetGenericArguments()[0].GetGenericTypeDefinition() != typeof(KeyValuePair<,>))))){
            var type = listProp.ClrType.GenericTypeArguments[0];
            var val = CreateInstance(listProp.ClrType);
            var c = new Random().Next(5);
            for (int i = 0; i <c;  i++){
                listProp.ClrType.GetMethod("Add", [type])
                    .Invoke(val, [RandomizeValue(type, listProp.PropertyInfo, null, listProp)]);
            }
        }
        return entity;

    } 
    private object? RandomizeValue(Type propType, PropertyInfo property, object? value, IPropertyBase prop) {
            if (property.GetCustomAttribute<ImageAttribute>()!=null){
                var bytes = FetchImage();
                if (typeof(string).IsAssignableFrom(property.PropertyType))
                    value = Convert.ToBase64String(bytes);
                else if(typeof(byte[]) .IsAssignableFrom(property.PropertyType)) value = bytes;
            }
            else if ( propType== typeof(string)) {
                if (property.Name.Contains("Username", StringComparison.OrdinalIgnoreCase))
                    value = Internet.UserName();
                else if (property.Name.Contains("Name")) 
                    value = _preGeneratedNames[Random.Shared.Next(_preGeneratedNames.Length)];
                else if (property.Name.Contains("Email")||property.GetCustomAttribute<EmailAddressAttribute>() != null)
                    value = Internet.Email();
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
            else if (property.GetCustomAttribute<PhoneAttribute>() != null || typeof(PhoneNumber).IsAssignableFrom(propType)) {
                if (typeof(PhoneNumber).IsAssignableFrom(propType)) {
                    value = new PhoneNumber{CountryCode = Randomizer.Number(999),Number = new Bogus.Faker().Phone.PhoneNumber()};    
                } else value = new Faker().Phone.PhoneNumber();
            } else if(typeof(Address).IsAssignableFrom(propType)){
                value = new {
                    City = new Faker().Address.City(), Neighborhood = new Faker().Address.County(),
                    State = new Faker().Address.State(), Street = new Faker().Address.StreetName(),
                    ZipCode = new Faker().Address.ZipCode()
                };
            } else if (propType.IsEnum){
                var enums= Enum.GetValues(propType);
                value = enums.GetValue(Randomizer.Number(enums.Length-1)) ?? enums.GetValue(0)!;
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
                    TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 => Randomizer.Number(
                        intMin as int? ?? Int16.MinValue, intMax as int? ?? Int16.MaxValue),
                    TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => Randomizer.UInt((ushort)intMin,
                        intMax as ushort? ?? UInt16.MaxValue),
                    TypeCode.Double => Randomizer.Double((double)decimalMin, (double)decimalMax),
                    TypeCode.Boolean => Randomizer.Bool(),
                    TypeCode.Single => Randomizer.Float((float)decimalMin, (float)decimalMax),
                    TypeCode.Decimal => Decimal.Round(Randomizer.Decimal(decimalMin, decimalMax), scale ?? 2),
                    TypeCode.Char => Randomizer.Char(),
                    TypeCode.Byte => Randomizer.Byte(),
                    TypeCode.SByte => Randomizer.SByte(),
                    _ => null
                };
            }
            return value;
        }
    
    private object CreateInstance(Type type) {
        return _constructorCache.GetOrAdd(type, t =>
            Expression.Lambda<Func<object>>(Expression.New(t)).Compile())();
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