using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Payment
{
    public uint Id { get; set; }
    
    public string? TransactionId { get; set; }
    public uint OrderId { get; set; }
    public Order Order { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }
}
