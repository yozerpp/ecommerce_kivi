using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Entity.Common;

[ComplexType]
public class PhoneNumber
{
    public int CountryCode { get; set; }
    public string Number { get; set;}
}