# gbPullback5Bar

A standalone NinjaTrader 8 strategy implementing a "reversal + N-bar pullback" continuation
pattern — a bar against the prevailing trend (the "pullback" bar), followed by a confirmation bar
that closes back through the pullback bar's open within a limited number of bars, entering in the
original trend direction.

This is an **original implementation**. It was written from the public NinjaScript API after
watching a trading-education video that demonstrated the concept using TradeSaber's commercial
"Predator X" product; it does not use, reference, or derive from TradeSaber's (or any other
vendor's) source code or compiled assemblies.

Built following this project's [GreyBeard-Typical NinjaTrader conventions](../GreyBeard-Typical-NinjaTrader.md)
— `gb`-prefixed namespace, numbered property groups, full on-chart dashboard/control panel, etc.

## Strategy logic

1. Track each closed bar's direction (bullish/bearish by close vs. open).
2. When a bar's direction flips against the prior bar (the "pullback" bar), remember its
   open/high/low and start counting.
3. If, within `PullbackBarLimit` bars (default 5, counting the pullback bar as #1), a later bar
   closes back through the pullback bar's open in the original trend direction, that's the entry
   trigger. Otherwise the setup expires.
4. Long → stop at the pullback bar's low minus `StopOffsetTicks`; Short → stop at the pullback
   bar's high plus `StopOffsetTicks` (default 2 ticks).
5. Fixed profit target in ticks (default 50).
6. Optional session-time filter and daily profit-target / max-loss circuit breakers.

## Dashboard

A draggable, minimizable on-chart panel (realtime only, never shown during Strategy Analyzer
backtests) with:
- Status row: FLAT/LONG/SHORT, and the live pullback-setup state ("Watching UP pullback 3/5")
- Instrument/account, session window state, day P&L
- While in a position: Entry / Stop / Target / Qty / uPnL
- Buttons: `AUTO`, `LONG`, `SHORT` (per-direction auto-entry toggles), `REV` (manual reverse),
  `MOVE SL TO BE`, `SL ▼/▲` `TP ▼/▲` (manual nudge by `ManualNudgeTicks`), `FLATTEN ALL`

## Files

- `gbPullback5Bar.cs` — the strategy. Deploy to `bin\Custom\Strategies\GreyBeard\` in NinjaTrader 8.
- `sa_config.json` — an example [cli-nt-bridge](https://github.com/eman007/cli-nt-bridge) Strategy
  Analyzer config (instrument, bar type, date range, strategy params) used for the backtest below.

## Backtest (informational, not a live-trading guarantee)

Run via `cli-nt-bridge` against NinjaTrader's real Strategy Analyzer (Tick Replay enabled):

| Setting | Value |
|---|---|
| Instrument | NQ 09-26 |
| Bar type | ninZaRenko 64/16 (custom Renko-style add-on bar type) |
| Date range | 2026-08-01 – 2026-09-12 |
| Session filter | 09:30–16:45 |
| Commission | applied (NQ round-trip) |

| Metric | Value |
|---|---|
| Total trades | 450 |
| Win rate | 88.4% (398W / 52L) |
| Profit factor | 6.85 |
| Net profit | $83,218 |
| Max drawdown | -$925 |

**Caveats before trusting this number:**
- The high win rate is largely mechanical: a 50-tick target sits inside a single 64-tick Renko
  brick, so a confirmed continuation entry doesn't need much follow-through to hit target. That's
  not the same as a validated statistical edge.
- Renko/Range-bar backtests are only meaningful with NinjaTrader's **Tick Replay** enabled
  (`IsTickReplay = true` on the Strategy Analyzer's data series) — without it, fills are
  unrealistic (same-bar entry/exit, impossible profit factors). This result was run with Tick
  Replay on.
- Single instrument, single ~6-week window, one parameter set — no out-of-sample or walk-forward
  validation yet. Forward-test on sim before risking real size.

## Configuration reference

| Group | Property | Default | Notes |
|---|---|---|---|
| 1. Strategy | `PullbackBarLimit` | 5 | Max bars (pullback bar = #1) before a confirmation must occur |
| | `StopOffsetTicks` | 2 | Ticks beyond the pullback bar's extreme |
| | `ProfitTargetTicks` | 50 | Fixed target distance |
| | `TakeLongs` / `TakeShorts` | true | Per-direction enable |
| | `Contracts` | 1 | Contracts per entry (stop/target cover the full size) |
| 2. Session | `EnableSession` | true | |
| | `SessionStart` / `SessionEnd` | 09:30 / 16:45 | |
| 3. Risk Management | `ManualNudgeTicks` | 4 | Dashboard SL/TP nudge step size |
| | `ManualBeOffsetTicks` | 0 | Signed; negative locks in a small loss |
| | `DailyProfitTarget` / `DailyMaxLoss` | 0 (disabled) | Blocks new entries once hit |
| 4. Dashboard | `ShowDashboard` | true | |
| | `DashboardCorner` | TopLeft | |
| | `DashboardStartMinimized` | false | |

## Backtesting / deploying it yourself

See [`cli-nt-bridge`](https://github.com/eman007/cli-nt-bridge) for the full edit → deploy →
compile → backtest loop against a real NinjaTrader 8 instance. Two gotchas not obvious from that
repo's README, hit while building this:
- `configure`'s top-level `instrument`/`barType`/`from`/`to` config.json fields are inert — set
  them as `params` keys instead, using the exact NinjaTrader property name (`Instrument`,
  `BarsPeriod` as `"Type:Value"` or `"Type:Value:Value2"`, `From`, `To`). Also set
  `InstrumentOrInstrumentList` alongside `Instrument`, or NinjaTrader's Run pops its own
  instrument-picker dialog and blocks headlessly.
- A custom add-on bar type's display name (e.g. `ninZaRenko`) is usually **not** the literal
  `BarsPeriodType` enum member `Enum.Parse` needs — set it once manually in the NinjaTrader GUI,
  then drive everything else (dates, instrument, strategy params) around it headlessly.
