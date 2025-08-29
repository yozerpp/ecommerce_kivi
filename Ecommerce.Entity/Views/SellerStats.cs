namespace Ecommerce.Entity.Views;

public class SellerStats
{
    public uint SellerId { get; set; }
    public uint OfferCount { get; set; }
    public uint ReviewCount { get; set; }
    public decimal ReviewAverage { get; set; }
    public decimal RatingTotal { get; set; }
    public uint SaleCount { get; set; }
    public decimal TotalSold { get; set; }
    public uint RefundCount { get; set; }
}