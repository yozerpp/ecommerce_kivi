namespace Ecommerce.Entity.Common.Meta;

public class SelfReferencingProperty : Attribute
{
    public bool BreakCycle { get; set; } = true;
}