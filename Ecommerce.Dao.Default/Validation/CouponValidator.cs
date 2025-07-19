using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class CouponValidator: IValidator<Coupon>
{
    public ValidationResult Validate(Coupon entity) {
        if(entity.ExpirationDate==null || entity.ExpirationDate < DateTime.Now) {
            return new ValidationResult("Coupon expiration date must be in the future.");
        }
        if (entity.DiscountRate <= 0){
            return new ValidationResult("Discount rate must be greater than zero.");
        }
        return new ValidationResult(null);
    }
}