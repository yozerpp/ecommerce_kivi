namespace Ecommerce.Entity;

public class ProductReview
{
    public ulong Id { get; set; }
    public ulong SessionId { get; set; }
    public uint SellerId { get; set;}
    public uint ProductId { get; set; }
    public uint? ReviewerId { get; set;}
    public decimal Rating { get; set; }
    public DateTime Created { get; set; }
    public string? Comment { get; set; }
    public string? Name { get; set; }
    public Session Session { get; set; }
    public ProductOffer Offer { get; set; }
    public Customer? Reviewer { get; set; }
    public bool CensorName { get; set; }
    public bool HasBought { get; set; }
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
    public override bool Equals(object? obj) {
        if (obj is not ProductReview review) return false;
        if(Id==default && (ProductId==default || SellerId==default||SessionId==default) )
            return ReferenceEquals(this,obj);
        return Id == default && (ProductId == default || SellerId == default || SessionId == default);
    }

    public override int GetHashCode() {
        if (Id==default && (SessionId==default || ProductId == default || SellerId == default))
            return base.GetHashCode();
        return HashCode.Combine(Id,ProductId, SellerId, SessionId);
    }
}