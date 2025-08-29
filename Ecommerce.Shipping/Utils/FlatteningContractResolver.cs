using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ecommerce.Shipping.Utils;
public class FlatteningContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);

        // Look for properties marked with [JsonFlatten]
        var flattenProps = props
            .Where(p => p.AttributeProvider.GetAttributes(typeof(JsonFlattenAttribute), true).Any())
            .ToList();

        foreach (var prop in flattenProps)
        {
            // Expand child object properties at parent level
            var childType = prop.PropertyType;
            var childProps = base.CreateProperties(childType, memberSerialization);

            foreach (var cp in childProps)
            {
                cp.ValueProvider = new ChildValueProvider(prop.ValueProvider, cp.ValueProvider);
                props.Add(cp);
            }

            // remove the original nested object
            props.Remove(prop);
        }

        return props;
    }

    private class ChildValueProvider : IValueProvider
    {
        private readonly IValueProvider _parent;
        private readonly IValueProvider _child;
        public ChildValueProvider(IValueProvider parent, IValueProvider child)
        {
            _parent = parent;
            _child = child;
        }
        public object GetValue(object target)
        {
            var parentObj = _parent.GetValue(target);
            return parentObj == null ? null : _child.GetValue(parentObj);
        }
        public void SetValue(object target, object value) { /* left unimplemented */ }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class JsonFlattenAttribute : Attribute { }
