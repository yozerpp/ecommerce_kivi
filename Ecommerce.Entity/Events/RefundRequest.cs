namespace Ecommerce.Entity.Events;

public class RefundRequest : Request
{
    public uint OrderId { get; set; }
    public uint ProductId { get; set; }
    public OrderItem Item { get; set; }
    public Customer? Customer { get; set; }
    public Seller Seller { get; set; }
    protected bool Equals(RefundRequest other) {
        return base.Equals(other) && OrderId == other.OrderId && ProductId == other.ProductId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || OrderId!=default&& ProductId!=default&& obj is RefundRequest other && Equals(other);
    }

    public override int GetHashCode() {
        if (OrderId == default && ProductId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), OrderId, ProductId);
    }
}