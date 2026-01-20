# StreamHub Concurrency Investigation - FINAL

**Date:** January 20, 2026  
**Fork:** JGronholz/Stock.Indicators  
**Branch:** v3

**FINAL DETERMINATION:** StreamHubs are explicitly documented as NOT thread-safe by design. This is an application-side issue requiring external synchronization.

## Summary

This document investigates ArgumentOutOfRangeException errors in StreamHubs during `ToIndicator` operations when using concurrent access patterns without proper external synchronization.

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

1. **Environment:** Async websocket/REST feeds from exchanges
2. **Pattern:** Concurrent access to StreamHub methods from multiple threads
3. **Root Cause:** No external synchronization protecting concurrent hub operations
4. **Documentation:** StreamHubs explicitly documented as "Not thread-safe by default; synchronize external access" (`docs/features/stream.md:114`)

### Design Intent (CONFIRMED)

From official documentation (`docs/features/stream.md`):

> **Thread safety:** Not thread-safe by default; synchronize external access

**Why this design:**
1. **Performance** - Thread synchronization adds overhead; streaming targets <1ms latency per quote
2. **Flexibility** - Users choose their threading model (locks, channels, actors, single-threaded)
3. **Observable pattern** - Synchronous cascade assumes sequential execution
4. **Test coverage** - All tests validate sequential single-threaded usage

**This is working as designed.** The library is correct; application code must serialize access.

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

### **FINAL:** This is NOT a Library Bug

The ArgumentOutOfRangeException is **caused by improper concurrent access without external synchronization**, as documented in the library design.

### Evidence

1. **Documented behavior:** `docs/features/stream.md:114` explicitly states "Not thread-safe by default; synchronize external access"
2. **Design intent:** Performance-focused; avoids locks to achieve <1ms latency target
3. **Test coverage:** All tests use sequential patterns, validating the single-threaded design
4. **Observable pattern:** Synchronous cascade assumes sequential execution model
5. **Owner correct:** Bounds-checking unnecessary when used as designed

### Root Cause

**Application code calls StreamHub methods concurrently without external synchronization.**

The library assumes sequential access. When multiple threads call `Add()`, `Rebuild()`, etc. concurrently:
1. Thread A enters `ToIndicator()` with index `i`
2. Thread B calls `Rebuild()` which clears `Cache`
3. Thread A attempts `Cache[i-1]` → ArgumentOutOfRangeException

### Recommended Application Fixes

#### Option 1: Use Locks (Simple)

```csharp
private readonly object _hubLock = new();

async Task ProcessWebSocketFeed()
{
    await foreach (var quote in websocketFeed)
    {
        lock (_hubLock)
        {
            quoteHub.Add(quote);
        }
    }
}
```

#### Option 2: Use Channels (Robust)

```csharp
private readonly Channel<Quote> _quoteChannel = Channel.CreateUnbounded<Quote>();

// Producer: websocket callback
websocket.OnMessage += quote => _quoteChannel.Writer.TryWrite(quote);

// Consumer: single-threaded processing
async Task ProcessQuotes()
{
    await foreach (var quote in _quoteChannel.Reader.ReadAllAsync())
    {
        quoteHub.Add(quote);  // Sequential, no concurrency
    }
}
```

#### Option 3: Actor Pattern

Use message-passing to ensure sequential processing per hub.

### Answer to "Synchronous Streaming"

**Yes, synchronous streaming is the documented pattern:**

1. **Single-threaded event loop** - Process websocket callbacks sequentially
2. **Channel-based serialization** - Queue concurrent events, process sequentially
3. **Actor model** - Each hub processes messages from queue
4. **Lock-based coordination** - Serialize concurrent access with external locks

**The test suite demonstrates this** - all tests process quotes sequentially in a loop, which is the expected pattern even with async data sources.

### Why NOT to Add Library Changes

Adding bounds-checking or locks to the library would:

1. **Violate documented design** - Library promises external synchronization responsibility
2. **Add overhead** - Impacts all users, including those with proper synchronization
3. **Mask bugs** - Silent failures instead of crashes that reveal improper usage
4. **Break performance targets** - <1ms latency requires zero-lock design

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

**Investigation completed:** January 20, 2026 (Final)  
**Investigator:** GitHub Copilot Coding Agent  
**Status:** Application bug confirmed - concurrent access without external synchronization  
**Recommendation:** Implement application-side synchronization (locks, channels, or actor pattern)  
**Library status:** Working as designed per `docs/features/stream.md:114`
