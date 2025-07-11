namespace Ecommerce.Entity;

public class Coupon
{
    public string Id { get; set; }
    public int? SellerId { get; set; }
    public Seller? Seller { get; set; }
    public DateTime ExpirationDate { get; set; }
    public float DiscountRate { get; set; }
    public override bool Equals(object? obj) {
        if (obj is not Coupon coupon) return false;
        if (Id ==default) return ReferenceEquals(this,obj);
        return Id .Equals(coupon.Id);
    }
}