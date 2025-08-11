using System.Reflection.Metadata;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Entity.Views;

public class ProductStats : IProductStats
{
    public uint? ProductId { get; set; }
    public int? SaleCount { get; set; }
    public uint? OrderCount { get; set; }
    public uint? ReviewCount { get; set; }
    public decimal? RatingAverage { get; set; }
    public decimal? RatingTotal { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public uint? FavorCount { get; set; }
    public uint? RefundCount { get; set; }
}