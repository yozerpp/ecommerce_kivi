using Ecommerce.Entity.Common;

namespace Ecommerce.Shipping.Entity;

public class Shipment
{
    public ulong Id { get; set; } //is tracking number
    public string? ApiId { get; set; }
    public string? TrackingNumber { get; set; }
    public uint ProviderId { get; set; }
    public Provider Provider { get; set; }
    public ulong OfferId { get; set; }
    public ShippingOffer ShippingOffer { get; set; }
    public Address DeliveryAddress { get; set; }
    public Address ShippingAddress { get; set; }
    public Address CurrentAddress { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    public PhoneNumber RecepientPhoneNumber { get; set; }
    public PhoneNumber SenderPhoneNumber { get; set; }
    public Status Status { get; set; }
    public string RecepientName { get; set; }
    public string SenderEmail { get; set; } //strip this
    public string RecepientEmail { get; set; }
}
public enum Status
{
    Created,
    InTransit,
    Delivered,
    Cancelled
}