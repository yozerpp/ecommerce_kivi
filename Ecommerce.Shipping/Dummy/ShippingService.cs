using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Ecommerce.Mail;
using Ecommerce.Shipping.Entity;
using Ecommerce.Shipping.Geliver.Types.Network;
using Microsoft.EntityFrameworkCore;
using ShipmentStatus = Ecommerce.Entity.Common.ShipmentStatus;

namespace Ecommerce.Shipping.Dummy;

public class ShippingService (ShippingContext _context):IShippingService
{


    public async Task<ICollection<ShippingOffer>> GetOffers(GetOfferOptions options) {
        var tot = options.Items.Sum(i => (i.ItemPrice ?? 0) * i.Quantity);
        var ctxId = Guid.NewGuid().ToString();
        if (options.Sender.Id == null!){
            options.Sender.Id=Guid.NewGuid().ToString();
            _context.DeliveryInfos.Add(options.Sender);
        }
        if (options.Recipient.Id == null!){
            options.Recipient.Id = Guid.NewGuid().ToString();
            _context.DeliveryInfos.Add(options.Recipient);
        }
        await _context.SaveChangesAsync();
        return _context.Providers.AsEnumerable().Select(p => new ShippingOffer(){
            Price = tot,
            Tax = tot * .2m,
            ContextId = ctxId,
            Currency = "TRY",
            Items = options.Items,
            Provider = p,
            ProviderId = p.Id,
            Sender = options.Sender,
            SenderId = options.Sender.Id,
            Recipient = options.Recipient,
            RecipientId = options.Recipient.Id,
            ApiId = null,
        }).ToArray();
    }

    public async Task<Shipment> AcceptOffer(AcceptOfferOptions options) {
        var offer = _context.ShippingOffers.Include(p => p.Sender).Include(shippingOffer => shippingOffer.Items)
            .Include(shippingOffer => shippingOffer.Provider).Include(shippingOffer => shippingOffer.Recipient).First(o => o.Id == options.OfferId);
        var ret =  _context.Shipments.Add(CreateShipmentFromOffer(offer)).Entity;
        await _context.SaveChangesAsync();
        await _context.ShippingOffers.Where(o => o.ContextId == offer.ContextId && o.Id != offer.Id).ExecuteDeleteAsync();
        return ret;
    }

    public async Task<Shipment> GetStatus(string id) {
        var sid = ulong.Parse(id);
        var s = _context.Shipments.First(s=>s.Id == sid);
        s.ShipmentStatus += 1;
        await _context.SaveChangesAsync();
        return s;
    }

    public Task CancelShipment(string id) {
        throw new NotImplementedException();
    }

    public Task<Shipment> Refund(string shipmentId, int count) {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Shipment>> AcceptOfferBatch(ICollection<AcceptOfferOptions> options) {
        var offerIds =  options.Select(o => o.OfferId).ToArray();
        var offers = _context.ShippingOffers.Include(p => p.Sender).Include(shippingOffer => shippingOffer.Items)
            .Include(shippingOffer => shippingOffer.Provider).Include(shippingOffer => shippingOffer.Recipient)
            .Where(o => offerIds.Contains(o.Id)).ToArray();
        var ret = offers.Select(CreateShipmentFromOffer).ToArray();
        _context.Shipments.AddRange(ret);
        var contextIds = offers.Select(o => o.ContextId).ToArray();
        await _context.SaveChangesAsync();
        await _context.ShippingOffers.Where(o => contextIds.Contains(o.ContextId) && !offerIds.Contains(o.Id)).ExecuteDeleteAsync();
        return ret;
    }


    public Task ChangeAddress(ChangeAddressOptions options) {
        throw new NotImplementedException();
    }

    private static Shipment CreateShipmentFromOffer(ShippingOffer offer) => new(){
        ShipmentStatus = ShipmentStatus.Processing,
        Items = offer.Items,
        ApiId = null,
        OfferId = offer.Id,
        Provider = offer.Provider,
        ProviderId = offer.ProviderId,
        Recipient = offer.Recipient,
        RecipientId = offer.RecipientId,
        Sender = offer.Sender,
        SenderId = offer.SenderId,
    };
}