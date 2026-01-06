namespace Skender.Stock.Indicators;

/// <summary>
/// Provider interface for StreamHub cache storage.
/// Enables custom cache implementations (e.g., write-through persistence).
/// </summary>
/// <typeparam name="T">Type of cached items</typeparam>
public interface ICacheProvider<T> : IList<T>
    where T : IReusable
{
    /// <summary>
    /// Removes all items matching the predicate.
    /// </summary>
    /// <param name="match">Predicate to match items</param>
    /// <returns>Number of items removed</returns>
    int RemoveAll(Predicate<T> match);

    /// <summary>
    /// Returns a read-only wrapper for the cache.
    /// </summary>
    /// <returns>Read-only list view</returns>
    IReadOnlyList<T> AsReadOnly();
}
