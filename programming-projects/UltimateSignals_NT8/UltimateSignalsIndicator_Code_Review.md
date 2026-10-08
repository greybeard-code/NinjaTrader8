# Code Review — `UltimateSignalsIndicator.cs`

**Date:** 2026-08-01
**Scope:** `UltimateScalper/UltimateSignalsIndicator.cs` (706 lines) and the parts of
`UltimateSignalsHelpers.cs` it calls into.
**Context:** [UltimateSignals_Decoded_Technical_Summary.md](UltimateSignals_Decoded_Technical_Summary.md)

Findings are ordered by impact. Each has the observed code, why it matters, and a concrete fix.
Line numbers refer to the decompiled source as supplied.

**Summary:** four live bugs (one silently disables a whole feature, two corrupt signals during
warm-up, one is the reported sell-capture defect), one significant performance hazard, and a
cluster of dead code that makes the file misleading to maintain. The core signal maths is sound —
the 3-EMA state machine and the MACD/Stochastic tier both faithfully reproduce their ToS sources.

---

## A. Live bugs

### A1 — The alert system can never fire *(silently dead feature)*

`:340-349`

```csharp
if (useAlerts && flag && waitForNextBarAlertUp.isNewBar)
{
    Alert("Up", Priority.Medium, "Up", Globals.InstallDir + "\\sounds\\Alert1.wav", 1, ...);
    waitForNextBarAlertUp.activate();
}
```

`WaitForNextBar.isNewBar` has a **private setter assigned only inside `check()`**:

```csharp
public bool isNewBar { get; private set; }
public void activate() { oxM_ = oRM_.CurrentBars[ohM_]; }
public bool check()   { int num = oRM_.CurrentBars[ohM_]; isNewBar = num != oxM_; return isNewBar; }
```

`check()` is **never called** on either instance. `isNewBar` therefore stays at its default `false`
for the life of the indicator, and both alert branches are unreachable. Masked in practice because
`useAlerts` defaults to `false`.

**Fix:** call `check()` before testing, which is the class's intended usage:

```csharp
if (useAlerts && flag && waitForNextBarAlertUp.check())
```

### A2 — `macd[]` is read further back than it is written *(corrupt early signals)*

`:302-323`

```csharp
if (CurrentBar < minBars) return;          // minBars = max(3+1, 5+1) = 6
macd[0] = fastMa[0] - slowMa[0];           // first write happens at bar 6
...
for (int i = 0; i < sequentialLength; i++)
    if (macd[i] >= macd[i + 1]) num++;     // reads back to macd[3]
macdup[0] = num == sequentialLength && macd[0] >= macd[trendLength];   // reads macd[5]
```

`macd` is only written from bar 6 onward, but on that very first pass it reads `macd[1]`…`macd[5]`
— bars 1-5, which were never assigned. Those reads return the series default, so the first several
`macdup`/`macddown` evaluations are computed against fabricated history and `upSignalPlot` /
`dnSignalPlot` can fire spuriously.

**Fix:** write `macd[0]` unconditionally and guard only the consumers:

```csharp
macd[0] = fastMa[0] - slowMa[0];
if (CurrentBar < minBars) return;
```

and raise `minBars` to cover the actual lookback: `Math.Max(sequentialLength + 1, trendLength + 1)`
is 6, but the deepest read is `macd[trendLength]`, so it must be at least `trendLength + 1` *after*
macd itself is warm. `minBars = slowLength + trendLength + 1` (32) is the honest figure.

### A3 — Tier-2/3 signals evaluate before the EMAs are warm

`:411-419` — `OnBarUpdateIndicator2` guards only on `CurrentBar < 1`, then immediately uses
`mov_avg9/14/21` (EMA 9/14/21) and the ZigZag. Those EMAs self-seed from bar 0, so from bar 1 they
are dominated by the seed value and the stacking test `ma9 > ma14 > ma21` is meaningless. Tier 1
guards properly (`minBars`); tier 2 does not.

**Fix:** `if (CurrentBar < slow_length + 1) { /* seed state series */ return; }`, and set
`BarsRequiredToPlot` accordingly.

### A4 — Buy and sell markers share one hard-coded brush *(the reported defect)*

`:248-249`

```csharp
AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "Buy Text Marker");
AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.Dot,        "Sell Text Marker");
```

Both `Brushes.Magenta`, fed straight into the custom renderer at `:563-564`. Any consumer that
identifies signals by colour — PredatorX `Long/ShortColorEntrySignal`, Infinity's marker rules —
cannot distinguish a buy from a sell. This is the whole reported problem.

**Fix:** two exposed `Brush` properties with different defaults, plus the `Serialize`
round-trip pair NT8 needs. Tiers 1 and 2 already use Lime/Red; matching that convention here costs
nothing:

```csharp
[XmlIgnore] [Display(Name="Buy Marker Brush",  GroupName="Colors", Order=0)]
public Brush BuyMarkerBrush  { get; set; }    // default Brushes.Lime
[Browsable(false)] public string BuyMarkerBrushSerialize
{ get => Serialize.BrushToString(BuyMarkerBrush); set => BuyMarkerBrush = Serialize.StringToBrush(value); }
// …same for SellMarkerBrush, default Brushes.Red
```

Note `UltimateAIProV3` in the same assembly already does exactly this via its
`"Large Arrow Settings"` group — this is a regression the successor product fixed.

---

## B. The near-miss: side-distinct text colour was intended but is unreachable

`:519-542`

```csharp
double location = ((!flag) ? High[num] : Low[num]);
Brush  color    = ((Colorbars[num] != 3) ? UPTICKBrush : Brushes.White);
if (flag2 && Colorbars[num] == 3 && signal[num] > 0 && signal[num + 1] <= 0)
{
    buyTextMarker[num] = Low[num] - TickSize;
    addText("BUY", num, location, color, -1);
}
```

The guard requires `Colorbars[num] == 3`, but `color` was selected on `Colorbars[num] != 3`. Inside
the branch the condition is *guaranteed* false, so `color` is **always `Brushes.White`**. The same
contradiction applies to the SELL block and `DOWNTICKBrush`.

So `UPTICKBrush` (RGB 3,128,0 — green) and `DOWNTICKBrush` (RGB 204,0,0 — red), constructed at
`:403-404`, are **dead**. They confirm the author intended the BUY/SELL text to be colour-coded by
side — and it renders white on the chart, which matches the screenshots exactly.

**This is close to a free fix for A4.** Making the text honour those brushes gives a side-distinct
visual immediately:

```csharp
Brush color = Brushes.White;                       // or expose it
addText("BUY",  num, location, UPTICKBrush,   -1);
addText("SELL", num, location, DOWNTICKBrush,  1);
```

Colour-keyed capture generally targets the arrow/marker rather than the text object, so A4 is still
the real fix — but this removes a visible inconsistency and costs one line each.

---

## C. Performance

### C1 — O(bars) rewrite on every tick

`:436-439` combined with `:223` and `:253`:

```csharp
Calculate           = Calculate.OnEachTick;
MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
...
for (int i = EI.xLastChangedBar; i <= CurrentBar; i++)
    calculateBarIndicator2(i);
```

`xLastChangedBar` is reset to `currentBar` once per *new bar* (inside `updateBar`), but the rewrite
loop runs on **every tick**. When the ZigZag retracts a pivot, `updateLastChangedBar` pulls
`xLastChangedBar` backward, and every subsequent tick until the next bar close re-runs
`calculateBarIndicator2` across that whole span. `calculateBarIndicator2` is not cheap — it touches
a dozen series and calls `Draw.Text`/`RemoveDrawObject`.

With `MaximumBarsLookBack.Infinite` there is no bound on the span. On an active instrument this is
a measurable CPU cost and a plausible source of chart lag.

**Fix, cheapest first:**
1. Only run the loop when the bar actually changed, or when `xLastChangedBar < CurrentBar`; on
   intra-bar ticks recompute the current bar only.
2. Cap the rewrite depth (the ZigZag cannot meaningfully revise a pivot hundreds of bars back for
   trading purposes).
3. Reconsider `MaximumBarsLookBack.Infinite` — it is needed only if `GetValueAt` reaches
   arbitrarily far back.

### C2 — Draw-object churn

`calculateBarIndicator2` calls `removeText(...)` on **every bar in the rewrite span where the
condition is false** — i.e. almost all of them, every tick. `RemoveDrawObject` on a tag that does
not exist is wasted work at tick frequency.

**Fix:** track which tags are live in a `HashSet<string>` and only call `RemoveDrawObject` for tags
actually present. (`UltimateAIProV3` does precisely this with its `glList_Tags` collection — the
pattern already exists in the codebase.)

---

## D. Correctness risks

### D1 — Exact floating-point equality on prices

`:447`

```csharp
bool flag = ((EISave.GetValueAt(xBar) == priceh.GetValueAt(xBar)) ? priceh[num] : pricel[num])
            - EISave[num + 1] >= 0.0;
```

`==` on two `double` prices. `flag` decides whether the bar is treated as a high pivot or a low
pivot, and it feeds `EIL`/`EIH`/`dir`/`signal` — so a one-ULP difference flips the ZigZag's
interpretation of the bar.

The codebase already has the right tool: `MathExtentions.ApproxCompare`, used in `TosStochastics`
and wrapped by `TosZigZagHighLow.IsPriceGreater` — which is itself **never called**.

**Fix:** `MathExtentions.ApproxCompare(EISave.GetValueAt(xBar), priceh.GetValueAt(xBar)) == 0`.

### D2 — Asymmetric initialisation

`:413-419`

```csharp
if (CurrentBar < 1)
{
    buysignal[0] = false;
    dir[0] = 0;
    signal[0] = 0;
    return;
}
```

`buysignal[0]` is seeded but **`sellsignal[0]` is not**, and neither are `buy`, `sell`, `Colorbars`,
`EISave`, `EIL`, `EIH`, `revLineTop`, `revLineBot`. `Series<bool>` defaults to `false` so the
practical impact is small, but in a file whose defining bug is a buy/sell asymmetry this is exactly
the kind of thing worth making symmetric.

**Fix:** seed both sides, or seed neither and rely on documented defaults.

### D3 — Unreachable branch

`:427-434`

```csharp
bool flag4 = !sell[1] && sell[0];
if (CurrentBar < 1) { sellsignal[0] = false; }
else { sellsignal[0] = (flag4 && !flag3) || (!(sellsignal[1] && flag3) && sellsignal[1]); }
```

The method already returned at `CurrentBar < 1` (D2), so this branch is dead — and `sell[1]` on the
line above would have thrown at bar 0 anyway. Remove it.

---

## E. Dead code and maintainability

### E1 — Every named constant is shadowed by a magic number

The class declares `superfast_length = 9`, `fast_length = 14`, `slow_length = 21`,
`percentamount = 0.01`, `revAmount = 0.05`, `atrreversal = 2.0`, `atrlength = 5`,
`averagelength = 5` — and then **none of them are used**:

```csharp
mov_avg9 = EMA.create(this, Close, 9);       // literal, not superfast_length
mah      = EMA.create(this, High,  5);       // literal, not averagelength
EI.PPercentageReversal = 0.01;               // literal, not percentamount
EI.PAtrLength          = 5;                  // literal, not atrlength
EI.PAtrReversal        = 2.0;                // literal, not atrreversal
EI.PAbsoluteReversal   = 0.05;               // literal, not revAmount
```

Changing a constant does nothing. This is the single most dangerous thing in the file for future
maintenance — it looks configurable and is not.

**Fix:** use the constants at the call sites, then promote them to `[NinjaScriptProperty]`.

### E2 — Genuinely unused members

| Member | Status |
|---|---|
| `MACDLength = 9` | assigned, never read — there is no MACD signal line anywhere |
| `bubbleoffset = 0.0005` | never read |
| `showarrows = true` | never read |
| `XPlotBotLine = 5`, `XPlotTopLine = 6` | never read (indices are hard-coded in `OnRenderTargetChanged`) |
| `OnRenderZigzag(...)` | ~60 lines, never called from `OnRender` |
| `UPTICKBrush`, `DOWNTICKBrush` | unreachable (§B) |
| `TosZigZagHighLow.IsPriceGreater` | never called (§D1) |
| `private void C()` / ctor | empty |

`OnRenderZigzag` being dead also means `EnhancedLines` (Values[2], `PlotStyle.Hash`,
`Brushes.Transparent`) is computed every bar and never drawn. Either wire it up or drop the plot.

### E3 — `LineDraw.render` reads the same bar twice

`UltimateSignalsHelpers.cs`, `LineDraw.render()`:

```csharp
int yByValue  = chartScale.GetYByValue(tRM_.GetValueAt(i));
int yByValue2 = chartScale.GetYByValue(tRM_.GetValueAt(i));   // should be i + 1
```

`topLine`/`botLine` therefore render as a staircase of flat segments instead of a connected line.
**Cosmetic only** — the plot values are correct, so programmatic consumers are unaffected.

### E4 — Magic-number state casts

`EI.SetState((State)2)`, `(int)State == 1`, `(int)State == 4` throughout. These are
`State.Configure`, `State.SetDefaults`, `State.DataLoaded`. Likely a decompilation artefact, but if
this file is now the maintained source they should be named.

---

## F. API surface

### F1 — No user-configurable parameters at all

There is not one `[NinjaScriptProperty]` on the type. Every input in §3 of the technical summary is
a private field or `const`. The indicator cannot be adapted to a different instrument, timeframe or
bar type — you get the author's ToS defaults or nothing.

At minimum, promote: the three EMA lengths, the MACD fast/slow, `trendLength`/`sequentialLength`,
the Stochastic K/D/smooth and OB/OS levels, the four ZigZag reversal inputs, `useAlerts`, and the
marker brushes from A4.

### F2 — `Calculate.OnEachTick` is forced

`:223` hard-codes it, so a consumer cannot opt into `OnBarClose` to get stable, non-repainting
values. Given the rewrite loop (C1) and the repaint behaviour, this is the single setting most worth
exposing — `OnBarClose` would materially reduce both the CPU cost and the signal churn.

### F3 — Plot naming

`"Up"`, `"Down"`, `"Long"`, `"Short"`, `"Buy Text Marker"`, `"Sell Text Marker"` are what a
condition builder lists. They are serviceable, but `"Up"`/`"Down"` versus `"Long"`/`"Short"` gives
no clue that the first pair is a MACD reversal tier and the second a ZigZag pivot tier. Renaming to
something like `"MACD Up"` / `"ZigZag Long"` / `"BUY Signal"` would make the three tiers
self-documenting at the point of selection.

---

## G. What is right

Worth stating, since the list above is long:

- The 3-EMA latched state machine (`:420-435`) faithfully reproduces the public ToS Trend Reversal
  study, including the transition-based latch — it is symmetric and correct.
- The MACD-reversal + Stochastic-gate tier (`:306-323`) is a clean, symmetric implementation.
- `Series.Reset()` for the idle sentinel is the correct NT8 idiom and is applied consistently
  across all six signal plots.
- Delegating to a private `IndicatorEngine` hierarchy rather than instantiating full NT8 indicators
  is a sound performance decision, and `MovingAverageFactory` / `PriceSeriesPicker` are tidy.
- The ZigZag rewrite loop, while expensive, is *correct* — it iterates oldest-to-newest so the
  `[num+1]` recursions read already-updated values.

---

## Implemented

All findings above are addressed in the GreyBeard rebuild, 2026-08-01:

- **[gbUltimateSignalsIndicator.cs](gbUltimateSignalsIndicator.cs)** — the indicator. Every fix
  A1–A4, B, C1–C2, D1–D3, E1–E3, F1–F3 applied, with the review tag cited at each site. Plot
  indices 0–8 preserved so existing configurations map across; triggers and trend state added at
  9–11.
- **[gbUltimateSignalsEngines.cs](gbUltimateSignalsEngines.cs)** — `GbUsZigZagHighLow`,
  `GbUsMa`, and the two enums.

Two deliberate deviations, both recorded in the file headers:

1. **NT8 built-ins replace the private `IndicatorEngine` hierarchy.** The vendor's EMA/SMA/HMA/WMA/
   ATR/MIN/MAX are formula-identical to NT8's, Wilder's MA is exactly `EMA(2P-1)`, and NT8's
   `Stochastics(D, K, smooth)` is the same calculation at the vendor's hard-coded High/Low/Close +
   SMA settings. ~900 lines of re-derived code removed. **Cost:** the Stochastic price-mode
   selectors (`priceH`/`priceL`/`priceC`) are gone — they were never set to anything but
   High/Low/Close, so behaviour is unchanged, but the flexibility is not carried forward.
2. **Custom SharpDX rendering replaced by plots + NT8 drawing objects.** SharpDX geometry is
   invisible to tag-keyed consumers; drawing objects are not. This is what makes the sell side
   capturable, and it deletes `TosArrowDraw`/`LineDraw` along with bug E3.

Partial: **D1** is fixed at the one site where it decides high-vs-low pivot handling. The two
further `==` comparisons inside the `zigDir` assignment are left as the vendor wrote them — there
both operands are copied from the same source on the same bar, so exact equality holds in practice,
and changing them would alter behaviour rather than just harden it.

**Not yet compiled or run.** Every NT8 API used was verified against the installed assemblies
(`Stochastics.K`, `Text.YPixelOffset`, `Series<T>` constructors, `Draw.ArrowUp/ArrowDown/Text`,
the `SMA/EMA/HMA/WMA/ATR` factories being public, `ISeries`/`MaximumBarsLookBack`/`PlotStyle`
namespace resolution), but that only proves the members exist — it does not substitute for a
compile and a chart run.

---

## Suggested order of work

1. **A1** (alerts dead) and **A4** (marker brushes) — small, high value.
2. **A2 / A3** (warm-up guards) — corrupt signals at the left edge of every chart.
3. **F2** then **C1** — expose `Calculate`, then bound the rewrite loop.
4. **D1** (float equality) — quiet but it can flip ZigZag direction.
5. **E1** (constants vs magic numbers) before any parameterisation work, or F1 will be built on sand.
6. **E2** — delete the dead members, or wire up `OnRenderZigzag` if the ZigZag line was meant to be
   visible.

---

Related: [UltimateSignals_Decoded_Technical_Summary.md](UltimateSignals_Decoded_Technical_Summary.md) ·
[UltimateSignals_Review.md](UltimateSignals_Review.md) ·
[UltimateSignals_Signals.md](UltimateSignals_Signals.md)
