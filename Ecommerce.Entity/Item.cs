namespace Ecommerce.Entity;

public interface IItem
{
    public uint SellerId { get; set; }
    public uint ProductId { get; set; }
    public ProductOffer ProductOffer { get; set; }
    public int Quantity { get; set; }
    public string? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
}