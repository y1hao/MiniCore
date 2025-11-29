namespace MiniCore.Framework.Data.Abstractions;

/// <summary>
/// Represents a session with the database and can be used to query and save instances of entities.
/// </summary>
public interface IDbContext : IDisposable
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that the database for the context exists. If it exists, no action is taken. If it does not exist, the database and all its schema are created.
    /// </summary>
    /// <returns>True if the database is created, false if it already existed.</returns>
    bool EnsureCreated();
}

