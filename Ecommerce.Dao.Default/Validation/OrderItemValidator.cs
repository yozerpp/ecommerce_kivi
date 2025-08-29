using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class OrderItemValidator : Spi.IValidator<OrderItem>
{
    private readonly IRepository<Coupon> _couponRepository;
    public OrderItemValidator(IRepository<Coupon> couponRepository) {
        _couponRepository = couponRepository;
    }
    public ValidationResult Validate(OrderItem entity) {
        var cid = entity.CouponId;
        var sid =  entity.SellerId;
        var res = _couponRepository.FirstP(c => new{
            C1 = c.ExpirationDate < DateTime.Now,
            C2 = c.SellerId == sid
        }, coupon => coupon.Id == cid, nonTracking:true);
        if(!res.C1)
            return new ValidationResult("Coupon used for this item is expired.", [nameof(Coupon.ExpirationDate)]);
        if (!res.C2)
            return new ValidationResult("Owner of the coupon must be the same as the owner of the product.", [nameof(Coupon.SellerId)]);
        return new ValidationResult(null); 
    }
}