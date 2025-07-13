using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class CartItemValidator : IValidator<CartItem>
{
    private readonly IRepository<Coupon> _repository;

    public CartItemValidator(IRepository<Coupon> repository) {
        this._repository = repository;
    }
    public ValidationResult Validate(CartItem entity) {
        if (entity.Coupon?.SellerId != null &&
            entity.Coupon.SellerId != entity.SellerId)
            return new ValidationResult("Owner of the coupon must be the same as the owner of the product.", [nameof(Coupon.SellerId)]);
        return new ValidationResult(null);
    }
}