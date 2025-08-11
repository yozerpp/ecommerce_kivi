namespace Ecommerce.Entity.Events;

public class DiscountNotification : Notification
{
    public uint SellerId {get;set;}
    public uint ProductId {get;set;}
    public ProductOffer ProductOffer {get;set;}
    public decimal DiscountAmount {get;set;}
    public decimal DiscountRate { get; set; }
    public Customer Customer {get;set;} 
    protected bool Equals(DiscountNotification other) {
        return base.Equals(other) && SellerId == other.SellerId && ProductId == other.ProductId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || ProductId!=default&&SellerId != default && obj is DiscountNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if(ProductId==default && SellerId == default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), SellerId, ProductId);
    }
}