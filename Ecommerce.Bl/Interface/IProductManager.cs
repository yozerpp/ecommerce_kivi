using System.Text.RegularExpressions;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface IProductManager
{
    public ICollection<Category> GetCategories(bool includeChildren = true, bool includeProperties = false);
    public void VisitCategory(SessionVisitedCategory category);
    public Category? GetCategoryById(uint id, bool includeChildren = false, bool includeProperties = true);
    public ICollection<Category> GetCategoriesByName(string name, bool includeChildren = false,
        bool includeProperties = true);

    public List<Product> Search(string predicateQuery, ICollection<SearchOrder> orders, bool includeImage = false, bool fetchReviews=false, bool fetchOffers=false, int page = 1, int pageSize = 20);
    public List<Product> Search(ICollection<SearchPredicate> predicates,
        ICollection<SearchOrder> ordering, bool includeImage = false, bool fetchReviews = false,
        bool fetchOffers = false, int page = 1, int pageSize = 20);
    public ICollection<ProductFavor> GetFavorites(Customer customer);
    public ICollection<ProductFavor> GetFavorers(uint productId);
    public ICollection<Product> GetMoreProductsFromCategories(Session session, int page = 1,
        int pageSize = 20);
    public bool Favor(ProductFavor favor);
    public Product? GetByIdWithAggregates(uint productId, bool fetchOffers = false, bool fetchReviews = true, bool fetchImage=true);
    public ICollection<ProductOffer> GetOffers(uint? productId = null, uint? sellerId = null, bool includeAggregates = true);
    public Product? GetById(uint id, bool withOffers = true);
    public void UnlistOffer(ProductOffer offer);
    public void Delete(Product product);
    public static (ICollection<SearchPredicate> preds, ICollection<SearchOrder> orders) ParseQuery(string query) {
        ICollection<SearchPredicate> preds=[];
        ICollection<SearchOrder> orders= [];
        if (query.Length > 0){
            var filter = query.Split('&');
            var order = filter[filter.Length - 1].Split("&&");
            filter[filter.Length - 1] = order.Length > 1 ? order[0] : filter[filter.Length - 1];
            order = order.Skip(order.Length>1?1:0).ToArray();
            preds = GetPredicates(filter);
            orders = GetOrdering(order);
        }

        return (preds, orders);
    }

    private static ICollection<SearchOrder> GetOrdering(string[] orderings)
    {
        var result = new List<SearchOrder>();
        foreach (var ordering in orderings)
        {
            var s = ordering.Split(',');
            bool desc = true;
            string propName;
            if (s.Length == 1) propName = s[0];
            else
            {
                propName = s[0];
                if (s[1].Equals("DESC")) desc = true;
                else if (s[1].Equals("ASC")) desc = false;
                else continue;
            }
            result.Add(new SearchOrder() { Ascending = !desc, PropName = propName });
        }

        return result;
    }

    private static ICollection<SearchPredicate> GetPredicates(string[] filter)
    {
        var predicates = new List<SearchPredicate>();
        foreach (var pred in filter)
        {
            var regex = new Regex("\\w+((<)|(>)|(=)|(%)|(>=)|(<=))\\w+");
            var m = regex.Match(pred);
            string op;
            SearchPredicate.OperatorType operatorType;
            if (m.Groups[2].Success)
            {
                operatorType = SearchPredicate.OperatorType.LessThan;
                op = m.Groups[2].Value;
            }
            else if (m.Groups[3].Success)
            {
                operatorType = SearchPredicate.OperatorType.GreaterThan;
                op = m.Groups[3].Value;
            }
            else if (m.Groups[4].Success)
            {
                operatorType = SearchPredicate.OperatorType.Equals;
                op = m.Groups[4].Value;
            }
            else if (m.Groups[5].Success)
            {
                operatorType = SearchPredicate.OperatorType.Like;
                op = m.Groups[5].Value;
            }
            else if (m.Groups[6].Success)
            {
                operatorType = SearchPredicate.OperatorType.GreaterThanOrEqual;
                op = m.Groups[6].Value;
            }
            else if (m.Groups[7].Success)
            {
                operatorType = SearchPredicate.OperatorType.LessThanOrEqual;
                op = m.Groups[7].Value;
            }
            else continue;
            var s = pred.Split(op);
            predicates.Add(new SearchPredicate() { Operator = operatorType, PropName = s[0], Value = s[1] });
        }

        return predicates;
    }
}

public static class OperatorTypeExtensions
{
    public static bool IsNumericOperator(this SearchPredicate.OperatorType op) {
        return (op & (SearchPredicate.OperatorType.LessThan | SearchPredicate.OperatorType.LessThanOrEqual |
                      SearchPredicate.OperatorType.GreaterThan | SearchPredicate.OperatorType.GreaterThanOrEqual)) != 0;
    }
}
public class SearchPredicate
{
    [Flags]
    public enum OperatorType
    {
        Equals = 1,
        GreaterThan = 2,
        GreaterThanOrEqual = 4,
        LessThan = 8,
        LessThanOrEqual = 16,
        Like = 32
    }
    public string PropName { get; set; }
    public string Value { get; set; }
    public OperatorType Operator { get; set;}
    public TypeCode CastType { get; set; } = default(TypeCode);
}

public class SearchOrder
{
    public string PropName { get; set; }
    public bool Ascending { get; set; }
}
