namespace Ecommerce.Shipping.Geliver.Types;

//ShipmentId parameter in query
public class RefundRequest
{
    public bool IsReturn => true;
    public bool WillAccept => true;
    /// <summary>
    /// Does it have to be the same with the delivery shipment's?
    /// </summary>
    public string ProviderServiceCode { get; set; }
    public long Count { get; set; }
    public string? SenderAddressId { get; set; }
    public Address? Address { get; set; }
}