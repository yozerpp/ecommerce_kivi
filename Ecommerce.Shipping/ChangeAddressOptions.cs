using Ecommerce.Entity.Common;

namespace Ecommerce.Shipping;

public class ChangeAddressOptions
{
    public required ulong ShipmentId { get; init; }
    public required Address NewAddress { get; init; }
}