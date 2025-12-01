using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Translates LINQ expressions to SQL queries.
/// </summary>
internal class QueryInfo
{
    public string Sql { get; set; } = string.Empty;
    public List<object?> Parameters { get; set; } = new();
    public Expression? SelectExpression { get; set; }
}

internal static class QueryTranslator
{
    public static QueryInfo Translate(Expression expression, string tableName)
    {
        var queryInfo = new QueryInfo();
        var whereClause = new StringBuilder();
        var orderByClause = new StringBuilder();
        int? skip = null;
        int? take = null;
        Expression? selectExpression = null;

        // Visit the expression tree
        var visitor = new QueryExpressionVisitor();
        visitor.Visit(expression);

        // Build WHERE clause
        if (visitor.WhereExpressions.Count > 0)
        {
            var whereParts = new List<string>();
            foreach (var whereExpr in visitor.WhereExpressions)
            {
                // Extract the body from lambda expressions
                Expression body = whereExpr;
                if (whereExpr is LambdaExpression lambdaExpr)
                {
                    body = lambdaExpr.Body;
                }
                
                var wherePart = TranslateWhereExpression(body, queryInfo.Parameters);
                if (!string.IsNullOrEmpty(wherePart))
                {
                    whereParts.Add(wherePart);
                }
            }
            if (whereParts.Count > 0)
            {
                whereClause.Append(string.Join(" AND ", whereParts));
            }
        }

        // Build ORDER BY clause
        if (visitor.OrderByExpressions.Count > 0)
        {
            var orderParts = new List<string>();
            foreach (var orderExpr in visitor.OrderByExpressions)
            {
                var orderPart = TranslateOrderByExpression(orderExpr);
                if (!string.IsNullOrEmpty(orderPart))
                {
                    orderParts.Add(orderPart);
                }
            }
            if (orderParts.Count > 0)
            {
                orderByClause.Append(string.Join(", ", orderParts));
            }
        }

        // Get Skip/Take
        skip = visitor.SkipCount;
        take = visitor.TakeCount;

        // Get Select expression (for projection)
        selectExpression = visitor.SelectExpression;

        // Build SQL
        queryInfo.Sql = QueryBuilder.BuildSelectQuery(
            tableName,
            whereClause.Length > 0 ? whereClause.ToString() : null,
            orderByClause.Length > 0 ? orderByClause.ToString() : null,
            skip,
            take
        );

        queryInfo.SelectExpression = selectExpression;

        return queryInfo;
    }

    public static QueryInfo TranslateCount(Expression expression, string tableName)
    {
        var queryInfo = new QueryInfo();
        var whereClause = new StringBuilder();

        var visitor = new QueryExpressionVisitor();
        visitor.Visit(expression);

        if (visitor.WhereExpressions.Count > 0)
        {
            var whereParts = new List<string>();
            foreach (var whereExpr in visitor.WhereExpressions)
            {
                // Extract the body from lambda expressions
                Expression body = whereExpr;
                if (whereExpr is LambdaExpression lambdaExpr)
                {
                    body = lambdaExpr.Body;
                }
                
                var wherePart = TranslateWhereExpression(body, queryInfo.Parameters);
                if (!string.IsNullOrEmpty(wherePart))
                {
                    whereParts.Add(wherePart);
                }
            }
            if (whereParts.Count > 0)
            {
                whereClause.Append(string.Join(" AND ", whereParts));
            }
        }

        queryInfo.Sql = QueryBuilder.BuildCountQuery(
            tableName,
            whereClause.Length > 0 ? whereClause.ToString() : null
        );

        return queryInfo;
    }

    private static string TranslateWhereExpression(Expression expression, List<object?> parameters)
    {
        // Handle UnaryExpression (e.g., Convert, Quote) - unwrap it
        if (expression is UnaryExpression unaryExpr)
        {
            // If it's a Quote (lambda), extract the operand
            if (unaryExpr.NodeType == ExpressionType.Quote)
            {
                return TranslateWhereExpression(unaryExpr.Operand, parameters);
            }
            // For other unary expressions (like Convert), just unwrap
            return TranslateWhereExpression(unaryExpr.Operand, parameters);
        }

        // Handle LambdaExpression - extract the body
        if (expression is LambdaExpression lambdaExpr)
        {
            return TranslateWhereExpression(lambdaExpr.Body, parameters);
        }
        
        if (expression is BinaryExpression binaryExpr)
        {
            var left = TranslateWhereExpression(binaryExpr.Left, parameters);
            var right = TranslateWhereExpression(binaryExpr.Right, parameters);

            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return string.Empty;

            var op = binaryExpr.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => "="
            };

            // For string equality comparisons, use explicit CAST with COLLATE BINARY for case-sensitive matching
            // SQLite's default TEXT collation is NOCASE (case-insensitive), so we need COLLATE BINARY
            bool isStringEquality = (binaryExpr.NodeType == ExpressionType.Equal || binaryExpr.NodeType == ExpressionType.NotEqual) &&
                                    IsStringType(binaryExpr.Left.Type) && IsStringType(binaryExpr.Right.Type);
            
            if (isStringEquality)
            {
                // Wrap column references with CAST to TEXT COLLATE BINARY for explicit case-sensitive comparison
                // This ensures that "ABC123" != "abc123" in SQLite
                if (left.StartsWith("[") && left.EndsWith("]"))
                {
                    left = $"CAST({left} AS TEXT) COLLATE BINARY";
                }
                // If right is also a column reference, do the same
                if (right.StartsWith("[") && right.EndsWith("]"))
                {
                    right = $"CAST({right} AS TEXT) COLLATE BINARY";
                }
            }

            return $"({left} {op} {right})";
        }

        if (expression is MemberExpression memberExpr)
        {
            // Handle nullable HasValue property - translate to IS NOT NULL
            if (memberExpr.Member.Name == "HasValue" && IsNullableType(memberExpr.Expression.Type))
            {
                var columnName = GetColumnName(memberExpr.Expression);
                return $"{columnName} IS NOT NULL";
            }

            // Handle nullable Value property - just get the column name
            if (memberExpr.Member.Name == "Value" && IsNullableType(memberExpr.Expression.Type))
            {
                return GetColumnName(memberExpr.Expression);
            }

            // Check if this is a captured variable or constant value (not a column access)
            // If the expression is not a ParameterExpression, it might be a captured variable
            if (!(memberExpr.Expression is ParameterExpression))
            {
                // Try to evaluate it as a constant value
                var value = GetConstantValue(memberExpr);
                if (value != null)
                {
                    var paramIndex = parameters.Count;
                    parameters.Add(value);
                    return $"@p{paramIndex}";
                }
            }

            // Handle regular member access - could be a property chain
            return GetColumnName(memberExpr);
        }

        if (expression is ConstantExpression constantExpr)
        {
            var paramIndex = parameters.Count;
            parameters.Add(constantExpr.Value);
            return $"@p{paramIndex}";
        }

        if (expression is MethodCallExpression methodCallExpr)
        {
            // Handle simple method calls like string.Contains, etc.
            if (methodCallExpr.Method.Name == "Contains" && methodCallExpr.Object is MemberExpression member)
            {
                var value = GetConstantValue(methodCallExpr.Arguments[0]);
                var paramIndex = parameters.Count;
                parameters.Add($"%{value}%");
                return $"[{member.Member.Name}] LIKE @p{paramIndex}";
            }
        }

        // Handle field access (e.g., captured variables like `now`)
        if (expression.NodeType == ExpressionType.MemberAccess)
        {
            // Try to evaluate the expression to get its value
            var value = GetConstantValue(expression);
            if (value != null)
            {
                var paramIndex = parameters.Count;
                parameters.Add(value);
                return $"@p{paramIndex}";
            }
        }

        return string.Empty;
    }

    private static string TranslateOrderByExpression((Expression Expression, bool Descending) orderExpr)
    {
        Expression expr = orderExpr.Expression;
        
        // Handle UnaryExpression with Quote node type (lambda expressions are quoted)
        while (expr is System.Linq.Expressions.UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Quote)
        {
            expr = unaryExpr.Operand;
        }
        
        // Extract the body from lambda expressions
        // Handle both LambdaExpression and compiler-generated expression types
        if (expr is LambdaExpression lambdaExpr)
        {
            expr = lambdaExpr.Body;
        }
        else if (expr.NodeType == ExpressionType.Lambda)
        {
            // Try to get Body property using reflection for compiler-generated lambda types
            var bodyProperty = expr.GetType().GetProperty("Body", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (bodyProperty != null && typeof(Expression).IsAssignableFrom(bodyProperty.PropertyType))
            {
                var bodyValue = bodyProperty.GetValue(expr);
                if (bodyValue is Expression bodyExpr)
                {
                    expr = bodyExpr;
                }
            }
        }
        
        // Handle UnaryExpression (conversions, etc.) - but not Quote
        while (expr is System.Linq.Expressions.UnaryExpression unaryExpr2 && unaryExpr2.NodeType != ExpressionType.Quote)
        {
            expr = unaryExpr2.Operand;
        }
        
        if (expr is MemberExpression memberExpr)
        {
            var direction = orderExpr.Descending ? "DESC" : "ASC";
            return $"[{memberExpr.Member.Name}] {direction}";
        }
        
        return string.Empty;
    }

    private static object? GetConstantValue(Expression expression)
    {
        if (expression is ConstantExpression constantExpr)
            return constantExpr.Value;

        // Handle member access (properties, fields) - try to evaluate
        if (expression is MemberExpression memberExpr)
        {
            // If accessing a field or property on a constant, evaluate it
            if (memberExpr.Expression is ConstantExpression constExpr)
            {
                var obj = constExpr.Value;
                if (obj != null)
                {
                    if (memberExpr.Member is System.Reflection.FieldInfo fieldInfo)
                    {
                        return fieldInfo.GetValue(obj);
                    }
                    if (memberExpr.Member is System.Reflection.PropertyInfo propInfo)
                    {
                        return propInfo.GetValue(obj);
                    }
                }
            }
            // Try to compile and evaluate the entire expression
            try
            {
                var lambda = Expression.Lambda(expression);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
            catch
            {
                return null;
            }
        }

        // Try to compile and evaluate
        try
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsStringType(Type type)
    {
        return type == typeof(string) || type == typeof(String);
    }

    private static bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private static string GetColumnName(Expression expression)
    {
        // Traverse member access chain to get the final column name
        if (expression is MemberExpression memberExpr)
        {
            // If this is accessing a property on a parameter (e.g., l.ExpiresAt), get the property name
            if (memberExpr.Expression is ParameterExpression)
            {
                return $"[{memberExpr.Member.Name}]";
            }
            // If this is a nested member access (e.g., l.ExpiresAt.Value), traverse up to get the base column
            if (memberExpr.Expression is MemberExpression parentMember)
            {
                // For l.ExpiresAt.Value, we want [ExpiresAt]
                // parentMember would be l.ExpiresAt, so get its member name
                if (parentMember.Expression is ParameterExpression)
                {
                    return $"[{parentMember.Member.Name}]";
                }
            }
            // Fallback: just use the member name
            return $"[{memberExpr.Member.Name}]";
        }
        
        // Fallback: try to get member name from expression
        if (expression is ParameterExpression)
        {
            return string.Empty; // Can't determine column from parameter alone
        }
        
        return string.Empty;
    }
}

/// <summary>
/// Visits expression trees to extract query information.
/// </summary>
internal class QueryExpressionVisitor : ExpressionVisitor
{
    public List<Expression> WhereExpressions { get; } = new();
    public List<(Expression Expression, bool Descending)> OrderByExpressions { get; } = new();
    public int? SkipCount { get; set; }
    public int? TakeCount { get; set; }
    public Expression? SelectExpression { get; set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Visit the source (first argument) first to process nested method calls
        if (node.Arguments.Count > 0)
        {
            Visit(node.Arguments[0]);
        }

        // Then process the current method call
        if (node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
        {
            switch (node.Method.Name)
            {
                case "Where":
                    if (node.Arguments.Count >= 2)
                    {
                        var predicate = node.Arguments[1];
                        WhereExpressions.Add(predicate);
                        Visit(predicate); // Visit the predicate to extract any nested expressions
                    }
                    break;

                case "OrderBy":
                    if (node.Arguments.Count >= 2)
                    {
                        var keySelector = node.Arguments[1];
                        OrderByExpressions.Add((keySelector, false));
                        Visit(keySelector); // Visit the key selector to extract member expression
                    }
                    break;

                case "OrderByDescending":
                    if (node.Arguments.Count >= 2)
                    {
                        var keySelector = node.Arguments[1];
                        OrderByExpressions.Add((keySelector, true));
                        Visit(keySelector); // Visit the key selector to extract member expression
                    }
                    break;

                case "Skip":
                    if (node.Arguments.Count >= 2)
                    {
                        SkipCount = GetConstantIntValue(node.Arguments[1]);
                    }
                    break;

                case "Take":
                    if (node.Arguments.Count >= 2)
                    {
                        TakeCount = GetConstantIntValue(node.Arguments[1]);
                    }
                    break;

                case "Select":
                    if (node.Arguments.Count >= 2)
                    {
                        SelectExpression = node.Arguments[1];
                        Visit(node.Arguments[1]); // Visit the select expression
                    }
                    break;
            }
        }

        return node;
    }

    private int? GetConstantIntValue(Expression expression)
    {
        if (expression is ConstantExpression constantExpr && constantExpr.Value is int intValue)
            return intValue;

        try
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            var result = compiled.DynamicInvoke();
            if (result is int i)
                return i;
        }
        catch
        {
        }

        return null;
    }
}

