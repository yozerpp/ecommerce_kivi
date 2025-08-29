namespace Ecommerce.Entity.Views;

public class SellerOrderStats
{
    public uint SellerId { get; set; }
    public decimal TotalComplete { get; set; }
    public uint CountComplete { get; set; }
    public decimal TotalInProgress { get; set; }
    public  uint CountInProgress { get; set; }
    public decimal TotalCanceled { get; set; }
    public uint CountCanceled { get; set; }
}
