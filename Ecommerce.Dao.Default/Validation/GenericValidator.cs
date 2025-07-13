using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.Dao.Default.Validation;

public class GenericValidator<TE>: IValidator<TE> where TE : class, new()
{
    private readonly IEntityType _entityType;
    public GenericValidator(IModel model) {
        _entityType = model.FindEntityType(typeof(TE))!;
    }
    public ValidationResult Validate(TE entity) {
        var (message, memberNames) =  Validate(entity, _entityType);
        return new ValidationResult(message, memberNames);
    }
    private static (string, IEnumerable<string>) Validate(object entity, ITypeBase entityType) {
        ValidationResult res;
        var messages = String.Empty;
        var memberNames = new List<string>();
        foreach (var property in entityType.GetProperties().Where(p=>!p.IsPrimaryKey()&&!p.IsForeignKey()&&!p.IsShadowProperty())){
            var positive = property.FindAnnotation(nameof(DefaultDbContext.Annotations.Validation_Positive));
            if (positive?.Value != null){
                if ((bool)positive.Value && property.PropertyInfo!.GetValue(entity) is int i && i < 0){
                    messages += property.Name + " cannot be negative. ";
                    memberNames.Add(property.Name);
                }

                if (!(bool)positive.Value && property.PropertyInfo!.GetValue(entity) is int i1 && i1 > 0){
                    messages += property.Name + " cannot be positive. ";
                    memberNames.Add(property.Name);
                }
            }
            int? maxLength;
            if ((maxLength = property.GetMaxLength()) != null &&
                property.PropertyInfo.GetValue(entity) is string s && s.Length > maxLength){
                messages += property.Name + " cannot be longer than " + maxLength + ". ";
                memberNames.Add(property.Name);
            }
        }
        foreach (var cProperty in entityType.GetComplexProperties().Where(p=>!p.IsShadowProperty())){
            var val = cProperty.PropertyInfo!.GetValue(entity);
            if(val==null) continue;
            var (ms, mem) = Validate(val, cProperty.ComplexType);
            messages += ms;
            memberNames.AddRange(mem);
        }
        return (messages, memberNames);
    }

}