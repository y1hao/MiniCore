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
            return $"[{memberExpr.Member.Name}]";
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

        return string.Empty;
    }

    private static string TranslateOrderByExpression((Expression Expression, bool Descending) orderExpr)
    {
        Expression expr = orderExpr.Expression;
        
        // Extract the body from lambda expressions
        if (expr is LambdaExpression lambdaExpr)
        {
            expr = lambdaExpr.Body;
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
        if (node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
        {
            switch (node.Method.Name)
            {
                case "Where":
                    if (node.Arguments.Count >= 2)
                    {
                        var predicate = node.Arguments[1];
                        WhereExpressions.Add(predicate);
                    }
                    break;

                case "OrderBy":
                    if (node.Arguments.Count >= 2)
                    {
                        var keySelector = node.Arguments[1];
                        OrderByExpressions.Add((keySelector, false));
                    }
                    break;

                case "OrderByDescending":
                    if (node.Arguments.Count >= 2)
                    {
                        var keySelector = node.Arguments[1];
                        OrderByExpressions.Add((keySelector, true));
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
                    }
                    break;
            }
        }

        return base.VisitMethodCall(node);
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

