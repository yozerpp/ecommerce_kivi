using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Ecommerce.Mail;
using Ecommerce.Shipping.Entity;

namespace Ecommerce.Shipping.Dummy;

public class ShippingService :IShippingService
{
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly IRepository<ShippingOffer> _offerRepository;
    private readonly IRepository<Provider> _providerRepository;
    public ShippingService(IRepository<Shipment> shipmentRepository, IRepository<ShippingOffer> offerRepository, IRepository<Provider> providerRepository) {
        _shipmentRepository = shipmentRepository;
        _offerRepository = offerRepository;
        _providerRepository = providerRepository;
    }
    public List<ShippingOffer> GetOffers(IEnumerable<Dimensions> dimensions, Address shipmentAddress, Address recipientAddress) {
        var ret =  _providerRepository.All().Select(p => {
            var a = dimensions.Sum(dimension=> decimal.Round(Random.Shared.NextInt64(1,10001)*0.001m,2) *
                                               (decimal)(dimension.Depth * dimension.Height * dimension.Width));
            var o = new ShippingOffer(){
                Amount = a,
                AmountTax = a * 0.18m,
                DeliveryAddress = shipmentAddress,
                ShippingAddress = recipientAddress,
                Currency = "TR",
                ProviderId = p.Id,
            };
            return _offerRepository.Add(o);
            }
        ).ToList();
        _offerRepository.Flush();
        return ret;
    }

    public Shipment? GetShipment(ulong id) {
        return _shipmentRepository.First(s => s.Id == id);
    }
    public Shipment? GetShipmentByTrackingNumber(string trackingNumber) {
        return _shipmentRepository.First(s => s.TrackingNumber == trackingNumber);
    }
    public List<Shipment> AcceptOffer(ICollection<IShippingService.ShipmentCreateOptions> args) {
        var offerIds = args.Select(a => a.OfferId).ToList();
        var name = args.First().RecepientNane;
        var ret = _offerRepository.Where(o => offerIds.Contains(o.Id)).Select((o, i) =>
            _shipmentRepository.Save(new Shipment(){
                DeliveryAddress = o.DeliveryAddress,
                ShippingAddress = o.ShippingAddress,
                CurrentAddress = o.ShippingAddress,
                OfferId = o.Id,
                RecepientName = name,
                RecepientPhoneNumber = args.ElementAt(i).RecepientPhoneNumber,
                SenderPhoneNumber = args.ElementAt(i).SenderPhoneNumber,
                RecepientEmail = args.ElementAt(i).RecepientEmail,
                SenderEmail = args.ElementAt(i).SenderEmail,
                Status = Status.Created,
                ProviderId = o.ProviderId,
            })
        ).ToList();
        _shipmentRepository.Flush();
        return ret;
    }
    public void UpdateAddress(ulong shipmentId, Address address) {
        _shipmentRepository.UpdateExpr([
            (s=>s.DeliveryAddress, address)
        ], s => s.Id == shipmentId);
    }
    public void UpdatePhoneNumber(ulong shipmentId, PhoneNumber phoneNumber) {
        _shipmentRepository.UpdateExpr([
        (s=>s.RecepientPhoneNumber, phoneNumber)
        ], s=>s.Id == shipmentId);
    }

    public void Cancel(ulong shipmentId) {
        //make api call
        _shipmentRepository.UpdateExpr([
            (s=>s.Status, Status.Cancelled)
        ], s => s.Id == shipmentId);
    }

    public void Deliver(ulong shipmentId) {
        _shipmentRepository.UpdateExpr([
            (s=>s.Status, Status.Delivered)
        ], s => s.Id == shipmentId);
    }
}