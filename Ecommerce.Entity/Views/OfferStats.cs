namespace Ecommerce.Entity.Views;

public class OfferStats
{
    public uint ProductId { get; init; }
    public uint SellerId { get; init; }
    public int RefundCount { get; init; }
    public int ReviewCount { get; init; }
    public decimal ReviewAverage { get; init; }
    public decimal RatingTotal { get; init; }
}