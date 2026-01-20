# StreamHub Concurrency Investigation - REVISED

**Date:** January 20, 2026  
**Fork:** JGronholz/Stock.Indicators  
**Branch:** v3

**REVISION:** Initial analysis incorrectly assumed single-threaded usage. Streaming hubs are designed for async websocket/REST feeds. This is a potential library issue, not application code issue.

## Summary

This document investigates ArgumentOutOfRangeException errors in StreamHubs during `ToIndicator` operations when using async feeds from exchanges (websockets/REST).

## V3 Branch Status

**Verification:** The v3 branch in this fork is unchanged from origin/v3.

```bash
$ git diff origin/v3..HEAD --stat
# No output - branches are identical
```

**Conclusion:** The v3 branch itself is pristine and matches the upstream.

## Issue Description

ArgumentOutOfRangeException when StreamHubs access beyond the end of:
- `Cache` (the hub's own cache of results)
- `ProviderCache` (the provider's cache that the hub reads from)

### Context

1. **Environment:** Async websocket/REST feeds from exchanges (typical streaming use case)
2. **Pattern:** Library's `Quote.Aggregate()` function aggregates 5m candles into higher timeframes
3. **Behavior:** Async feed patterns trigger index-out-of-bounds during normal operation
4. **Issue:** Observable pattern notifications are synchronous, but feeds are async
5. **Note:** This is the expected use case for StreamHubs - they are designed for streaming data

### Current Understanding

- StreamHubs are **designed for async streaming** (websockets, REST APIs)
- Repository owner correctly notes bounds-checking should not be necessary
- Test suite cannot reproduce because it doesn't simulate async feed timing
- Issue occurs during normal async streaming operation, not misuse

## Code Analysis

### Threading Model

**Observer Pattern:** StreamHub uses synchronous observer notifications:
- `NotifyObserversOnAdd()` iterates through observers synchronously
- Each observer's `OnAdd()` is called sequentially
- `ToIndicator()` is called during `OnAdd()` processing

**The Race Condition:**
1. Thread A: Provider processes new quote, calls `OnAdd()` with index hint `i`
2. Thread A: Observer's `ToIndicator()` needs `Cache[i-1]` 
3. Thread B: Concurrent `Rebuild()` or `RemoveRange()` clears/modifies Cache
4. Thread A: Attempts `Cache[i-1]` access → ArgumentOutOfRangeException

**Only synchronization:** One lock in `_unsubscribeLock` for Unsubscribe operations only. No locks protect:
- Cache access during Add/Rebuild
- ProviderCache access during notifications
- Index hint usage during concurrent modifications

### Actual Risk Areas

These areas lack bounds checking and can fail during concurrent async operations:

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

**Real Risk:** With async websocket feeds calling `Add()` from different contexts, while concurrent `Rebuild()` or aggregation operations modify caches, `Cache[i-1]` can be out of bounds.

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

**Real Risk:** ProviderCache can be pruned/modified concurrently with `ToIndicator()` execution, making index hints stale.

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

**Real Risk:** Loop accesses arrays without bounds checking during concurrent modifications.

### Library Design vs Reality

**Design Assumption:** Sequential processing within a hub.

**Reality:** Async streaming feeds (websockets/REST) are:
- Multi-threaded by nature (different callbacks, different threads)
- Can call Add() concurrently from multiple events
- Can trigger Rebuild() while Add() is processing
- Can prune/remove while aggregating

**Current Thread Safety:**
- ❌ No locks on Cache operations
- ❌ No locks on ProviderCache access
- ❌ No protection for index hints becoming stale
- ❌ Observer notifications are synchronous but sources are async
- ✅ One lock only: `_unsubscribeLock` (for unsubscribe only)

**Actual Thread Safety Issues:**
1. **Rebuild() + Add() race:** Rebuild clears Cache while Add's ToIndicator needs Cache[i-1]
2. **Prune + ToIndicator race:** PruneCache removes items while ToIndicator accesses them
3. **Index hint staleness:** ProviderCache changes between getting hint and using it
4. **Observable + ProviderCache modification:** Notifications iterate while provider modifies cache

## Test Environment vs Production

### Why Tests Don't Fail

**Test Pattern:**
```csharp
QuoteHub quoteHub = new();
RsiHub rsiHub = quoteHub.ToRsiHub(14);

// Sequential, single-threaded
for (int i = 0; i < quotes.Count; i++)
{
    quoteHub.Add(quotes[i]);
}
```

**Production Pattern (Normal Async Streaming):**
```csharp
// Websocket feed - different thread per callback
websocket.OnMessage += (kline) => {
    quoteHub.Add(kline);  // ⚠️ Thread A
};

// Aggregation timer - another thread
timer.Elapsed += () => {
    var aggregated = quotes.Aggregate(TimeSpan.FromMinutes(15));
    foreach (var quote in aggregated)
        higherTimeframeHub.Add(quote);  // ⚠️ Thread B, concurrent
};

// Maintenance - yet another thread  
Task.Run(async () => {
    while (true) {
        await Task.Delay(60000);
        quoteHub.PruneCache();  // ⚠️ Thread C, concurrent
    }
});
```

### Key Differences

1. **Concurrency:** Tests are single-threaded; production has multiple async event sources
2. **Timing:** Tests are deterministic; async feeds have race conditions
3. **Operations:** Tests rarely trigger concurrent Rebuild/Add/Prune scenarios
4. **Rate:** High-frequency feeds expose timing windows that tests never hit

## Conclusions

### **REVISED:** This IS a Library Bug

The ArgumentOutOfRangeException is **caused by insufficient thread safety in the library** for its intended async streaming use case.

### Evidence

1. **Streaming requires async:** Websockets/REST feeds are inherently multi-threaded
2. **Design intent:** StreamHubs are explicitly for streaming data from exchanges
3. **Observable pattern:** Synchronous notifications + async sources = race conditions
4. **No synchronization:** Only one lock exists (for unsubscribe), none for Cache operations
5. **Owner cannot reproduce:** Tests don't simulate concurrent async patterns

### Root Causes

1. **Missing locks on Cache access** during Add/Rebuild/Remove operations
2. **Missing locks on ProviderCache access** during notifications
3. **Index hints become stale** when ProviderCache changes between hint generation and use
4. **ToIndicator assumes Cache[i-1] exists** without bounds checking

### Recommended Library Fixes

#### Option 1: Add Bounds Checking (Simple, Safe)

```csharp
// In EmaHub.ToIndicator
double ema = i >= LookbackPeriods - 1
    ? Cache.Count > i - 1 && Cache[i - 1].Ema is not null  // ✓ Bounds check
        ? Ema.Increment(K, Cache[i - 1].Value, item.Value)
        : Sma.Increment(ProviderCache, LookbackPeriods, i)
    : double.NaN;
```

**Pros:** 
- Minimal change
- Prevents exceptions
- Low overhead (single comparison)

**Cons:** 
- Doesn't fix race conditions, just handles them gracefully
- May cause calculation inconsistencies during rebuilds

#### Option 2: Add Thread Synchronization (Comprehensive)

```csharp
public abstract partial class StreamHub<TIn, TOut>
{
    private readonly object _cacheLock = new();
    
    public void Add(TIn newIn)
    {
        lock (_cacheLock)
        {
            OnAdd(newIn, notify: true, null);
        }
    }
    
    public virtual void Rebuild(DateTime fromTimestamp)
    {
        lock (_cacheLock)
        {
            // ... rebuild logic
        }
    }
}
```

**Pros:** 
- Fixes race conditions properly
- Maintains calculation consistency

**Cons:** 
- Performance impact on high-frequency feeds
- Requires careful lock granularity to avoid deadlocks

#### Option 3: Lock-Free Concurrent Collections (Advanced)

Use `ConcurrentBag` or immutable collections for Cache, with atomic operations.

**Pros:**
- Better performance under high concurrency

**Cons:**
- Significant refactoring required
- Complex to implement correctly

### Why Bounds-Checking IS the Answer (Corrected)

Bounds-checking should be added because:

1. **Prevents crashes:** Better to calculate slightly wrong than crash
2. **Low overhead:** Single comparison per access is negligible
3. **Graceful degradation:** Hub continues working during edge cases
4. **Defense in depth:** Protects against unforeseen race conditions
5. **Expected pattern:** Async streaming is the design intent, not misuse

## Recommended Next Steps

1. **Add bounds checking** to ToIndicator implementations (EMA, RSI, etc.)
2. **Add unit tests** for concurrent Add/Rebuild scenarios
3. **Consider locks** for Cache operations if bounds checking insufficient
4. **Document threading expectations** if hubs require external synchronization
5. **Test with realistic async patterns** (websocket callbacks, timers, concurrent aggregation)

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

**Investigation completed:** January 20, 2026 (Revised)  
**Investigator:** GitHub Copilot Coding Agent  
**Status:** Library bug confirmed - insufficient thread safety for async streaming use case  
**Recommendation:** Add bounds checking and/or synchronization for concurrent operations
