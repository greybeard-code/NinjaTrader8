# GreyKo

A renko bar type for NinjaTrader 8 with two OHLC modes on one grid. It builds the same bricks as
ninZaRenko and KingRenko$ (same closes, same bar times, same volumes) and lets you choose what the open,
high and low of each bar mean: what actually traded (**Traded**) or ninZaRenko's full-body bars
(**Full Brick**).

v1.0.3, 2026-10-30. Id 91003. No vendor code and no decompiled source is used: the grid and the OHLC
conventions were measured from chart exports.

Files: `GreyKo.cs` (the bar type) and `GreyKo-v1.0.zip` (the same file as an importable package).

## Install

1. In NinjaTrader: **Tools > Import > NinjaScript Add-On...** and pick `GreyKo-v1.0.zip`
   (or copy `GreyKo.cs` to `Documents\NinjaTrader 8\bin\Custom\BarsTypes\` and compile).
2. In the Data Series dialog choose **Type: GreyKo**.

| Dialog name | Meaning |
|---|---|
| Brick Size | B, in ticks |
| Trend Threshold | T, in ticks, 1 <= T <= B |
| OHLC Mode | dropdown: **Close = Traded** (default), **Highs and lows = Full Brick** |

The selector and the chart title read `GreyKo 64/4` in both modes. NT8 fixes the dropdown's entry text
("Close", "Highs and lows"); a bar type can rename a setting or reorder it but cannot rename the entries of
an enum. The mapping above is the whole key. The choice survives a workspace save and reload.

**Which mode?** Use the one your strategy or indicator was developed on. If you do not know, pick
**Traded**: every number in it is a price that really traded. If the logic came from a ninZaRenko chart
(an ATR or bar-range rule, a body test), pick **Full Brick**.

## How the bars are built

The grid is the ninZaRenko grid, defined by Brick Size B and Trend Threshold T (T <= B):

- A bar continuing the trend completes when price reaches the previous close +/- T.
- A reversal completes when price reaches the previous close -/+ (2B - T).
- Comparisons are strict (price must pass the level, not touch it), the close is clamped onto the level,
  and the tick that breaks out belongs to the NEW bar.
- A fast move that crosses several levels prints one brick per level. The in-between bricks carry volume 0.
- A gap of more than 30 minutes between two trades (the 17:00-18:00 ET halt, weekends) closes the forming
  brick as a partial bar and starts a fresh grid at the next trade. A seed bar that never completed is
  removed.
- All comparisons use whole ticks (integers), so instruments with 0.1 or 0.01 ticks cannot mis-call a touch
  on a floating-point error.

All of this is shared by both modes. Only the open, high and low differ.

## The two OHLC modes

Same grid, same closes, same volumes, same bar times.

| | **Traded** (dropdown "Close", default) | **Full Brick** (dropdown "Highs and lows") |
|---|---|---|
| open | the first trade of the bar | `close -/+ B`, a price that need not have traded; while forming: `anchor - dir*(B-T)` |
| high / low | the real extremes, widened to the clamped close | the real extremes, widened to that synthetic open and the clamped close |
| in-between bricks (one trade crossing several levels) | flat: O = H = L = C, volume 0 | full body B: `high = max(O,C)`, `low = min(O,C)`, volume 0 |
| looks like | small bodies (about T-1 ticks) | full-brick bodies that overlap, like ninZaRenko |
| same as | KingRenko$ | ninZaRenko |

The difference is large. On MNQ 12/4 the median bar range is 7 ticks Traded and 12 Full Brick, so ATR and
range rules differ by about 40% for the same closes. Switching the dropdown changes the OHLC convention and
nothing else, so you can find out whether a strategy depends on it in seconds.

## How it was checked

- **Chart exports, same feed.** NQ 64/4 charts exported with an indicator that writes each bar to CSV, 18 October
  to 2 November 2026 (about 50,500 bars each):
  - Traded against KingRenko$: 99.70% identical closes. The remaining differences cluster in fast bursts at
    the 18:00 session reopen.
  - Full Brick against ninZaRenko: every bar identical (open, high, low, close, volume, time).
- **Offline tests of the brick logic.** The brick logic is a plain class with no NinjaTrader types. It was
  tested against hand-walked cases, invariants over random tick streams (including gaps, ties, 0.1 and 0.01
  ticks, negative prices, and 100,000-tick jumps), and in-process restart and rewind. The tests were checked
  by breaking the logic on purpose and confirming they fail.
- **Python mirror.** [gbBacktester](https://github.com/greybeard-code/gbBacktester) builds the same bars as
  `gk<B>-<T>` (Traded) and `gr<B>-<T>` (Full Brick).

## Limits

- **It does not make a strategy better.** The same closes give the same signals from close-based logic.
  Results can still move a lot with the OHLC mode; that is a reason to test, not to expect an improvement.
- **It does not self-heal.** Like ninZaRenko and KingRenko$, dropped or reordered ticks shift the grid until the
  next 18:00 ET re-anchor.
- **Resuming on a fresh post-gap bar.** If NinjaTrader hands the bar type a series whose last bar is a brand-new
  bar from the 18:00 reopen, before its first brick has completed, the bar type cannot tell it is a fresh seed.
  It anchors on the previous bar's close and the next trade can draw bricks across the gap. This needs a reload
  at exactly that moment.
- **No burst control.** There is no minimum bar time.
- **Live charts are not reproducible tick for tick.** A live chart re-anchors on every feed reconnect; only
  rebuilt bars match another builder.
- Templates bound to ninZaRenko or another bar type must be re-pointed at GreyKo; nothing converts them.

## Versions

| | |
|---|---|
| 1.0.0 | OHLC mode as a number, own id 91003 and class names |
| 1.0.1 | the mode is a dropdown (the built-in Price type setting), listed after Trend Threshold |
| 1.0.2 | the dropdown is labelled just "OHLC Mode" |
| 1.0.3 | the modes are called Traded and Full Brick; the selector name carries no mode text; the trace is no longer wired to the Output window |
