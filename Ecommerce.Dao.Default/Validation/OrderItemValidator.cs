using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using FluentValidation;
using FluentValidation.Results;

namespace Ecommerce.Dao.Default.Validation;

public class OrderItemValidator : AbstractValidator<OrderItem>
{
    private readonly IRepository<Coupon> _repository;
    public OrderItemValidator(IRepository<Coupon> repository) {
        this._repository = repository;
    }
    public override ValidationResult Validate(ValidationContext<OrderItem> context) {
        if (context.InstanceToValidate.Coupon != null &&
            context.InstanceToValidate.Coupon.SellerId != context.InstanceToValidate.SellerId)
            return new ValidationResult([
                new ValidationFailure("Coupon", "Owner of the coupon must be the same as the owner of the product.")
            ]);
        return new ValidationResult();
    }
}