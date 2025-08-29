using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Shipping.Entity;

public class Provider
{
    public uint Id { get; set; }
    public string? ApiId { get; set; }
    [ShopName]
    public string Name { get; set; }
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}