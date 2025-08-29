namespace Ecommerce.Entity.Views;

public class OfferStats
{
    public uint? ProductId { get; init; }
    public uint? SellerId { get; init; }
    public uint? RefundCount { get; init; }
    public uint? ReviewCount { get; init; }
    public decimal? ReviewAverage { get; init; }
    public decimal? RatingTotal { get; init; }
}