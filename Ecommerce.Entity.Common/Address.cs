using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Entity.Common;
[ComplexType]
public class Address
{
    public static Address Empty = new(){Line1 = "", Line2 = "", District = "", City = "", Country = "", ZipCode = ""};
    public string Line1 { get; set; }
    public string Line2 { get; set; } = "";
    public string District { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string ZipCode { get; set; }
    public override string ToString() {
        return $"{Line1}\n{Line2} {District}/{City} {Country} {ZipCode}";
    }
}