using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Entity.Common;
[ComplexType]
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Neighborhood { get; set; }
    public string ZipCode { get; set; }
 }