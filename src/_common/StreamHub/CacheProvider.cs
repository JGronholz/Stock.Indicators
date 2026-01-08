namespace Skender.Stock.Indicators;

/// <summary>
/// Default cache provider implementation using List&lt;T&gt;.
/// </summary>
/// <typeparam name="T">Type of cached items</typeparam>
public class CacheProvider<T> : List<T>, ICacheProvider<T>
    where T : IReusable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheProvider{T}"/> class.
    /// </summary>
    public CacheProvider()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity</param>
    public CacheProvider(int capacity)
        : base(capacity)
    {
    }

    /// <inheritdoc/>
    //IReadOnlyList<T> ICacheProvider<T>.AsReadOnly() => AsReadOnly();
    public new IReadOnlyList<T> AsReadOnly() => base.AsReadOnly();

    /// <inheritdoc/>
    public bool TryFindIndex(DateTime timestamp, out int index)
    {
        index = IndexOf(timestamp, false);
        return index != -1;
    }

    /// <inheritdoc/>
    public int IndexOf(T cachedItem, bool throwOnFail)
    {
        int low = 0;
        int high = Count - 1;
        int firstMatchIndex = -1;
        DateTime targetTimestamp = cachedItem.Timestamp;

        while (low <= high)
        {
            int mid = (low + high) >> 1;
            int comparison = this[mid].Timestamp.CompareTo(targetTimestamp);

            if (comparison == 0)
            {
                // Found a match by Timestamp,
                // store the index of the first match
                if (firstMatchIndex == -1)
                {
                    firstMatchIndex = mid;
                }

                // Verify with Equals for an exact match
                if (this[mid].Equals(cachedItem))
                {
                    return mid; // exact match found
                }

                high = mid - 1; // continue searching to the left
            }
            else if (comparison < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        // If a timestamp match was found but no exact
        // match, try to find an exact match in the range
        // of duplicate timestamps (e.g. Renko bricks),
        // biased towards later duplicates.
        if (firstMatchIndex != -1)
        {
            // Find the last occurrence of the matching timestamp
            for (int i = Count - 1; i >= firstMatchIndex; i--)
            {
                if (this[i].Timestamp == targetTimestamp
                 && this[i].Equals(cachedItem))
                {
                    return i; // exact match found among duplicates
                }
            }
        }

        // not found
        return throwOnFail
            ? throw new ArgumentException(
                "Matching source history not found.", nameof(cachedItem))
            : -1;
    }

    /// <inheritdoc/>
    public int IndexOf(DateTime timestamp, bool throwOnFail)
    {
        int low = 0;
        int high = Count - 1;

        while (low <= high)
        {
            int mid = (low + high) >> 1;
            DateTime midTimestamp = this[mid].Timestamp;

            if (midTimestamp == timestamp)
            {
                return mid;
            }
            else if (midTimestamp < timestamp)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        // not found
        return throwOnFail
            ? throw new ArgumentException(
                "Matching source history not found.", nameof(timestamp))
            : -1;
    }

    /// <inheritdoc/>
    public int IndexGte(DateTime timestamp)
    {
        int low = 0;
        int high = Count;
        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            if (this[mid].Timestamp < timestamp)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        // At this point, low is the index of the first
        // element that is greater than or equal to timestamp
        // or Count if all elements are less than timestamp.
        // If low is equal to Count, it means there are
        // no elements greater than or equal to timestamp.
        return low < Count ? low : -1;
    }
}
