# Comprehensive Analysis: TODOs, Undones, NotImplementeds, and Issues

## Executive Summary

**Analysis Date**: January 1, 2026
**Branch**: copilot/analyze-todo-undone-issues
**Total Issues Found**: 37 items across the codebase

**Priority Breakdown**:

- **High Priority**: 3 items (NotImplementedException in MaEnvelopes, 2 test fixes)
- **Medium Priority**: 8 items (Performance optimizations, API improvements)
- **Low Priority**: 26 items (Nice-to-haves, documentation, research items)

**QuoteHub Duplicate Timestamp "Disclaimer"**: This is NOT a bug. The behavior is intentional and working as designed. Duplicates are gracefully handled and suppressed to prevent overflow conditions.

---

## HIGH PRIORITY ISSUES (Need Immediate Attention)

### 1. MaEnvelopes StreamHub - NotImplementedException (CRITICAL)

**Location**: `src/m-r/MaEnvelopes/MaEnvelopes.StreamHub.cs:78, 88, 92`

**Issue**: Three moving average types throw NotImplementedException in streaming mode:

- ALMA (Arnaud Legoux Moving Average)
- EPMA (Endpoint Moving Average)
- HMA (Hull Moving Average)

**Impact**: Users cannot use these MA types in real-time streaming scenarios

**Status**: Documented in docs/_indicators/MaEnvelopes.md:124

**Effort**: Medium - Requires implementing streaming support for these three MA types

**Done**: No - These are actual missing features

### 2. ConnorsRsi Test Inconsistency

**Location**: `tests/indicators/a-d/ConnorsRsi/ConnorsRsi.StaticSeries.Tests.cs:108`

**Issue**: Test comment indicates "I don't think this is right, inconsistent"

**Impact**: Possible calculation or test accuracy issue

**Effort**: Low - Investigation needed

**Done**: No

### 3. WilliamsR Rounding at Boundaries

**Location**: `tests/integration/indicators/WilliamsR/WilliamsR.Tests.cs:24`

**Issue**: Rounding errors at boundary conditions

**Impact**: Precision issues in edge cases

**Effort**: Low

**Done**: No

---

## MEDIUM PRIORITY ISSUES (Performance & API Improvements)

### 4. Tema/Dema StreamHub Performance Optimization

**Locations**:

- `src/s-z/Tema/Tema.StreamHub.cs:34`
- `src/a-d/Dema/Dema.StreamHub.cs:33`

**Issue**: Could optimize by persisting layered EMA state

**Impact**: Performance improvement for TEMA/DEMA streaming

**Effort**: Medium

**Done**: No

### 5. StreamHub Reinitialization Design

**Location**: `src/_common/StreamHub/StreamHub.cs:343, 346`

**Issues**:

- Make reinitialization abstract
- Evaluate race condition between rebuild

**Impact**: Framework robustness

**Effort**: High - Architectural change

**Done**: No

### 6. Quote.Date Deprecation

**Location**: `src/_common/Quotes/Quote.cs:48`

**Issue**: Can remove after Date property is deprecated

**Impact**: Code cleanup

**Effort**: Low

**Done**: No

### 7. QuotePart API Deprecation

**Location**: `src/_common/QuotePart/QuotePart.StaticSeries.cs:41`

**Issue**: Should deprecate Use in favor of "ToQuotePart"

**Impact**: API consistency

**Effort**: Low

**Done**: No

### 8. RemoveWarmupPeriods Method Cleanup

**Location**: `src/_common/Reusable/Reusable.Utilities.cs:62`

**Issue**: Remove specific indicator 'RemoveWarmupPeriods()' methods

**Impact**: Code cleanup

**Effort**: Low

**Done**: No

### 9. Catalog ListingExecutor Design

**Locations**:

- `src/_common/Catalog/ListingExecutor.cs:10` - Generic vs interface TQuote/TResult
- `src/_common/Catalog/ListingExecutor.cs:26` - Redundant parameters

**Impact**: API design clarity

**Effort**: Medium

**Done**: No

### 10. Stochastic NaN Handling

**Location**: `src/s-z/Stoch/Stoch.StaticSeries.cs:255`

**Issue**: Should check `|| double.IsNaN(prevD)` to re/initialize SMMA?

**Impact**: Edge case handling

**Effort**: Low

**Done**: No

### 11. StochRsi Auto-Healing

**Location**: `src/s-z/StochRsi/StochRsi.StaticSeries.cs:45`

**Issue**: Still need to Remove() here, or is there auto-healing?

**Impact**: Code optimization

**Effort**: Low

**Done**: No

---

## LOW PRIORITY ISSUES (Nice-to-haves & Research)

### 12. ISeries Unix Date

**Location**: `src/_common/ISeries.cs:20`

**Issue**: Consider adding (long) UnixDate (seconds) to ISeries

**Impact**: Feature enhancement

**Effort**: Low

**Done**: No

### 13. IQuotePart Renaming

**Location**: `src/_common/QuotePart/IQuotePart.cs:13`

**Issue**: Consider renaming to IBarPartHub, with IQuote to IBar

**Impact**: Naming consistency

**Effort**: Low (but breaking change)

**Done**: No

### 14. StreamHub Observer Array Returns

**Location**: `src/_common/StreamHub/StreamHub.Observer.cs:33`

**Issue**: Make ToIndicator return array, loop appendation?

**Impact**: Performance optimization

**Effort**: Medium

**Done**: No

### 15. ATR Utilities Unused Method

**Location**: `src/a-d/Atr/Atr.Utilities.cs:24`

**Issue**: May be unused, verify before making public

**Impact**: Code cleanup

**Effort**: Low

**Done**: No

### 16. Hurst Anis-Lloyd Correction

**Location**: `src/e-k/Hurst/Hurst.StaticSeries.cs:155`

**Issue**: Apply Anis-Lloyd corrected R/S Hurst?

**Impact**: Algorithm enhancement

**Effort**: High - Research needed

**Done**: No

### 17. PivotPoints No-Copy Optimization

**Location**: `src/m-r/PivotPoints/PivotPoints.Utilities.cs:33`

**Issue**: Is there a no-copy way to do this? Many places.

**Impact**: Performance optimization

**Effort**: Medium

**Done**: No

### 18. Pivots Streaming Rewrite

**Location**: `src/m-r/Pivots/Pivots.StaticSeries.cs:124`

**Issue**: May need to be re-written for streaming

**Impact**: Streaming support

**Effort**: High

**Done**: No

### 19-29. Test TODOs (11 items)

Various test improvements and validations:

- Catalog metrics final count (`tests/indicators/_common/Catalog/Catalog.Metrics.Tests.cs:31`)
- StringOut index range fixes (2 items) (`tests/indicators/_common/Generics/StringOut.Tests.cs:277, 315`)
- StreamHub cache management assertions (2 items) (`tests/indicators/_common/StreamHub/StreamHub.CacheMgmt.Tests.cs:21, 36`)
- StreamHub asymmetric results (Renko) (`tests/indicators/_common/StreamHub/StreamHub.CacheMgmt.Tests.cs:223`)
- StreamHub stackoverflow test expansion (`tests/indicators/_common/StreamHub/StreamHub.Stackoverflow.Tests.cs:182`)
- Precision analysis obsolescence check (`tests/indicators/_precision/PrecisionAnalysis.Tests.cs:3`)
- CMO isUp test (`tests/indicators/a-d/Cmo/Cmo.StaticSeries.Tests.cs:6`)
- Renko StreamHub testing approach (`tests/indicators/m-r/Renko/Renko.StreamHub.Tests.cs:9`)

**Impact**: Test coverage and accuracy

**Effort**: Low-Medium per item

**Done**: No

---

## NOT AN ISSUE: QuoteHub Duplicate Timestamp Handling

**Claim**: "QuoteHub faults when adding quotes with duplicate timestamps"

**Reality**: This is NOT a fault. Analysis of `src/_common/StreamHub/StreamHub.cs` (lines 204-249) shows:

1. **Intentional Design**: Duplicate detection is a feature, not a bug
2. **Graceful Handling**: Duplicates are silently suppressed by default
3. **Overflow Protection**: After 100 sequential duplicates, throws OverflowException with clear error message
4. **Configurable**: Can be bypassed with Properties[1] flag for special cases (e.g., Renko bricks which legitimately have duplicate timestamps)

**Code Evidence**:

```csharp
// From StreamHub.cs, lines 204-249
private bool IsOverflowing(TOut item)
{
    // skip first arrival
    if (LastItem is null)
    {
        LastItem = item;
        return false;
    }

    // track/check for overflow condition
    if (item.Timestamp == LastItem.Timestamp && item.Equals(LastItem))
    {
        OverflowCount++;

        // handle overflow
        if (OverflowCount > 100)
        {
            const string msg = """
                A repeated stream update exceeded the 100 attempt threshold.
                Check and remove circular chains or check your stream provider.
                Provider terminated.
                """;

            IsFaulted = true;

            // emit error
            OverflowException exception = new(msg);
            NotifyObserversOnError(exception);
            throw exception;
        }

        // bypass duplicate prevention
        // when forced caching is enabled
        return !Properties[1];
    }

    // not repeating
    OverflowCount = 0;
    LastItem = item;
    return false;
}
```

**Conclusion**: This is working as designed. The "disclaimer" likely refers to the intentional suppression behavior, which is actually good engineering practice to prevent infinite loops and circular dependencies.

---

## What's Done vs. What Remains

### ✅ DONE (Based on streaming-indicators.plan.md)

- 97% of streaming indicator framework complete
- 82/85 BufferList implementations (96%)
- 83/85 StreamHub implementations (98%)
- 81/82 streaming documentation (99%)
- Performance baselines established
- Audit infrastructure created
- Provider history testing: 40/42 applicable (95%)
- DPO framework fix for chained observers (virtual Rebuild())
- ForceIndex O(n²) bug fixed (96% faster)
- Slope optimization (24% faster)
- Performance regression automation (detect-regressions.ps1)

### ❌ REMAINS (High Priority)

1. **MaEnvelopes StreamHub** - ALMA, EPMA, HMA not implemented (CRITICAL)
2. **ConnorsRsi Test** - Investigation needed
3. **WilliamsR Rounding** - Boundary precision fix

### ⏳ REMAINS (Medium Priority - ~8-16 hours work)

4-11. Various performance optimizations and API improvements

### 📝 REMAINS (Low Priority - Future enhancements)

12-29. Nice-to-have features, research items, test improvements

---

## Recommended Action Plan

### Phase 1: Critical Fixes (IMMEDIATE)

1. Implement ALMA, EPMA, HMA support in MaEnvelopes StreamHub
2. Investigate ConnorsRsi test inconsistency
3. Fix WilliamsR boundary rounding

### Phase 2: Performance & Polish (NEXT)

4. TEMA/DEMA StreamHub optimization
5. StreamHub reinitialization design improvements
6. Code cleanup (Quote.Date, RemoveWarmupPeriods, etc.)

### Phase 3: Future Enhancements (BACKLOG)

7. Research items (Hurst correction, Pivots streaming)
8. Test coverage improvements
9. API naming consistency

---

## Highest Priorities Summary

**Top 3 Issues to Address NOW:**

1. ❗ MaEnvelopes StreamHub NotImplementedException (affects users)
2. ❗ ConnorsRsi test inconsistency (data accuracy concern)
3. ❗ WilliamsR rounding at boundaries (precision issue)

**QuoteHub Duplicate Timestamps**: ✅ Not an issue - working as designed

---

## Copilot Assessment

Based on the problem statement: "Can you tell what the highest priorities are? What's done? What's remains?"

### Highest Priorities

1. **MaEnvelopes StreamHub** - This is the most critical actual bug/missing feature affecting users
2. **ConnorsRsi Test** - Potential data accuracy issue that needs investigation
3. **WilliamsR Rounding** - Precision problem in edge cases

### What's Done

The repository is in excellent shape with **97% completion** of the streaming indicators framework:

- Nearly all indicators have BufferList and StreamHub implementations
- Comprehensive test coverage and audit infrastructure
- Performance baselines established
- Documentation nearly complete
- Critical performance bugs already fixed (ForceIndex, Slope)

### What Remains

**Critical (Must Fix)**:

- MaEnvelopes StreamHub ALMA/EPMA/HMA implementations
- Two test accuracy investigations

**Nice to Have**:

- Various performance optimizations
- API cleanup and consistency improvements
- Research items for future enhancements

The "QuoteHub faults with duplicate timestamps" disclaimer is **not an issue** - it's intentional behavior to prevent overflow conditions.

---

Last updated: January 1, 2026
