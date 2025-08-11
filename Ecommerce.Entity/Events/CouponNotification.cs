using System.Linq.Expressions;

namespace Ecommerce.Entity.Events;

public class CouponNotification : Notification
{
    public uint SellerId { get; set; }
    public string CouponId { get; set; }
    public Seller Seller { get; set; }
    public Coupon Coupon { get; set; }
    public Customer Customer { get; set; }
    protected bool Equals(CouponNotification other) {
        return base.Equals(other) && SellerId == other.SellerId && CouponId == other.CouponId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || SellerId!=default && CouponId!=default && obj is CouponNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if (SellerId == default && CouponId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), SellerId, CouponId);
    }

    public class WithDiscount : CouponNotification
    {
        public decimal Discount { get; set; }
    }
    public static Expression<Func<CouponNotification, WithDiscount>> DiscountProjection = notification =>
        new WithDiscount(){
            Id = notification.Id,
            Coupon = notification.Coupon,
            SellerId = notification.SellerId,
            CouponId = notification.CouponId,
            Seller = notification.Seller,
            UserId = notification.UserId,
            User = notification.User,
            Discount = notification.Coupon.DiscountRate,
        };
}