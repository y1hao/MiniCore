using System.Linq;
using System.Linq.Expressions;
using MiniCore.Framework.Data;
using MiniCore.Framework.Data.Internal;

namespace MiniCore.Framework.Data.Extensions;

/// <summary>
/// Extension methods for IQueryable to support async operations.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Asynchronously returns a list that contains the elements from the input sequence.
    /// </summary>
    public static async Task<List<TSource>> ToListAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default)
        where TSource : class
    {
        if (source is DbSet<TSource> dbSet)
        {
            return await dbSet.ToListAsync(cancellationToken);
        }

        // For other IQueryable implementations, execute synchronously
        // This is a fallback for when the query has been transformed
        var provider = source.Provider;
        if (provider is Internal.QueryProvider queryProvider)
        {
            var result = await queryProvider.ExecuteAsync<IEnumerable<TSource>>(source.Expression, cancellationToken);
            return result?.ToList() ?? new List<TSource>();
        }

        // Last resort: materialize synchronously
        return source.ToList();
    }
}

