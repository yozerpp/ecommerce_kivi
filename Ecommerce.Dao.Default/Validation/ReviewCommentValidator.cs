using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class ReviewCommentValidator : IValidator<ReviewComment>
{
    public ValidationResult Validate(ReviewComment entity) {
        if(entity.Name==null && entity.User==default && entity.UserId == default)
            return new ValidationResult("You must enter a name to comment anonymously.");
        return new ValidationResult(null);
    }
}