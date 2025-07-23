using System.Reflection;
using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.WebImpl.Pages.Shared;

public class EntityMapper
{
    private readonly string[] _includeProperties;
    private readonly string[] _excludeProperties;
    private readonly Type? _type;
    private readonly IModel _model;
    private readonly Localizer _localization;
    public class Factory
    {
        private readonly IModel _model;
        private readonly Localizer _localization;
        public Factory(IModel model, Localizer loc) {
            _localization = loc;
            _model = model;
        }
        public EntityMapper Create(Type? type = null, string[]? includeProperties = null, string[]? excludeProperties = null) {
            return new EntityMapper(_localization,_model, type, includeProperties, excludeProperties);
        }
    }
    private EntityMapper(Localizer localization,IModel model, Type? type = null, string[]? includeProperties = null,
        string[]? excludeProperties = null) {
        _localization = localization;
        _type = type;
        _model = model;
        _includeProperties = includeProperties ??[];
        _excludeProperties = excludeProperties ??[];
    }
    public static object ParseProp(Type tp, string val) {
        var pars = tp.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, [typeof(string)]);
        if (pars!=null )   
            return pars.Invoke(null, [val]);
        if (tp.IsEnum)
            return Enum.Parse(tp, val);
        if (tp.IsArray){
            var elems = val.Split(',');
            var t = tp.GetElementType();
            var a = Array.CreateInstance(t, elems.Length);
            for (int i = 0; i < elems.Length; i++){
                a.SetValue(ParseProp(t, elems[i]), i);
            }

            return a;
        }
        if (typeof(string).IsAssignableFrom(tp)) return val;
        throw new ArgumentException("Type deserialization not supported: " + tp.FullName);
    }
    private static readonly string _btn = "<button type='submit' class='btn btn-primary'>Kaydol</button>\n";
    public HtmlString GetFormInputs(string proprety,Type? type=null, string[]? includeProperties = null, string[]? excludeProperties = null, bool root = true) {
        includeProperties ??= _includeProperties;
        excludeProperties ??= _excludeProperties;
        type ??= _type ?? throw new ArgumentNullException("type or _type must be provided");
        var t = GetEntityType(type);
        StringBuilder html = new StringBuilder();
        html.Append("<div>\n");
        foreach (var propertyInfo in GetMappedProperties(includeProperties, excludeProperties, type.GetProperties(), t) ){
            var displayName = _localization.GetLocalization(type, propertyInfo.Name);
            displayName = displayName == "PasswordHash" ? "Şifre" : displayName;
            html.Append("\t<div class='input-group'>\n")
                .Append($"\t\t<label @Html.DisplayName({displayName})>{displayName}</label></label>\n").Append("<span>\t</span>")
                .Append($"\t\t<input type='text' name='{proprety}.{propertyInfo.Name}' class='form-control'/>")
                .Append("\t</div>\n");
        }
        if (t == null) return new HtmlString(html.Append("</div>").ToString());
        foreach (var complexProperty in t.GetComplexProperties()){
            html.Append(GetFormInputs($"{proprety}.{complexProperty.Name}",
                complexProperty.ComplexType.ClrType,
                NestIncludes(includeProperties),
                NestIncludes(excludeProperties), false));
        }
        return new HtmlString(html.Append("</div>").ToString());
    }
    private string GetLocalized(string colName, Type type) {
        return _localization.GetLocalization(type, colName);
    }
    public IEnumerable<(string, object)> ToPairs(object entity, string[]? includeProperties = null,
        string[]? excludeProperties = null) {
        includeProperties ??= _includeProperties;
        excludeProperties ??= _excludeProperties;
        var t = GetEntityType(entity.GetType());
        IEnumerable<PropertyInfo> properties = entity.GetType().GetProperties();
        if (t != null)
            properties = GetMappedProperties(includeProperties, excludeProperties, properties, t);
        
        foreach (var pairsProperty in ToPairsProperties(entity, properties)){
            yield return (GetLocalized(pairsProperty.Item1, t?.ClrType ?? entity.GetType()), pairsProperty.Item2);
        }
        if (t == null) yield break;
        var nestedExcludes = NestIncludes(excludeProperties);
        var nestedIncludes = NestIncludes(includeProperties);
        foreach (var navigation in t.GetNavigations()
                     .Where(n => !n.IsCollection && n.IsOnDependent && !n.IsShadowProperty()).Where(n=>!excludeProperties.Contains(n.Name))){
            if(!includeProperties.Any(i => i.Split('_').First().Equals(navigation.Name) || 
                                           i.Split('_').First().Equals(navigation.Name))) continue;
            object? n = navigation.PropertyInfo.GetValue(entity);
            if (n==null) continue;
            foreach (var valueTuple in ToPairs(n, nestedIncludes, nestedExcludes).Where(kv=>nestedIncludes.Contains(kv.Item1)).Select(s=>($"{navigation.Name}_{s.Item1}", s.Item2))){
                yield return (GetLocalized(valueTuple.Item1, t?.ClrType??entity.GetType()), valueTuple.Item2);
            }
        }
        foreach (var complexToPair in ComplexToPairs(entity, t.GetComplexProperties())){
            yield return (GetLocalized(complexToPair.Item1, t?.ClrType??entity.GetType()), complexToPair.Item2);
        }
    }

    private static IEnumerable<PropertyInfo> GetMappedProperties(string[] includeProperties, string[] excludeProperties, IEnumerable<PropertyInfo> properties, IEntityType? t) {
        return properties.Where(p1 => {
            var p2 = t?.FindProperty(p1.Name);
            return p2 == null && t?.FindNavigation(p1.Name) == null &&
                   t?.FindComplexProperty(p1.Name) == null && !excludeProperties.Contains(p1.Name)|| //property from base class.
                   p2 != null && !excludeProperties.Contains(p1.Name) && !p2.IsShadowProperty() &&
                   (includeProperties.Contains(p1.Name) || !p2.IsKey() && !p2.IsForeignKey() && !p2.IsPrimaryKey());
        });
    }

    private static IEnumerable<(string, object)> ComplexToPairs(object entity, IEnumerable<IComplexProperty> complexProperties)
    {
        foreach (var complexProperty in complexProperties)
        {
            var complexValue = complexProperty.PropertyInfo?.GetValue(entity);
            var prefix = complexProperty.Name;
            if (complexValue == null)
            {
                foreach (var prop in complexProperty.ComplexType.GetProperties())
                    yield return ($"{prefix}_{prop.Name}", "null");
            }
            else
            {
                var nestedPairs = ToPairsProperties(complexValue, complexProperty.ComplexType.GetProperties().Select(p => p.PropertyInfo!));
                foreach (var (name, val) in nestedPairs)
                {
                    yield return ($"{prefix}_{name}", val);
                }
            }
        }   
    }        
    private static IEnumerable<(string, object)> ToPairsProperties(object entity, IEnumerable<PropertyInfo> properties)
    {
        foreach (var prop in properties)
        {
            var name = prop.Name;
            var value = prop.GetValue(entity);
            switch (Type.GetTypeCode(prop.PropertyType))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.DateTime:
                case TypeCode.String:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Object:
                    yield return (name, value?.ToString() ?? "null");
                    break;

                default:
                    yield return (name, "null");
                    break;
            }
        }
    }
    private static IEnumerable<string>ColumnNamesComplex(IEnumerable<IComplexProperty> complexProperties) {
        foreach (var complexProperty in complexProperties){
            foreach (var p in ColumnNamesProperties(complexProperty.ComplexType.GetProperties().Select(c=>c.PropertyInfo!)).Select(s=>$"{complexProperty.Name}_{s}")){
                yield return p;
            }
        }
    }
    private static IEnumerable<string> ColumnNamesProperties(IEnumerable<PropertyInfo> properties) {
        foreach (var prop in properties)
        {
            switch (Type.GetTypeCode(prop.PropertyType))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.DateTime:
                case TypeCode.String:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Object: //
                    yield return prop.Name;
                    break;
                    //ret.AddRange(ColumnNames(prop.PropertyType));
                    break;
            }
        }
    }
    public IEnumerable<string> ColumnNames(Type? entityType=null, string[]? exclude=null, string[]? include=null, bool onlyDeclared = false) {
        include ??= _includeProperties;
        exclude ??= _excludeProperties;
        entityType ??= _type ?? throw new ArgumentNullException("entityType or _type must be provided");
        var t = GetEntityType(entityType);
        IEnumerable<PropertyInfo> properties = onlyDeclared?entityType.GetProperties().Where(p=>p.DeclaringType==entityType):entityType.GetProperties();
        if (t != null)
            properties = properties.Where(p1 => {
                var p2 = t.FindProperty(p1.Name);
                return p2==null && t.FindNavigation(p1.Name)==null  && t.FindComplexProperty(p1.Name)==null || //property from base class.
                       p2!=null&& !exclude.Contains(p1.Name) && !p2.IsShadowProperty() &&
                       (include.Contains(p1.Name)||!p2.IsKey() && !p2.IsForeignKey() && !p2.IsPrimaryKey());
            });
        properties = properties.Where(p => !exclude.Contains(p.Name));
        foreach (var columnNamesProperty in ColumnNamesProperties(properties)){
            yield return GetLocalized(columnNamesProperty, t?.ClrType??entityType);
        }

        if (t == null) yield break;
        var nestedIncludes = NestIncludes(include);
        var nestedExcludes = NestIncludes(exclude);
        foreach (var navigation in t.GetNavigations()
                     .Where(n => (!onlyDeclared || n.DeclaringEntityType.ClrType == entityType)&&!n.IsCollection && n.IsOnDependent && !n.IsShadowProperty()).Where(n=>!exclude.Contains(n.Name))){
            if(!include.Any(i => i.Split('_').First().Equals(navigation.Name) || 
                                 i.Split('_').First().Equals(navigation.Name))) continue;
            foreach (var cn in ColumnNames(navigation.TargetEntityType.ClrType,nestedExcludes,nestedIncludes).Where(s=>nestedIncludes.Contains(s)).Select(s=>$"{navigation.Name}_{s}")){
                yield return GetLocalized(cn, t?.ClrType??entityType);
            }
        }
        foreach (var se in ColumnNamesComplex(t.GetComplexProperties())){
            yield return GetLocalized(se, t?.ClrType??entityType);
        }
    }

    private IEntityType? GetEntityType(Type clrType)
    {
        return _model.FindEntityType(clrType) ?? (clrType.BaseType!=null?_model.FindEntityType(clrType.BaseType): null);
    }
    private static string[] NestIncludes(params string[] exclude) {
        return exclude.Select(s => string.Join('_',s.Split('_').Skip(1))).ToArray();
    }
}