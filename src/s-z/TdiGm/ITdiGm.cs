namespace Skender.Stock.Indicators;

/// <summary>
/// Interface for TdiGm configuration parameters
/// </summary>
public interface ITdiGm
{
    /// <summary>RSI period for the base oscillator</summary>
    int RsiPeriod { get; }

    /// <summary>Band length for middle band and standard deviation</summary>
    int BandLength { get; }

    /// <summary>Fast moving average period on RSI</summary>
    int FastLength { get; }

    /// <summary>Slow moving average period on RSI</summary>
    int SlowLength { get; }
}
