using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Dao.Spi;

public interface IValidator<T>
{
    public ValidationResult Validate(T entity);
}