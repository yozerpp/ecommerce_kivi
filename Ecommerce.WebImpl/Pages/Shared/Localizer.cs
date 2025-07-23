using System.Linq.Expressions;

namespace Ecommerce.WebImpl.Pages.Shared;

public class Localizer
{
    private readonly Dictionary<Type, Dictionary<string, string>> _localizations;

    private Localizer(Dictionary<Type, Dictionary<string, string>> locs) {
        _localizations = locs;
    }
    public string GetLocalization(Type type, string propertyName) {
        while (type!=null){
            if (_localizations.TryGetValue(type, out var dict) && dict.TryGetValue(propertyName, out var value)) {
                return value;
            }
            type = type.BaseType;
        }
        return propertyName;
    }
    public string GetOriginal(Type type, string localizedName) {
        if (_localizations.TryGetValue(type, out var dict)) {
            foreach (var kvp in dict) {
                if (kvp.Value == localizedName) {
                    return kvp.Key;
                }
            }
        }
        return localizedName; // fallback to localized name if not found
    }
    public class Builder
    {
        private readonly Dictionary<Type, Dictionary<string, string>> _localizations = new();
        public Builder Add<T>( params (Expression<Func<T, object>>, string)[] configure) {
            if (configure == null || configure.Length == 0) return this;
            var dict = _localizations[typeof(T)] = new Dictionary<string, string>();
            foreach (var conf in configure){
                var exp = conf.Item1.Body is UnaryExpression body ? body.Operand : conf.Item1.Body;
                dict[((MemberExpression)exp).Member.Name] = conf.Item2;
            }
            return this;
        }

        public Localizer Build() {
            return new Localizer(_localizations);
        }
    }
}