using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class ProductManager : IProductManager
{
    private readonly IRepository<Product, DbContext> _productRepository;

    public ProductManager(IRepository<Product, DbContext> productRepository)
    {
        this._productRepository = productRepository;
    }
    public List<Product> Search(ICollection<SellerManager.SearchPredicate> predicates, ICollection<SellerManager.SearchOrder> ordering, int page=0, int pageSize=20)
    {
        ordering = ordering.Where(o => typeof(Product).GetProperty(o.PropName) != null).ToList();
        return _productRepository.Where(p =>
        {
            bool matches = true;
            var t = p.GetType();
            foreach (var predicate in predicates)
            {
                var v = t.GetProperty(predicate.PropName)?.GetValue(p);
                bool m = false;
                if (SellerManager.SearchPredicate.OperatorType.Equals == predicate.Operator)
                    m = predicate.Value.Equals(v);
                else if (SellerManager.SearchPredicate.OperatorType.Like == predicate.Operator)
                {
                    m = predicate.Value.Contains(v?.ToString() ?? string.Empty);
                }
                else
                {
                    if (v == null) m = false;
                    else if (decimal.TryParse(predicate.Value, out var val))
                        throw new ArgumentException("Search filter " + predicate.PropName + " is not a valid number: " +
                                                    predicate.Value);
                    else
                    {
                        switch (predicate.Operator)
                        {
                            case SellerManager.SearchPredicate.OperatorType.Equals: m = val == (decimal)v!; break;
                            case SellerManager.SearchPredicate.OperatorType.GreaterThan: m = val > (decimal)v!; break;
                            case SellerManager.SearchPredicate.OperatorType.LessThan: m = val < (decimal)v!; break;
                            case SellerManager.SearchPredicate.OperatorType.GreaterThanOrEqual : m = val >= (decimal)v!; break;
                            case SellerManager.SearchPredicate.OperatorType.LessThanOrEqual: m = val <= (decimal)v!; break;
                        }
                    }
                }

                matches = matches && m;
            }

            return matches;
        }, page * pageSize, (page+1)*pageSize,p =>
        {
            return ordering.Select(o => p.GetType().GetProperty(o.PropName).GetValue(p));
        });
    }
}
