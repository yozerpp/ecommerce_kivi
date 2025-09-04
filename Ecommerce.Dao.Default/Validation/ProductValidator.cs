using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;

namespace Ecommerce.Dao.Default.Validation;

public class ProductValidator : IValidator<Product>
{
    private readonly IRepository<Category> _categoryRepository;

    public ProductValidator(IRepository<Category> categoryRepository) {
        _categoryRepository = categoryRepository;
    }

    public ValidationResult Validate(Product entity) {
        var category = entity.Category ?? _categoryRepository.First(c => c.Id == entity.CategoryId, includes:[[nameof(Category.CategoryProperties)]], nonTracking:true);
        // if(entity.CategoryProperties==null && category.CategoryProperties.Any(p=>p.IsRequired)) return new ValidationResult("Category Properties cannot be null.");
        // foreach (var catProp in category.CategoryProperties){
        //     object? val;
        //     if ((val = entity.CategoryProperties.FirstOrDefault(p=>p.CategoryPropertyId == catProp.Id)) == null&&catProp.IsRequired)
        //         return new ValidationResult("Product is missing required category property: " + catProp.PropertyName);
        //     if(catProp.EnumValues!=null && !catProp.EnumValues.Contains(val.ToString()))
        //         return new ValidationResult("Product has invalid value for category property: " + catProp.PropertyName + ". Valid values are: " + string.Join(", ", catProp.EnumValues));
        // }
        return new  ValidationResult(null);
    }
}