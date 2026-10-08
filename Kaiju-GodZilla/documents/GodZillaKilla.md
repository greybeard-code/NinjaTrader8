# GodZillaKilla — ATM Trading Strategy

**Version:** 1.9.4
**Namespace:** `NinjaTrader.NinjaScript.Strategies.Playr101`
**Author:** Playr101
**Credits:** GreyBeard, ninZa.co, RenkoKings, ES, rbro112

GodZillaKilla is a NinjaTrader 8 strategy that reads signals from the six GodZilla Suite sub-indicators and executes trades using either NT8 ATM templates or strategy-managed Fixed-Ticks orders. It is designed for live and replay trading on any chart type.

---

## Version History

| Version | Summary |
|---|---|
| **1.9.4** | **Startup performance controls (new, default off).** New `Historical Processing Mode` property: `FullHistoricalProcessing` (default) preserves prior behavior; `SignalWarmUpOnly` lets child indicators warm up through history while GodZilla skips its own historical management, execution, historical tick-series work, alerts, and strategy-owned historical drawings — lighter startup when running several live/Sim ATM instances at once. New **Performance / Historical** category adds `Process Historical 1-Tick Series`, `Draw Historical Signal Arrows`, `Draw Historical Background Colors`, `Draw Historical T Markers`, and `Draw Historical ATM Entry/Exit Markers` (all apply only in FullHistoricalProcessing), plus `Load Only Required Signal Engines` — skips instantiating child indicators whose signals aren't used by Set 1, an enabled Set 2, or a chart display toggle; `OnBarUpdate`'s null-guard now only requires the engines actually in use. `Show Bar Status Indicator` now defaults to off for new instances (per-chart convenience, not needed for trading). All new properties default to legacy full-processing behavior — existing charts and templates are unaffected until explicitly changed. |
| **1.9.3** | **Performance pass (no signal/entry behavior change).** Data thread: early bail in the pending-entry tick handler, session-window times cached as ints, ATM position reads routed through the per-tick memo, and the six-signal read/normalize/write pipeline deduplicated to one compute per bar shared by visuals and entry logic. gbKingOrderBlock: reflection removed from the per-bar backup/revert path, O(1) SortedList last-element access, inactive zone lists pruned at 2×OrderBlockAge, and SharpDX brushes/gradients/text formats cached with proper RenderTarget lifecycle. gbPANAKanal: `Dictionary` → `SortedList` for line info, fixing an O(n²)-per-frame render loop. NewsSignals, gbBarStatus, and gbSuperJumpBoost also received render-thread brush/measurement caching. Only visible difference: KO inactive zones older than 2×OrderBlockAge no longer draw on far scroll-back. Full write-up in [GodZilla-1.9.3-Performance-Pass.md](GodZilla-1.9.3-Performance-Pass.md). |
| **1.9.2** | Trade log trimmed to 11 columns — removed `SignalCombo`, `UsedSignals`, and `LastTradeLine`, which duplicated the `Trigger` column (e.g. `SET1-G3:KO+PA+SU`). Dropped the now-dead `TradeRecord` fields and per-row `Build*` computations that only fed those columns; `TradeResult` retained. No change to signals, entries, or PnL accounting. |
| **1.9.1** | **Performance & stability pass (no signal/entry behavior change).** Per-tick hot path trimmed: the playback/News-Filter connection check is cached (no reflection per tick), the ATM market-position lookup is fetched once per tick (staleness backstop and close-detection poll share it during open trades), and signal reads use a series-based `SafeSignalRead` overload that avoids a `Func<double>` closure allocation per read. Trade markers (`_markerList`) are now bounded — completed markers older than `DRAW_TAG_KEEP` bars are evicted with their draw objects, so the list no longer grows unbounded over long sessions. Removed a dead per-bar group-evaluation block, removed the vestigial always-true `_strategyEnabled` gate (master arm is `_autoArm`), and removed a cross-thread `BuildDashboardSnapshot()` call from the Auto-Arm button handler. Defense #8 forced-close PnL estimate now uses `Math.Round` instead of `RoundToTickSize`; martingale-close totals refresh inline instead of one tick late. |
| **1.9** | **Confirmation Bars** — new `Confirmation Bars` property (range 0–25, default 0). When set to N, the entry is held after the group trigger fires. On bar N, if price has moved in the signal direction (close higher than signal bar for long, lower for short), the entry submits. If not, the setup is dropped. Default 0 = immediate entry, identical to prior behavior. **Trade Marker** — new `Show Trade Marker` property (default on) draws a green `T` below the bar on long entries and a red `T` above on short entries, visible on both historical and realtime bars. **Enable Signal Tracking** moved from Signals to Dashboard Display section alongside the stats display properties it controls. |
| **1.8.4** | Signal operator defaults changed from `Equal` to `GreaterOrEqual` (long) / `LessOrEqual` (short) for all 12 indicator pairs in Set 1 and Set 2. This matches GodZuki's defaults and accepts any signal value at or beyond the threshold (e.g. KO raw=2 now counts as a long vote under the default KO_LongValue=1). Existing saved chart templates are unaffected — serialized operator values are preserved; only fresh instances and "Reset to Defaults" are impacted. |
| **1.8.3** | **ATM Playback reliability — complete trade lifecycle rewrite.** Previous versions detected trade open/close and computed PnL entirely via ATM API polling (`GetAtmStrategyMarketPosition`, `GetAtmStrategyRealizedProfitLoss`). `GetAtmStrategyRealizedProfitLoss` returns 0 in Playback, causing $0 PnL and no CSV log. 1.8.3 introduces `AtmOpenTrade` — an object populated at entry fill time capturing price, quantity, direction, and signal trigger. PnL is computed from fill prices (`(exitPx – entryPx) × PointValue × qty`), which is accurate in all modes. Entry fill detection uses `OnExecutionUpdate` (fires in Sim/Live) supplemented by `GetAtmStrategyMarketPosition` polling in the tick handler (catches Playback where the execution event does not fire). Close detection uses position polling via `GetAtmStrategyMarketPosition`, with price-based PnL as fallback when the API returns 0. **Defense #9 hardened:** added an age guard (`_atmIdsSetUtc` must be older than the registration timeout) so Defense #9 cannot fire during the brief window between `AtmStrategyCreate` callback and position confirmation on a live entry — previously this cleared freshly submitted IDs before the fill could arrive. **Signal Data Box plots removed:** the 15 transparent plots added in 1.8.2 (Set1/Set2 group signals, S1\_/S2\_ per-indicator signals, Both signal) are removed — GodZuki is the correct place to expose Data Box signal output. **Debug output cleaned up:** all `DIAG:*` diagnostic prints are now gated on `EnableDebug`; `DAILY PNL CHECK` throttled to once per 5 seconds (previously fired at tick rate). |
| **1.9** | **ATM reliability hardening.** Defense #9 added: evicts zombie ATM IDs in the dead zone where `isAtmStrategyCreated=true` but `_atmPositionConfirmed=false` (neither Defense #3 nor #8 could fire). `State.Terminated` now clears all ATM fields so stale IDs cannot survive a disable/re-enable cycle. ATM template pre-flight validation added: `ValidateAtmTemplate()` checks the template XML file exists on disk before any IDs are generated — a missing template logs a warning at enable time, blocks the entry cleanly (no zombie IDs created), fires a chart `Alert`, and draws a centered on-chart overlay visible even when the dashboard or control panel are hidden. Martingale template validated independently with elevated alert copy ("recovery blocked"). `AtmStrategyCreate` callback extended to handle non-`NoError` codes: clears IDs immediately on failure rather than waiting for Defense #3's 10-second timeout. |
| **1.8** | Control panel visual overhaul to "noble" dark navy style (matching Whisky). Gradient+glow title text, SVG pill minimize button, custom ControlTemplate buttons with hover/press effects. `ControlPanelSize` property added (`Large`/`Medium`/`Small`/`Minimized` = 100%/75%/50%/title-only). Double-click cycles all four states; pill button toggles `Minimized` ↔ `Large`. CSV log filename now uses `Time[0]` (bar time) instead of `DateTime.Now`, so replay/playback sessions produce correctly dated files. |
| **1.7.4** | Control panel converted to floating draggable panel (Whisky style). Title bar drag to reposition; double-click title bar cycles scale (100% → 75% → 50%); `▼`/`▶` minimize button collapses body to title bar only. Account name added below Instrument. `ControlPanelLeft`/`ControlPanelTop` properties persist position across chart reloads. Dashboard Display properties reordered: HUD settings first, control panel settings at bottom. |
| **1.7.3** | Per-indicator **Require** flags added for both Set 1 and Set 2. When a `Require` flag is enabled, that indicator must be among the signals that fired in the trigger direction — a count that reaches Required Count without the required indicator does not trigger. Defaults to false (no change to existing behavior). HUD signal tracking split into two lines: `Set1 Enabled:` and `Set2 Enabled:` (Set 2 line hidden when Set 2 is disabled). Required indicators are prefixed with `+` on both lines. |
| **1.7.2** | Session PnL reset now fires from `Bars.IsFirstBarOfSession` on the primary bar series at the correct futures session open (e.g. 1700 CST for ES), not from the tick series at midnight. Martingale recovery blocked and `EnableMartingaleOnStopLoss` hidden in FixedTicks mode. `NC_Brush` hidden when `UseNCSignals = false`. NobleCloud Properties panel labels renamed from "NC:" to "NobleCloud:". |
| **1.7.1** | `LogEnabled` defaults to true (required for MONARCH trade ingestion). ATM strategy field now shows a dropdown populated from ATM templates on disk (`FriendlyAtmConverter`). Descriptions added to all Properties panel fields. Namespace import updated to `GreyBeard`. Sub-indicator enums and category attributes moved to namespace scope. |
| **1.7.0** | Reentrancy guard added to `FlattenEverything` (`_flattenLock` / `_flattenInProgress`) to prevent double-flatten on rapid tick sequences. `Account.Positions` iteration wrapped in `lock (Account.Positions)`. |
| **1.6.9** | `gbBarStatus` sub-indicator added with `ShowBarStatusIndicator` property. Dashboard auto-sizing: HUD box width now measured dynamically from text content via `MeasureHudTextWidth()` using DirectWrite TextLayout — eliminates text clipping at all sizes. Properties panel reorganized: "Display" split into "Dashboard Display" / "Indicator Display" / "ATM Marker Display". `IsExitOnSessionCloseStrategy` default changed to true. `UseNCSignals` default changed to false. |
| **1.6.6** | NobleCloud integration as 6th signal source (Set 1 and Set 2). OnOrderUpdate rejection handling for FixedTicks mode. RealtimeErrorHandling changed to StopCancelClose. Defense #3/#8 extended to martingale ATM path. WPF button click handler unsubscribe on disable (defense #5). _tradeMap upgraded to ConcurrentDictionary. FixedTicks PnL baseline for fresh-start accuracy. SafeSignalRead applied to all six signals. Indicator null diagnostic. Open PnL HUD row conditional on UseUnrealizedPnl. Entry/Exit labels anchor to bar High/Low. Confluence stats replace per-group stats. CSV log expanded to 14 columns. |
| 1.6.5 | Fixed CategoryOrder collision (Display/NobleCloud both at 12). Fixed CSV log header to match 14-column output. Fixed martingale close path to use `WriteTradeLogRecord`. Applied Defense #8 `WriteTradeLogRecord` patches to both normal ATM and martingale ATM stale-ID paths. |
| 1.6.4 | Internal bump by Playr101. |
| 1.6.3 | Added NobleCloud (NC) as sixth signal indicator. Defense #8 mid-trade staleness detection. |

---

## Order Management Modes

### ATM Strategy Mode
Submits a market entry order and immediately attaches a pre-configured NT8 ATM template for stop-loss, profit target, and trailing stop management. ATM templates are selected from the NT8 ATM library at configuration time — the ATM Strategy field shows a dropdown populated from templates on disk.

- Entry is via `AtmStrategyCreate` at bar close (pending signal queued on bar 0, executed on bar 1 tick series)
- Supports a one-time **Martingale Recovery** entry in the opposite direction after a stop-loss, using a separate ATM template
- Optional **ATM Plot Markers** draw entry/exit lines and labels directly on the price panel

### Fixed Ticks Mode
Strategy-managed entries with configurable quantity, stop-loss ticks, and profit target ticks. Supports optional breakeven logic (move stop to entry ± offset after price moves a set distance in favor).

---

## Signal System

### Sub-Indicators
GodZillaKilla instantiates all six GodZilla Suite sub-indicators at `State.DataLoaded`. Each exposes a `Signal_Trade` series; the strategy reads `Signal_Trade[0]` every bar close.

### Signal Configuration
Each indicator has independent Long and Short threshold values and comparison operators (`Equal`, `GreaterOrEqual`, `GreaterThan`, `LessOrEqual`, `LessThan`, `NotEqual`).

**Default operators:** `GreaterOrEqual` for long signals, `LessOrEqual` for short signals. Combined with the default threshold values (KO/SJ/SU/NC = ±1, PA/TH Set 1 = ±2, PA/TH Set 2 = ±3), a signal fires when the sub-indicator value meets or exceeds the threshold in the trigger direction — capturing strong signal magnitudes (e.g. KO breakout at raw=2) as well as the baseline magnitude.

### Trigger Sets
Two independent group trigger sets can be configured:

| Setting | Description |
|---|---|
| **Set 1 Required Count** | Minimum number of enabled Set 1 signals that must agree on the same bar |
| **Set 2 Required Count** | Minimum number of enabled Set 2 signals that must agree |

**Required Count behavior:** With N indicators enabled and Required Count = R, the trigger fires when at least R signals agree in the same direction (long or short). Flat signals (0) are ignored. If both long and short sides both reach R on the same bar, the conflict guard suppresses the trigger. Setting Required Count = N effectively requires all enabled signals to agree.

If both sets fire in conflicting directions on the same bar, no entry is taken.

### Require Flags
Each indicator in Set 1 and Set 2 has a corresponding **Require** flag (`Set 1 Require KingOrderBlock`, `Set 2 Require PANAKanal`, etc.). All default to `false`.

When a Require flag is enabled, that indicator must be **one of the signals that actually fired** in the trigger direction on the entry bar. Meeting the Required Count without the required indicator vetoes the trigger entirely.

**Example:** Set 1 has 6 indicators enabled, Required Count = 3, PANAKanal is required. If Sumo + JumpBoost + ThunderZilla all fire long (`longAgree = 3 = needed`) but PANAKanal did not fire, the trigger is suppressed. PANAKanal must appear among the 3 agreeing signals for the entry to proceed.

Multiple indicators can be required simultaneously — all required indicators must be present in the agreeing set.

### EMA Filter
Optional EMA filter using a short and long period. When enabled:
- Long entries require short EMA > long EMA (bullish trend)
- Short entries require short EMA < long EMA (bearish trend)

---

## Filters

### Session Time Filters
Up to three configurable trading windows (TF1, TF2, TF3) with optional flatten-at-window-end per window. An additional Skip Window suppresses entries during a configurable midday or news window.

### News Filter
Integrates with `gbNewsSignals` for real-time economic calendar blocking. Configurable pre/post block minutes, impact level toggles (High / Medium / Low), and NT8 Alert integration. Live chart only — automatically disabled in Strategy Analyzer and Market Replay.

---

## Risk Management

| Feature | Description |
|---|---|
| **Daily Profit Target** | Flattens all positions and disables entries when total PnL reaches the target |
| **Daily Loss Limit** | Same for loss side |
| **Use Unrealized PnL** | Includes open position PnL in the daily limit calculation |
| **Start Fresh On Enable** | Ignores historical trade PnL when the strategy is enabled in realtime |
| **Martingale On Stop Loss** | Fires one recovery trade in the opposite direction after a stop-loss event |

---

## Defense Mechanisms

GodZillaKilla includes nine layered defenses against NT8 lifecycle edge cases:

| Defense | Trigger | Action |
|---|---|---|
| #1 | Entry order fills before ATM registration | Position adopted; ATM state corrected |
| #2 | Duplicate fill events | Second fill ignored via execution ID tracking |
| #3 | ATM ID never confirmed (registration timeout) | Stale ID cleared after 10 seconds |
| #4 | Draw object pool exhaustion | Rolling 250-bar cleanup of per-prefix draw tags |
| #5 | Naked position at strategy enable | Detects and adopts pre-existing account position |
| #6 | Position inherited from prior session | Captured as baseline; PnL calculated from delta |
| #7 | `TryGetAtmMarketPositionSafe` failure | Falls back to account-level position check |
| #8 | Mid-trade ATM ID goes stale (HDS bounce) | Writes trade log, flattens at account level, resets all ATM state |
| #9 | Zombie ATM ID surviving a disable/re-enable cycle | Dead-zone eviction: `isAtmStrategyCreated=true` but `_atmPositionConfirmed=false` — neither #3 nor #8 can fire; #9 clears the ID immediately |

**Defense #8 detail:** Fires inside `EvictStaleAtmIdsIfTimedOut` on every tick. Detects a mismatch between the ATM reporting Flat and the account still holding a position. `WriteTradeLogRecord` is called **before** clearing `_atmPositionConfirmed` — both the normal ATM path and the martingale ATM path are protected. The estimated PnL from `dailyUnrealizedPnL` is used for the forced-close log record.

**Defense #9 detail:** Targets the dead zone that neither #3 nor #8 covers. Root cause: NT8 reuses the same strategy C# instance across disable/re-enable; `State.Terminated` previously did not clear ATM fields; `State.Realtime` resets `_atmPositionConfirmed=false` but not `atmStrategyId`. A trade that closed while the strategy was disabled leaves `isAtmStrategyCreated=true` and a non-empty `atmStrategyId` — blocking the entry guard and flooding the NT8 trace with "does not exist" errors at tick rate. Primary fix is `State.Terminated` clearing all ATM fields; Defense #9 is belt-and-suspenders for cases where `Terminated` does not run (crash, NT8 internal error). **Age guard (v1.8.3+):** Defense #9 adds the condition that `_atmIdsSetUtc` must be `DateTime.MinValue` (cleared between sessions) or older than `ATM_REGISTRATION_TIMEOUT_SEC`. This prevents the defense from misfiring during the brief window between the `AtmStrategyCreate` callback setting `isAtmStrategyCreated=true` and the entry fill arriving on the next tick.

**FlattenEverything reentrancy guard (v1.7.0+):** `_flattenInProgress` flag and `_flattenLock` prevent double-flatten when rapid ticks fire the method concurrently. The inner check inside the lock ensures thread safety under NT8's mixed threading model.

---

## ATM Template Validation

GodZillaKilla validates both the normal and martingale ATM template files at two points:

**At enable (`State.Realtime`):** Both template names are checked against the NT8 templates folder on disk. A missing template:
- Prints a warning to the NT8 Output window with the full expected file path
- Sets `_templateWarningText` so the on-chart overlay is shown immediately

**At entry time (before `AtmStrategyCreate`):** If the template file is still missing when a signal fires, the entry is aborted before any IDs are generated. This prevents the zombie ID scenario entirely — no IDs means no polling loop, no trace flood. The chart `Alert` fires at `Priority.High`.

**On-chart overlay:** A centered red warning box is rendered on the chart canvas via SharpDX — visible even when `ShowDashboard = false` or `DashboardPosition = Hidden`. Line 1 always reads `⚠  ATM TEMPLATE MISSING`; Line 2 shows the specific template name(s). The overlay clears automatically when the strategy is disabled (`State.Terminated`) and is suppressed at next enable if the template has been restored.

**`AtmStrategyCreate` callback:** If NT8 reports a non-`NoError` callback code (e.g., file loaded but strategy rejected), IDs are cleared immediately rather than waiting for Defense #3's 10-second timeout.

To restore a missing template: open the NT8 ATM Strategy Manager, recreate the template with the exact name shown in the warning, save it, then disable and re-enable GodZillaKilla.

---

## Dashboard (HUD)

The SharpDX overlay panel shows:
- Strategy name and version
- Master arm status (ENABLED / DISABLED) with L / S / REV sub-status
- Session status (IN SESSION / OUT OF SESSION)
- News filter status (if enabled)
- Strategy PnL, Daily PnL, Open PnL (Open PnL row visible only when Use Unrealized PnL is enabled)
- Risk target and loss limit settings
- Current trade status (IDLE / IN POSITION)
- Last trade summary with PnL
- Optional signal tracking stats (per-indicator win/loss counts and confluence combo stats)
  - **Set1 Enabled** row — lists active Set 1 indicators; required indicators are prefixed with `+` (e.g. `KO, +PA, TH`)
  - **Set2 Enabled** row — same for Set 2; hidden when Set 2 is disabled

The HUD box auto-sizes to fit its content — width is measured dynamically using DirectWrite TextLayout so text is never clipped regardless of font size or dashboard size setting.

Position: configurable (`TopLeft` / `TopRight` / `BottomLeft` / `BottomRight` / `Center` / `Hidden`).
Size: `Tiny` / `Small` / `Normal` / `Large` / `Huge`.

---

## Control Panel

A floating WPF panel in the "noble" dark navy style shows instrument name, account name, and the five control buttons (ARM LONG / ARM SHORT / REV / AUTO ARM / CLOSE ALL). It can be freely repositioned and resized:

| Interaction | Effect |
|---|---|
| Drag title bar | Move the panel anywhere on the chart |
| Double-click title bar | Cycle size: `Large` → `Medium` → `Small` → `Minimized` → `Large` |
| Click pill button | Toggle `Minimized` (title bar only) ↔ `Large` |

`ControlPanelLeft` / `ControlPanelTop` are saved back to the strategy properties on mouse-up, so the panel reappears in the same location after a chart reload. `ControlPanelSize` persists the last scale state.

Button functions:
- **ARM LONG / ARM SHORT** — arm or disarm each direction independently; active state shows green/red glow
- **AUTO ARM** — master toggle; ON re-arms both directions and REV, OFF disarms all three
- **REV** — enable/disable reverse-on-opposite-signal
- **CLOSE ALL** — flatten all positions immediately

---

## Audio Alerts

- Individual signal alerts — fires when a single sub-indicator signal passes the filter
- Group trigger alerts — fires on a Set 1 or Set 2 confluence trigger
- Both have independent sound file selection and per-bar deduplication

---

## CSV Trade Log

When `LogEnabled = true` (default), a CSV file is created at `State.DataLoaded`:

**Filename:** `GodZilla_[AccountName]_YYYYMMDD_HHmmss.csv`

**Columns (14):**
`OpenTime, Account, Instrument, OpenPrice, Qty, CloseTime, Trigger, Direction, AtmStrategyName, RealizedPnL, SignalCombo, UsedSignals, TradeResult, LastTradeLine`

One row is written per closed trade. Defense #8 forced-close events also write a log row using the estimated unrealized PnL at time of detection. These CSV files are read by the MONARCH Intelligence Report System to generate daily and weekly performance reports.

---

## Key Properties Quick Reference

| Category | Key Properties |
|---|---|
| ATM Parameters | `OrderMode`, `AtmStrategy`, `MartingaleAtmStrategy`, `FixedOrderQuantity`, `FixedStopLossTicks`, `FixedProfitTargetTicks` |
| Signals | `ConfirmationBars`, `GroupTriggerSet1RequiredCount`, `UseKOSignals`…`UseNCSignals`, `RequireKOSignal`…`RequireNCSignal`, `KO_LongOperator`…`NC_ShortValue`, `G2_RequireKOSignal`…`G2_RequireNCSignal` |
| Filters | `EnableEmaFilter`, `EmaShortPeriod`, `EmaLongPeriod`, `EnableNewsFilter` |
| Session | `EnableTF1`…`EnableTF3`, `StartTime1`…`EndTime3`, `EnableSkipTimeWindow` |
| Risk | `EnableDailyProfitTarget`, `DailyProfitTarget`, `EnableDailyLossLimit`, `DailyLossLimit` |
| Dashboard Display | `ShowDashboard`, `DashboardPosition`, `DashboardSize`, `EnableSignalTracking`, `ShowIndividualSignalStats`, `ShowGroupSignalTrackingStats`, `ShowControlPanel`, `ControlPanelPosition`, `ControlPanelLeft`, `ControlPanelTop`, `ControlPanelSize` |
| Indicator Display | `ShowTradeMarker`, `ShowBarStatusIndicator` |
| ATM Marker Display | `ShowEntryExitMarkers` |
| Audio Alerts | `EnableSignalAudioAlerts`, `IndividualSignalAlertSound`, `GroupSignalAlertSound` |
| Logging | `LogEnabled`, `EnableDebug` |

---

← [README.md](../README.md)
