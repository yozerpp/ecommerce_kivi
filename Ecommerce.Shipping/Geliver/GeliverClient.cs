using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Entity.Common;
using Ecommerce.Shipping.Entity;
using Ecommerce.Shipping.Geliver.Types;
using Ecommerce.Shipping.Geliver.Types.Network;
using Ecommerce.Shipping.Utils;
using Newtonsoft.Json;
using Address = Ecommerce.Shipping.Geliver.Types.Address;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Ecommerce.Shipping.Geliver;

public class GeliverClient : AApiClient
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web){Converters = { new JsonStringEnumConverter() }}; 
    public GeliverClient(string apiKey) : base(apiKey, "https://api.geliver.io/api/v1/") {
        
    }
    public async Task<string> PostAddress(Address address, CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,"addresses"){
            Content = JsonContent.Create<Address>(address),
        }, HttpCompletionOption.ResponseContentRead, cancellationToken);
        res.EnsureSuccessStatusCode();
        var b = await res.Content.ReadFromJsonAsync<Address>(cancellationToken);
        return b?.Data.Id ?? throw null;
    }
    public async Task<LocationListResponse<City>> GetCities(CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "cities?countryCode=TR"),cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<LocationListResponse<City>>(cancellationToken) ?? throw null;
    }
    public async Task<LocationListResponse<District>> GetDistricts(string cityCode,CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"districts?countryCode=TR&cityCode={cityCode}"),cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<LocationListResponse<District>>(cancellationToken)??throw null;
    }

    public async Task DeleteAddress(string addressId, CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"addresses/{addressId}"),cancellationToken);
        res.EnsureSuccessStatusCode();
        
    }

    public async Task<OffersResponse> GetOffers(OfferRequest offerRequest, CancellationToken cancellationToken = default) {
        var res =await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "shipments"){
            Content = new StringContent(JsonConvert.SerializeObject(offerRequest, JsonSerializerSettings),Encoding.UTF8, "application/json")
        }, cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<OffersResponse>(_jsonSerializerOptions,cancellationToken)??throw null;
    }

    public async Task<GeliverShipment> AcceptOffer(string offerId, CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "transactions"){
            Content = new StringContent(JsonConvert.SerializeObject(new{ offerID = offerId }),Encoding.UTF8, "application/json"),
        }, cancellationToken);
        res.EnsureSuccessStatusCode();
        var r =  await res.Content.ReadFromJsonAsync<ShipmentResponse>(cancellationToken) ??throw null;
        return r.Data.Shipment;
    }

    public async Task<bool> CancelShipment(string shipmentId, CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "shipments/" + shipmentId),cancellationToken);
        res.EnsureSuccessStatusCode();
        var b = await res.Content.ReadFromJsonAsync<ShipmentResponse>(cancellationToken);
        return b?.Result ??throw null;
    }

    public async Task<ShipmentResponse> Refund(string id, int count, string serviceCode, string senderAddressId, CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "shipments/" + id){
                Content = new StringContent(JsonConvert.SerializeObject(new RefundRequest(){
                    Count = count,
                    ProviderServiceCode = serviceCode,
                    SenderAddressId = senderAddressId,
                }), Encoding.UTF8, "application/json")
            },
            cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ShipmentResponse>(cancellationToken) ?? throw null;
    }
    public async Task<GeliverShipment> CancelShipment(string shipmentId, RefundRequest refundRequest,
        CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "shipments/" + shipmentId){
            Content = new StringContent(JsonConvert.SerializeObject(refundRequest), Encoding.UTF8, "application/json"),
        },cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<GeliverShipment>(cancellationToken);
    }

    public async Task<TrackingStatus> GetStatus(string shipmentId,
        CancellationToken cancellationToken = default) {
        var res = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "shipments/" + shipmentId),
            cancellationToken);
        var b =await res.Content.ReadFromJsonAsync<TrackingStatusResponse>(_jsonSerializerOptions,cancellationToken) ?? throw null;
        return b.Data.TrackingStatus;
    }
    private static JsonSerializerSettings JsonSerializerSettings => new(){ ContractResolver = new FlatteningContractResolver() };
}