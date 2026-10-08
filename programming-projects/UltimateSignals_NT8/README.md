# UltimateSignals_NT8

GreyBeard rebuilds of two third-party NinjaTrader 8 indicators — **UltimateSignals** and
**UltimateAIProV3** — plus the tooling used to review, validate, and drop-in-replace them.

Both vendor indicators are ThinkOrSwim ports (3-EMA trend state, MACD, Stochastic, a threshold
ZigZag) distributed as licensed, Agile.NET-packed DLLs. The starting complaint was simple: **SELL
signals were not capturable** by PredatorX Order Entry or Infinity Algo Engine. The root cause,
confirmed once decoded source became available, was the same in both products — a signal-side
color/plot collision that no amount of strategy-side configuration could work around. See
[UltimateSignals_Decoded_Technical_Summary.md](UltimateSignals_dll_info/UltimateSignals_Decoded_Technical_Summary.md)
for how the packed DLLs were decoded.

## The gb files

| File | What it is |
|---|---|
| [gbUltimateSignalsIndicator.cs](gbUltimateSignalsIndicator.cs) | Full rebuild of `UltimateSignalsIndicator.cs`. Fixes the magenta-on-magenta BUY/SELL text-marker collision, the dead alert wiring, and tier2/3 warm-up bugs. Drops the vendor's private MA/ZigZag engine hierarchy for NT8 built-ins; replaces SharpDX rendering with real, tagged plots/drawing objects so every signal is capturable. |
| [gbUltimateSignalsEngines.cs](gbUltimateSignalsEngines.cs) | Support types for gbUltimateSignalsIndicator: `GbUsMaMode`/`GbUsMa` (MA mode switch), `GbUsZigZagHighLow` (ToS ZigZag port), `GbWmaSeries`/`GbHmaSeries` (self-contained WMA/HMA — see below). |
| [gbUltimateAIPro.cs](gbUltimateAIPro.cs) | Full rebuild of `UltimateAIProV3.cs`. Headline fix: the vendor's `BUY`/`SELL` plots were registered and exposed but **never written** — always `NaN`. Also fixes the short-side offset/enable copy-paste bug (short arrows used the long buffer's settings) and six more magenta/stroke collisions. |
| [gbUltimateAIProEngines.cs](gbUltimateAIProEngines.cs) | Support types for gbUltimateAIPro: `GbUaiPriceSeries`, `GbUaiZigZagHighLow` (a second, different ZigZag model from gbUsZigZagHighLow — V3 uses a running max/min state machine, not threshold comparison). |
| [gbUltimateSignalsIndicator_Manual.md](gbUltimateSignalsIndicator_Manual.md) | **gbDetail.** Operation, plot reference (indices 0-11), consumption recipes for PredatorX/Infinity/strategy code, parameter reference, repaint behavior, known limitations. |
| [gbUltimateAIPro_Manual.md](gbUltimateAIPro_Manual.md) | **gbDetail.** Same structure for gbUltimateAIPro: four signal layers, plot reference (indices 0-18), the HTF-zone architecture change (below), parameter reference, known limitations. |
| [UltimateSignalsIndicator_Code_Review.md](UltimateSignalsIndicator_Code_Review.md) | **gbDetail.** Full bug-by-bug review of the vendor source that gbUltimateSignalsIndicator was built from, plus an "Implemented" section mapping each finding to its fix. |
| [UltimateAIProV3_Code_Review.md](UltimateAIProV3_Code_Review.md) | **gbDetail.** Same, for `UltimateAIProV3.cs` — dead BUY/SELL, the copy-paste bug, performance issues (unbounded per-tick rewrite, whole-chart `MRO` scan), and the self-referencing HTF pattern. |
| [gbSignalProbe.cs](gbSignalProbe.cs) | Diagnostic tool. Late-bound (no compile-time reference to any target), resolves an indicator on the chart by name and reports plot-capturability, live plot map, and repaint behavior via write-once + REVISION CSV logging. Built for exactly this project's vendor-vs-gb validation. |
| [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs) | A narrower, older fix: re-emits the *vendor* UltimateSignals' buy/sell as clean plots and distinctly-tagged arrows, for anyone who wants to keep running the licensed DLL as-is rather than switch to gbUltimateSignalsIndicator. |
| [gbTrendReversal.cs](gbTrendReversal.cs) | A related but separate port — Phase 1 of a different ThinkScript study ("Trend Reversal", 3-EMA latched state) chosen as a from-scratch alternative signal source, built with a capturable contract from day one. See [ToS_TrendReversal_Port_Plan.md](UltimateSignals_dll_info/ToS_TrendReversal_Port_Plan.md). |

## Why HMA/WMA are hand-rolled

`gbUsMa` calls NT8's own `SMA()`/`EMA()` built-ins directly, but HMA/WMA are computed by
`GbWmaSeries`/`GbHmaSeries` in `gbUltimateSignalsEngines.cs` instead of `owner.HMA()`/`owner.WMA()`.
NT8's **Export NinjaScript** tool doesn't offer WMA/HMA as includable system files and fails before
it can even prompt to bundle them — a known NT8 export gap, not a bug here. The local classes
reproduce NT8's own `@WMA.cs`/`@HMA.cs` formula exactly; only where the computation runs changed.

## Two NinjaScript compile traps hit along the way

Both are now permanent entries in [GreyBeard-Typical-NinjaTrader.md §6](../GreyBeard-Typical-NinjaTrader.md):

1. **Enums used as `[NinjaScriptProperty]` types must live in the global namespace**, not inside
   `...Indicators.GreyBeard`. NT8's generated wrapper region emits custom enum types unqualified.
2. **Self-referencing indicators are incompatible with hand-authored source.** NT8 appends its
   generated code region unconditionally once a type is registered; an indicator that calls its own
   factory method on a second data series can't get a first clean registration. gbUltimateAIPro's
   HTF Ultimate Zones were reworked from a self-referencing child instance to an in-instance
   `ComputeHtfBar()` method for exactly this reason.

## Original vendor source

[UltimateScalper/](UltimateScalper/) holds the real decoded/decompiled vendor source (7 files,
~4,800 lines total) — `UltimateSignalsIndicator.cs`, `UltimateAIProV3.cs` + `_Enums.cs`,
`UltimateSignalsHelpers.cs`, `WiZigZagHighLowTOS1v0.cs`, `StochasticFullTOS1v0.cs`,
`WilderMA1v0.cs`. Read-only reference — used to write the two code reviews above, not modified.
Used with the permission of the license holder who provided it.

[UltimateSignals_dll_info/](UltimateSignals_dll_info/) holds the earlier, pre-decoded analysis:
the original packed `UltimateSignals.dll` + `Info.xml`, screenshots, and the port plan. The
metadata-only `UltimateSignals_Review.md` / `_Signals.md` / `_Validation_Process.md` still sit at
the top level of this folder — superseded by the code reviews above now that real source exists,
but kept rather than deleted since they document how the packed DLL was analyzed before that
source arrived.

## Status

Both gb indicators compile clean and export standalone. Neither has been validated on a chart yet —
that's the current step, using gbSignalProbe against the vendor originals and the gb rebuilds side
by side. See the "Known limitations" section of each manual for what's explicitly not yet verified
(gbUltimateAIPro's BUY/SELL is a new implementation with no vendor behavior to diff against, since
the vendor's was dead code).
