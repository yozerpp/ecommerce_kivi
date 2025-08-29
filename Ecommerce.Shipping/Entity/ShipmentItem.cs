using Ecommerce.Entity.Common;

namespace Ecommerce.Shipping.Entity;

public class ShipmentItem
{
    public ulong Id { get; set; }
    public string ItemId { get; set; }
    public string? ItemSku { get; set; }
    public string? ItemName { get; set; }
    public decimal? ItemPrice { get; set; }
    public uint Quantity { get; set; }
    public Dimensions Dimensions { get; set; } // Dimensions of the whole package.
}