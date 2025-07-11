namespace Ecommerce.Entity.Projections;

public class ProductWithAggregates : Product
{
    public uint SaleCount { get; set; }
    public uint ReviewCount { get; set; }
    public float ReviewAverage { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}