using System.Linq.Expressions;

namespace Ecommerce.WebImpl.Middleware;

public class AuthorizeByPropertyAttribute(Func<object, object?> left, Func<object, object?>? right = null, bool acceptNullEquality= false) : Attribute
{
    public bool Check(object handlerInstance, object? rightValue = null) {
        return left.Invoke(handlerInstance)?.Equals(rightValue ?? right.Invoke(handlerInstance)) ?? acceptNullEquality;        
    }
}