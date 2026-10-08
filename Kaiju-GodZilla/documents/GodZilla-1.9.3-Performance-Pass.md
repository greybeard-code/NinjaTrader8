# GodZilla Suite — Performance Pass (GodZillaKilla 1.9.3)

**Scope:** GodZillaKilla strategy + five indicators (gbKingOrderBlock, gbPANAKanal, gbBarStatus, gbSuperJumpBoost, NewsSignals).
**Goal:** reduce per-tick latency on the data thread, per-frame cost on the render thread, and unbounded memory growth over long sessions.
**Intent:** zero functional change — same signals, same entries, same visuals — with one documented display trade-off (noted below). All changes compile clean in NT8.

---

## GodZillaKilla.cs (strategy) — data-thread hot path

**1. Early bail in the pending-entry tick handler.**
`ProcessPendingEntriesOnTickSeries()` runs on every tick of the 1-tick series. It was evaluating trading-window times (`ToTime` ×8) and the news-block series on every tick, then discovering nothing was queued. It now returns immediately when no reverse or signal entry is pending — the gate work only runs on the small fraction of ticks where an entry is actually waiting. Behavior-identical: the `Clear*` calls it skips were no-ops in that state.

**2. Session-window times cached as ints.**
`CheckTradingTimeframes` / `CheckFlattenTimeframes` re-converted the eight `StartTime1…SkipEndTime` DateTime properties to HHMMSS ints on every call (per tick + per bar + per HUD snapshot). They're now converted once in `State.DataLoaded` — safe because NT8 rebuilds the strategy on any property change.

**3. ATM position reads go through the per-tick memo.**
`GetCurrentTradePosition()` was calling the raw `GetAtmStrategyMarketPosition` while a tick-scoped cache (`GetAtmStrategyMarketPositionTickCached`) already existed for other callers. It can be hit 2–3× per callback (reversal gate, pending-entry gate, debug prints); those now collapse to one NT8 ATM lookup. The memo is reset at the top of *both* the tick-series handler and the primary-bar path, so a bar-close callback never consumes a value memoized on the previous tick.

**4. Signal pipeline deduplicated (one compute per bar).**
The six `Signal_Trade` reads + six `ComputeSignal` normalizations + six series writes were executed twice per primary bar — once in the visuals path, again in the entry logic. A new `ComputeBarSignalSnapshot()` (guarded by a bar-index check) computes once; both consumers read the shared snapshot. The completion marker is set only after the series writes, so an exception mid-compute forces a clean recompute instead of serving a half-built snapshot.

---

## gbKingOrderBlock.cs — the heaviest indicator

**5. Reflection removed from the bar path.**
`ChangePropertiesListActive` resolved `BackupProperties`/`RevertProperties` via `GetMethod()` + `method.Invoke()` and allocated an `object[]` **per order block per first-tick-of-bar** (decompiler residue from the ninZa original). All element types already derive from `BackupExtension`, so the generic now carries a `where TValue : BackupExtension` constraint and calls directly.

**6. Property-pair resolution cached per type.**
Inside `BackupExtension.BackupOrRevertProperties`, every call re-checked the `[BackupProperties]` attribute and re-resolved the `"Backup"`-stripped source property by reflection, for every property of every element. The cache now stores interleaved `[backup, source]` `PropertyInfo` pairs per type (resolved once); per-call work is just the value copies. Same public signature — no caller changes.

**7. O(1) last-element access.**
`SortedList<K,V>` has no `IList` fast path for LINQ, so `.Last()` walked the entire swing/BOS/imbalance lists every first-tick-of-bar. Replaced with `Keys[Count-1]` / `Values[Count-1]`.

**8. Inactive zone lists are now pruned.**
Broken order blocks / imbalances moved to the inactive lists and stayed there forever; `OnRender` enumerated both lists in full every frame. `PruneInactiveZoneLists()` now evicts entries keyed more than **2× `OrderBlockAge`** bars back (default 500 → ~1000 bars kept), once per bar. Since zones leave the active lists within `OrderBlockAge` bars of creation, everything evicted has been off the right edge for at least a full `OrderBlockAge` window.
> **The one visible trade-off:** scrolling back more than ~2×OrderBlockAge bars no longer shows old inactive zones. Raise `OrderBlockAge` to keep more history.

**9. SharpDX resources cached.**
`DrawOneBox` created and disposed a `GradientStopCollection` + `LinearGradientBrush` (or solid DX brush) **per visible zone per frame**; lines, swing points, and text did the same. Now: solid brushes cached per source WPF brush, gradient brushes created once per gradient-stop array (only start/end anchors updated per zone), text formats cached per font. Lifecycle follows the standard NT8 pattern — `ReferenceEquals(RenderTarget, …)` gate at the top of `OnRender`, disposal in a new `OnRenderTargetChanged` override and in `State.Terminated`.

---

## gbPANAKanal.cs

**10. `Dictionary<int, LineInfo>` → `SortedList<int, LineInfo>`.**
Two wins: (a) the render loop was calling `ElementAt(i)` per index on a Dictionary — **O(n²) per frame** with inactive lines visible — now O(1) `Values[i]`; (b) "most recent line" was determined by Dictionary insertion-order enumeration, which isn't contractual after the `Remove`/re-add cycles the code performs — keys are bar indexes, so sorted order is the actual intent. `ContainsKey`/`Add`/`Remove`/indexer semantics carry over unchanged.

---

## NewsSignals.cs

**11. Column measurement cached.**
`OnRender` built three DirectWrite `TextLayout`s per news line per frame just to recompute column widths. The list is published atomically as a new instance at most once a minute, so widths are now re-measured only when the list reference (or panel width, which affects wrapping) changes.

**12. Device brushes cached** per source WPF brush (line/background/time-strip colors are a small fixed set of field references). Invalidated with the existing TextFormat disposal on render-target change; disposed in `Terminated`.

---

## gbBarStatus.cs (runs OnEachTick)

**13. Auto-scale rays redraw only on change.**
The two transparent `Draw.Ray` calls (they feed the bound values into auto-scale) were re-issued on **every tick**, but the bounds are derived from prior-bar prices and fixed within a bar. Now gated on the value actually moving (~once per bar). The rays are horizontal and extend infinitely right, so an older anchor bar renders identically.

**14. Brush churn removed from `OnRender`.**
Stable brushes (bound strokes, label text) go through a per-WPF-brush DX cache. `eBrush` is deliberately *excluded* from the cache — in gradient mode it's a brand-new WPF brush every frame (dictionary would leak) — so it gets exactly **one** DX conversion per frame shared by the progress bar, info text, and symbol (previously four).

---

## gbSuperJumpBoost.cs

**15. Same DX brush cache** for all five per-frame sites: bar-highlight fills, zone lines, extremum levels, and marker text. All brushes there are stable field/property references. Same lifecycle (OnRender gate + Terminated disposal).

---

## Not changed

gbThunderZilla, gbSumoPullback, and gbNobleCloud reviewed clean — per-bar state machines with no hot-path issues. Two low-value items were deliberately skipped: time-anchoring the strategy's completed trade markers, and throttling the HUD snapshot during historical replay.

## Verification notes for testers

- HUD should show **v1.9.3**.
- Signals, entries, arrows, back-brushes, zones, and lines should be pixel/tick-identical to 1.9.2.
- Only expected difference: KO inactive zones older than ~2×OrderBlockAge bars are no longer drawn when scrolling far back.
- Watch for: memory staying flat over a multi-hour session (previously the KO inactive lists and per-frame DX allocations crept), and lower CPU during fast markets on charts with KO + PANA loaded.
