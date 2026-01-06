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
    IReadOnlyList<T> ICacheProvider<T>.AsReadOnly() => AsReadOnly();
}
