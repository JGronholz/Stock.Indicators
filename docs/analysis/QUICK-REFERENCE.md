# TODO Analysis Quick Reference

**Analysis Date**: January 1, 2026

## Priority Checklist

### 🔴 HIGH PRIORITY (Fix Immediately)

- [ ] **MaEnvelopes StreamHub** - Implement ALMA, EPMA, HMA support
  - Location: `src/m-r/MaEnvelopes/MaEnvelopes.StreamHub.cs:78, 88, 92`
  - Impact: Users cannot use these MA types in streaming mode
  - Effort: Medium

- [ ] **ConnorsRsi Test** - Investigate inconsistency
  - Location: `tests/indicators/a-d/ConnorsRsi/ConnorsRsi.StaticSeries.Tests.cs:108`
  - Impact: Potential calculation accuracy issue
  - Effort: Low

- [ ] **WilliamsR Rounding** - Fix boundary precision
  - Location: `tests/integration/indicators/WilliamsR/WilliamsR.Tests.cs:24`
  - Impact: Precision errors in edge cases
  - Effort: Low

### 🟡 MEDIUM PRIORITY (Performance & API)

- [ ] **TEMA/DEMA StreamHub** - Optimize layered EMA state
  - Locations: `src/s-z/Tema/Tema.StreamHub.cs:34`, `src/a-d/Dema/Dema.StreamHub.cs:33`
  - Effort: Medium

- [ ] **StreamHub Reinitialization** - Design improvements
  - Location: `src/_common/StreamHub/StreamHub.cs:343, 346`
  - Effort: High

- [ ] **Quote.Date Deprecation** - Remove after deprecation complete
  - Location: `src/_common/Quotes/Quote.cs:48`
  - Effort: Low

- [ ] **QuotePart API** - Deprecate Use in favor of ToQuotePart
  - Location: `src/_common/QuotePart/QuotePart.StaticSeries.cs:41`
  - Effort: Low

- [ ] **RemoveWarmupPeriods** - Cleanup specific indicator methods
  - Location: `src/_common/Reusable/Reusable.Utilities.cs:62`
  - Effort: Low

- [ ] **Catalog ListingExecutor** - Address generic vs interface design
  - Location: `src/_common/Catalog/ListingExecutor.cs:10, 26`
  - Effort: Medium

- [ ] **Stochastic NaN** - Review SMMA reinitialization
  - Location: `src/s-z/Stoch/Stoch.StaticSeries.cs:255`
  - Effort: Low

- [ ] **StochRsi Auto-Healing** - Determine if Remove() needed
  - Location: `src/s-z/StochRsi/StochRsi.StaticSeries.cs:45`
  - Effort: Low

### ⚪ LOW PRIORITY (Future Enhancements)

- [ ] **ISeries Unix Date** - Add Unix timestamp support
  - Location: `src/_common/ISeries.cs:20`

- [ ] **IQuotePart Renaming** - Rename to IBarPartHub
  - Location: `src/_common/QuotePart/IQuotePart.cs:13`

- [ ] **StreamHub Array Returns** - Optimize ToIndicator
  - Location: `src/_common/StreamHub/StreamHub.Observer.cs:33`

- [ ] **ATR Utilities** - Verify unused method
  - Location: `src/a-d/Atr/Atr.Utilities.cs:24`

- [ ] **Hurst Correction** - Research Anis-Lloyd R/S
  - Location: `src/e-k/Hurst/Hurst.StaticSeries.cs:155`

- [ ] **PivotPoints No-Copy** - Optimize ToList() calls
  - Location: `src/m-r/PivotPoints/PivotPoints.Utilities.cs:33`

- [ ] **Pivots Streaming** - Rewrite for streaming support
  - Location: `src/m-r/Pivots/Pivots.StaticSeries.cs:124`

- [ ] **Test TODOs** - Various test improvements (11 items)
  - See TODO-ANALYSIS.md for details

## QuoteHub Duplicate Timestamps

✅ **NOT AN ISSUE** - Working as designed

The QuoteHub duplicate timestamp handling is intentional:

- Duplicates are gracefully suppressed by default
- Overflow protection after 100 sequential duplicates
- Configurable for special cases (Renko bricks)

**Do not "fix" this - it's correct behavior.**

## Summary Statistics

- **Total Issues**: 37 items
- **High Priority**: 3 items
- **Medium Priority**: 8 items
- **Low Priority**: 26 items

## Quick Action Items

**This Week**:

1. Fix MaEnvelopes StreamHub NotImplementedException
2. Investigate ConnorsRsi test
3. Fix WilliamsR rounding

**This Month**:

4. TEMA/DEMA optimization
5. API cleanup tasks
6. Code cleanup (deprecations, unused code)

**This Quarter**:

7. Research items (Hurst, Pivots)
8. Test improvements
9. Performance optimizations

---

For detailed analysis, see: [TODO-ANALYSIS.md](./TODO-ANALYSIS.md)

Last updated: January 1, 2026
