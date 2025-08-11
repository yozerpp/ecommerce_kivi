using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Entity;

namespace Ecommerce.Shipping;


public interface IShippingService
{
    public List<ShippingOffer> GetOffers(IEnumerable<Dimensions> dimensions, Address shipmentAddress,
        Address recipientAddress);

    public Shipment? GetShipment(ulong id);
    public List<Shipment> AcceptOffer(ICollection<ShipmentCreateOptions> offerIds);
    public void UpdateAddress(ulong shipmentId, Address address);
    public void UpdatePhoneNumber(ulong shipmentId, PhoneNumber phoneNumber);
    public void Cancel(ulong shipmentId);
    public void Deliver(ulong shipmentId);
    public class ShipmentCreateOptions
    {
        public ulong OfferId { get; set; }
        public string SenderEmail { get; set; }
        public string RecepientEmail { get; set; }
        public PhoneNumber SenderPhoneNumber { get; set; }
        public PhoneNumber RecepientPhoneNumber { get; set; }
    }

}