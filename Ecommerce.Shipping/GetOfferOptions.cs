using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Dto;
using Ecommerce.Shipping.Entity;

namespace Ecommerce.Shipping;

public class GetOfferOptions
{
    public required OrderInfo OrderInfo { get; init; }
    public required ICollection< ShipmentItem> Items { get; init; }
    public bool PaymentOnDelivery { get; init; }
    public required DeliveryInfo Sender { get; init; }
    public required DeliveryInfo Recipient { get; init; }
}