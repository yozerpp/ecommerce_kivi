namespace Ecommerce.Shipping.Entity;

public class ShipmentItem
{
    public ulong ShipmentId { get; set; }
    public string ItemId { get; set; }
    public Shipment Shipment { get; set; }
}