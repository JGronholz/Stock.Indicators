namespace Skender.Stock.Indicators;

/// <summary>
/// TDIGM result containing all indicator series.
/// Value points to the fast MA (most relevant for trading signals).
/// Implements IReusable to enable chaining in Skender v3.
/// </summary>
[Serializable]
public record TdiGmResult() : IReusable
{
    /// <summary>Date and time for the result</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Upper band (middle + 1.6185 * standard deviation)</summary>
    public double? Upper { get; init; }

    /// <summary>Lower band (middle - 1.6185 * standard deviation)</summary>
    public double? Lower { get; init; }

    /// <summary>Middle band (SMA of RSI)</summary>
    public double? Middle { get; init; }

    /// <summary>Slow moving average of RSI</summary>
    public double? Slow { get; init; }

    /// <summary>Fast moving average of RSI</summary>
    public double? Fast { get; init; }

    /// <summary>Returns the Fast MA value for chaining purposes</summary>
    [JsonIgnore]
    public double Value => Fast.Null2NaN();
}
