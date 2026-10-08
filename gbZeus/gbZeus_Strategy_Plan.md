# gbZeus — native strategy to replace PredatorX (design & plan)

> **Status: built and shipping — currently v1.4.5.** This document is the
> original design rationale, kept because the *why* still holds. Where the
> shipped code diverged from the plan, the divergence is marked **[as built]**
> inline. For how to actually run the strategy, read `gbZeus_Trading_Guide.md`;
> for the change history, read the changelog at the top of `NT8 code/gbZeus.cs`.

## What actually shipped vs. this plan

| Planned | As built |
|---|---|
| Single entry window 10:15–15:00 ET | **Five** independent entry windows + **one skip window**, GodZillaKilla layout (v1.2.0 → 3 windows, v1.4.0 → 5). Window 1 still ships on at 10:15–15:00 — read in **chart time**, not pinned to ET. Windows 2–5 ship off, pre-filled at 07:00 / 11:00 / 13:00 / 21:00 (one hour each) as untested suggestions. |
| "One position at a time; ignore opposite signals while in a trade" | Configurable **`On Reverse Signal`**: `DoNothing` (default as of v1.4.x) / `ClosePosition` / `CloseAndReverse` (v1.3.0). Same-direction signals are still ignored while in a trade. |
| `EntryStartTime` / `EntryEndTime` | `EnableSession` / `SessionStart` / `SessionEnd` (+ `…2` … `…5`), `EnableSkipWindow` / `SkipStart` / `SkipEnd` |
| `PropDailyLossLimit`, `PropTrailingThreshold` | `UseDailyLoss` / `DailyLoss`, `UseDailyProfit` / `DailyProfit`, `DailyFlatten`, `MaxTradesPerDay`, `MaxStopTicks` |
| `GbPanelCorner` | `ZeusPanelCorner` — house rule: all `gb*` strategies compile into one assembly, so every enum needs a per-strategy prefix |
| `REV` button on the dashboard | **Not built.** The Terminator_V2 reference suite doesn't wire one either. |
| Optional stop-hunt re-entry (`ReenterAfterStop`) | **Not built.** Still open if Python parity ever needs it. |
| Base brackets in `State.Configure` | Brackets are set per entry in `SubmitEntry` via the signal-name `SetStopLoss`/`SetProfitTarget` overloads. No `State.Configure` block. |
| `IsSuspendedWhileInactive` / `ShowTransparentPlotsInDataBox` | Not set — those are the plot-display flags, and this strategy defines no plots. |
| Python↔NT8 parity gate (step 4 below) | **Still outstanding.** So are backtests of everything added after v1.1.0. |

## Review of the Playr101 v1.4.0–v1.4.3 contribution

Three issues found; all resolved in **v1.4.4**.

1. ✅ **Fixed — all five windows shipped ON, contradicting the file's own
   documentation.** `SetDefaults` set `EnableSession2`–`EnableSession5` to
   `true`, while both the v1.4.0 changelog entry *and* the comment directly
   above that code said "windows 2-5 … ship OFF". A fresh chart traded five
   one-hour slots around the clock instead of one. Code now matches the stated
   intent.
2. ✅ **Resolved — `ReverseAction` default was changed to `DoNothing` with no
   changelog entry.** Confirmed deliberate and kept: `DoNothing` is the original
   pre-v1.3.0 behaviour and the conservative default (hold to the candle-back
   stop or band target; act on an opposing signal only if the user opts in).
   Now recorded in the v1.4.4 changelog so it isn't re-flagged.
3. ✅ **Fixed — window 1's default had moved off the design point.** v1.4.0 set
   it to 04:00–05:00; v1.4.4 restores **10:15–15:00**, the only session
   configuration any published number for this strategy was measured on. The
   shipping default is therefore parity-ready again. Windows 2–5 keep their
   pre-filled 07:00 / 11:00 / 13:00 / 21:00 suggestions (all off, none
   backtested); the 04:00 suggestion no longer has a slot.

The rest of the contribution reviewed clean: the 3→5 window extension is a
faithful widening (`_wasInWindow` resized, all five wired into `InEntryWindow`,
the flatten latch and `WindowText`), the v1.4.2 whole-grid property hiding is
consistent with what each setting actually feeds, and the v1.4.3 "Chart Time"
relabel corrects a genuinely misleading "ET" suffix — the window logic always
read `Time[0]`, so the times were never pinned to Eastern.

## v1.4.5 — dashboard spiderweb-count fix

A later doc/code review found the dashboard's spiderweb readout (`Web N/5`,
`STAND-ASIDE`) hardcoded the `5` instead of reading **Spiderweb Line Count**.
Cosmetic only: actual entry suppression always read the real setting via the
indicator's `SpiderwebWarning`, so nobody was mistakenly let into or kept out
of a trade by this — the display was just wrong for anyone who changed
Spiderweb Line Count away from its default of 5. Fixed in `UpdateDashboard`.

## Why replace PredatorX

PredatorX consumes the `GodTrades` indicator's `SignalCode` and manages orders,
but it **cannot do the one exit the methodology hinges on**: a target that
tracks the *opposite Bollinger band, re-priced every bar*. Its brackets are
tick/RR/ATR/trail only, and it does not read the indicator's `SuggestedStop/
TargetPrice` plots. The Zekey template therefore fell back to fixed-tick exits,
which the backtest showed are the losing regime (see
`GodTrades_Zekey_Evaluation.md`: -$32k @1000t; the fixed 60t target collapses
avg win $730→$283, dropping R:R 2.24→1.11).

A native NinjaScript strategy owns its exits and can implement the faithful
band-to-band target + candle-back stop directly. It also removes the
third-party dependency and makes the NT8 side trade-for-trade comparable to the
Python backtester (`strategies/god_trades.py`), which PredatorX never was.

## Architecture — keep the indicator as the single source of signal truth

```
GodTrades.cs (v16.6, unchanged)            gbZeus.cs (new)
  gap engine → SignalCode, MasterLong/    ── reads indicator plots each bar
  Short, SuggestedStopPrice,                 → owns entries, stop, dynamic
  SpiderwebWarning (public Series)             band target, session, sizing
```

- `gbZeus` instantiates the `GodTrades` indicator in `State.DataLoaded`
  (via the generated `GodTrades(...)` factory) and holds the reference. It does
  **not** re-implement gap logic — the indicator stays the one authority, so
  chart visuals and strategy decisions can never diverge.
- Signals are read from `gt.SignalCode[0]` (`±1` BG, `±2` FC) — or
  `gt.MasterLongSignal/MasterShortSignal`. Both signal types tradeable, each
  behind its own enable toggle (mirror the Python `enable_bg/enable_fc`).
- `Calculate = OnBarClose` to match the indicator and the Python engine.

### The one subtlety: the band target must be LIVE, not the frozen plot
`GodTrades.SuggestedTargetPrice` is only written on signal bars (it's the band
*at signal time*). For a target that tracks the band each bar, the strategy
computes its **own** `Bollinger(BollingerStdDev, BollingerPeriod)` with
parameters kept identical to the indicator, and reads `Upper[0]/Lower[0]` every
`OnBarUpdate`. The candle-back stop, by contrast, is static once set, so it can
be captured from `gt.SuggestedStopPrice[0]` (or `Low[0]/High[0]`) at entry.

## Execution rules (methodology-faithful)

- **Entry:** on a valid `SignalCode` while flat and in the session window,
  submit a market order (fills next bar open — same 1-bar posture PredatorX had
  with `ObjectEntryBarsAgo=1`). One position at a time; ignore further
  same-direction signals while in a trade.
- **Reverse signal** *(v1.3.0)* **[as built]**: a signal *against* an open
  position is handled by `On Reverse Signal` — `DoNothing` (default; the
  original ride-to-stop behaviour), `ClosePosition`, or `CloseAndReverse`.
  The close half is deliberately ungated; the reverse half must clear every
  entry filter (`EntryAllowed`, shared with the flat-entry path) and degrades
  to a plain close when it cannot.
- **Stop:** back of the signal candle ± `StopOffsetTicks`, from the indicator's
  suggested price. Set via `SetStopLoss(signalName, CalculationMode.Price,
  price, false)`; **never moved to break-even automatically**, never trailed.
  This is the deliberate opposite of the Zekey template. (The dashboard's
  manual `MOVE SL TO BE` button is the one sanctioned exception — see
  conventions below.)
- **Target:** the opposite Bollinger band, re-priced every bar via
  `SetProfitTarget(signalName, CalculationMode.Price, band)` — the signal-name
  overload updates the live order tied to that entry. Long → `Upper[0]`,
  short → `Lower[0]`. Entries therefore need **explicit signal names**
  (`EnterLong(Quantity, "gtLong")`), not the anonymous default.
- **Session:** entries gated to 10:15–15:00 ET; flatten open positions at
  window close. Gap lines still accumulate 24h (the indicator runs all session);
  only *entries* are gated — same as `god_trades.py`. **[as built]** widened in
  v1.2.0 to three independent windows (each with its own flatten-at-end flag)
  plus one skip window that blocks entries without ever flattening, then to
  **five** windows in v1.4.0. Flatten is edge-detected per window and latched,
  so back-to-back windows hand a trade over instead of flushing it. As of
  v1.4.4 only window 1 ships on, at the original 10:15–15:00 chart time, so the
  parity posture vs. `god_trades.py` is unchanged from v1.1.0.
- **Spiderweb stand-aside:** skip entries while `gt.SpiderwebWarning[0] == 1`
  (the rule PredatorX left purely visual). Toggle: `SpiderwebSuppress`.
- **Sizing:** 1 NQ default (`Quantity`), the deck's instrument. Prop-firm
  guardrails (below) rather than PredatorX's 5-contract scale-out.
- **Optional — stop-hunt re-entry** (`ReenterAfterStop`, default off): if the
  original entry price prints again within `ReenterWindowBars` after a stop, and
  the candle closes back in the trade direction, re-enter with a fresh
  candle-back stop. Mirrors `god_trades.py` so parity holds. **[as built] not
  implemented** — still open if the parity gate needs it.

## Config surface (mirror the Python names for 1:1 parity)

Signal (pass-through to the indicator, or duplicate to keep the strategy self-
contained): `MinimumGapSizeTicks`, `MinimumBarsBeforeValid`, `BollingerPeriod/
StdDev`, `BollingerBandProximityTicks`, `ConfirmationBarsAfterTouch`,
`ContinuationConfirmationMode`, `EnableBollingerGap`, `EnableContinuation`,
midpoint-filter percents. Execution: `Quantity`, `EntryStartTime/EntryEndTime`,
`FlattenAtWindowEnd`, `StopOffsetTicks`, `SpiderwebSuppress`, `ReenterAfterStop`,
`ReenterWindowBars`, plus an `ExitMode { BandTarget | FixedTicks }` so the fixed
regime can be A/B'd against band-to-band inside one binary. Trade management
(GreyBeard standard names, verbatim): `ManualNudgeTicks` (default 4),
`ManualBeOffsetTicks` (default 0, signed). Dashboard: `ShowDashboard` (true),
`DashboardCorner` (`GbPanelCorner`, default TopLeft), `DashboardStartMinimized`
(false). Groups are numbered (`"0. Developer"`, `"1. …"`) to force grid order.

**[as built]** the shipped groups are `0. Developer`, `1. Signal`, `2. Session`,
`3. Exits`, `4. Filters`, `5. General`, `6. Dashboard`, `7. Manual Control`,
`8. Risk`. Names that changed from the plan are in the table at the top. The
session group hides the times belonging to a disabled window via
`ICustomTypeDescriptor` (the same pattern GodZillaKilla uses), which is what
makes the twenty window properties tolerable in the grid. v1.4.2 extended that
hiding to the whole grid — BG-only and FC-only settings, the midpoint children,
exit-mode-specific settings, the dashboard/manual block and the daily-risk
children all hide when their controlling switch is off, via a single
`ModifyConditionalProperties`. v1.4.1 added `[RefreshProperties(All)]` to every
conditional child so the grid re-queries immediately on any edit, not just on
the Enable toggles.

## Prop-firm guardrails (match the user's other strategies)
Daily realized-loss lockout, optional per-trade max loss, min-hold gate, and
end-of-session flat. Surface `PropDailyLossLimit`, `PropTrailingThreshold` as
inputs. Backtest already shows 1 NQ breaches a $2k trailing floor, so these are
information/kill-switch, not a fix — sizing to the floor is a separate question.

## Validation workflow (the established Python↔NT8 parity gate)
1. Python is the reference: `strategies/god_trades.py` (band exit, session
   gated) already runs over full history.
2. Build `gbZeus`, run it in **NT8 Playback** on the same NQ 1000-tick
   dates.
3. Export executions → `tools/convert_nt8_executions.py` →
   `tools/compare_nt8.py` against the Python trades CSV (the GodZillaKilla
   precedent). Reconcile the known ~1-bar entry offset; expect matching entry
   bars, stop/target prices within a tick, and matching P&L sign per trade.
4. Only after parity: forward-test on Sim101, then evaluate live-sizing.

## GreyBeard house conventions (from `../GreyBeard-Typical-NinjaTrader.md`)

Applied by default, independent of the trading logic:

- **Namespace / file / location:** `NinjaTrader.NinjaScript.Strategies.GreyBeard`,
  file `gbZeus.cs` under `bin/Custom/Strategies/GreyBeard/`.
- **`0. Developer` group** (read-only, no `[NinjaScriptProperty]`, expression-
  bodied): `Author => "GreyBeard"`, `Version`, `Website =>
  "https://greybeardconsulting.net/"`. Every `GroupName` is number-prefixed for
  deterministic grid ordering. **[as built]** `Version` returns the `VERSION`
  constant, so the property grid always shows the changelog's current number.
- **Display flags in `SetDefaults`**: `IsSuspendedWhileInactive = false`,
  `ShowTransparentPlotsInDataBox = true`. **[as built] not set** — these are
  the plot-display flags and the strategy defines no plots.
- **Order plumbing:** `StopTargetHandling = PerEntryExecution`; base brackets set
  once in `State.Configure` (ticks); all live stop/target moves through the
  signal-name `SetStopLoss`/`SetProfitTarget(CalculationMode.Price, …)`
  overloads; **never `SetTrailStop`** (NT8 silently ignores it once `SetStopLoss`
  is in play).

### On-chart dashboard + control panel (full suite)

A draggable, minimizable, corner-anchored WPF panel — created only in
`State.Realtime`, torn down in `State.Terminated`, never in Strategy Analyzer:

- **Info rows:** instrument/account, FLAT/LONG/SHORT, key state (session
  open?, spiderweb?, ExitMode), and while in a position Entry / Stop / Target /
  Qty / uPnL.
- **Buttons:** `AUTO ON/OFF`, `LONG ON/OFF`, `SHORT ON/OFF` (LONG/SHORT greyed
  while AUTO off), `REV`, `MOVE SL TO BE`, `SL ▼/▲`, `TP ▼/▲` (▲ always raises
  price, ▼ lowers, for both directions; SL red, TP green), `FLATTEN ALL` /
  `CLOSE ALL` (closes + pauses AUTO).
- **Manual-command pattern (critical):** Click handlers run on the WPF UI
  thread and only set flags (`volatile bool _pendingBE`,
  `Interlocked.Add(ref _pendingSlNudgeTicks, ±ManualNudgeTicks)`), never call
  order APIs. A `ProcessManualCommands()` on the strategy thread drains them
  (`Interlocked.Exchange`) and does the work — called from **`OnMarketData`**
  (tick-level) so FLATTEN/BE act immediately. BE beats a same-window SL nudge;
  validate the new price is on the correct side of market before submitting.
- Frozen `SolidColorBrush` statics via a `MakeFrozen` helper; templated buttons
  (`ControlTemplate` + `TemplateBinding`), not bare `Button`s.

### How the dashboard reconciles with "never break-even"

The methodology forbids *automatic* break-even/trailing, and the strategy honors
that — no auto-BE, no trail. But the GreyBeard suite still ships the manual
`MOVE SL TO BE` and `SL/TP` nudge buttons, which the conventions doc explicitly
calls out as *the one sanctioned place a stop may move backward*, because the
user is intentionally overriding. So: automatic behavior stays faithful; manual
discretion is available. `AUTO OFF` + the manual buttons also turn this into a
usable discretionary cockpit for trading the signals by hand.

## Provenance
- Clean-room: the strategy is original; it only *consumes* the user's own
  `GodTrades` indicator. No PredatorX code is referenced or reproduced.

## Risks / open questions
- **Entry timing parity:** next-bar-open (NT8 market) vs Python's signal-bar
  close. Decide whether to match Python exactly (limit-at-close) or accept the
  realistic 1-bar delay and align the Python run to it. Recommend the latter.
- **Band target never reached before session flat:** the faithful target is
  wide; many trades will exit at the window-close flat, not the band. That's
  the methodology's actual behavior and is already what the Python §6 numbers
  reflect — not a bug, but sets the expectation (this is a ~break-even edge).
- **Indicator-as-strategy-input warning:** referencing an overlay indicator
  that draws objects from inside a strategy is fine, but confirm no double-draw/
  performance issue in Playback; if so, run the indicator in signal-only mode
  (disable its `Draw.*` via the existing show-toggles).
- **Should the strategy re-derive signals instead of hosting the indicator?**
  Hosting keeps one source of truth (preferred); re-deriving decouples but risks
  drift. Recommend hosting.

## Phased build — status

1. ✅ **Done** (v1.0.0). Skeleton: GreyBeard scaffolding (namespace/folder,
   `0. Developer` group, numbered groups, `PerEntryExecution`). Host the
   indicator, read `SignalCode`, signal-named market entry in-session, 1
   contract, band target + candle stop, no BE. Compiles clean.
2. ✅ **Done** (v1.0.1–v1.0.2). Spiderweb stand-aside, window-flat, `ExitMode`
   toggle, risk guardrails.
3. ✅ **Done** (v1.0.0). Dashboard + control-button suite and the `OnMarketData`
   manual-command drain, lifted from the `Terminator_V2.cs` / `GodZillaKilla.cs`
   pattern.
4. ⬜ **Outstanding.** Playback + `compare_nt8.py` parity gate vs
   `god_trades.py` (AUTO ON, dashboard idle) — verify the automated path
   matches the research. **This is the gate that has never been run.**
5. ⬜ **Not started.** Optional stop-hunt re-entry; re-run parity.

Added after the original plan, and **likewise unmeasured**:

6. ⬜ v1.1.0 release hardening; v1.2.0 three windows + skip window; v1.3.0
   `On Reverse Signal`; v1.4.0–v1.4.3 (Playr101) five windows, whole-grid
   property hiding, chart-time labels. All compile clean through NT8's own
   Roslyn (via `cli-nt-bridge`), none has been backtested. `CloseAndReverse` in
   particular changes trade frequency substantially in chop and should be A/B'd
   against `ClosePosition` and `DoNothing` before it goes anywhere near a
   funded account. **The five shipped default windows have no measurement
   behind them at all** — see "Open defects" above.

**Scope check before coding:** the backtest says the underlying edge is
marginal. This strategy is worth building as *faithful tooling* (it makes the
NT8 side finally match the research and removes the third-party black box), but
it is not expected to convert a break-even edge into a winning one. Build it to
trade the methodology correctly and to close the Python↔NT8 loop — not on an
expectation of alpha the data doesn't support.

**And after coding, the same check still applies.** The strategy is built and
compiles clean, but step 4 — the parity gate that was the whole point — has not
been run, and nothing added after v1.1.0 has been measured at all. The extra
windows and `On Reverse Signal` widen what Zeus *can* do; they are not evidence
that any of it is profitable. Treat every setting past the defaults as untested
until the parity gate and a proper A/B say otherwise.
