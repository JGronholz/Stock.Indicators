namespace Skender.Stock.Indicators;

/// <summary>
/// Volume Profile result containing volume distribution across price levels.
/// Includes both per-candle and cumulative volume profiles.
/// </summary>
[Serializable]
public record VolumeProfileResult : ISeries
{
#pragma warning disable CA5362 // Do not refer to potentially dangerous types in JSON deserializer
    // This reference is intentional for maintaining cumulative state in a linked-list pattern.
    // The previousResult is private and not serialized, only used for incremental calculations.
    private readonly VolumeProfileResult? previousResult;
#pragma warning restore CA5362

    // internal cumulative store kept per-result to avoid shared mutable state
    private readonly Dictionary<decimal, decimal> _cumulative;

    /// <summary>
    /// Initializes a new instance of the VolumeProfileResult class.
    /// </summary>
    /// <param name="quote">The quote to process for this volume profile result.</param>
    /// <param name="previousResult">The previous result for cumulative calculations, or null for the first result.</param>
    public VolumeProfileResult(IQuote quote, VolumeProfileResult? previousResult)
    {
        this.previousResult = previousResult;

        if (quote is null)
        {
            throw new ArgumentNullException(nameof(quote));
        }

        Timestamp = quote.Timestamp;
        High = quote.High;
        Low = quote.Low;
        Volume = quote.Volume;

        // copy cumulative totals from previous result (do not share the same dictionary)
        _cumulative = previousResult?._cumulative != null
            ? new Dictionary<decimal, decimal>(previousResult._cumulative)
            : new Dictionary<decimal, decimal>();
    }

    /// <summary>Date and time of the quote</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>High price of the quote</summary>
    public decimal High { get; private set; }

    /// <summary>Low price of the quote</summary>
    public decimal Low { get; private set; }

    /// <summary>Volume of the quote</summary>
    public decimal Volume { get; private set; }

    private IEnumerable<VolumeProfileValue> _volumeProfile = Array.Empty<VolumeProfileValue>();

    /// <summary>
    /// Volume distribution across price levels for this single candle.
    /// Each entry contains a price and the volume traded at that price level.
    /// </summary>
    public IEnumerable<VolumeProfileValue> VolumeProfile
    {
        get => _volumeProfile;
        internal set {
            _volumeProfile = value ?? Array.Empty<VolumeProfileValue>();

            // update cumulative totals incrementally (per-result dictionary)
            foreach (VolumeProfileValue item in _volumeProfile)
            {
                if (_cumulative.ContainsKey(item.Price))
                {
                    _cumulative[item.Price] += item.Volume;
                }
                else
                {
                    _cumulative[item.Price] = item.Volume;
                }
            }

            // ensure totals sum exactly to previous total + this.Volume to avoid tiny rounding errors
            decimal previousTotal = previousResult?._cumulative.Sum(kvp => kvp.Value) ?? 0M;
            decimal expectedTotal = previousTotal + Volume;
            decimal currentTotal = _cumulative.Sum(kvp => kvp.Value);
            decimal diff = expectedTotal - currentTotal;
            if (diff != 0M && _cumulative.Count > 0)
            {
                decimal maxKey = _cumulative.Keys.Max();
                _cumulative[maxKey] += diff;
            }
        }
    }

    /// <summary>
    /// Cumulative volume distribution across all price levels up to and including this result.
    /// Aggregates volume from all previous candles plus the current one.
    /// </summary>
    public IEnumerable<VolumeProfileValue> CumulativeVolumeProfile
    {
        get {
            List<VolumeProfileValue> vpvrValues = _cumulative.Select((kvp) => new VolumeProfileValue(kvp.Key, kvp.Value)).ToList();
            vpvrValues.Sort((first, second) => first.Price.CompareTo(second.Price));
            return vpvrValues;
        }
    }
}

/// <summary>
/// Represents the volume traded at a specific price level.
/// </summary>
/// <param name="price">The price level.</param>
/// <param name="volume">The volume traded at this price level.</param>
public class VolumeProfileValue(decimal price, decimal volume)
{
    /// <summary>Price level</summary>
    public decimal Price { get; set; } = price;

    /// <summary>Volume traded at this price level</summary>
    public decimal Volume { get; set; } = volume;
}
