using System.Linq.Expressions;
using Ecommerce.Entity.Common;

namespace Ecommerce.Entity.Events;

public class OrderNotification : Notification
{
    public uint OrderId { get; set; }
    public uint ProductId { get; set; }
    public OrderItem Item { get; set; }
    public Seller Seller { get; set; }
    protected bool Equals(OrderNotification other) {
        return base.Equals(other) && OrderId == other.OrderId && ProductId == other.ProductId;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || OrderId!=default&& ProductId!=default&& obj is OrderNotification other && Equals(other);
    }

    public override int GetHashCode() {
        if(OrderId==default&& ProductId==default) return base.GetHashCode();
        return HashCode.Combine(base.GetHashCode(), OrderId, ProductId);
    }

    public class WithRelated : OrderNotification
    {
       public Address ShippingAddress { get; set; }
       public ProductOffer Offer { get; set; }
    }
    public static Expression<Func<OrderNotification, WithRelated>> AddressProjection=notification => new WithRelated
        {
            Id = notification.Id,
            UserId = notification.UserId,
            OrderId = notification.OrderId,
            ProductId = notification.ProductId,
            Item = notification.Item,
            ShippingAddress = notification.Item.Order.ShippingAddress,
            Offer = notification.Item.ProductOffer,
        };
}