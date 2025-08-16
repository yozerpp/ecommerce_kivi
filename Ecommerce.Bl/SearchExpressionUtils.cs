using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

namespace Ecommerce.Bl;

public static class SearchExpressionUtils
{
    public static void Build<T>(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering,
        out Expression<Func<T, bool>> predicateExpr, out ICollection<(Expression<Func<T, object>>,bool)> orderByExpressions) {
        var param = Expression.Parameter(typeof(T), "t");
        var rootNode = new MiddleBranchNode(){
            Level = -1,Prop = null!
        };
        ParseNodes(rootNode, typeof(T), predicates);
        var expr = Visit(param,rootNode);
        predicateExpr = (Expression<Func<T, bool>>)Expression.Lambda(expr, param);
        orderByExpressions = OrderByExpression<T>(ordering, param);
    }
    
    public static void Build<T>(string queryString, ICollection<SearchOrder> ordering,
        out Expression<Func<T, bool>> predicateExpr, out ICollection<(Expression<Func<T, object>>,bool)> orderByExpressions)
    {
        var param = Expression.Parameter(typeof(T), "t");
        var parser = new Parser(typeof(T), queryString);
        var rootNode = parser.Parse();
        var expr = Visit(param, rootNode);
        predicateExpr = (Expression<Func<T, bool>>)Expression.Lambda(expr, param);
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
    private interface IPredicateNode
    {
    }
    private abstract class BranchNode : IPredicateNode
    {
        public int GroupIndex { get; init; }
        public required PropertyInfo Prop { get; set; }
        public abstract Expression? Accept(Expression left);
    }
    private class MiddleBranchNode : BranchNode
    {
        public int Level { get; init; }
        public bool And { get; init; }
        public ICollection<BranchNode> Children { get; } =[];
        public BranchNode? this[int i, PropertyInfo p] => Children.FirstOrDefault(c => c.GroupIndex == i && c.Prop == p);
        public override Expression? Accept(Expression left) {
            if (Prop == null) return AggregateChildren(left);
            var propAccess = Expression.Property(left, Prop);
            Expression? ret;
            if (this.Prop.PropertyType.IsGenericType && 
                typeof(ICollection<>).IsAssignableFrom(this.Prop.PropertyType.GetGenericTypeDefinition())) 
                ret = GetAsCollectionPredicate(propAccess);
            else ret = GetAsPropertyPredicate(propAccess);
            if (ret == null) return null;
            return Expression.Condition(Expression.NotEqual(propAccess, Expression.Constant(null)),
                ret, Expression.Constant(false));
        }
        private Expression? GetAsCollectionPredicate(Expression propertyAccess) { //this calls empty any if no children matches
            var t = this.Prop.PropertyType.GetGenericArguments()[0];
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name.Equals(nameof(Enumerable.Any)) && m.GetParameters().Length == 2 &&
                            m.ReturnType == typeof(bool))
                .MakeGenericMethod(t);
            var lambdaParam = Expression.Parameter(t, "t" + this.Level);
            var lambdaBody=AggregateChildren(lambdaParam);
            lambdaBody ??= Expression.Constant(true);
            var arg = Expression.Lambda(lambdaBody, lambdaParam);
            return Expression.Call(anyMethod, propertyAccess, arg);
        }
        private Expression? GetAsPropertyPredicate(Expression propertyAccess) {
            return AggregateChildren(propertyAccess);
        }
        private Expression? AggregateChildren(Expression left) {
            var childExpressions = this.Children.Select(n => Visit(left, n)).ToArray();
            if (childExpressions.All(c => c == null)) return null;
            childExpressions = childExpressions.Where(c => c != null).ToArray();
            var and = childExpressions.First();
            return childExpressions.Skip(1).Aggregate(and, (ands, child) => this.And ?Expression.AndAlso(ands, child!): Expression.Or(ands, child!));
        }
    }
    private class LeafBranchNode : BranchNode
    {
        public required SearchPredicate Comparison { get; init; }
        public override  Expression? Accept(Expression left) {
            left = Expression.Property(left, Prop);
            var error = GetComparison(  left, out var check);
            if (error) return null;
            return !Prop.PropertyType.IsPrimitive ?Expression.Condition(Expression.NotEqual(left, Expression.Constant(null)),
                check, Expression.Constant(false)): check;
        }
        private bool GetComparison(Expression left, out Expression check) {
            Type constantType;
            if ((constantType = Nullable.GetUnderlyingType(Prop.PropertyType)) != null){
                left= Expression.Property(left, Prop.PropertyType.GetProperty("Value")!);
            }
            else constantType = Prop.PropertyType;
            if (Comparison.CastType == default && Comparison.Operator.IsNumericOperator()&&!constantType.IsNumeric())
                Comparison.CastType = TypeCode.Decimal;
            if (Comparison.CastType != default)
                left = CreateConversionExpression(left, GetTypeFromTypeCode(Comparison.CastType));
            Expression? right;
            if (Comparison.Value == null || Comparison.Value.Equals("null"))
                right = Expression.Constant(null);
            else{
                right = GetConstant(Comparison.CastType!=default?GetTypeFromTypeCode(Comparison.CastType):constantType, Comparison);
            }
            if(right==null){
                check = null;
                return true;
            }
            switch (Comparison.Operator){
                case SearchPredicate.OperatorType.Equals:
                    check = Expression.Equal(left, right);
                    break;
                case SearchPredicate.OperatorType.Like:
                    if (constantType != typeof(string))
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
                    throw new Exception("Unsupported operator: " + Comparison.Operator);
            }
            return false;
        }
    }
    
    private class Parser
    {
        private readonly string _query;
        private int _pos;
        private readonly Type _rootType;

        public Parser(Type rootType, string query)
        {
            _rootType = rootType;
            _query = query;
            _pos = 0;
        }

        public MiddleBranchNode Parse()
        {
            var rootNode = new MiddleBranchNode() { Level = -1, Prop = null! };
            _pos = 0; // Reset position for parsing
            ParseBranch(rootNode, _rootType, 0);
            return rootNode;
        }

        private void ParseBranch(MiddleBranchNode parentNode, Type currentType, int level)
        {
            SkipWhitespace();
            if (_pos >= _query.Length) return;

            char operatorChar = _query[_pos];
            if (operatorChar != '&' && operatorChar != '|')
            {
                throw new FormatException($"Expected '&' or '|' at position {_pos}");
            }
            parentNode.And = (operatorChar == '&');
            _pos++; // Consume operator

            SkipWhitespace();
            if (_query[_pos] != '(')
            {
                throw new FormatException($"Expected '(' at position {_pos}");
            }
            _pos++; // Consume '('

            while (_pos < _query.Length)
            {
                SkipWhitespace();
                if (_query[_pos] == ')')
                {
                    _pos++; // Consume ')'
                    return;
                }

                if (_query[_pos] == '&' || _query[_pos] == '|')
                {
                    // Nested branch
                    var newBranchNode = new MiddleBranchNode() { Level = level, Prop = null! }; // Prop will be set later if it's a property
                    parentNode.Children.Add(newBranchNode);
                    ParseBranch(newBranchNode, currentType, level + 1);
                }
                else
                {
                    // Leaf node (predicate)
                    ParseLeaf(parentNode, currentType, level);
                }

                SkipWhitespace();
                if (_query[_pos] == ',')
                {
                    _pos++; // Consume ','
                }
                else if (_query[_pos] != ')')
                {
                    throw new FormatException($"Expected ',' or ')' at position {_pos}");
                }
            }
            throw new FormatException("Unclosed branch in query string.");
        }

        private void ParseLeaf(MiddleBranchNode parentNode, Type currentType, int level)
        {
            var propName = ReadIdentifier();
            SkipWhitespace();

            var operatorType = ReadOperator();
            SkipWhitespace();

            var value = ReadValue();
            
            var searchPredicate = new SearchPredicate
            {
                PropName = propName,
                Operator = operatorType,
                Value = value
            };

            var (pred, labeled) = GetGroups(searchPredicate);
            var currentNode = parentNode;
            var currentPropType = currentType;

            for (int i = 0; i < labeled.Count; i++)
            {
                var (groupIndex, name) = labeled.ElementAt(i);
                var prop = currentPropType.GetProperty(name);
                if (prop == null)
                {
                    // This property path is invalid for the current type, skip this predicate
                    // or throw an error depending on desired behavior.
                    // For now, we'll just break and not add this leaf.
                    return; 
                }

                if (i == labeled.Count - 1)
                {
                    // This is the last part of the path, so it's a LeafBranchNode
                    var leafNode = new LeafBranchNode
                    {
                        GroupIndex = groupIndex,
                        Comparison = pred,
                        Prop = prop
                    };
                    currentNode.Children.Add(leafNode);
                }
                else
                {
                    // This is an intermediate part of the path, so it's a MiddleBranchNode
                    var existingNode = currentNode.Children.FirstOrDefault(c => c.GroupIndex == groupIndex && c.Prop == prop) as MiddleBranchNode;
                    if (existingNode == null)
                    {
                        existingNode = new MiddleBranchNode
                        {
                            Level = level + i,
                            GroupIndex = groupIndex,
                            Prop = prop,
                            And = parentNode.And // Inherit parent's AND/OR logic for nested properties
                        };
                        currentNode.Children.Add(existingNode);
                    }
                    currentNode = existingNode;
                    currentPropType = prop.PropertyType;
                    if (currentPropType.IsGenericType && typeof(ICollection<>).IsAssignableFrom(currentPropType.GetGenericTypeDefinition()))
                    {
                        currentPropType = currentPropType.GetGenericArguments()[0];
                    }
                }
            }
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            int start = _pos;
            while (_pos < _query.Length && (char.IsLetterOrDigit(_query[_pos]) || _query[_pos] == '_' || _query[_pos] == '\''))
            {
                _pos++;
            }
            if (_pos == start) throw new FormatException($"Expected identifier at position {start}");
            return _query.Substring(start, _pos - start);
        }

        private SearchPredicate.OperatorType ReadOperator()
        {
            SkipWhitespace();
            int start = _pos;
            while (_pos < _query.Length && (
                _query[_pos] == '=' || _query[_pos] == '<' || _query[_pos] == '>' || _query[_pos] == '%'
            ))
            {
                _pos++;
            }
            var opStr = _query.Substring(start, _pos - start);
            return opStr switch
            {
                "=" => SearchPredicate.OperatorType.Equals,
                "%" => SearchPredicate.OperatorType.Like,
                "<" => SearchPredicate.OperatorType.LessThan,
                ">" => SearchPredicate.OperatorType.GreaterThan,
                "<=" => SearchPredicate.OperatorType.LessThanOrEqual,
                ">=" => SearchPredicate.OperatorType.GreaterThanOrEqual,
                _ => throw new FormatException($"Unsupported operator '{opStr}' at position {start}")
            };
        }

        private string ReadValue()
        {
            SkipWhitespace();
            int start = _pos;
            // Read until a comma, closing parenthesis, or end of string
            while (_pos < _query.Length && _query[_pos] != ',' && _query[_pos] != ')')
            {
                _pos++;
            }
            if (_pos == start) throw new FormatException($"Expected value at position {start}");
            return _query.Substring(start, _pos - start);
        }

        private void SkipWhitespace()
        {
            while (_pos < _query.Length && char.IsWhiteSpace(_query[_pos]))
            {
                _pos++;
            }
        }
    }

    private static void ParseNodes(in MiddleBranchNode root,Type rootType ,ICollection<SearchPredicate> searchPredicates) {
        foreach (var (pred, labeled) in searchPredicates.Select(GetGroups)){
            var parent = root;
            var type = rootType;
            var i = 0;
            foreach (var valueTuple in labeled){
                var prop = type.GetProperty(valueTuple.Item2);
                if(prop==null) break;
                var cur = parent[valueTuple.Item1, prop]; 
                if (cur == null){
                    if (i == labeled.Count - 1)
                        cur = new LeafBranchNode(){
                            GroupIndex = valueTuple.Item1,
                             Comparison = pred,
                            Prop = prop
                        };
                    else cur = new MiddleBranchNode(){
                            Level = i,
                            GroupIndex = valueTuple.Item1,
                            Prop = prop,
                        };
                    parent.Children.Add(cur);
                }
                if (i < labeled.Count - 1)
                    parent = (MiddleBranchNode)cur;
                type = cur.Prop.PropertyType;
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                i++;
            }
        }
    }

    private static Expression? Visit(Expression left, IPredicateNode right) {
        return right switch{
            LeafBranchNode leaf => VisitLeaf(left, leaf),
            MiddleBranchNode middle => VisitMiddleBranch(left, middle),
            _ => throw new NotSupportedException("Unsupported predicate node type: " + right.GetType().Name)
        };
    }

    
    private static Expression? VisitLeaf(Expression left, LeafBranchNode right) {
        return right.Accept(left);
    }
    private static Expression? VisitMiddleBranch(Expression left, MiddleBranchNode node) {
         return node.Accept(left);
    }

    private static (SearchPredicate, ICollection<(int, string)>) GetGroups(SearchPredicate searchPredicate) {
        return (searchPredicate, searchPredicate.PropName.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(s => {
            var splt = s.Split('\'');
            int groupIndex;
            if (splt.Length == 1) groupIndex =  0;
            else groupIndex = int.Parse(splt[1]);
            return (groupIndex, splt[0]);
        }).ToArray());
    }
    // public static Expression<Func<T, bool>> PredicateExpression<T>(ICollection<SearchPredicate> predicates, ParameterExpression param) {
    //     Expression? checks = null;
    //     if (predicates.Count == 0){
    //         return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), param);
    //     }
    //     var grouped = predicates.GroupBy(p => {
    //         var s = p.PropName.Split('_', StringSplitOptions.RemoveEmptyEntries);
    //         if (s.Length == 1) return p.PropName;
    //         return string.Join('_', s.Take(s.Length - 1));
    //     });
    //     foreach (var group in grouped){ //TODO nested access inside Any, Nested Selects
    //         Expression left;
    //         ParameterExpression? lambdaParam = null;
    //         Expression? groupChecks = null; 
    //         var constantType = typeof(object);
    //         var propName = group.Key.Split('_').First().Split('\'', StringSplitOptions.RemoveEmptyEntries).First(); //second is for group index;
    //         var rootProperty = typeof(T).GetProperty(propName);
    //         foreach (var predicate in group){
    //             if (GetLeft(param,predicate.PropName,rootProperty, out left, ref lambdaParam, ref constantType)) continue;// prop_
    //             if (GetComparison(predicate, null, left, out var check)) continue;
    //             groupChecks = groupChecks == null ? check : Expression.AndAlso(groupChecks, check);
    //         }
    //         if (lambdaParam != null){
    //             var t = rootProperty.PropertyType.GetGenericArguments()[0];
    //                 groupChecks = Expression.Call(null, Expression.Property(param, rootProperty), Expression.Lambda(groupChecks, lambdaParam));
    //         }
    //         checks = checks == null ? groupChecks : Expression.AndAlso(checks, groupChecks);
    //     }
    //     var predicateExpression = Expression.Lambda<Func<T, bool>>(checks!, param);
    //     return predicateExpression;
    // }
    //TODO Only supports one level of grouping
    
    // private static bool GetLeft(ParameterExpression param, string key, PropertyInfo? rootProperty, out Expression left,
    //     ref ParameterExpression? lambdaParam, [DisallowNull] ref Type? constantType) {
    //     var path = key.Split('_', StringSplitOptions.RemoveEmptyEntries);
    //     if (key.Contains('_')){
    //         if(path.Length ==1){
    //             left = null;
    //             return true;
    //         }
    //         left = Expression.Property(param, rootProperty);
    //         if (rootProperty.PropertyType.IsGenericType && typeof(ICollection<>).IsAssignableFrom(rootProperty.PropertyType.GetGenericTypeDefinition())){ 
    //             if(path.Length > 2 )throw new NotImplementedException("Nested queries on collection properties is not implemented");
    //             var t  =rootProperty.PropertyType.GetGenericArguments()[0];
    //             lambdaParam ??= Expression.Parameter(t, "t");
    //             var lambdaProp = t.GetProperty(path[1]);
    //             left = PropertyExpression(lambdaParam, lambdaProp, out constantType);
    //         }
    //         else{
    //             bool cont = false;
    //             foreach (var nav in path.Skip(1)){
    //                 rootProperty = rootProperty.PropertyType.GetProperty(nav);
    //                 if (rootProperty == null){
    //                     cont = true; break;
    //                 }
    //                 left = PropertyExpression(left, rootProperty, out constantType);
    //             }
    //             if (cont) return true;
    //         }
    //     }
    //     else{
    //         if (rootProperty == null){
    //             left = null;
    //             return true;
    //         }
    //         left = PropertyExpression(param, rootProperty, out constantType);
    //     }
    //     return false;
    // }


    private static Expression PropertyExpression(Expression left, PropertyInfo? prop,
        out Type? constantType) {
        left = Expression.Property(left, prop);
        if (Nullable.GetUnderlyingType(prop.PropertyType) != null){
            left = Expression.Property(left, prop.PropertyType.GetProperty("Value")!);
            constantType = Nullable.GetUnderlyingType(prop.PropertyType);
        }
        else constantType = prop.PropertyType;
        return left;
    }

    public static bool IsNumeric(this Type type)
    {
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        TypeCode typeCode = Type.GetTypeCode(underlyingType);
        
        return typeCode == TypeCode.Byte ||
               typeCode == TypeCode.SByte ||
               typeCode == TypeCode.Int16 ||
               typeCode == TypeCode.Int32 ||
               typeCode == TypeCode.Int64 ||
               typeCode == TypeCode.UInt16 ||
               typeCode == TypeCode.UInt32 ||
               typeCode == TypeCode.UInt64 ||
               typeCode == TypeCode.Single ||
               typeCode == TypeCode.Double ||
               typeCode == TypeCode.Decimal;
    }
    public static void Main() {
        var preds = new []{
            new SearchPredicate(){
                PropName =
                    string.Join('_', nameof(Product.CategoryProperties), nameof(ProductCategoryProperties.Value)),
                Operator = SearchPredicate.OperatorType.Like,
                Value = "opt1"
            },
            new SearchPredicate(){
                PropName = string.Join('_', nameof(Product.CategoryProperties),
                    nameof(ProductCategoryProperties.CategoryPropertyId)),
                Operator = SearchPredicate.OperatorType.Equals,
                Value = "2",
            },
            new SearchPredicate(){
                PropName = string.Join('_', nameof(Product.Category), nameof(Category.Id)),
                Operator = SearchPredicate.OperatorType.Equals, Value = "1"
            },
            new SearchPredicate()
                { PropName = nameof(Product.Name), Operator = SearchPredicate.OperatorType.Like, Value = "Like" },
        };
        Build<Product>(preds, [], out var predicateExpr, out var orderByExpr);
        Console.WriteLine(predicateExpr.ToString());
    }
    private static ConstantExpression? GetConstant(Type? property, SearchPredicate predicate) {
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
                right =  GetConstant(underLyingType, predicate);
                
                break;
        }

        return right;
        
    }
        public static Expression CreateConversionExpression(Expression inputExpression, Type targetType)
    {
        if (inputExpression == null) throw new ArgumentNullException(nameof(inputExpression));
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));

        Type sourceType = inputExpression.Type;
        Type underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        Type underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // --- Attempt Parsing if source is string ---
        if (underlyingSourceType == typeof(string))
        {
            MethodInfo? parseMethod = null;

            // Try to find a static Parse(string) method on the underlying target type
            parseMethod = underlyingTargetType.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(string) },
                null
            );

            // Special handling for Enum.Parse
            if (parseMethod == null && underlyingTargetType.IsEnum)
            {
                parseMethod = typeof(Enum).GetMethod(
                    "Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Type), typeof(string), typeof(bool) }, // Enum.Parse(Type, string, bool)
                    null
                );
                if (parseMethod != null)
                {
                    // Call Enum.Parse(targetType, inputExpression, true)
                    Expression enumParseCall = Expression.Call(
                        parseMethod,
                        Expression.Constant(underlyingTargetType),
                        inputExpression,
                        Expression.Constant(true) // ignoreCase
                    );
                    // Wrap in null check if target is nullable
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return Expression.Condition(
                            Expression.Equal(inputExpression, Expression.Constant(null, typeof(string))),
                            Expression.Constant(null, targetType),
                            Expression.Convert(enumParseCall, targetType)
                        );
                    }
                    return Expression.Convert(enumParseCall, targetType); // Convert result to actual enum type
                }
            }

            if (parseMethod != null)
            {
                // If target is nullable (e.g., int?), handle null input string gracefully
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return Expression.Condition(
                        Expression.Equal(inputExpression, Expression.Constant(null, typeof(string))),
                        Expression.Constant(null, targetType), // If input is null, result is null of nullable type
                        Expression.Convert(Expression.Call(parseMethod, inputExpression), targetType) // Parse and convert to nullable
                    );
                }
                // Direct parse for non-nullable target
                return Expression.Call(parseMethod, inputExpression);
            }
        }
        try
        {
            return Expression.Convert(inputExpression, targetType);
        }
        catch (InvalidOperationException ex)
        {
            // Catch specific exceptions from Expression.Convert if the conversion is truly invalid
            throw new NotSupportedException($"Conversion from '{sourceType.Name}' to '{targetType.Name}' is not supported.", ex);
        }
    }
    public static Type? GetTypeFromTypeCode(TypeCode typeCode)
    {
        switch (typeCode)
        {
            case TypeCode.Boolean:  return typeof(bool);
            case TypeCode.Byte:     return typeof(byte);
            case TypeCode.Char:     return typeof(char);
            case TypeCode.DateTime: return typeof(DateTime);
            case TypeCode.Decimal:  return typeof(decimal);
            case TypeCode.Double:   return typeof(double);
            case TypeCode.Int16:    return typeof(short);
            case TypeCode.Int32:    return typeof(int);
            case TypeCode.Int64:    return typeof(long);
            case TypeCode.SByte:    return typeof(sbyte);
            case TypeCode.Single:   return typeof(float);
            case TypeCode.String:   return typeof(string);
            case TypeCode.UInt16:   return typeof(ushort);
            case TypeCode.UInt32:   return typeof(uint);
            case TypeCode.UInt64:   return typeof(ulong);
            case TypeCode.Object:   return typeof(object); // General object type
            case TypeCode.DBNull:   return typeof(DBNull);
            case TypeCode.Empty:    return null; // Represents a null reference, no specific Type
            default:                return null; // Or throw an ArgumentOutOfRangeException
        }
    }
}
