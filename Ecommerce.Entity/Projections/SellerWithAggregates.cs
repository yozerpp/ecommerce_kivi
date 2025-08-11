namespace Ecommerce.Entity.Projections;

public class SellerWithAggregates : Seller
{
    public uint OfferCount { get; set; }
    public uint ReviewCount { get; set; }
    public float ReviewAverage { get; set; }
    public uint SaleCount { get; set; }
    public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}