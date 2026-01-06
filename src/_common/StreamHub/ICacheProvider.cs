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

    /// <summary>
    /// Try to find index position of the provided timestamp.
    /// </summary>
    /// <param name="timestamp">Timestamp to seek.</param>
    /// <param name="index">Index of timestamp or -1 when not found.</param>
    /// <returns>True if found.</returns>
    bool TryFindIndex(DateTime timestamp, out int index);

    /// <summary>
    /// Get the cache index based on item equality.
    /// </summary>
    /// <param name="cachedItem">Time-series object to find in cache.</param>
    /// <param name="throwOnFail">Throw exception when item is not found.</param>
    /// <returns>Index position.</returns>
    /// <exception cref="ArgumentException">When item is not found.</exception>
    int IndexOf(T cachedItem, bool throwOnFail);

    /// <summary>
    /// Get the cache index based on a timestamp.
    /// </summary>
    /// <remarks>
    /// Only use this when you are looking for a point in time
    /// without a matching item for context.
    /// </remarks>
    /// <param name="timestamp">Timestamp of cached item.</param>
    /// <param name="throwOnFail">Throw exception when timestamp is not found.</param>
    /// <returns>Index position.</returns>
    /// <exception cref="ArgumentException">When timestamp is not found.</exception>
    int IndexOf(DateTime timestamp, bool throwOnFail);

    /// <summary>
    /// Get the first cache index on or after a timestamp.
    /// </summary>
    /// <remarks>
    /// Only use this when you are looking for a point in time
    /// without a matching item for context.
    /// </remarks>
    /// <param name="timestamp">Timestamp of cached item.</param>
    /// <returns>First index position or -1 if not found.</returns>
    int IndexGte(DateTime timestamp);
}
