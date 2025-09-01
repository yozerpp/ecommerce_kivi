using System.Collections.Concurrent;
using System.Globalization;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Dummy;
using Ecommerce.Shipping.Entity;
using Ecommerce.Shipping.Geliver.Types;
using Ecommerce.Shipping.Geliver.Types.Network;
using Microsoft.EntityFrameworkCore;
using Address = Ecommerce.Entity.Common.Address;
using Dimensions = Ecommerce.Entity.Common.Dimensions;
using ShipmentStatus = Ecommerce.Entity.Common.ShipmentStatus;

namespace Ecommerce.Shipping.Geliver;

public class GeliverService(GeliverClient _client, ShippingContext _context) : IShippingService
{
    private readonly ProviderStore _providerStore = new(_context.Providers);
    private readonly LocationCodeStore  _locationCodeStore = new(_client);
    public async Task<ICollection<ShippingOffer>> GetOffers(GetOfferOptions options) {
        if (options.Recipient.Address.ApiId == null){
            options.Recipient.Id =  options.Recipient.Address.ApiId = await RegisterAddress(options.Recipient.Name, null, options.Recipient.Address, options.Recipient.PhoneNumber, true);
            _context.DeliveryInfos.Add(options.Recipient);
        }
        if (options.Sender.Address.ApiId == null){
            options.Sender.Id = options.Sender.Address.ApiId = await RegisterAddress(options.Sender.Name, null, options.Sender.Address, options.Recipient.PhoneNumber, false);
            _context.DeliveryInfos.Add(options.Sender);
        }
        var offersResponse = await _client.GetOffers(new OfferRequest(){
            Dimensions = options.Items.Aggregate( new Dimensions(), (dimensions, item) => dimensions + item.Dimensions).ToGeliver(),
            Phone = options.Recipient.PhoneNumber.ToString().Replace(" ","").Replace("x",""),
            Items = options.Items.Select(i=>i.ToGeliver()).ToArray(),
            Order = options.OrderInfo.ToGeliver(),
            ProductPaymentOnDelivery = options.PaymentOnDelivery,
            RecipientAddressId = options.Recipient.Address.ApiId,
            SenderAddressId = options.Sender.Address.ApiId,
        });
        var ret= offersResponse.Data.Offers.List.Select(offer => {
            var p = _providerStore.GetProvider(offer.ProviderServiceCode.ToString(), offer.ProviderCode,out var saved);
            if (!saved) _context.Attach(p);
            var pid = p.Id;
            var days = int.Parse(offer.AverageEstimatedTimeHumanReadible.Split("gün", StringSplitOptions.TrimEntries).First());
            var hrs = int.Parse(offer.AverageEstimatedTimeHumanReadible.Split("gün", StringSplitOptions.TrimEntries)
                .Last().Split("saat", StringSplitOptions.TrimEntries).First());
            return _context.ShippingOffers.Add(new ShippingOffer(){
                Price = offer.Amount,
                Currency = offer.Currency,
                Tax = offer.AmountTax,
                ProviderId = pid,
                DeliveryTime = new DateTime(1, 1, days, hrs,0,0, DateTimeKind.Unspecified),
                Provider = p,
                ApiId = offer.Id,
                SenderId = options.Sender.Id,
                RecipientId = options.Recipient.Id,
                Items = options.Items,
                ContextId = offersResponse.Data.TrackingStatus.Id,
            }).Entity;
        }).ToArray();
        await _context.SaveChangesAsync();
        return ret;
    }
    public async Task<Shipment> AcceptOffer(AcceptOfferOptions options) {
        var offer = await _context.ShippingOffers.Include(o => o.Provider).Include(o => o.Items)
            .Include(shippingOffer => shippingOffer.Recipient).Include(shippingOffer => shippingOffer.Sender)
            .FirstAsync(o=>o.Id == options.OfferId);
        if(offer?.ApiId == null) throw new ArgumentOutOfRangeException(string.Join('.',nameof(AcceptOfferOptions), nameof(options.OfferId)), options.OfferId.ToString());
        var shipmentResponse = await _client.AcceptOffer(offer.ApiId);
        var ret =  _context.Shipments.Add(GetShipment(shipmentResponse,offer)).Entity;
        await _context.ShippingOffers.Where(o => o.ContextId == offer.ContextId && o.Id != offer.Id).ExecuteDeleteAsync();
        await _context.SaveChangesAsync();
        return ret;
    }

    public async Task<ICollection<Shipment>> AcceptOfferBatch(AcceptOfferOptions[] options) {
        var offers = options.Select(options => _context.ShippingOffers
            .Include(o => o.Provider).Include(o => o.Items)
            .Include(shippingOffer => shippingOffer.Recipient).Include(shippingOffer => shippingOffer.Sender)
            .First(o => o.Id == options.OfferId)).ToArray();
        if(offers.Any(o=>o.ApiId==null)) throw new ArgumentOutOfRangeException(string.Join('.',nameof(AcceptOfferOptions), nameof(options), nameof(AcceptOfferOptions.OfferId)));
        var shipments = await Task.WhenAll(offers.Select(async o => {
            var resp = await _client.AcceptOffer(o.ApiId);
            return GetShipment(resp, o);
        }));
        _context.Shipments.AddRange(shipments);
        await _context.SaveChangesAsync();
        var oids = offers.Select(o => o.Id).ToArray();
        var cids = offers.Select(o => o.ContextId).ToArray();
        await _context.ShippingOffers.Where(o => !oids.Contains(o.Id) && cids.Contains(o.ContextId))
            .ExecuteDeleteAsync();
        return shipments;
    }

    public async Task<Shipment> GetStatus(string id) {
         var st = await _client.GetStatus(id);
         var status = st.TrackingSubStatusCode switch{
             TrackingSubStatusCode.package_departed => ShipmentStatus.InTransit,
             TrackingSubStatusCode.delivery_scheduled or TrackingSubStatusCode.package_accepted or TrackingSubStatusCode.delivery_rescheduled => ShipmentStatus
                 .InPickup,
             TrackingSubStatusCode.out_for_delivery => ShipmentStatus.InDelivery,
             TrackingSubStatusCode.delivered => ShipmentStatus.Delivered,
             _=>ShipmentStatus.Processing
         };
         var loc = st.LocationName;
         var s = await _context.Shipments.FirstOrDefaultAsync(s => s.ApiId == id);
         if(s==null) throw new ArgumentOutOfRangeException(nameof(id), id);
         s.ShipmentStatus = status;
         s.LastLocation = loc;
         await _context.SaveChangesAsync();
         return s;
    }

    public (bool,bool) ValidateAddress(Address address) {
        return (_locationCodeStore.GetCity(address.City) != null ,_locationCodeStore.GetDistrict(address.City, address.District) != null);
    }

    public async Task CancelShipment(string id) {
        if (!await _client.CancelShipment(id))
            throw null;
    }

    public async Task<Shipment> Refund(string shipmentId, int count) {
        var old = await _context.Shipments.AsNoTracking().Include(shipment => shipment.Provider)
            .Include(shipment => shipment.Sender)
            .Include(shipment => shipment.Items).Include(shipment => shipment.Recipient).FirstOrDefaultAsync(s => s.ApiId == shipmentId)??throw new ArgumentOutOfRangeException(nameof(shipmentId), shipmentId);
        var res = await _client.Refund(shipmentId, count, old.Provider.ApiId,old.Sender.Address.ApiId);
        var s = res.Data.Shipment;
        foreach (var shipmentItem in old.Items){
            _context.Entry(shipmentItem).State = EntityState.Unchanged;
        }
        var e= _context.Entry(new Shipment(){
            ApiId = s.Id,
            Items = old.Items,
            Price = old.Price,
            Tax = old.Tax,
            ProviderId = old.ProviderId,
            RecipientId = old.RecipientId,
            SenderId = old.SenderId,
            ShipmentStatus = ShipmentStatus.Processing,
        });
        e.State = EntityState.Added;
        await _context.SaveChangesAsync();
        e.Entity.Sender = old.Recipient;
        e.Entity.Recipient = old.Sender;
        e.Entity.Provider = old.Provider;
        return e.Entity;
    }
    public async Task<ICollection<Shipment>> AcceptOfferBatch(ICollection<AcceptOfferOptions> options) {
        var offerIds = options.Select(o => o.OfferId).ToArray();
        var offers = await _context.ShippingOffers.Include(o => o.Provider).Include(o => o.Items)
            .Include(shippingOffer => shippingOffer.Recipient).Include(shippingOffer => shippingOffer.Sender)
            .Where(o => offerIds.Contains(o.Id)).ToArrayAsync();
        if(offers.Length < options.Count) throw new ArgumentOutOfRangeException(string.Join('.',nameof(AcceptOfferOptions), nameof(AcceptOfferOptions.OfferId)));
        var shipments = await Task.WhenAll(offers.Select(async o => {
            var resp = await _client.AcceptOffer(o.ApiId);
            return GetShipment(resp, o);
        }));
        _context.Shipments.AddRange(shipments);
        await _context.SaveChangesAsync();
        var contextIds = offers.Select(o => o.ContextId).ToArray();
        await _context.ShippingOffers.Where(o => contextIds.Contains(o.ContextId) && !offerIds.Contains(o.Id)).ExecuteDeleteAsync();
        return shipments;
    }

    public Task ChangeAddress(ChangeAddressOptions options) {
        throw new NotImplementedException();
    }

    private static Shipment GetShipment(GeliverShipment geliverShipment,ShippingOffer offer) {
        return new Shipment(){
            ApiId = geliverShipment.Id,
            ShipmentStatus = ShipmentStatus.Processing,
            Items = offer.Items,
            OfferId = offer.Id,
            ProviderId = offer.ProviderId,
            Provider = offer.Provider,
            Recipient = offer.Recipient,
            Price = offer.Price,
            Tax = offer.Tax,
            RecipientId = offer.RecipientId,
            Sender = offer.Sender,
            SenderId = offer.SenderId,
            TrackingUri = geliverShipment.TrackingUrl,
            TrackingNumber = geliverShipment.TrackingNumber,
        };
    }
    public async Task SetTrackingNumber(string apiId,string trackingNumber, Uri trackingUrl) {
        var c = await _context.Shipments.Where(s => s.ApiId == apiId).ExecuteUpdateAsync(s =>
            s.SetProperty(s => s.TrackingNumber, trackingNumber).SetProperty(s => s.TrackingUri, trackingUrl));
        if(c==0) throw new ArgumentOutOfRangeException(nameof(apiId), apiId);
    }
    private async Task<string> RegisterAddress(string fullName, string? shortName,Address address,PhoneNumber phoneNumber, bool isRecipient) {
        return await _client.PostAddress(new Types.Address(){
            Address1 = address.Line1,
            Address2 = address.Line2,
            CityCode = _locationCodeStore.GetCity(address.City)?.CityCode ??
                       throw new ArgumentOutOfRangeException(string.Join('.', nameof(Address), nameof(Address.City))),
            DistrictID = _locationCodeStore.GetDistrict(address.City, address.District)?.DistrictId ??
                         throw new ArgumentOutOfRangeException(string.Join('.', nameof(Address),
                             nameof(Address.District))),
            CityName = address.City,
            DistrictName = address.District,
            CountryCode = "TR",
            IsRecipientAddress = isRecipient,
            Name = fullName,
            Phone = phoneNumber.ToString(),
            ShortName = shortName,
            Zip = address.ZipCode
        });
    }

    private class ProviderStore(DbSet<Provider> _providerRepository)
    {
        private static readonly Dictionary<string, Provider> ProviderCache = new();

        public Provider GetProvider(string providerCode, string? providerName, out bool saved) {
            if (ProviderCache.TryGetValue(providerCode, out var provider)){
                saved = false;
                return provider;
            }
            if ((provider = _providerRepository.FirstOrDefault(p => p.ApiId == providerCode)) == null){
                provider = _providerRepository.Add(new Provider(){
                    ApiId = providerCode, Name = providerName ?? providerCode
                }).Entity;
                saved = true;
            }else saved = false;
            ProviderCache[providerCode] = provider;
            return provider;
        }
    }
    private class LocationCodeStore(GeliverClient _client)
    {
       private static ConcurrentDictionary<string, City>? _cityCodes;
       private static readonly IDictionary<City, IDictionary<string, District>> DistrictCodes = new Dictionary<City, IDictionary<string, District>>();

       internal City? GetCity(string cityName) {
           if (_cityCodes == null){
               var cities = _client.GetCities().Result;
               _cityCodes = new ConcurrentDictionary<string, City>(cities.Data.Select(c=>new KeyValuePair<string, City>(c.Name.ToUpperInvariant(), c)));
           }
           return !_cityCodes.TryGetValue(cityName.ToUpperInvariant(), out var city) ? null : city;
       }
       internal District? GetDistrict(string cityName, string districtName) {
           var c = GetCity(cityName);
           if (c == null) return null;
           IDictionary<string, District>? dict;
           lock (c){
               if (!DistrictCodes.TryGetValue(c, out dict)){
                   var districts =  _client.GetDistricts(c.CityCode).Result;
                   dict = DistrictCodes[c] = new Dictionary<string, District>();
                   foreach (var district in districts.Data){
                       dict[district.Name.ToUpperInvariant()] = district;
                   }
               }
           }
           return dict.TryGetValue(districtName.ToUpperInvariant(), out var dtrict) ? dtrict : null;
       }
    }
}