using System.Dynamic;

namespace Ecommerce.Entity;

// This class will represent the owned entity for product category properties.
// It will use DynamicObject to allow flexible property access based on category definitions.
public class ProductCategoryProperties : DynamicObject
{
    private readonly Dictionary<string, object?> _properties = new Dictionary<string, object?>();

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return _properties.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        _properties[binder.Name] = value;
        return true;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        return _properties.Keys;
    }

    // Method to get the underlying dictionary, useful for EF Core configuration
    public IDictionary<string, object?> GetPropertiesDictionary()
    {
        return _properties;
    }
}
