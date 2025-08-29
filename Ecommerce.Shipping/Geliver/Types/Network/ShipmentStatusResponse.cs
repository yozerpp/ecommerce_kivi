using Ecommerce.Shipping.Geliver.Types.Network;

namespace Ecommerce.Shipping.Geliver.Types;

//required ShipmentID
public class ShipmentStatusResponse : AResponse<ShipmentStatusResponse>
{
    public string TrackingNumber { get; set; }
    public string TrackingUrl { get; set; }
    public TrackingStatus TrackingStatus { get; set; }
}