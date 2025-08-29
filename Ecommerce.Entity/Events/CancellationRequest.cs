namespace Ecommerce.Entity.Events;

public class CancellationRequest : Request
{
    public uint OrderId { get; set; }
    public Staff? Staff { get; set; }
    public Order Order { get; set; }
    public Customer Customer { get; set; }
}