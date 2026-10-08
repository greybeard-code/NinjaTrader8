# gbUltimateAIPro — Operation and Technical Reference

**Version:** 1.0.0 · **Date:** 2026-08-01 · **Status:** compiles clean; not yet validated on a chart

GreyBeard rebuild of the vendor `UltimateAIProV3` NT8 indicator. Signal mathematics is preserved;
the defects in [UltimateAIProV3_Code_Review.md](UltimateAIProV3_Code_Review.md) are fixed, every
hard-coded input is exposed, and — the headline change — the BUY/SELL reversal signal is
**implemented**, having never fired in the vendor build at all.

**Files**

| File | Contents |
|---|---|
| `gbUltimateAIPro.cs` | The indicator |
| `gbUltimateAIProEngines.cs` | `GbUaiZigZagHighLow`, `GbUaiPriceSeries`, `GbUaiPrice`, `GbUaiHtfType` |
| `gbUltimateSignalsEngines.cs` | **Required dependency** — `GbUsMaMode`, `GbUsMa` |

Install all three to `Documents\NinjaTrader 8\bin\Custom\Indicators\GreyBeard\`.

---

## 1. Relationship to gbUltimateSignalsIndicator

Same ToS lineage (3-EMA trend + ZigZag + MACD + Stochastic) as
[gbUltimateSignalsIndicator](gbUltimateSignalsIndicator_Manual.md), but a different, independently
ported ZigZag — the vendor's V3 uses `WiZigZagHighLowTOS1v0`, a running high/low state machine,
where UltimateSignals uses a threshold-comparison ZigZag. The two are not interchangeable; that is
why `GbUaiZigZagHighLow` exists as its own engine rather than reusing `GbUsZigZagHighLow`.

V3 adds two things UltimateSignals does not have: **breakout-confirmed large arrows** (a signal that
must trade through a trailing level before it counts) and **Ultimate Zones**, a higher-timeframe
support/resistance overlay.

---

## 2. Operation — four signal layers

### Layer 1 — MACD reversal, Stochastic-gated (`upSignal` / `dnSignal`)

Identical mechanism to gbUSI's tier 1. Momentum exhaustion: MACD turned this bar, Stochastic is not
already stretched in the direction of the turn.

```
upSignal fires when  macdDown[1]  AND  macd[0] > macd[1]  AND  StochK < Overbought
dnSignal fires when  macdUp[1]    AND  macd[0] < macd[1]  AND  StochK > Oversold
```

### Layer 2 — ZigZag dots (`DotLong` / `DotShort`, plots 10/11)

Fires on every ZigZag signal flip, in either trend state. A **pending**, unconfirmed mark — it
becomes a large arrow only if price later breaks through a trailing level (Layer 3). If it doesn't,
it sits on the chart as a dot and nothing else happens.

### Layer 3 — Confirmed breakout (`longS` / `shortS`, plots 4/5) — **the vendor's headline signal**

A pending long dot is promoted to a confirmed long arrow when price trades through a trailing level
anchored above the prior bar's high (mirrored below the prior low for short):

```
buyTrailLevel  = High[1] + HighBufferTicks * TickSize     (set when the dot appears)
confirmed when High[0] >= buyTrailLevel
```

This is the **large arrow** — V3's primary, most prominent visual signal, and the one
`gbUaiWrapperStrategy` already trades via `longS`/`shortS`. It is a genuinely different signal from
BUY/SELL (Layer 4): a breakout confirmation of a ZigZag dot, not a reversal-from-neutral condition.

Non-repainting once confirmed — it depends only on closed-bar extremes, not on the ZigZag's own
retraction. It can, however, sit pending indefinitely if price never breaks the trail, and the
pending dot itself inherits the ZigZag's repaint risk (§6).

### Layer 4 — BUY / SELL (`BUY` / `SELL`, plots 8/9) — **implemented, per your instruction**

The vendor registered these plots, exposed four alert properties for them, and **never assigned
them** — `UltimateAIProV3_Code_Review.md` finding A1. This rebuild implements them using the same
condition UltimateSignals uses for its tier 3: a ZigZag flip while the 3-EMA trend state is
**neutral**.

```
BUY  fires when a ZigZag flip-up   occurs AND colorState == 3 (neither long nor short state active)
SELL fires when a ZigZag flip-down occurs AND colorState == 3
```

This is deliberately the complement of Layer 2/3's "in-trend" dots: dots and breakout arrows fire
regardless of trend state, BUY/SELL fires only when the 3-EMA trend is flat. Read BUY/SELL as
*reversal from consolidation*; read the confirmed long/short arrow as *pullback resuming a trend*.

If your actual usage of V3 relied on the *appearance* of BUY/SELL rather than a real signal — i.e.
you never noticed they were dead because you were trading `longS`/`shortS` — verify this condition
matches what you expected before wiring anything to it.

### 3-EMA trend state (gates the interpretation of layers 2–4, exposed as `TrendState`)

```
buyCond   = EMA(Superfast) > EMA(Fast) > EMA(Slow)  AND  Low  > EMA(Superfast)
sellCond  = EMA(Superfast) < EMA(Fast) < EMA(Slow)  AND  High < EMA(Superfast)
stopBuy   = EMA(Superfast) ≤ EMA(Fast)     stopSell = EMA(Superfast) ≥ EMA(Fast)
```

Latches on transition, clears on the MA cross — same mechanism as gbUSI. `TrendState`: `+1` long,
`−1` short, `0` neutral.

### Reversal lines (`botLine` / `topLine`)

Track the ZigZag reversal rails, same role as gbUSI's stop lines.

### Ultimate Zones — higher-timeframe support/resistance (`botLineHtf` / `topLineHtf`)

Runs the same pivot/trend/reversal-line pipeline as the primary series, but against a second data
series (`AddDataSeries((BarsPeriodType)HtfType, HtfPeriod)`) — by default 15-minute bars. Confirmed
HTF reversal lines are drawn as horizontal lines back to `HtfDaysToLoad` days, tagged
`GBUAI_ZONE_TOP_<binary-time>` / `GBUAI_ZONE_BOT_<binary-time>`, and mapped onto the primary
timeline as `botLineHtf`/`topLineHtf` for programmatic use.

**Architectural note, not present in the vendor build:** V3 computed this by spawning a *second full
instance of itself* on the HTF series via its own NinjaScript factory. That pattern cannot be
reproduced for a from-scratch indicator — NT8 only generates the constructor wrapper for a type
after it has successfully compiled once, so a brand-new self-referencing indicator can never compile
its first build. `gbUltimateAIPro.ComputeHtfBar()` runs the same pivot/trend/reversal-line
recursion in-instance against `BarsArray[1]` instead, with its own parallel `htf*` state series and
a second `GbUaiZigZagHighLow` bound to series 1. One fewer indicator instance than the vendor's
design, and it sidesteps the self-reference constraint entirely. See
`GreyBeard-Typical-NinjaTrader.md` §6.2 for the general pattern.

---

## 3. Plot reference — the automation contract

Indices 0–13 are unchanged from `UltimateAIProV3` so an existing chart template maps across.

| Idx | Plot name | Layer | Value when active | Idle |
|:--:|---|---|---|---|
| 0 | `upSignal` | 1 | `Low − offset` | `NaN` |
| 1 | `dnSignal` | 1 | `High + offset` | `NaN` |
| 2 | `Colorbars` | — | 1/2/3 | — |
| 3 | `EnhancedLines` | — | pivot price | `NaN` |
| 4 | `long` (`longS`) | 3 | `Low` | `NaN` |
| 5 | `short` (`shortS`) | 3 | `High` | `NaN` |
| 6 | `botLine` | — | rail price | `NaN` |
| 7 | `topLine` | — | rail price | `NaN` |
| 8 | `BUY` | 4 | `Low − TickSize` | `NaN` — **was always NaN in the vendor build** |
| 9 | `SELL` | 4 | `High + TickSize` | `NaN` — **was always NaN in the vendor build** |
| 10 | `long Dot` | 2 | `Low − offset` | `NaN` |
| 11 | `short Dot` | 2 | `High + offset` | `NaN` |
| 12 | `botLine HTF` | Zones | rail price | `NaN` |
| 13 | `topLine HTF` | Zones | rail price | `NaN` |
| 14 | `BuyTrigger` | 4 | `1` | `0` |
| 15 | `SellTrigger` | 4 | `1` | `0` |
| 16 | `LongTrigger` | 3 | `1` | `0` |
| 17 | `ShortTrigger` | 3 | `1` | `0` |
| 18 | `TrendState` | — | `+1` / `−1` | `0` |

**Use 14–17 for automation, not 4/5/8/9.** Same NaN-vs-numeric reasoning as gbUSI.

Two trigger pairs exist because layers 3 and 4 are different signals — a strategy wanting the
breakout-confirmed arrow reads `LongTrigger`/`ShortTrigger`; one wanting the neutral-state reversal
reads `BuyTrigger`/`SellTrigger`. They are not mutually exclusive on the same bar in general, though
in practice a bar cannot be both in-trend (layer 3's precondition) and neutral (layer 4's) at once.

```csharp
var uai = gbUltimateAIPro(9, 14, 21, 5, 26, GbUsMaMode.EMA, 5, 3, 10, 10, 3, 80, 20,
    GbUaiPrice.High, GbUaiPrice.Low, 0.01, 0.05, 0, 5, 2.0,
    true, GbUaiHtfType.Minute, 15, 1, 3,
    1, 1, 1, 1, true, 250, false, 10);

bool breakoutLong  = uai.LongTrigger[0]  > 0.5;
bool breakoutShort = uai.ShortTrigger[0] > 0.5;
bool reversalBuy   = uai.BuyTrigger[0]   > 0.5;
bool reversalSell  = uai.SellTrigger[0]  > 0.5;
```

### Draw objects

With `Emit Draw Objects` on (default): confirmed BUY/SELL arrows tagged `GBUAI_BUY_<bar>` /
`GBUAI_SELL_<bar>`; breakout-confirmation lines tagged `GBUAI_LONG_<bar>` / `GBUAI_SHORT_<bar>`;
HTF zone lines tagged `GBUAI_ZONE_TOP_*` / `GBUAI_ZONE_BOT_*`. All prefixes are fixed and
side-distinct.

---

## 4. Consuming it

### PredatorX Order Entry

```
DD_Entry1_SignalSource  = gbUltimateAIPro
LongSignalTag1          = GBUAI_LONG        ShortSignalTag1        = GBUAI_SHORT
LongColorEntrySignal1   = <BuyMarkerBrush>  ShortColorEntrySignal1 = <SellMarkerBrush>
```

for the breakout-confirmed arrow, or `GBUAI_BUY` / `GBUAI_SELL` for the neutral-state reversal.
Keep every brush pair distinct — this rebuild exists to fix the exact defect (finding B3) where the
vendor defaulted six separate stroke pairs to `Brushes.Magenta`.

### Infinity Algo Engine

Select `LongTrigger`/`ShortTrigger` or `BuyTrigger`/`SellTrigger` and test `> 0.5`.

### A strategy

Read `longS`/`shortS` directly, as `gbUaiWrapperStrategy` already does for the vendor build — those
plot indices are unchanged, so no change is required there beyond swapping the factory call. Add a
confirmation delay regardless (§6).

---

## 5. Parameter reference

| Group | Parameter | Default | Notes |
|---|---|:--:|---|
| **1. Trend** | Superfast EMA | 9 | **Vendor V3 used 8.** Set to 8 to reproduce V3 exactly; ToS source and gbUSI both use 9. |
| | Fast EMA | 14 | |
| | Slow EMA | 21 | |
| **2. MACD Tier** | MACD Fast/Slow | 5 / 26 | |
| | MACD Average Type | EMA | |
| | Trend/Sequential Length | 5 / 3 | |
| **3. Stochastic** | K/D Period, Smooth | 10 / 10 / 3 | `D Period` is computed but unused by the signal logic — carried over from the vendor for parameter-surface parity |
| | Overbought/Oversold | 80 / 20 | |
| **4. ZigZag** | ZigZag Price High/Low | High / Low | |
| | Percentage/Absolute/Tick/ATR Reversal | 0.01 / 0.05 / 0 / 2.0 | |
| | ATR Length | 5 | |
| **5. Ultimate Zones** | Enable | true | |
| | Zone Type / Period | Minute / 15 | |
| | Days To Load | 1 | |
| | Max Zone Levels | 3 | Vendor hard-coded 3; now a property |
| **6. Buffers** | Enable / Offset per signal | see file | Presentation only — does not gate the underlying computation |
| **7/8. Large/Small Arrow** | Buffer ticks, strokes | see file | Large = breakout confirmation trail; small properties exist but are not yet wired to a signal (§7) |
| **9. Signal Output** | Emit Draw Objects | true | |
| | Buy/Sell Marker Brush | Lime / Red | **Must differ** — startup warning if equal |
| **10. Performance** | Max Rewrite Bars | 250 | Caps ZigZag retraction recalculation depth |
| **11. Alerts** | Enable, Rearm Seconds, Sound | false / 10 / Alert2.wav | |

---

## 6. Repaint behaviour

Same structural source as gbUSI: **the ZigZag pivot is repaint-capable**, so Layer 2 (dots) and
anything depending on an unconfirmed pivot can be revised. `GbUaiZigZagHighLow.Retractions` is a
live counter of how often this happens — feed it to
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) Stage 2.

**Layer 3 (confirmed breakout) is the safer of the two arrow signals** — once `High[0] >=
buyTrailLevel` has actually happened on a closed bar, that fact doesn't un-happen even if the
underlying dot's ZigZag pivot is later retracted. The confirmation is real; only the *labeling* of
which dot it confirmed could be revised in an edge case.

**Layer 1 (MACD/Stochastic) does not repaint** — pure function of closed bars, same as gbUSI.

Prefer `Calculate.OnBarClose`. Apply a confirmation delay before trading Layer 2 or Layer 4 output.

---

## 7. Known limitations

- **Not yet validated on a chart.** Compiles clean on both files; no bar-by-bar comparison against
  the vendor build has been run.
- **Small-arrow trailing lines not ported.** `HighBufferTicksSmall`/`LowBufferTicksSmall` and the
  four small-arrow stroke properties exist (parity with the vendor's property surface) but are not
  wired to a signal — V3's `SmallArrowsIdentification`/`LineLevel_Small` trail is not yet
  implemented. `upSignal`/`dnSignal` fire without a corresponding trailing-confirmation step here.
- **BUY/SELL is a new implementation**, not a restored one — the vendor's was dead code with no
  observable behaviour to match against. Verify the condition (§2, Layer 4) is what you actually
  want before automating on it.
- **`ComputeHtfBar` is a hand-duplicated copy** of the primary series' pivot/trend/reversal-line
  logic, not a shared generic — every read in it is series-1-relative. If a bug is found in one, check
  the other; they are not guaranteed to stay in sync automatically.
- **Zones only compute the current HTF bar** — no retro-rewrite on retraction, unlike the primary
  series' `MaxRewriteBars`-bounded loop.
- **`Stochastic price selectors` not exposed** — same deviation as gbUSI; never set to anything but
  High/Low/Close in the vendor build, so behaviour is unaffected.

---

Related: [UltimateAIProV3_Code_Review.md](UltimateAIProV3_Code_Review.md) ·
[gbUltimateSignalsIndicator_Manual.md](gbUltimateSignalsIndicator_Manual.md) ·
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) ·
[GreyBeard-Typical-NinjaTrader.md](../GreyBeard-Typical-NinjaTrader.md) §6
