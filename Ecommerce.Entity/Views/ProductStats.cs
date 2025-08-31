namespace Ecommerce.Entity.Views;

public class ProductStats
{
    public uint ProductId { get; set; }
    public uint SaleCount { get; set; }
    public uint OrderCount { get; set; }
    public uint ReviewCount { get; set; }
    public decimal RatingAverage { get; set; }
    public decimal RatingTotal { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public uint FavorCount { get; set; }
    public uint RefundCount { get; set; }
}
public class ProductRatingStats
{
    public uint ProductId { get; set; }
    public uint ReviewCount { get; set; }
    public uint FiveStarCount { get; set; }
    public uint FourStarCount { get; set; }
    public uint ThreeStarCount { get; set; }
    public uint TwoStarCount { get; set; }
    public uint OneStarCount { get; set; }
    public uint ZeroStarCount { get; set; }
}