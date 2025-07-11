using System.Linq.Expressions;
using System.Reflection;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

namespace Ecommerce.Bl;

public static class SearchExpressionUtils
{
    public static void Build<T>(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering,
        out Expression<Func<T, bool>> predicateExpr, out ICollection<Expression<Func<T, object>>> orderByExpressions) {
        var param = Expression.Parameter(typeof(T), "t");
        predicateExpr = PredicateExpression<T>(predicates, param);
        orderByExpressions = OrderByExpression<T>(ordering, param);
    }
    public static ICollection<Expression<Func<T, object>>> OrderByExpression<T>(ICollection<SearchOrder> orders,
        ParameterExpression parameter) {
        var ret = new List<Expression<Func<T, object>>>();
        foreach (var searchOrder in orders){
            var property = typeof(Product).GetProperty(searchOrder.PropName);
            if (property == null) continue;
            var left = Expression.Property(parameter, property);
            Expression<Func<T, object>> orderByExpression = Expression.Lambda<Func<T, object>>(Expression.Convert(left, typeof(object)), parameter);
            ret.Add(orderByExpression);
        }

        return ret;
    }
    public static Expression<Func<T, bool>> PredicateExpression<T>(ICollection<SearchPredicate> predicates, ParameterExpression param) {
        Expression? checks = null;
        if(predicates.Count==0){
            checks = Expression.Constant(true);
        }
        foreach (var predicate in predicates){
            MemberExpression left;
            PropertyInfo property;
            if (predicate.PropName.Contains('_')){
                var splt = predicate.PropName.Split('_');
                left = Expression.Property(param, splt[0]);
                property = typeof(Product).GetProperty(splt[0]);
                if (property == null)break;
                bool cont = false;
                foreach (var nav in splt.Skip(1)){
                    property = property.PropertyType.GetProperty(nav);
                    if (property == null){
                        cont = true; break;
                    }
                    left = Expression.Property(left, property);                            
                }
                if (cont) continue;
            }
            else{
                property = typeof(Product).GetProperty(predicate.PropName);
                if (property == null) continue;
                left = Expression.Property(param, property);
            }
            ConstantExpression right;
            if (predicate.Value == null || predicate.Value.Equals("null"))
                right = Expression.Constant(null);
            else{
                switch (Type.GetTypeCode(property.PropertyType)){
                    case TypeCode.UInt16:
                    case TypeCode.UInt64:
                    case TypeCode.UInt32:
                        if (!UInt32.TryParse(predicate.Value, out UInt32 value1))
                            continue;
                        right = Expression.Constant(value1);
                        break;
                    case TypeCode.Int16:
                    case TypeCode.Int64:
                    case TypeCode.Int32:
                        if (!Int32.TryParse(predicate.Value, out int value2))
                            continue;
                        right = Expression.Constant(value2);
                        break;
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                        if (!decimal.TryParse(predicate.Value, out decimal value3))
                            continue;
                        right = Expression.Constant(value3);
                        break;
                    case TypeCode.Boolean:
                        if (!bool.TryParse(predicate.Value, out bool boolVal))
                            continue;
                        right = Expression.Constant(boolVal);
                        break;
                    case TypeCode.Char:
                        if (!char.TryParse(predicate.Value, out char charVal))
                            continue;
                        right = Expression.Constant(charVal);
                        break;
                    case TypeCode.String:
                        right = Expression.Constant(predicate.Value);
                        break;
                    case TypeCode.Object: //complex property, should be covered by the initial traversal property traversal.
                    default:
                        throw new Exception("Unsupported property type for predicate: " + property.PropertyType);
                }
            }
            Expression check;
            switch (predicate.Operator){
                case SearchPredicate.OperatorType.Equals:
                    check = Expression.Equal(left, right);
                    break;
                case SearchPredicate.OperatorType.Like:
                    if (property.PropertyType != typeof(string))
                        throw new Exception("Like operator can only be used with string properties.");
                    var method = typeof(string).GetMethod("Contains", [typeof(string)]);
                    if (method == null)
                        throw new Exception("String.Contains method not found.");
                    check = Expression.Call(left, method, right);
                    break;
                case SearchPredicate.OperatorType.GreaterThan:
                    check =Expression.GreaterThan(left, right);
                    break;
                case SearchPredicate.OperatorType.GreaterThanOrEqual:
                    check = Expression.GreaterThanOrEqual(left, right);
                    break;
                case SearchPredicate.OperatorType.LessThan:
                    check = Expression.LessThan(left, right);
                    break;
                case SearchPredicate.OperatorType.LessThanOrEqual:
                    check = Expression.LessThanOrEqual(left, right);
                    break;
                default:
                    throw new Exception("Unsupported operator: " + predicate.Operator);
            }
            checks = checks == null ? check : Expression.AndAlso(checks, check);
        }
        var predicateExpression = Expression.Lambda<Func<T, bool>>(checks!, param);
        return predicateExpression;
    }
}