using Ecommerce.Entity.Common;

namespace Ecommerce.Shipping.Entity;

public class ShippingOffer
{
    public ulong Id { get; set; }
    public decimal Amount { get; set; } //scale 2
    public Address DeliveryAddress { get; set; }
    public Address ShippingAddress { get; set; }
    public uint ProviderId { get; set; }
    public Provider Provider { get; set; }
    public string Currency { get; set; }
    public decimal AmountTax { get; set; }
}