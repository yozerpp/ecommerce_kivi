using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Dto;
using Ecommerce.Shipping.Geliver.Types;

namespace Ecommerce.Shipping.Entity;

public class Shipment
{
    public ulong Id { get; set; }
    public string? ApiId { get; set; }
    public string? TrackingNumber { get; set; }
    public Uri? TrackingUri { get; set; }
    public uint ProviderId { get; set; }
    public decimal Price { get; set; }
    public decimal Tax { get; set; }
    public Provider Provider { get; set; }
    public ulong OfferId { get; set; }
    public string RecipientId { get; set; }
    public string SenderId { get; set; }
    public DeliveryInfo Recipient { get; set; }
    public DeliveryInfo Sender { get; set; }
    public string? LastLocation { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    public ShipmentStatus ShipmentStatus { get; set; }
}
