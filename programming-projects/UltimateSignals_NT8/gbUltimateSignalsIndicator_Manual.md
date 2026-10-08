# gbUltimateSignalsIndicator — Operation and Technical Reference

**Version:** 1.0.0 · **Date:** 2026-08-01 · **Status:** compiles clean; not yet validated on a chart

GreyBeard rebuild of the vendor "Ultimate Signals" NT8 indicator. Signal mathematics is preserved;
the defects in [UltimateSignalsIndicator_Code_Review.md](UltimateSignalsIndicator_Code_Review.md)
are fixed, every hard-coded input is exposed, and the buy/sell sides are made distinguishable so an
order engine can act on them.

**Files**

| File | Contents |
|---|---|
| `gbUltimateSignalsIndicator.cs` | The indicator |
| `gbUltimateSignalsEngines.cs` | `GbUsZigZagHighLow`, `GbUsMa`, `GbUsMaMode`, `GbUsZigZagPriceMethod` |

Install to `Documents\NinjaTrader 8\bin\Custom\Indicators\GreyBeard\`.

---

## 1. Lineage

A port of ThinkOrSwim studies, layered:

| Layer | ToS origin |
|---|---|
| 3-EMA trend state (9/14/21) | community "Trend Reversal" study |
| ZigZag pivots | ToS built-in `ZigZagHighLow` |
| MACD reversal | ToS built-in `MACD` |
| Stochastic gate | ToS built-in `StochasticFull` |

The 3-EMA layer is the same study `gbTrendReversal.cs` implements standalone — see
[ToS_TrendReversal_Port_Plan.md](ToS_TrendReversal_Port_Plan.md). This indicator is that study plus
three confirmation engines.

---

## 2. Operation — the three signal tiers

The indicator emits **three independent up/down pairs**. They are not variations of one signal;
each comes from a different engine and means a different thing. This is the single most important
thing to understand before trading it.

### Tier 1 — MACD reversal, Stochastic-gated *(most frequent)*

Plots `MACD Up` / `MACD Down`. Small triangle up / dot.

```
macd       = MA(Close, MacdFast) − MA(Close, MacdSlow)
macdUp     = macd rose for SequentialLength consecutive bars  AND  macd[0] ≥ macd[TrendLength]
macdDown   = macd fell for SequentialLength consecutive bars  AND  macd[0] <  macd[TrendLength]

MACD Up   fires when  macdDown[1]  AND  macd[0] > macd[1]  AND  StochK < Overbought
MACD Down fires when  macdUp[1]    AND  macd[0] < macd[1]  AND  StochK > Oversold
```

Read it as **momentum exhaustion**: MACD was trending one way, turned this bar, and the Stochastic
is not already stretched in the direction of the turn. It fires often and is a *context* signal, not
an entry.

Plot value = `Low − 1 tick` (up) or `High + 1 tick` (down) when active, `Reset()` otherwise.

### Tier 2 — ZigZag pivot flip, in an established trend

Plots `ZigZag Long` / `ZigZag Short`. Large triangle up / dot.

Fires when the ZigZag signal state flips direction **while the 3-EMA trend state is engaged**
(`colorState != 3`, i.e. either the long or short latch is on). Reads as a pullback ending inside a
trend.

### Tier 3 — BUY / SELL *(the tradeable pair)*

Plots `BUY Signal` / `SELL Signal`, plus `BuyTrigger` / `SellTrigger`, plus the drawn arrows and
text. Large arrows and the white-on-chart `BUY`/`SELL` labels in the vendor build.

Fires when the ZigZag signal state flips **while the 3-EMA trend state is NOT engaged**
(`colorState == 3`). Reads as a reversal from a neutral, non-trending state.

**Tiers 2 and 3 are mutually exclusive by construction** — the same ZigZag flip produces either a
tier-2 or a tier-3 mark depending on whether the EMA trend latch happens to be on. That is the
vendor's design, preserved here.

### The 3-EMA trend state that gates tiers 2 and 3

```
buyCond   = EMA9 > EMA14 > EMA21  AND  Low  > EMA9
sellCond  = EMA9 < EMA14 < EMA21  AND  High < EMA9
stopBuy   = EMA9 ≤ EMA14          stopSell = EMA9 ≥ EMA14

buyState  latches on the transition into buyCond, clears when stopBuy
colorState = 1 (long) | 2 (short) | 3 (neutral)
```

Exposed as the `TrendState` plot: `+1` long, `−1` short, `0` neutral.

### Stop lines

`Stop Line Long` / `Stop Line Short` track the ZigZag reversal rails — the green and red horizontal
levels. They hold the prior bar's extreme at the flip and carry forward while the corresponding
trend state persists.

---

## 3. Plot reference — the automation contract

Indices 0–8 are unchanged from the vendor build, so an existing configuration maps across.

| Idx | Plot name | Tier | Value when active | Value when idle |
|:--:|---|---|---|---|
| 0 | `MACD Up` | 1 | `Low − TickSize` | `NaN` |
| 1 | `MACD Down` | 1 | `High + TickSize` | `NaN` |
| 2 | `ZigZag Line` | — | pivot price | `NaN` |
| 3 | `ZigZag Long` | 2 | `Low` | `NaN` |
| 4 | `ZigZag Short` | 2 | `High` | `NaN` |
| 5 | `Stop Line Long` | — | rail price | `NaN` |
| 6 | `Stop Line Short` | — | rail price | `NaN` |
| 7 | `BUY Signal` | 3 | `Low − TickSize` | `NaN` |
| 8 | `SELL Signal` | 3 | `High + TickSize` | `NaN` |
| 9 | `BuyTrigger` | 3 | `1` | `0` |
| 10 | `SellTrigger` | 3 | `1` | `0` |
| 11 | `TrendState` | — | `+1` / `−1` | `0` |

**Use 9/10 for automation, not 7/8.** Plots 0–8 carry a price when active and `NaN` when idle — the
standard NT8 idiom, but most condition builders offer only `> < >= <= == !=` and cannot express
`IsNaN`. Plots 9–11 are always numeric.

From C#:

```csharp
var us = gbUltimateSignalsIndicator(/* params */);
bool buy  = us.BuyTrigger[0]  > 0.5;
bool sell = us.SellTrigger[0] > 0.5;
```

### Draw objects

With `Emit Draw Objects` on (default), tier 3 also emits real NinjaTrader drawing objects with
stable, side-distinct tags:

```
GBUS_BUY_<absoluteBarIndex>        GBUS_SELL_<absoluteBarIndex>
GBUS_BUYTEXT_<absoluteBarIndex>    GBUS_SELLTEXT_<absoluteBarIndex>
```

Tag **prefixes** are fixed and never shared between sides, which is what a tag-keyed consumer needs.

---

## 4. Consuming it

### PredatorX Order Entry

PredatorX matches drawing objects by tag and colour:

```
DD_Entry1_SignalSource  = gbUltimateSignalsIndicator
LongSignalTag1          = GBUS_BUY          ShortSignalTag1        = GBUS_SELL
LongColorEntrySignal1   = <Buy Text Brush>  ShortColorEntrySignal1 = <Sell Text Brush>
UseColorEntrySignal1    = on
```

Keep the two brushes different. **This is the entire reason this rebuild exists** — the vendor build
painted both sides `Brushes.Magenta`, so no colour rule could separate them.

Then check `TakeLongs` / `TakeShorts` are both on.

### Infinity Algo Engine

Select `BuyTrigger` / `SellTrigger` and test `> 0.5`. Check `LongSwitchedOn` / `ShortSwitchedOn`.

### A strategy

See §3. Apply a confirmation delay — §6.

---

## 5. Parameter reference

| Group | Parameter | Default | Notes |
|---|---|:--:|---|
| **1. Trend** | Superfast EMA | 9 | Must be < Fast < Slow or the stacking test never passes |
| | Fast EMA | 14 | |
| | Slow EMA | 21 | |
| **2. MACD Tier** | MACD Fast | 5 | |
| | MACD Slow | 26 | |
| | MACD Average Type | EMA | SMA/EMA/HMA/WMA/Wilders |
| | Trend Length | 5 | MACD must beat its value this many bars back |
| | Sequential Length | 3 | Consecutive bars MACD must rise/fall |
| **3. Stochastic** | K Period | 10 | |
| | D Period | 10 | Computed but unused by the signal logic |
| | Smooth | 3 | |
| | Overbought | 80 | Gates tier-1 up |
| | Oversold | 20 | Gates tier-1 down |
| **4. ZigZag** | Price Method | Average | Average = EMA(High)/EMA(Low); HighLow = raw extremes |
| | Smoothing Length | 5 | EMA length when Price Method is Average |
| | Percentage Reversal | 0.01 | |
| | Absolute Reversal | 0.05 | |
| | Tick Reversal | 0 | |
| | ATR Length | 5 | |
| | ATR Reversal | 2.0 | Dominant term — the ATR multiple drives the reversal band |
| **5. Signal Output** | Emit Draw Objects | true | Required for tag-based capture |
| | Show BUY/SELL Text | true | |
| | Text Pixel Offset | 50 | |
| | Buy Text Brush | Lime | **Must differ from Sell** |
| | Sell Text Brush | Red | |
| **6. Performance** | Max Rewrite Bars | 250 | Caps ZigZag retraction recalculation depth |
| **7. Alerts** | Enable Alerts | false | |
| | Alert Rearm Seconds | 10 | |
| | Alert Sound File | Alert1.wav | |

All defaults reproduce the vendor's hard-coded values.

---

## 6. Repaint behaviour — read this before automating

**Tiers 2 and 3 repaint. This is inherent to the ZigZag, not a defect.**

A ZigZag pivot is only confirmed once price has reversed past a threshold. Until then the engine may
place a pivot and later **retract** it — which retracts the tier-2/tier-3 signal that depended on it,
and removes the drawn arrow and label. The vendor's own `removeText` path did exactly this.

The ZigZag engine exposes a `Retractions` counter — a direct, live count of how often this happens.
That is the number Stage 2 of
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) asks for.

**Consequences:**

- A historical chart shows the *revised* record, not what a live trader saw. Backtests over history
  are optimistic by an unknown margin.
- Never act on a tier-2/3 signal on the bar it first appears. Re-read it N bars later and confirm it
  is still there — the pattern `gbUaiWrapperStrategy` uses for UltimateAI2.
- Prefer `Calculate.OnBarClose`. It is selectable here; the vendor build forced `OnEachTick`.

**Tier 1 does not repaint** — MACD and Stochastic are pure functions of closed bars.

---

## 7. Technical details

### Architecture

```
OnBarUpdate
  ├─ zigZag.OnBarUpdate()          re-evaluates only on a new bar; may retract a prior pivot
  ├─ UpdateMacdTier()              tier 1; pure, current-bar only
  └─ UpdateTrendAndPivotTiers()
       ├─ 3-EMA latch → colorState
       └─ rewrite loop → CalculateZigZagBar(absBar) for each bar in the dirty span
            ├─ pivot bookkeeping (pivotSave / pivotLow / pivotHigh / zigDir / zigSignal)
            ├─ tier 2 marks
            ├─ reversal rails
            └─ tier 3 marks + draw objects
```

### The rewrite loop

When the ZigZag retracts a pivot it pulls `XLastChangedBar` backward, and every dependent bar from
there forward must be recomputed — the `zigDir`/`zigSignal` recursions each read `[b+1]`.

```csharp
int floor = Math.Max(zigZag.XLastChangedBar, CurrentBar - MaxRewriteBars);
for (int absBar = floor; absBar <= CurrentBar; absBar++)
    CalculateZigZagBar(absBar);
```

It iterates oldest → newest so each `[b+1]` read sees already-updated values, and runs only on a bar
boundary. Intra-bar ticks recompute the forming bar alone. The vendor ran the whole unbounded span
on every tick.

### Moving averages

`GbUsMa.Create` maps the mode enum onto NT8's own indicators. Wilder's is expressed as `EMA(2P−1)`,
which is exact — Wilder's alpha is `1/P`, EMA's is `2/(N+1)`, so `N = 2P−1`.

### Series lookback

All state series use `MaximumBarsLookBack.Infinite` because the retraction path can reach back an
arbitrary distance. This is a real memory cost on long histories and the reason `Max Rewrite Bars`
exists.

---

## 8. Changes from the vendor source

Review tags in brackets. Full detail in
[UltimateSignalsIndicator_Code_Review.md](UltimateSignalsIndicator_Code_Review.md).

### Bugs fixed

| Tag | Was | Now |
|---|---|---|
| **A1** | Alerts could never fire — code tested `WaitForNextBar.isNewBar` but never called `check()`, the only thing that assigns it | `Alert()` self-rearms via `Alert Rearm Seconds` |
| **A2** | `macd[]` read 5 bars back but only written from bar 6, so early MACD signals ran on unwritten history | `macd` written from bar 0; consumers separately gated |
| **A3** | Tiers 2/3 evaluated from bar 1 with EMA 9/14/21 still seed-dominated | Gated on a real warm-up |
| **A4** | Buy and sell markers both `Brushes.Magenta` — no colour-keyed consumer could separate them | Lime / Red, user-settable, with a same-colour warning |
| **B** | `UPTICKBrush`/`DOWNTICKBrush` unreachable: colour chosen on `colorState != 3` inside a branch requiring `== 3`, so text was always white | Text genuinely side-coloured |
| **C1** | Rewrite loop ran every tick over an unbounded span | Bar-boundary only, depth-capped |
| **C2** | `RemoveDrawObject` called for tags that mostly didn't exist | Live tags tracked in a `HashSet` |
| **D1** | Exact `==` on doubles decided high-vs-low pivot handling | Tick-scaled tolerance |
| **D2** | `buysignal` seeded at warm-up, `sellsignal` not | Both sides seeded identically |
| **D3** | Unreachable `CurrentBar < 1` branch | Removed |
| **E3** | `LineDraw` read the same bar for both endpoints — stop lines drew as a staircase | Ordinary NT8 line plots |

### Structural changes

| Tag | Change |
|---|---|
| **E1** | Every named constant was shadowed by a magic number at the call site — changing a constant did nothing. All values now flow from properties. |
| **E2** | Dead members removed: `MACDLength` (no signal line exists), `bubbleoffset`, `showarrows`, `XPlotBotLine`/`XPlotTopLine`, `OnRenderZigzag` (~60 lines, never called), `IsPriceGreater`. |
| **F1** | Zero parameters were exposed. All inputs are now `[NinjaScriptProperty]`. |
| **F2** | `Calculate` was forced to `OnEachTick`. Left at the NT8 default so `OnBarClose` is selectable. |
| **F3** | Plots renamed so the three tiers are self-describing in a condition builder. |

### Deliberate deviations

1. **NT8 built-ins replace the private `IndicatorEngine` hierarchy** (~900 lines removed). The
   vendor's EMA/SMA/HMA/WMA/ATR/MIN/MAX are formula-identical to NT8's; Wilder's is `EMA(2P−1)`;
   NT8's `Stochastics(D,K,smooth)` matches `TosStochastics` at the vendor's hard-coded settings.
   **Cost:** the Stochastic's `priceH`/`priceL`/`priceC` selectors are gone. They were never set to
   anything but High/Low/Close, so behaviour is unchanged — but the flexibility is not carried
   forward. Ask if you want it back.
   **Exception — HMA/WMA:** `GbUsMa` calls NT8's `SMA()`/`EMA()` directly, but HMA/WMA are computed
   by two small local classes (`GbWmaSeries`/`GbHmaSeries` in `gbUltimateSignalsEngines.cs`) instead
   of `owner.HMA()`/`owner.WMA()`. NT8's "Export NinjaScript" tool doesn't offer WMA/HMA as
   includable system files and fails before it can even prompt to bundle them, so a hand-off build
   referencing NT8's built-in HMA/WMA wouldn't export cleanly. The local classes reproduce
   `@WMA.cs`/`@HMA.cs`'s formula exactly (same weighting, same warm-up behavior) — the only
   difference is where the computation lives, not the result.
2. **Custom SharpDX rendering replaced by plots + NT8 drawing objects.** SharpDX geometry cannot be
   captured by tag; drawing objects can. This is what makes the sell side reachable, and it removes
   `TosArrowDraw`/`LineDraw` along with bug E3.

### Partially applied

**D1** is fixed at the site where it decides high-vs-low pivot handling. The two further `==`
comparisons inside the `zigDir` assignment are left as written — there both operands are copied from
the same source on the same bar, so exact equality holds in practice, and changing them would alter
behaviour rather than harden it.

---

## 9. Known limitations

- **Not yet validated on a chart.** It compiles, and every NT8 API was verified against the
  installed assemblies, but no bar-by-bar comparison against the vendor build has been run. Do that
  before trading it — Stage 2 of the validation process.
- **Tiers 2/3 repaint** (§6).
- **`D Period` is computed but unused** — the signal logic reads only `K`. Kept because it feeds
  NT8's `Stochastics` construction and matches the vendor's parameter surface.
- **Stochastic price modes not exposed** (§8, deviation 1).
- **No parity harness yet.** `gbSignalProbe.cs` can log this and the vendor build side by side into
  two CSVs for a diff — that is the intended comparison method.

---

Related: [UltimateSignalsIndicator_Code_Review.md](UltimateSignalsIndicator_Code_Review.md) ·
[UltimateSignals_Signals.md](UltimateSignals_Signals.md) ·
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) ·
[gbSignalProbe.cs](gbSignalProbe.cs)
