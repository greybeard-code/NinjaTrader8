# Code Review — `UltimateAIProV3.cs`

**Date:** 2026-08-01
**Scope:** `UltimateScalper/UltimateAIProV3.cs` (1,887 lines), `UltimateAIProV3_Enums.cs`,
`WiZigZagHighLowTOS1v0.cs`, `StochasticFullTOS1v0.cs`, `WilderMA1v0.cs`
**Relationship to UltimateSignals:** successor product. Same ToS lineage (3-EMA trend + ZigZag +
MACD + Stochastic), rebuilt on NT8 indicators instead of a private engine hierarchy, plus a
higher-timeframe "Ultimate Zones" overlay and trailing/binding signal lines.

Ordered by impact.

**Summary:** V3 is architecturally better than UltimateSignals — real NT8 indicators, per-signal
enable/colour/offset properties, a genuine HTF feature. But it carries **two dead public features**,
**a short-side copy-paste bug**, **the same magenta colour collision that made UltimateSignals'
sell side uncapturable** (in three separate places), and a performance profile that is materially
worse than UltimateSignals'.

---

## A. Dead features — declared, exposed, never implemented

### A1 — `BUY` and `SELL` plots are never written

`:854-858` exposes them; `:981-984` registers them; `:1161-1162` resets them. **Nothing ever assigns
them.** Grep the file: `BUY[` and `SELL[` appear only inside `ResetPlot`.

```csharp
BUY_plot = Plots.Length;   AddPlot(new Stroke(Brushes.Transparent, …), PlotStyle.TriangleUp, "BUY");
SELL_plot = Plots.Length;  AddPlot(new Stroke(Brushes.Transparent, …), PlotStyle.Dot,        "SELL");
…
public Series<double> BUY  => Values[BUY_plot];      // always NaN
public Series<double> SELL => Values[SELL_plot];     // always NaN
```

Their alert properties exist too — `pmAlerts_BUY_Enable`, `pmAlerts_BUY_File`,
`pmAlerts_SELL_Enable`, `pmAlerts_SELL_File`, all `[NinjaScriptProperty]`, all defaulting to `true`
— and `PlaySound(pmAlerts_BUY_File)` **never appears in the file**.

**Consequence:** the tier-3 BUY/SELL signal that UltimateSignals computes (`calculateBarIndicator2`,
the white on-chart labels) **does not exist in V3**. A consumer binding to `BUY`/`SELL` gets a
permanently empty series and four settings that do nothing. This is the single most important thing
to know about V3.

The live signals in V3 are: `upSignal`, `dnSignal`, `longS`, `shortS`, `glBuffer_Dot_longS`,
`glBuffer_Dot_shortS`. (`gbUaiWrapperStrategy` reads `longS`/`shortS` — correctly.)

### A2 — `isConf` computed every bar, never read

`:1585` computes a confirmation flag against a reversal-amount threshold. Nothing consumes it. Either
a removed feature or an unfinished one.

### A3 — `trIsBind` never set

`LineLevel_Small.trIsBind` gates both small-line `while` loops (`:1187`, `:1214`) and is never
assigned `true`. The loops terminate only via `break` or the index counter.

---

## B. Correctness bugs

### B1 — The short side uses the long side's offset and enable flag

`LargeArrowsIdentification_0`, `:1362` and `:1381`:

```csharp
if (flag4)   // short signal
{
    shortS[num] = num2 + (double)pmBuffer_long_Offset * TickSize;   // ← long offset
    if (pmBuffer_long_Enable)                                        // ← long enable
    {
        … PlotBrushes[shortS_plot][num] = … 
    }
}
```

Both occurrences. `pmBuffer_short_Offset` is exposed as a user property and **is** used for the
short *dot* (`:1625`, `:1698`), but the short *arrow* ignores it. Setting "short Offset" moves the
dot and not the arrow; setting "long Offset" moves both long and short arrows.

Worse, `pmBuffer_long_Enable` gates whether the short arrow's brush is applied — so disabling the
long buffer silently changes short rendering.

**This is a genuine per-side asymmetry, the same family of defect that made UltimateSignals'
sell side unusable.**

### B2 — Small buy/sell line colours are inverted relative to the large ones

`:943-952`:

```csharp
buy_tmp_line_stroke        = new Stroke(Brushes.LimeGreen);   // large buy  = green
sell_tmp_line_stroke       = new Stroke(Brushes.Red);         // large sell = red
buy_tmp_line_stroke_small  = new Stroke(Brushes.Red);         // small buy  = RED
sell_tmp_line_stroke_small = new Stroke(Brushes.LimeGreen);   // small sell = GREEN
```

The small pair is swapped. Either a copy-paste error or a deliberate convention that is at minimum
badly confusing — a red line marking a buy level.

### B3 — Four "broken line" strokes are all Magenta, and both WithDot arrows are Magenta

```csharp
buy_broken_line_stroke        = Magenta      sell_broken_line_stroke        = Magenta
buy_broken_line_stroke_small  = Magenta      sell_broken_line_stroke_small  = Magenta
pmBuffer_Long_WithDot_Stroke  = Magenta      pmBuffer_Short_WithDot_Stroke  = Magenta
```

**This is the UltimateSignals A4 defect, repeated three times.** A colour-keyed consumer (PredatorX
`Long/ShortColorEntrySignal`, Infinity marker rules) cannot separate:

- a buy break line from a sell break line
- a confirmed long arrow from a confirmed short arrow

Unlike UltimateSignals these *are* user-settable, so it is a bad default rather than a hard block —
but it ships broken and matches the exact symptom already reported on the other product.

### B4 — `num7 = CurrentBar - 10` used with two different comparisons

`:1598` computes it once, then guards with `> 0` at `:1603`/`:1623` and `!= 0` at
`:1643`/`:1648`/`:1676`/`:1696`.

`!= 0` is true for `CurrentBar < 10` (negative) as well as `CurrentBar > 10`. So the reversal-line
and tier-3-style blocks run during early bars while the dot blocks do not. Almost certainly
unintended; at best it is an obscure way to write "at least 10 bars loaded".

### B5 — `botLine`/`topLine` written one bar behind

`:1668-1675`:

```csharp
if (revLineBot[c] != 0.0)  botLine[num] = revLineBot[c];   // num = c + 1
```

UltimateSignals writes the same index (`botLine[num] = revLineBot[num]`). V3 writes the line one bar
*older* than the value it came from, and never resets when the condition is false — it relies
entirely on the bulk reset loop. Verify against a chart before assuming either is correct.

### B6 — `MovingAverage` default branch ignores its parameter

`:1792-1809`:

```csharp
default: return SMA(Input, length);      // `Input` — the indicator's own input
case UltimateAIPro_AverageType.SIMPLE: return SMA(input, length);   // `input` — the argument
```

Unreachable while the enum stays complete, but it is a live trap for anyone adding a value.

### B7 — `Colorbars` third branch is unreachable

`:1579` — same defect as UltimateSignals. By the time the third ternary is evaluated both signals
are non-1, so `(buysignal == 0 || sellsignal == 0)` is always true and the `: 0` never fires.

### B8 — `EnhancedLines` guard is always true

`:1586-1590`: `num6 = flag5 ? 1 : 0;` then `if (num6 <= 1 && EI[c] != 0.0)`. `num6` is 0 or 1, so
`num6 <= 1` is a tautology.

### B9 — `WiCurrentBar` is always 0

`WiZigZagHighLowTOS1v0.cs:47`:

```csharp
protected int WiCurrentBar => Math.Max(0,
    (((State != State.Historical || (IsTickReplays[0] ?? false)) && Calculate != Calculate.OnBarClose) ? 1 : 0)
    + CurrentBarOffset - 1);
```

`CurrentBarOffset` is **never assigned**, so it is always 0. The expression evaluates to
`max(0, 1-1) = 0` in real time and `max(0, 0-1) = 0` historically. The whole offset mechanism is
inert; every `state[WiCurrentBar + 1]` is simply `state[1]`. It works, but a maintainer will lose
time on it. `lastBar` is likewise assigned and never read.

---

## C. Performance — materially worse than UltimateSignals

### C1 — `MRO` scans the entire chart on every tick

`:1130`:

```csharp
int num = MRO((Func<bool>)(() => EI.ZZDot[0] > 0.0), 2, CurrentBar);
```

`MRO` = most-recent-occurrence. Look-back period is `CurrentBar` — **the whole loaded history** —
and it evaluates a delegate per bar. This runs on **every tick**, on every series update.

### C2 — Then a double rewrite loop, also every tick

`:1135-1167`:

```csharp
for (num3 = num2; num3 >= 0; num3--) { …30 ResetPlot calls… }
for (num4 = num2; num4 >= 0; num4--) { Update(num4); }
```

`num2` is bounded by `lbPot`, which tracks the second-most-recent ZigZag pivot — unbounded in
practice. That is ~30 series writes plus a full `Update()` per bar in the span, per tick.

### C3 — `Update()` resolves indicators inside the loop

Each `Update(c)` call performs:

```csharp
MovingAverage(Close, averageType, fastLength)[c]     // indicator lookup
MovingAverage(Close, averageType, slowLength)[c]     // indicator lookup
SUM(macd1, sequentialLength)[c]                      // indicator lookup
EMA(close, 8); EMA(close, 14); EMA(close, 21);       // three lookups
ATR(atrlength)[c]                                    // three separate calls, :1581
Price(priceH)[c] / Price(priceL)[c]                  // ~12 calls
```

NT8 caches the instances, but the resolution still happens per call, and this is inside a loop that
is inside a per-tick path. **Combined, C1–C3 are roughly O(bars²) per tick in the worst case.**

Hoisting every indicator to `State.DataLoaded` and caching the three `ATR(atrlength)[c]` reads into
one local is close to free and would be the single largest win.

### C4 — `glList_Tags` is a `List<string>` with `Contains`

`LineLevel_Large.DestroyTemp` does `s.glList_Tags.Contains(text)` — O(n) on a list that grows for
the life of the chart. Should be a `HashSet<string>`.

### C5 — HTF loop calls `GetBar` per HTF bar

`ActionHtfLines`, `:1424-1479`: iterates every HTF bar and calls `BarsArray[0].GetBar(dateTime2)`
(a binary search) for each, plus `Draw.HorizontalLine` per level. It breaks once three distinct
top and bottom levels are seen, which usually bounds it — but the break is on
`num4 > 2 && num2 > 2`, so a session with few distinct levels scans everything.

### C6 — `ResetLineObjects` enumerates all draw objects

`:1496` — `DrawObjects.ToList()` materialises every drawing object on the chart, then string-splits
tags. Runs on each date change. Acceptable at that frequency, but it will stall a chart carrying
thousands of objects.

---

## D. Configuration and lifecycle

### D1 — `IsSuspendedWhileInactive = true`

`:885`. The indicator **stops calculating when it is not the active chart object**. For an indicator
whose entire purpose is to feed a strategy or an order engine, that is a correctness hazard, not a
performance win. GreyBeard convention requires `false`.

### D2 — Nearly all signal parameters are private

`:374-402` — `trendLength`, `sequentialLength`, `fastLength`, `slowLength`, `MACDLength`,
`averageType`, `overbought`, `oversold`, `KPeriod`, `DPeriod`, `priceH/L/C`, `avgType`, `method`,
plus the ZigZag fields `percentamount`, `revAmount`, `atrreversal`, `atrlength`, `averagelength` —
all private with hard-coded values.

What *is* exposed is presentation: enables, offsets, strokes, alert files. **The user can restyle
the indicator but cannot tune a single signal input.** Same limitation as UltimateSignals, dressed
better.

### D3 — `MACDLength` assigned, never used

`:898`, `:382`. As in UltimateSignals — there is no MACD signal line anywhere.

### D4 — Debug output left in production

`:1100`, `:1104` print on every `Historical`/`Realtime` transition. `:1124` prints on every new day.
`Logger(...)` is called with `debug: true` from every alert path and writes to **both**
`NinjaScript.Log` (which surfaces in the NT8 Log tab) and `Print`. That is log spam on a live chart.

### D5 — `Random`-generated instance ID

`:1067-1074` builds a 5–20 character random `glID`, used only in `Logger` output. Harmless, but it
makes log lines non-reproducible across restarts.

### D6 — Magic-number state and enum casts

`MaximumBarsLookBack = (MaximumBarsLookBack)1`, `EI.SetState((State)2)`, `(int)State == 4`,
`(BarsPeriodType)pmHTF_Type`. The last is load-bearing — `EnumHtfType_UltimateAIPro` is deliberately
numbered to match `BarsPeriodType` (`Minute = 4`, `Day = 5`, …). That coupling is undocumented and
will break silently if NT8 ever renumbers `BarsPeriodType`.

---

## E. What V3 does better than UltimateSignals

Worth recording, because the port should keep these:

- **Real NT8 indicators** (`EMA`, `SMA`, `WMA`, `HMA`, `ATR`, `SUM`) instead of a private engine
  hierarchy — the same simplification `gbUltimateSignalsIndicator` had to make retroactively.
- **Per-signal enable / stroke / offset properties** for all six buffers. Genuinely useful.
- **`PlotBrushes[...][bar]` for per-bar colouring** — the "with dot" confirmation state recolours the
  arrow on the bar itself. This is the right NT8 mechanism and is a real feature.
- **HTF Ultimate Zones** — a legitimate multi-timeframe overlay via `AddDataSeries`, with a
  self-referencing instance on the HTF series. Expensive, but the concept is sound.
- **Trailing / binding signal lines** — the large and small line levels that trail a signal until
  price breaks them. Nothing equivalent exists in UltimateSignals.
- **`FilePathPicker` on sound properties** — proper NT8 property editor usage.

---

## F. Recommended work order for the GreyBeard port

1. **Decide the fate of `BUY`/`SELL`** (A1) — this is a product question, not a code question. See
   the open questions below.
2. **Fix B1** (short-side offset/enable) and **B3** (magenta defaults) — the two defects that
   directly cause the reported capture problem.
3. **Hoist all indicators out of `Update()`** (C3), then bound the rewrite loop (C1/C2) the way
   `gbUltimateSignalsIndicator` bounds its own — a `MaxRewriteBars` cap plus bar-boundary gating.
4. **`IsSuspendedWhileInactive = false`** (D1) and strip the debug logging (D4).
5. **Expose the signal parameters** (D2).
6. **Add the capturable contract** — 1/0 trigger plots and stable per-side draw tags, as in
   `gbUltimateSignalsIndicator`.
7. Clean up B4–B8, A2, A3, B9.

---

## G. Open questions before building `gbUltimateAIPro`

These change what the indicator *is*, so they are worth answering rather than guessing:

1. **`BUY`/`SELL` (A1).** Implement them properly by porting the tier-3 logic from
   `UltimateSignalsIndicator.calculateBarIndicator2` (which does compute them), remove them, or keep
   them present-but-empty for drop-in compatibility?
2. **EMA 8 vs 9.** V3's trend stack is `EMA(8)/14/21` (`:1556`); UltimateSignals and the public ToS
   study both use `9/14/21`. Deliberate change or typo? Which should the port default to?
3. **HTF Ultimate Zones.** Keep as-is, keep but optimise, or drop? It is the most expensive
   component and the most involved to port.
4. **Drop-in vs successor.** Preserve V3's 14-plot order exactly so existing chart templates map
   across, or reorganise for clarity?

---

Related: [UltimateSignalsIndicator_Code_Review.md](UltimateSignalsIndicator_Code_Review.md) ·
[gbUltimateSignalsIndicator_Manual.md](gbUltimateSignalsIndicator_Manual.md) ·
[UltimateSignals_Review.md](UltimateSignals_Review.md)
