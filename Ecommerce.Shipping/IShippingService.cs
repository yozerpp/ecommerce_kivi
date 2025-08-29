using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Entity;

namespace Ecommerce.Shipping;


public interface IShippingService
{
    public Task<ICollection<ShippingOffer>> GetOffers(GetOfferOptions options);
    public Task<Shipment> AcceptOffer(AcceptOfferOptions options);
    public Task<Shipment> GetStatus(string id);
    public Task CancelShipment(string id);
    public Task<Shipment> Refund(string shipmentId, int count);
    public Task<ICollection<Shipment>> AcceptOfferBatch(ICollection<AcceptOfferOptions> options);
    /// <exception cref="NotImplementedException">Not Implemented</exception>
    public Task ChangeAddress(ChangeAddressOptions options);
}