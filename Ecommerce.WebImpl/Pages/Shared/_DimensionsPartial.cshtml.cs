using Ecommerce.Entity.Common;

namespace Ecommerce.WebImpl.Pages.Shared;

public class _DimensionsPartial
{
    public Dimensions? Dimensions { get; set; }
    public bool Editable { get; set; } = false;
    public string InputName { get; set; } = "dimensions";
    public string? OnChange { get; set; }
}
