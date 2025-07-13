using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Entity;

public class Coupon
{
    [MaxLength(10),MinLength(6)]
    public string Id { get; set; }
    public uint? SellerId { get; set; }
    public Seller? Seller { get; set; }
    public DateTime ExpirationDate { get; set; } = DateTime.Now + TimeSpan.FromDays(7);
    public float DiscountRate { get; set; }
    public override bool Equals(object? obj) {
        if (obj is not Coupon coupon) return false;
        if (Id ==default) return ReferenceEquals(this,obj);
        return Id .Equals(coupon.Id);
    }
}