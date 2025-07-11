namespace Ecommerce.Entity;

public class ReviewComment
{
    public int ProductId { get; set; }
    public int SellerId { get; set; }
    public int RaterId { get; set; }
    public ProductReview Review { get; set; }
    public int CommenterId { get; set; }
    public User Commenter { get; set; }
    public string Comment { get; set; }
    public override bool Equals(object? obj) {
        if (obj is not ReviewComment reviewComment) return false;
        if (ProductId == default&& SellerId ==default&&RaterId==default&&CommenterId==default) return ReferenceEquals(this,reviewComment);
        return ProductId == reviewComment.ProductId && SellerId == reviewComment.SellerId
            && CommenterId == reviewComment.CommenterId&& RaterId == reviewComment.RaterId;
    }
    
    public override int GetHashCode() {
        if (ProductId == default&& SellerId ==default&&RaterId==default&&CommenterId==default) 
            return base.GetHashCode();
        return HashCode.Combine(ProductId, SellerId, RaterId, CommenterId);
    }
}