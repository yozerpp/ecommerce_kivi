using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class ProductManager<TP> : IProductManager<TP> where TP : Product, new()
{
    private readonly IRepository<TP> _productRepository;

    public ProductManager(IRepository<TP> productRepository)
    {
        this._productRepository = productRepository;
    }
    public List<TP> Search(ICollection<SellerManager.SearchPredicate> predicates, ICollection<SellerManager.SearchOrder> ordering, int page=0, int pageSize=20)
    {
        // Filter out invalid ordering properties
        var validOrdering = ordering.Where(o => typeof(TP).GetProperty(o.PropName) != null).ToList();
        
        // Build the predicate expression
        Expression<Func<TP, bool>> predicate = p => true;
        
        foreach (var searchPredicate in predicates)
        {
            var propName = searchPredicate.PropName;
            var value = searchPredicate.Value;
            var op = searchPredicate.Operator;
            
            // Get property info to validate it exists
            var propInfo = typeof(TP).GetProperty(propName);
            if (propInfo == null) continue;
            
            Expression<Func<TP, bool>> currentPredicate = null;
            
            if (op == SellerManager.SearchPredicate.OperatorType.Equals)
            {
                currentPredicate = p => propInfo.GetValue(p) != null && propInfo.GetValue(p).ToString() == value;
            }
            else if (op == SellerManager.SearchPredicate.OperatorType.Like)
            {
                currentPredicate = p => propInfo.GetValue(p) != null && propInfo.GetValue(p).ToString().Contains(value);
            }
            else
            {
                // Numeric comparisons
                if (!decimal.TryParse(value, out var numValue))
                    throw new ArgumentException("Search filter " + propName + " is not a valid number: " + value);
                
                switch (op)
                {
                    case SellerManager.SearchPredicate.OperatorType.GreaterThan:
                        currentPredicate = p => propInfo.GetValue(p) != null && Convert.ToDecimal(propInfo.GetValue(p)) > numValue;
                        break;
                    case SellerManager.SearchPredicate.OperatorType.LessThan:
                        currentPredicate = p => propInfo.GetValue(p) != null && Convert.ToDecimal(propInfo.GetValue(p)) < numValue;
                        break;
                    case SellerManager.SearchPredicate.OperatorType.GreaterThanOrEqual:
                        currentPredicate = p => propInfo.GetValue(p) != null && Convert.ToDecimal(propInfo.GetValue(p)) >= numValue;
                        break;
                    case SellerManager.SearchPredicate.OperatorType.LessThanOrEqual:
                        currentPredicate = p => propInfo.GetValue(p) != null && Convert.ToDecimal(propInfo.GetValue(p)) <= numValue;
                        break;
                }
            }
            
            if (currentPredicate != null)
            {
                var oldPredicate = predicate;
                predicate = p => oldPredicate.Compile()(p) && currentPredicate.Compile()(p);
            }
        }
        
        // Build ordering expressions
        Expression<Func<TP, object>>[] orderByExpressions = null;
        if (validOrdering.Any())
        {
            orderByExpressions = validOrdering.Select(o =>
            {
                var propInfo = typeof(TP).GetProperty(o.PropName);
                Expression<Func<TP, object>> expr = p => propInfo.GetValue(p);
                return expr;
            }).ToArray();
        }
        
        return _productRepository.Where(predicate, page * pageSize, pageSize, orderByExpressions);
    }
}
