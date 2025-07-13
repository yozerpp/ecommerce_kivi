using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface IProductManager
{
    public List<ProductWithAggregates> SearchWithAggregates(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering,
       bool includeImage, bool fetchReviews = false, int page = 1, int pageSize = 20);
}
public struct SearchPredicate
{
    public enum OperatorType
    {
        Equals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Like
    }
    public string PropName { get; set; }
    public string Value { get; set; }
    public OperatorType Operator { get; set; }
}

public struct SearchOrder
{
    public string PropName { get; set; }
    public bool Ascending { get; set; }
}