using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class ReviewValidator : IValidator<ProductReview>
{
    public ValidationResult Validate(ProductReview entity) {
        if (entity.Name == null && entity.Reviewer == default && entity.ReviewerId == default)
            return new ValidationResult("You must enter a name to review anonymously.");
        return new ValidationResult(null);
    }
}