using Ecommerce.Shipping.Utils;

namespace Ecommerce.Shipping.Geliver.Types.Network;

public class OfferRequest
{
    public string SenderAddressId { get; set; }
    
    public bool Test => true;
    
    public string ReturnAddressId => SenderAddressId;
    public string Phone { get; set; }
    public string RecipientAddressId { get; set; }
    public ICollection<Item> Items { get; set; }
    [JsonFlatten]
    public Dimensions Dimensions { get; set; }
    public bool ProductPaymentOnDelivery { get; set; }
    public Order Order { get; set; }
    
}