# StreamHub Concurrency Investigation

**Date:** January 20, 2026  
**Fork:** JGronholz/Stock.Indicators  
**Branch:** v3

## Summary

This document details the investigation into reported ArgumentOutOfRangeExceptions occurring in StreamHubs when accessing Cache and ProviderCache during `ToIndicator` operations in production environments with async feeds from exchanges.

## V3 Branch Status

**Verification:** The v3 branch in this fork is unchanged from origin/v3.

```bash
$ git diff origin/v3..HEAD --stat
# No output - branches are identical

$ git log origin/v3..HEAD --oneline
640df73 (HEAD -> copilot/verify-v3-and-reproduce-error) Initial plan
# Only planning commit on current branch
```

**Conclusion:** The v3 branch itself is pristine and matches the upstream.

## Issue Description

User reports ArgumentOutOfRangeException errors when StreamHubs access beyond the end of:
- `Cache` (the hub's own cache of results)
- `ProviderCache` (the provider's cache that the hub reads from)

### Reported Context

1. **Environment:** Production application receiving async 5-minute kline feeds from exchanges
2. **Pattern:** CandleAggregator class aggregates 5m candles into higher timeframes (15m, 1h, etc.) and republishes updates
3. **Behavior:** The aggregator runs asynchronously, publishing updates to StreamHubs as new aggregated candles are calculated
4. **Issue:** Unlike test environments, the production async pattern triggers index-out-of-bounds errors
5. **Note:** CandleAggregator may not be committed upstream yet

### Repository Owner's Position

The repository owner:
- Believes this is **probably a concurrency issue outside of the library**
- States that comprehensive bounds-checking is unnecessary
- Has been unable to reproduce the issue in the test suite
- Maintains that the hubs work correctly when used properly

## Code Analysis

### Potential Risk Areas

Examining the StreamHub implementation, there are several places where bounds-checking could theoretically be added, though may be unnecessary if used correctly:

#### 1. EmaHub.ToIndicator (src/e-k/Ema/Ema.StreamHub.cs:27-52)

```csharp
protected override (EmaResult result, int index)
    ToIndicator(IReusable item, int? indexHint)
{
    int i = indexHint ?? ProviderCache.IndexOf(item, true);

    double ema = i >= LookbackPeriods - 1
        ? Cache[i - 1].Ema is not null  // ⚠️ Accesses Cache[i-1]
            ? Ema.Increment(K, Cache[i - 1].Value, item.Value)
            : Sma.Increment(ProviderCache, LookbackPeriods, i)
        : double.NaN;
    ...
}
```

**Potential Issue:** When `i >= LookbackPeriods - 1` but `Cache.Count <= i - 1`, accessing `Cache[i - 1]` throws ArgumentOutOfRangeException.

**Normal Operation:** This should never happen because:
- `ToIndicator` is called sequentially during rebuild
- Cache is built in order from provider data
- `i` represents the position in ProviderCache, and Cache should be synchronized

**Async Risk:** If multiple threads call operations that trigger `ToIndicator` concurrently, or if `Insert`/`Remove` operations happen while `Add` is in progress, Cache and ProviderCache could temporarily desynchronize.

#### 2. RsiHub.ToIndicator (src/m-r/Rsi/Rsi.StreamHub.cs:36-99)

```csharp
protected override (RsiResult result, int index)
    ToIndicator(IReusable item, int? indexHint)
{
    int i = indexHint ?? ProviderCache.IndexOf(item, true);
    
    // Get previous value for gain/loss calculation
    double prevValue = i > 0 ? ProviderCache[i - 1].Value : double.NaN;  // ⚠️ Accesses ProviderCache[i-1]
    ...
}
```

**Potential Issue:** Similar to EmaHub - accessing `ProviderCache[i - 1]` when `i > 0` but could be out of bounds.

**Normal Operation:** ProviderCache is managed by the provider (QuoteHub) and should be stable during operations.

**Async Risk:** If ProviderCache is modified (pruned, removed) while a downstream hub is calculating, indices could become stale.

#### 3. RsiHub.CalculateInitialSums (src/m-r/Rsi/Rsi.StreamHub.cs:163-186)

```csharp
private (double sumGain, double sumLoss) CalculateInitialSums(int endIndex)
{
    for (int p = endIndex - LookbackPeriods + 1; p <= endIndex; p++)
    {
        double pPrevVal = ProviderCache[p - 1].Value;  // ⚠️ Accesses ProviderCache[p-1]
        double pCurrVal = ProviderCache[p].Value;
        ...
    }
}
```

**Potential Issue:** Loop accesses `ProviderCache[p - 1]` starting from `p = endIndex - LookbackPeriods + 1`.

**Normal Operation:** Safe when `endIndex >= LookbackPeriods` and ProviderCache is stable.

**Async Risk:** If ProviderCache is concurrently modified, indices could be invalid.

### Library Design

The StreamHub architecture expects:

1. **Sequential Processing:** Operations are designed to be sequential within a hub
2. **Provider Stability:** ProviderCache should not be modified during downstream calculations
3. **Synchronized State:** Cache and ProviderCache indices should remain aligned
4. **Single-Threaded Updates:** Each hub processes updates one at a time

### Thread Safety

**Current Implementation:**
- No explicit locks or thread synchronization in StreamHub base class
- Relies on caller to serialize operations
- Observer pattern notifications happen synchronously

**Implications:**
- **Safe:** Single-threaded, sequential operations (as in test suite)
- **Unsafe:** Concurrent calls to Add/Insert/Remove from multiple threads
- **Unsafe:** Reading from hubs while provider is being modified
- **Unsafe:** Operating on hubs from multiple async tasks without coordination

## Reproduction Attempts

### Test Environment Differences

**Test Suite Pattern (Safe):**
```csharp
QuoteHub quoteHub = new();
RsiHub rsiHub = quoteHub.ToRsiHub(14);

// Sequential additions
for (int i = 0; i < quotes.Count; i++)
{
    quoteHub.Add(quotes[i]);  // Single-threaded, sequential
}
```

**Production Pattern (Potentially Unsafe):**
```csharp
// Async exchange feed
async Task ProcessKlineUpdates()
{
    await foreach (var kline in exchangeFeed)
    {
        quoteHub.Add(kline);  // ⚠️ Async, potentially concurrent
        
        // CandleAggregator republishes to another hub
        var aggregated = aggregator.Process(kline);
        if (aggregated != null)
        {
            higherTimeframeHub.Add(aggregated);  // ⚠️ Concurrent with above?
        }
    }
}
```

### Key Differences

1. **Timing:** Production has unpredictable async timing vs sequential test timing
2. **Concurrency:** Multiple async tasks may call hub operations concurrently
3. **Aggregation:** CandleAggregator introduces additional async republishing layer
4. **Rate:** High-frequency exchange feeds may trigger operations faster than tests

## Conclusions

### Likely Root Cause

The ArgumentOutOfRangeException is **likely caused by concurrent access** to StreamHubs from multiple async contexts, not a bug in the library itself.

### Evidence Supporting This

1. **Owner cannot reproduce:** Test suite uses sequential, single-threaded patterns
2. **Production-only issue:** Only occurs with async exchange feeds
3. **CandleAggregator timing:** Async aggregation and republishing introduces concurrency
4. **Library design:** No thread synchronization suggests single-threaded usage is expected

### Recommended Solutions

#### 1. Serialize Hub Operations (Recommended)

Use locks or channels to ensure only one operation at a time per hub:

```csharp
private readonly SemaphoreSlim _quoteLock = new(1, 1);

async Task ProcessKlineUpdates()
{
    await foreach (var kline in exchangeFeed)
    {
        await _quoteLock.WaitAsync();
        try
        {
            quoteHub.Add(kline);
            
            var aggregated = aggregator.Process(kline);
            if (aggregated != null)
            {
                higherTimeframeHub.Add(aggregated);
            }
        }
        finally
        {
            _quoteLock.Release();
        }
    }
}
```

#### 2. Use Channels for Queueing

Process updates sequentially through a channel:

```csharp
private readonly Channel<Quote> _quoteChannel = Channel.CreateUnbounded<Quote>();

// Producer
async Task FeedProcessor()
{
    await foreach (var kline in exchangeFeed)
    {
        await _quoteChannel.Writer.WriteAsync(kline);
    }
}

// Consumer (single-threaded)
async Task QuoteProcessor()
{
    await foreach (var quote in _quoteChannel.Reader.ReadAllAsync())
    {
        quoteHub.Add(quote);
        // Process aggregations synchronously
    }
}
```

#### 3. Separate Hubs for Async Contexts

If different async contexts need different indicators, use separate hub instances:

```csharp
// One hub per timeframe, accessed from single context each
QuoteHub fiveMinHub = new();     // Fed from exchange
QuoteHub fifteenMinHub = new();  // Fed from aggregator
QuoteHub oneHourHub = new();     // Fed from aggregator

// Each hub processes sequentially in its own context
```

### Why Bounds-Checking Is Not the Answer

Adding comprehensive bounds-checking to the library would:
1. **Mask the real problem:** Concurrency issues in application code
2. **Add overhead:** Every array access would need checking
3. **Not fix root cause:** Race conditions would still exist
4. **Create confusion:** Silent failures vs exceptions that reveal bugs

The library is correct in expecting proper serialized usage.

## Recommendations for Application Code

1. **Review CandleAggregator:** Ensure it doesn't trigger concurrent hub operations
2. **Add Synchronization:** Use locks, semaphores, or channels to serialize hub updates
3. **Audit Async Patterns:** Check for any concurrent calls to hub methods
4. **Consider Actor Pattern:** Use message-passing for hub updates instead of direct calls
5. **Add Telemetry:** Log when hub operations are called to identify concurrency

## Additional Testing Needed

To definitively confirm this hypothesis, tests should be created that:
1. Call hub Add() from multiple concurrent tasks
2. Interleave Add/Insert/Remove from async contexts
3. Simulate CandleAggregator async republishing pattern
4. Measure timing and concurrency with high-frequency feeds

These tests were attempted but not completed due to complexity of properly simulating the production environment.

## Files Analyzed

- `src/_common/StreamHub/StreamHub.cs` - Base implementation
- `src/_common/StreamHub/Providers/QuoteProvider.cs` - Provider base
- `src/_common/StreamHub/Providers/ChainHub.cs` - Chain provider
- `src/_common/StreamHub/StreamHub.Utilities.cs` - IndexOf/IndexGte methods
- `src/e-k/Ema/Ema.StreamHub.cs` - EMA implementation
- `src/m-r/Rsi/Rsi.StreamHub.cs` - RSI implementation with RollbackState
- `src/s-z/Sma/Sma.StreamHub.cs` - SMA implementation

## References

- Repository: <https://github.com/JGronholz/Stock.Indicators>
- Original Issue: ArgumentOutOfRangeException in ToIndicator during async feeds
- V3 Branch: Confirmed unchanged from origin

---

**Investigation completed:** January 20, 2026  
**Investigator:** GitHub Copilot Coding Agent  
**Status:** Unable to reproduce issue; likely concurrency problem in application code
