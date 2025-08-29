using Ecommerce.Entity.Common;

namespace Ecommerce.Shipping.Entity;

public class DeliveryInfo
{
    public string Id { get; set; }
    public Address Address { get; set; }
    public PhoneNumber PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}