using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;

namespace Ecommerce.Entity;

public class ProductOffer
{
    public uint SellerId { get; set; }
    public uint ProductId { get; set; }
    public Seller? Seller { get; set; }
    public Product? Product { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountedPrice => Price * Discount;
    public uint Stock { get; set; }
    public OfferStats? Stats { get; set; }
    public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<OrderItem> BoughtItems { get; set; } = new List<OrderItem>();
    public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
    public override bool Equals(object? obj)
    {
        if (obj is ProductOffer other){
            if (ProductId == default || SellerId == default) return ReferenceEquals(this, other);
            return ProductId == other.ProductId && SellerId == other.SellerId;
        }
        return false;
    }


    public override int GetHashCode()
    {
        if(ProductId==default || SellerId==default) return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId);
    }
}
