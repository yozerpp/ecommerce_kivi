using System.Linq.Expressions;
using System.Reflection;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

namespace Ecommerce.Bl;

public static class SearchExpressionUtils
{
    public static void Build<T>(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering,
        out Expression<Func<T, bool>> predicateExpr, out ICollection<(Expression<Func<T, object>>,bool)> orderByExpressions) {
        var param = Expression.Parameter(typeof(T), "t");
        predicateExpr = PredicateExpression<T>(predicates, param);
        orderByExpressions = OrderByExpression<T>(ordering, param);
    }
    public static ICollection<(Expression<Func<T, object>>,bool)> OrderByExpression<T>(ICollection<SearchOrder> orders,
        ParameterExpression parameter) {
        var ret = new List<(Expression<Func<T, object>>,bool)>();
        foreach (var searchOrder in orders){
            var property = typeof(T).GetProperty(searchOrder.PropName);
            if (property == null) continue;
            var left = Expression.Property(parameter, property);
            Expression<Func<T, object>> orderByExpression = Expression.Lambda<Func<T, object>>(Expression.Convert(left, typeof(object)), parameter);
            ret.Add((orderByExpression, searchOrder.Ascending));
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
                property = typeof(T).GetProperty(splt[0]);
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
                property = typeof(T).GetProperty(predicate.PropName);
                if (property == null) continue;
                left = Expression.Property(param, property);
                if(Nullable.GetUnderlyingType(property.PropertyType)!=null)
                    left = Expression.Property(left, property.PropertyType.GetProperty("Value")!);
            }
            ConstantExpression? right;
            if (predicate.Value == null || predicate.Value.Equals("null"))
                right = Expression.Constant(null);
            else{
                right = GetConstant<T>(property.PropertyType, predicate);
            }
            if(right==null) continue;
            Expression check;
            switch (predicate.Operator){
                case SearchPredicate.OperatorType.Equals:
                    check = Expression.Equal(left, right);
                    break;
                case SearchPredicate.OperatorType.Like:
                    if (property.PropertyType != typeof(string))
                        throw new Exception("Like operator can only be used with string properties.");
                    var method = typeof(string).GetMethod("Contains", [typeof(string)]);
                    check = Expression.Condition(Expression.NotEqual(left, Expression.Constant(null)),
                        Expression.Call(left, method,right),
                        Expression.Constant(false));
                    
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

    private static ConstantExpression? GetConstant<T>(Type? property, SearchPredicate predicate) {
        ConstantExpression? right;
        switch (Type.GetTypeCode(property)){
            case TypeCode.UInt16:
            case TypeCode.UInt64:
            case TypeCode.UInt32:
                if (!UInt32.TryParse(predicate.Value, out UInt32 value1))
                    return null;
                right = Expression.Constant(value1);
                break;
            case TypeCode.Int16:
            case TypeCode.Int64:
            case TypeCode.Int32:
                if (!Int32.TryParse(predicate.Value, out int value2))
                    return null;
                right = Expression.Constant(value2);
                break;
            case TypeCode.Single:
                if(!float.TryParse(predicate.Value, out float value5))
                    return null;
                right = Expression.Constant(value5);
                break;
            case TypeCode.Double:
                if(!double.TryParse(predicate.Value, out double value4))
                    return null;
                right= Expression.Constant(value4);
                break;
            case TypeCode.Decimal:
                if (!decimal.TryParse(predicate.Value, out decimal value3))
                    return null;
                right = Expression.Constant(value3);
                break;
            case TypeCode.Boolean:
                if (!bool.TryParse(predicate.Value, out bool boolVal))
                    return null;
                right = Expression.Constant(boolVal);
                break;
            case TypeCode.Char:
                if (!char.TryParse(predicate.Value, out char charVal))
                    return null;
                right = Expression.Constant(charVal);
                break;
            case TypeCode.String:
                right = Expression.Constant(predicate.Value);
                break;
            case TypeCode.Object: //complex property, should be covered by the initial traversal property traversal.
            default:
                Type underLyingType;
                if ((underLyingType = Nullable.GetUnderlyingType(property)) == null)
                    throw new Exception("Unsupported property type for predicate: " + property);
                right =  GetConstant<T>(underLyingType, predicate);
                
                break;
        }

        return right;
    }
}