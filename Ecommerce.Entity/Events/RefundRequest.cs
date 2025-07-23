namespace Ecommerce.Entity.Events;

public class RefundRequest : Request
{
    public uint OrderId { get; set; }
    public uint SellerId { get; set; }
    public uint ProductId { get; set; }
    public OrderItem Item { get; set; }
    protected bool Equals(RefundRequest other) {
        return base.Equals(other) && OrderId == other.OrderId && SellerId == other.SellerId && ProductId == other.ProductId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || OrderId!=default&& SellerId!=default&& ProductId!=default&& obj is RefundRequest other && Equals(other);
    }

    public override int GetHashCode() {
        if (OrderId == default && SellerId == default && ProductId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), OrderId, SellerId, ProductId);
    }
}