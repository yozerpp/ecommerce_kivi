using Ecommerce.Shipping.Geliver.Types.Network;

namespace Ecommerce.Shipping.Geliver.Types;

public class OffersResponse : AResponse<OffersResponse>
{
    public TrackingStatus TrackingStatus { get; set; }
    public OfferList Offers { get; set; }
}

public class OfferList
{
    public Offer Cheapest { get; set; }
    public Offer Fastest { get; set; }
    public ICollection<Offer> List { get; set; }
    public uint PercentageCompleted { get; set; }
    public ErrorCode? LastErrorCode { get; set; }
}