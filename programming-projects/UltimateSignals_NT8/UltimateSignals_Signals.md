# UltimateSignals — Signal Contract for Automation

How to read this indicator's output from a strategy, from Infinity Algo Engine, or from any other
consumer that needs to place trades.

> **Status of this document.** Everything in §1 (names, types, attributes, count) is **verified** from
> the assembly's metadata. Everything in §2 (plot *order* and *value semantics*) is **inferred** and
> carries a `CONFIRM` tag — the DLL is packed, so `OnStateChange` could not be read to recover the
> `AddPlot` calls. Run [gbSignalProbe.cs](gbSignalProbe.cs) once and every `CONFIRM`
> becomes a fact. Do not wire real money to an unconfirmed row.
>
> See [UltimateSignals_Review.md](UltimateSignals_Review.md) §4 first — this indicator is very likely
> a **repainting** signal source. §5 below is not optional.

---

## 1. The public surface (verified)

Namespace `NinjaTrader.NinjaScript.Indicators`, type `UltimateSignalsIndicator`.

**Constructors — there are no parameters at all:**

```csharp
UltimateSignalsIndicator()
UltimateSignalsIndicator(ISeries<double> input)
```

No `[NinjaScriptProperty]` exists on the type. Lengths, overbought/oversold levels, ZigZag reversal
amounts and colours are all hard-coded private fields. There is nothing to configure and nothing to
optimise.

**Nine public series.** Every one is `NinjaTrader.NinjaScript.Series<double>`, decorated
`[Browsable(false)]` + `[XmlIgnore]`, get-only — the standard NT8 idiom for
`public Series<double> Name { get { return Values[n]; } }`:

```
upSignalPlot   dnSignalPlot   EnhancedLines
long_          short_
botLine        topLine
buyTextMarker  sellTextMarker
```

Note the trailing underscores on `long_` and `short_` — `long` and `short` are C# keywords. You must
write them with the underscore.

---

## 2. Plot map

Declaration order in metadata, which is the order the properties were written in the source and
therefore the expected `Values[]` index. **`CONFIRM` every row before use.**

| Idx | Property | Layer | Expected meaning |
|:--:|---|---|---|
| 0 | `upSignalPlot` | Signal (tier 1) | Early long signal. Fires often; the weakest tier. |
| 1 | `dnSignalPlot` | Signal (tier 1) | Early short signal. |
| 2 | `EnhancedLines` | Line | Trend/state line, not an entry. |
| 3 | `long_` | Pivot (tier 2) | ZigZag-confirmed long pivot. |
| 4 | `short_` | Pivot (tier 2) | ZigZag-confirmed short pivot. |
| 5 | `botLine` | Level | ZigZag lower reversal rail (`revLineBot`). |
| 6 | `topLine` | Level | ZigZag upper reversal rail (`revLineTop`). |
| 7 | `buyTextMarker` | Entry (tier 3) | The bar that carries the **BUY** label. |
| 8 | `sellTextMarker` | Entry (tier 3) | The bar that carries the **SELL** label. |

The three-tier reading comes from the renderer's own field names — `renderArrowUpSignal` /
`renderArrowDownSignal`, `renderArrowLong` / `renderArrowShort`, `renderArrowBuy` / `renderArrowSell`
— which is three independent up/down arrow pairs plus `drawTopLine` / `drawBotLine`.

**`buyTextMarker` / `sellTextMarker` are the tradeable pair.** They are the ones the indicator labels.
Tiers 1 and 2 are context.

### 2.1 Value convention

NT8 signal plots almost always carry **a price when the signal is active and `double.NaN` when it is
not** (that is what `[Browsable(false)]` + `PlotStyle.TriangleUp`-style arrow plots do, and what
`ShowTransparentPlotsInDataBox` exists for).

```csharp
bool active = !double.IsNaN(series[0]);   // correct
bool wrong  = series[0] > 0;              // WRONG — NaN > 0 is false, but so is a valid 0.0 price,
                                          //   and any non-NaN sentinel like -1 also fails
```

Getting this wrong on one side only is a classic cause of "the sell signals don't work" — see
[UltimateSignals_Review.md](UltimateSignals_Review.md) §6.3, cause 2. **`CONFIRM` with the probe:** if
the idle value turns out to be `0` rather than `NaN`, switch the test to `!= 0` on both sides
together, never one side at a time.

---

## 3. Consuming it from a NinjaScript strategy

```csharp
private UltimateSignalsIndicator us;

protected override void OnStateChange()
{
    if (State == State.SetDefaults)
    {
        Name                        = "gbUltimateSignalsTrader";
        Calculate                   = Calculate.OnBarClose;   // see §5
        IsSuspendedWhileInactive    = false;
        EntriesPerDirection         = 1;
    }
    else if (State == State.DataLoaded)
    {
        us = UltimateSignalsIndicator();
        // Optional, for eyeballing parity between what you trade and what you see:
        // AddChartIndicator(us);
    }
}

protected override void OnBarUpdate()
{
    if (CurrentBar < BarsRequiredToTrade) return;

    bool buy  = !double.IsNaN(us.buyTextMarker[0]);
    bool sell = !double.IsNaN(us.sellTextMarker[0]);

    if (buy  && Position.MarketPosition != MarketPosition.Long)
        EnterLong(1, "US_Buy");

    if (sell && Position.MarketPosition != MarketPosition.Short)
        EnterShort(1, "US_Sell");
}
```

**Symmetry rule.** Write the long and short tests as a mirrored pair in one edit, from the same
template, and never patch one side alone. Every "one side stopped working" bug this repo has seen
traces back to the two sides drifting apart.

**`AddChartIndicator` caution.** Per house convention, `AddChartIndicator` can trigger a secondary
data series and change bar handling. If the strategy's signals stop matching the chart after you add
it, bootstrap the indicator manually instead.

---

## 4. Consuming it from Infinity Algo Engine

Infinity is a condition builder over other indicators' plots (`dictIndicatorInfo`,
`dictSignalBarInfo`), with its own markers and order engine. To drive it from UltimateSignals:

1. Install UltimateSignals first — **it is currently not installed** (Review §6.2). Import
   `UltimateSignals_NT8.zip` via *Tools → Import → NinjaScript Add-On*, restart NT8, and confirm
   `UltimateSignalsIndicator` appears in the indicator list.
2. Add the indicator to the chart so Infinity can enumerate its plots.
3. Build the long condition on `buyTextMarker` and the short condition on `sellTextMarker`, using the
   value convention from §2.1 — the same operator on both sides.
4. Check the per-side gates before blaming the signal:

   | Setting | Effect if wrong |
   |---|---|
   | `LongSwitchedOn` / `ShortSwitchedOn` | **Side is silently disabled. Markers still draw.** |
   | `CpEnableLongShort` | Hides the toggles entirely |
   | `RefPriceBuy` / `RefPriceSell`, `RefPriceOffset` | Limit priced through the market, never fills |
   | `OrderType`, `OrderSlmOffset`, `OrderLMTValidPeriod`, `OrderCancelMode` | Order cancelled before fill |
   | `WaitUntilFlat`, `NoneATMInvalidatesWaitUntilFlat` | Blocks the second side while the first is open |
   | `EntryCooldownTime` | Suppresses clustered signals |
   | `MoneyManagementEnabled`, `MaxDailyLoss`, `MaxDailyProfit` | Stops all trading for the session |

   `ShortSwitchedOn` is the first thing to check for a dead sell side.

---

## 5. Consuming it from PredatorX — and why the sell side won't capture

`PredatorXOrderEntryLT_V3.9.2.5.dll` is a **strategy**
(`NinjaTrader.NinjaScript.Strategies.TradeSaberPredator.PredatorXOrderEntryLT`), not just an order
pad, and it *does* accept external signals — 3 entry banks, 3 exit banks, 6 filter banks:

```
UseSignals, EntrySignalCount
UseEntrySignal1, EntrySignal1Mode, DD_Entry1_SignalSource, SignalConfirmation1,
                 EntryCandleSignalBreak1, SignalEntryTickOffset1
  LongSignalTag1         ShortSignalTag1            <- match a draw object by TAG
  LongColorEntrySignal1  ShortColorEntrySignal1     <- match a draw object by COLOUR
  UseColorEntrySignal1
TakeLongs, TakeShorts, StratOn, FullSemiAuto
```

**It keys on drawing objects — tag and colour — not on plots.** That is the crux.

### 5.1 Why the SELL signal is not capturable

UltimateSignals paints its BUY arrow and its SELL arrow in the **same brush**. Measured off the
licence holder's screenshots, modal RGB of the arrow cores across three samples per side:

| Glyph | Direction | Modal RGB | Separable by colour? |
|---|---|---|---|
| Large arrow at BUY | up | `255, 0, 255` | |
| Large arrow at SELL | down | `255, 0, 255` | **No — identical `#FF00FF`** |
| Small arrow (tier 1) | up | `0, 255, 0` | |
| Small arrow (tier 1) | down | ~`214, 15, 0` | **Yes — red vs green** |

So `ShortColorEntrySignal1` cannot be given a colour that matches the sell arrow without also
matching every buy arrow. The side is unrecoverable where PredatorX makes its decision.

Tag capture fails for a different reason: the vendor generates labels through an
`addText` / `removeText` / `textTag` helper trio, so tags are per-bar and are **deleted and recreated**
as signals revise. There is no stable per-side string to type into `ShortSignalTag1`.

### 5.2 Three fixes, best last

1. **Point PredatorX at the tier-1 arrows.** They *are* colour-separable — green up, red down. Set
   `LongColorEntrySignal1` / `ShortColorEntrySignal1` accordingly with `UseColorEntrySignal1` on.
   Free and immediate; but tier 1 fires far more often than the BUY/SELL tier, so add
   `SignalConfirmation1` or a `UseFilterSignal*` bank to tighten it.
2. **Tag capture**, if the tags turn out to encode the side. Check with `gbSignalProbe` first.
3. **Use the bridge — the robust fix.** [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs) reads
   the two sides where they *are* distinct — the vendor's separate `Series`, cleanly separable from C#
   even though the arrows are not separable by colour — and re-emits them as:
   - two named plots, `BuySignal` / `SellSignal`, carrying **1 or 0** (never `NaN`, which most
     condition builders cannot express), and
   - arrows in two **different** configurable brushes with stable, side-distinct tags.

   Then configure:

   ```
   DD_Entry1_SignalSource = gbUltimateSignalsBridge
   LongSignalTag1  = GB_US_BUY          ShortSignalTag1  = GB_US_SELL
   LongColorEntrySignal1 = <Buy Arrow Brush>   ShortColorEntrySignal1 = <Sell Arrow Brush>
   ```

   Keep the two brushes different — that difference is the entire fix. The same bridge serves
   Infinity (§4): its condition builder can select the `BuySignal` / `SellSignal` plots directly.

Set the bridge's `Buy Plot Index` / `Sell Plot Index` from what `gbSignalProbe` reports, not from the
inferred defaults (7 and 8).

### 5.3 Other consumers

- **Infinity Algo Engine** (§4) — condition builder + order engine, works off the bridge's plots.
- **A NinjaScript strategy** (§3) — the path with real backtest and audit capability.
- **Manual** — trade the labels by hand while PredatorX handles brackets. Use this during validation,
  before any automation.

---

## 6. Mandatory: confirmation delay

Because this indicator can retract markers (`removeText`, ZigZag pivots, a separate
`stateHistoricalIndicator2` path — Review §4), **never act on a marker on the bar it first appears.**

Use the same defence `gbUaiWrapperStrategy` uses for UltimateAI2:

```csharp
// ConfirmationBars: the marker must still be present N bars after it first printed.
private bool StillThere(Series<double> s, int confirmBars)
{
    if (CurrentBar < confirmBars) return false;
    return !double.IsNaN(s[confirmBars]);      // re-read the older bar; if it was revised away, this is NaN
}

bool buy  = StillThere(us.buyTextMarker,  ConfirmationBars);
bool sell = StillThere(us.sellTextMarker, ConfirmationBars);
```

This costs `ConfirmationBars` of entry slippage and buys you a signal that actually existed. Start at
1 and raise it only if the probe log (§2 of the validation process) shows revisions reaching further
back.

`Calculate.OnBarClose` for the same reason. `OnEachTick` will show you intrabar markers that vanish.

---

## 7. Quick reference

```csharp
UltimateSignalsIndicator us = UltimateSignalsIndicator();

us.upSignalPlot[0]    // 0  tier-1 long
us.dnSignalPlot[0]    // 1  tier-1 short
us.EnhancedLines[0]   // 2  trend line
us.long_[0]           // 3  tier-2 long pivot      (note the underscore)
us.short_[0]          // 4  tier-2 short pivot     (note the underscore)
us.botLine[0]         // 5  lower reversal rail
us.topLine[0]         // 6  upper reversal rail
us.buyTextMarker[0]   // 7  BUY  ← trade this
us.sellTextMarker[0]  // 8  SELL ← trade this

// active test — CONFIRM the idle sentinel with the probe before relying on it
bool active = !double.IsNaN(us.buyTextMarker[0]);
```
