namespace Ecommerce.Entity.Views;

public class SellerStats
{
    public uint SellerId { get; set; }
    public uint OfferCount { get; set; }
    public uint ReviewCount { get; set; }
    public float ReviewAverage { get; set; }
    public float RatingTotal { get; set; }
    public uint SaleCount { get; set; }
    public decimal TotalSold { get; set; }
    public int RefundCount { get; set; }
}