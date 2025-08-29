using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Dto;

namespace Ecommerce.Shipping.Entity;

public class ShippingOffer
{
    public ulong Id { get; set; }
    public string? ApiId { get; set; }
    public string? ContextId { get; set; }
    public decimal Price { get; set; } //scale 2
    public string RecipientId { get; set; }
    public string SenderId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public DeliveryInfo Recipient { get; set; }
    public DeliveryInfo Sender { get; set; }
    public uint ProviderId { get; set; }
    public Provider Provider { get; set; }
    public string Currency { get; set; }
    public decimal Tax { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
}