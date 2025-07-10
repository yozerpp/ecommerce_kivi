using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        
        // Build the predicate expression tree
        var parameter = Expression.Parameter(typeof(TP), "p");
        Expression combinedExpression = Expression.Constant(true);
        
        foreach (var searchPredicate in predicates)
        {
            var propName = searchPredicate.PropName;
            var value = searchPredicate.Value;
            var op = searchPredicate.Operator;
            
            // Get property info to validate it exists
            var propInfo = typeof(TP).GetProperty(propName);
            if (propInfo == null) continue;
            
            // Create property access expression
            var propertyAccess = Expression.Property(parameter, propInfo);
            Expression currentExpression = null;
            
            if (op == SellerManager.SearchPredicate.OperatorType.Equals)
            {
                // Handle string equality
                if (propInfo.PropertyType == typeof(string))
                {
                    var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
                    var equalsCheck = Expression.Equal(propertyAccess, Expression.Constant(value));
                    currentExpression = Expression.AndAlso(nullCheck, equalsCheck);
                }
                else
                {
                    // Convert property to string and compare
                    var toStringMethod = typeof(object).GetMethod("ToString");
                    var propertyAsString = Expression.Call(Expression.Convert(propertyAccess, typeof(object)), toStringMethod);
                    var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                    var equalsCheck = Expression.Equal(propertyAsString, Expression.Constant(value));
                    currentExpression = Expression.AndAlso(nullCheck, equalsCheck);
                }
            }
            else if (op == SellerManager.SearchPredicate.OperatorType.Like)
            {
                // Handle string contains
                if (propInfo.PropertyType == typeof(string))
                {
                    var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var containsCheck = Expression.Call(propertyAccess, containsMethod, Expression.Constant(value));
                    currentExpression = Expression.AndAlso(nullCheck, containsCheck);
                }
                else
                {
                    // Convert property to string and check contains
                    var toStringMethod = typeof(object).GetMethod("ToString");
                    var propertyAsString = Expression.Call(Expression.Convert(propertyAccess, typeof(object)), toStringMethod);
                    var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var containsCheck = Expression.Call(propertyAsString, containsMethod, Expression.Constant(value));
                    currentExpression = Expression.AndAlso(nullCheck, containsCheck);
                }
            }
            else
            {
                // Numeric comparisons
                if (!decimal.TryParse(value, out var numValue))
                    throw new ArgumentException("Search filter " + propName + " is not a valid number: " + value);
                
                var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                
                // Convert property to decimal for comparison
                Expression propertyAsDecimal;
                if (propInfo.PropertyType == typeof(decimal) || propInfo.PropertyType == typeof(decimal?))
                {
                    propertyAsDecimal = propertyAccess;
                }
                else
                {
                    var convertMethod = typeof(Convert).GetMethod("ToDecimal", new[] { typeof(object) });
                    propertyAsDecimal = Expression.Call(convertMethod, Expression.Convert(propertyAccess, typeof(object)));
                }
                
                var valueConstant = Expression.Constant(numValue);
                Expression comparisonExpression = null;
                
                switch (op)
                {
                    case SellerManager.SearchPredicate.OperatorType.GreaterThan:
                        comparisonExpression = Expression.GreaterThan(propertyAsDecimal, valueConstant);
                        break;
                    case SellerManager.SearchPredicate.OperatorType.LessThan:
                        comparisonExpression = Expression.LessThan(propertyAsDecimal, valueConstant);
                        break;
                    case SellerManager.SearchPredicate.OperatorType.GreaterThanOrEqual:
                        comparisonExpression = Expression.GreaterThanOrEqual(propertyAsDecimal, valueConstant);
                        break;
                    case SellerManager.SearchPredicate.OperatorType.LessThanOrEqual:
                        comparisonExpression = Expression.LessThanOrEqual(propertyAsDecimal, valueConstant);
                        break;
                }
                
                if (comparisonExpression != null)
                {
                    currentExpression = Expression.AndAlso(nullCheck, comparisonExpression);
                }
            }
            
            if (currentExpression != null)
            {
                combinedExpression = Expression.AndAlso(combinedExpression, currentExpression);
            }
        }
        
        var predicate = Expression.Lambda<Func<TP, bool>>(combinedExpression, parameter);
        
        // Build ordering expressions
        Expression<Func<TP, object>>[] orderByExpressions = null;
        if (validOrdering.Any())
        {
            orderByExpressions = validOrdering.Select(o =>
            {
                var propInfo = typeof(TP).GetProperty(o.PropName);
                var param = Expression.Parameter(typeof(TP), "p");
                var propertyAccess = Expression.Property(param, propInfo);
                var converted = Expression.Convert(propertyAccess, typeof(object));
                return Expression.Lambda<Func<TP, object>>(converted, param);
            }).ToArray();
        }
        
        return _productRepository.Where(predicate, page * pageSize, pageSize, orderByExpressions);
    }
}
