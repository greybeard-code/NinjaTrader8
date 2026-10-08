# UltimateSignals (NT8) — Technical Review

**Reviewed:** 2026-07-31
**Artifact:** `NinjaScript/UltimateSignals_NT8/` (`Info.xml`, `UltimateSignals.cs`, `UltimateSignals.dll`)
**Assembly:** `UltimateSignals, Version=1.0.0.1` · MVID `D28D5A0C-8617-4FAD-9154-E18C2EB0C121`
**Method:** PE/CLI metadata inspection (`System.Reflection.Metadata`), signature decoding, string extraction, chart-image analysis. IL bodies could **not** be read — see §2.

---

## 1. Headline findings

| # | Finding | Severity |
|---|---|---|
| F1 | **The BUY arrow and the SELL arrow are painted in the same brush — magenta `#FF00FF`, measured on both.** PredatorX and Infinity capture external signals by drawing-object **colour** and **tag**. A colour rule for the short side therefore also matches every long arrow. **This is why the SELL signal is not capturable.** See §6. | **Root cause** |
| F2 | Not installed on *this* machine — it lives on the licence holder's machine. Nothing under `Documents\NinjaTrader 8` here matches `UltimateSignals*`, so nothing can be reproduced locally without a licence; use the late-bound tooling in §7. | Medium |
| F3 | The DLL is **packed and licence-locked** (Agile.NET 6.9.1.7, HWID online activation, `[SuppressIldasm]`). Logic is not auditable, and it can self-terminate if activation fails. | High |
| F4 | The indicator exposes **zero user-settable parameters**. Nothing can be tuned — no lengths, no colours, no OB/OS levels, no toggles. | High |
| F5 | It ships **9 public `Series<double>` plots**, which is a clean automation surface. See [UltimateSignals_Signals.md](UltimateSignals_Signals.md). | Positive |
| F6 | It contains `addText` / **`removeText`** / `textTag` methods — the indicator can **delete BUY/SELL labels it has already drawn**. Combined with a ZigZag core, this is a **repainting** design. | High |
| F7 | The DLL **bundles a private copy of NinjaTrader.Custom** (all DrawingTools, `NinjaTrader.Custom.Resource`, and the partial `Indicator`/`Strategy`/`MarketAnalyzerColumn`/`PerformanceMetric` classes). Type-collision hazard with your other vendor DLLs. | Medium |
| F8 | Built against **NinjaTrader.Core 8.1.5.2**; your install is **8.1.5.2**. Compatible. (`Info.xml` claims export version 8.1.4.2 — cosmetic mismatch only.) | OK |

---

## 2. Why the logic could not be read

`UltimateSignals.cs` (2.4 KB) is only the NinjaScript-generated cache/wrapper — the `UltimateSignalsIndicator()` factory methods. All real code is in the DLL.

The DLL is protected by **Agile.NET / SecureTeam v6.9.1.7** (declared in `Info.xml` as `<Agile>6.9.1.7</Agile>`):

- Sections `.text` (0x9AF98 virtual) and `.10u` (0x2FF1F virtual) have **`PointerToRawData = 0` and `SizeOfRawData = 0`** — method bodies are not present in the file; they are materialised at runtime by an unpacker stub. `Assembly.Load` fails outright with *"Enclosing type(s) not found for type 'EC32F094'"*.
- Type names are obfuscated (`EC32F094`, `phM=.pRM=`, `3RU=.3BU=`); string literals are encrypted; there is a synthetic assembly reference `C416F009 0.0.0.0` and native module refs `user32.dll` / `kernel32`.
- Assembly attributes: `[SuppressIldasm]`, `[module: UnverifiableCode]`. (Your dotPeek cache at `AppData\Local\JetBrains\dotPeek\...\UltimateSignals.cs` contains only the same 2.4 KB of assembly-level metadata — dotPeek got no further.)

**Unpacking it is out of scope**: the packer is the licence-enforcement mechanism, so defeating it would be circumventing the vendor's protection. Everything below is derived from metadata that survives packing (type/member names, signatures, attributes) plus plaintext strings in the stub.

That constraint is exactly why §3 of [UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) is empirical rather than code-based.

---

## 3. What it actually is

Member names were **not** obfuscated, so the engine is legible even though the code is not. Helper types and plaintext strings:

```
UltimateSignalsNamespace.TosStochastics      "TOS Stochastic full"
UltimateSignalsNamespace.TosWildersMa        "TOS Wilder MA"
UltimateSignalsNamespace.TosZigZagHighLow    "TOS ZigZagHighLow"
UltimateSignalsNamespace.TosArrowDraw
UltimateSignalsNamespace.IndicatorEngine, LineDraw, CheckNewBar, WaitForNextBar,
  PriceMode, Method, MaMode, ATR, EMA, HMA, SMA, WMA, MAX, MIN
```

**This is a port of ThinkOrSwim studies.** Private fields on `UltimateSignalsIndicator` map to four classic ToS components:

| Component | Fields |
|---|---|
| **Three-MA trend** (the ToS 9/14/21 buy-sell study) | `superfast_length`, `fast_length`, `slow_length`, `mov_avg9`, `mov_avg14`, `mov_avg21`, `buy`, `buysignal`, `sell`, `sellsignal`, `Colorbars` |
| **MACD** | `fastLength`, `slowLength`, `MACDLength`, `averageType`, `macd`, `macdup`, `macddown` |
| **Full Stochastic** | `KPeriod`, `DPeriod`, `overbought`, `oversold`, `priceH`, `priceL`, `priceC`, `avgType`, `stochastic` |
| **ZigZagHighLow** | `percentamount`, `revAmount`, `atrreversal`, `atrlength`, `averagelength`, `showarrows`, `bubbleoffset`, `EI`, `EISave`, `EIH`, `EIL`, `dir`, `signal`, `revLineTop`, `revLineBot`, `mah`, `mal`, `priceh`, `pricel` |
| Cross-cutting | `trendLength`, `sequentialLength`, `minBars`, `useAlerts`, `waitForNextBarAlertUp`, `waitForNextBarAlertDn`, `fastMa`, `slowMa`, `method` |

Execution shape (`UltimateSignalsIndicator` methods):

```
OnStateChange
OnBarUpdate ├─ OnBarUpdateIndicator1                      (MA / MACD / Stochastic engine)
            └─ initializeIndicator2 → stateHistoricalIndicator2
               → OnBarUpdateIndicator2 → calculateBarIndicator2   (ZigZag engine)
addText / removeText / textTag                            (BUY / SELL labels)
OnRender / OnRenderZigzag / OnRenderTargetChanged         (SharpDX custom rendering)
```

**Character of the signal:** a Stochastic overbought/oversold fade, gated by a 3-MA trend state and MACD, with entries anchored to ZigZag pivots. That is a **mean-reversion / counter-trend** engine. Expect it to underperform badly on the side that fights a strong trend — which matters directly for the SELL complaint (§6).

---

## 4. The repaint problem (F6)

Three independent structural signals point the same way:

1. **`removeText`** — the indicator has a code path that *erases a text marker it previously drew*. A BUY or SELL label that has already printed can be withdrawn.
2. **`TosZigZagHighLow`** — a ZigZag pivot is not confirmed until price has reversed by the reversal amount. Pivot-anchored marks are, by definition, placed in the past once the future is known.
3. **`stateHistoricalIndicator2`** — a dedicated historical-state path separate from the live path, i.e. history is computed differently from real time.

You have hit this exact failure mode before on a sibling product: `gbUaiWrapperStrategy` was built with `Confirmation Bars` and `Reversing Signal` specifically because **UltimateAI2 revises signals after the fact**. Treat UltimateSignals as guilty until proven innocent.

**Consequence:** any evaluation of this indicator done by scrolling back over a historical chart, or by running a backtest, is unreliable — it shows the revised record, not what a live trader saw. §2 of the validation process exists solely to settle this.

---

## 5. Distribution and packaging concerns

**Licence enforcement (F3).** Plaintext strings in the unpacked stub:

```
activation.php?code=      &hwid=      type=activation&code=
deactivation.php?hash=    &hash=      type=deactivation&hash=
user-agent   Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2)
"This code requires valid serial number to run."
"Program will be terminated."
```

Machine-locked (HWID) online activation, with an explicit termination path. Practical implications: it needs outbound HTTP to the vendor at load; a hardware change or a dead vendor server disables it; and *"Program will be terminated"* suggests failure may take NinjaTrader down rather than just disabling the indicator. Do not put this on a chart that is running live automation until you have seen it survive a restart offline.

**Bundled NinjaTrader.Custom (F7).** The DLL defines its own copies of `NinjaTrader.NinjaScript.DrawingTools.*` (AndrewsPitchfork, Fibonacci*, GannFan, RegressionChannel, RiskReward, Ruler, ShapeBase, Text, TextFixed, TimeCycles, TrendChannel, PathTool, Polygon, Region*, PriceLevel, Line/Ray/ExtendedLine/HorizontalLine/VerticalLine, ChartMarker/Dot/Square/Diamond/ArrowUp/ArrowDown/Triangle*, `Draw`), plus `NinjaTrader.Custom.Resource` and the partial `Indicator`, `Strategy`, `MarketAnalyzerColumn` and `PerformanceMetric` classes. This is what makes it 1 MB.

Your `bin\Custom` already has vendor DLLs that redefine core indicator types:

- `PredatorXOrderEntryLT_V3.9.2.5.dll` → its own `EMA, SMA, RSI, ATR, Stochastics, ADX, Bollinger, KeltnerChannel, ParabolicSAR, TEMA, VWMA, HMA, LinReg, TMA, ZLEMA, VMA, KAMA, T3, MIN, MAX, StdDev, CMO, SUM, DEMA`
- `MaverickUltimateEdgeSuite26.13.3.dll` → its own `ADX, VMA, MACD, RSI, ATR, EMA, VOL, CMO, SMA, SUM`
- `PredatorIndicators.dll` → its own `EMA, TEMA, VOL, VWAPmodLT, ZLTEMAmodLT, ZeroLagHATEMAmodLT`

Adding a fourth DLL that redefines the whole DrawingTools namespace increases the chance of load-order-dependent behaviour. Install it, then verify your **existing** drawing tools and strategies still behave — that is step 1.4 of the validation process.

**No parameters (F4).** `UltimateSignalsIndicator` has exactly two constructors — `()` and `(ISeries<double> input)` — and no `[NinjaScriptProperty]` anywhere. Every length, level and colour in §3 is a hard-coded private field. You cannot adapt it to SaberRenko 70/4 versus a 5-minute chart; you get the vendor's ToS defaults or nothing. Whatever it does out of the box is the whole product.

---

## 6. The SELL complaint — why the sell side cannot be captured

**Reported symptom:** the SELL signal is not capturable by Infinity Algo Engine or PredatorX. The
screenshots come from the licence holder's machine, where the indicator runs normally.

### 6.0 Root cause

**How these consumers capture a third-party signal.** PredatorX Order Entry
(`NinjaTrader.NinjaScript.Strategies.TradeSaberPredator.PredatorXOrderEntryLT` — it is a *strategy*,
not just an order pad) exposes three banks of external-signal hooks: 3 entry, 3 exit, 6 filter. Each
one is keyed by **tag** and **colour**, per side:

```
UseSignals, EntrySignalCount
UseEntrySignal1, EntrySignal1Mode, DD_Entry1_SignalSource, SignalConfirmation1
  LongSignalTag1        ShortSignalTag1           <- match a draw object by TAG
  LongColorEntrySignal1 ShortColorEntrySignal1    <- match a draw object by COLOUR
  UseColorEntrySignal1
```

It does **not** read plots. It reads **drawing objects**, and it tells long from short by the tag
string or the brush.

**What UltimateSignals paints.** Measured directly off the screenshots by colour-clustering the
glyphs and taking the modal RGB of each arrow's core:

| Glyph | Direction | Modal RGB | Distinguishable by colour? |
|---|---|---|---|
| Large arrow at **BUY** | up | `255, 0, 255` | |
| Large arrow at **SELL** | down | `255, 0, 255` | **No — identical** |
| Small arrow (tier 1) | up | `0, 255, 0` | |
| Small arrow (tier 1) | down | ~`214, 15, 0` | **Yes — red vs green** |

(Samples across three separate arrows per side; the ±15 spread on the individual readings is JPEG
compression around a true `#FF00FF` and `#00FF00`.)

**The BUY and SELL arrows are the same colour.** So `ShortColorEntrySignal1` cannot be set to
anything that matches the sell arrow without also matching every buy arrow. The side is
unrecoverable at the point where PredatorX makes the decision. That is the whole bug — and it is a
*port* defect, not a flaw in the original study (§6.4).

Tag-based capture is no better: the vendor generates its labels through an `addText` / `removeText` /
`textTag` helper trio (§3), so tags are generated per bar and are **deleted and recreated** as
signals revise. There is no stable per-side tag string to type into `ShortSignalTag1`.

### 6.1 What the screenshots show

`bandicam_2026-07-31_17-49-31-023.jpg` and `bandicam_2026-07-31_17-52-22-258.jpg` — MNQ 09-26, **SaberRenko 70/4**, with `Infinity Algo Engine$` and the HelloWin.io watermark on the chart.

Measured by colour-clustering the magenta arrow glyphs (arrow direction taken from head width, top vs bottom quartile):

| Screenshot | Chart-clock span | Magenta UP (buy) | Magenta DOWN (sell) |
|---|---|---|---|
| `...17-49-31` | 03:08 – 09:10 | 5 | 5 |
| `...17-52-22` | 10:12 – 11:05 | 8 | **10** |

Glyph vocabulary on those charts: a **small red down arrow + large magenta down arrow + white "SELL"** at sells; a **small green up arrow + large magenta up arrow + white "BUY"** at buys.

Two things follow immediately:

- **Sell signals are firing normally — they are not missing.** The second screenshot produced *more* sells than buys. Nothing is silently suppressed at the signal-generation layer.
- I checked the one magenta down arrow that appeared to have no "SELL" label (screenshot 1, ~07:07). Zooming in, the label **is** there — it is hidden underneath your blue annotation stroke. There is no missing-label bug.

### 6.2 The fix

Three options, best last.

1. **Capture the tier-1 arrows instead.** They *are* colour-separable — green up ≈ `#00FF00`, red down. Set `LongColorEntrySignal1` to the green and `ShortColorEntrySignal1` to the red. Free and immediate. The catch: tier-1 fires far more often than the BUY/SELL tier (§3), so it is a looser signal — pair it with `SignalConfirmation1` or one of the six `UseFilterSignal*` banks.
2. **Tag capture**, if the vendor's draw-object tags happen to encode the side. Run `gbSignalProbe` and inspect the tags before betting on this; `removeText` means they churn.
3. **Bridge it — the robust fix.** [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs) reads the two sides where they *are* distinct (the vendor's separate `Series`, cleanly separable from C# even though the arrows are not separable by colour) and re-emits them as two named plots carrying 1/0, plus arrows in two **different** configurable brushes with stable, side-distinct tags `GB_US_BUY_*` / `GB_US_SELL_*`. Point `DD_Entry1_SignalSource` at the bridge instead of at UltimateSignals and the sell side becomes capturable by tag, by colour, or by plot.

### 6.2b The other consumer on that chart

The white BUY/SELL *text* in the screenshots is **Infinity Algo Engine**, which owns exactly that feature set:

```
MarkerEnabled, MarkerStringSignalBuy, MarkerStringSignalSell, MarkerStringSignalNone,
MarkerBrushBullish, MarkerBrushBearish, MarkerFont, MarkerOffset
```

Infinity is a *signal aggregator* — `dictIndicatorInfo`, `dictSignalBarInfo`, condition lists, ATM/order-entry integration. It reads other indicators' output and renders its own markers on top. The bridge in 6.2 serves it too: its condition builder can select the bridge's named `BuySignal` / `SellSignal` plots.

### 6.3 Second-order causes, once capture works

Sells are *generated* fine — screenshot 2 produced 10 of them. Once they can be captured, the remaining questions are fills and profitability:

1. **Per-side gates.** PredatorX has `TakeLongs` / `TakeShorts`; Infinity has `LongSwitchedOn` / `ShortSwitchedOn` and `CpEnableLongShort`. Either left off gives "markers draw, orders never go".
2. **Value convention.** NT8 signal series carry a price when active and `double.NaN` when idle. A condition builder offering only `> < >= <= == !=` cannot express `IsNaN`. The bridge sidesteps this by emitting 1/0.
3. **Fill mechanics** — `RefPriceSell`, `OrderSlmOffset`, `OrderType`, `OrderLMTValidPeriod`, `WaitUntilFlat`, `EntryCooldownTime`, `MaxDailyLoss`.
4. **Regime, not a bug.** Screenshot 2 covers a ~280-point MNQ advance (≈28,100 → 28,380) in under an hour, into which the system fired 10 sells. A Stochastic-OB fade (§3) produces exactly that: a stream of counter-trend shorts at pullback highs, every one losing, while the longs in the same window work. Expected behaviour for an oscillator fade in a trend — settle it with the regime split in §4 of the validation process, then filter the losing bucket rather than trade it.
5. **SaberRenko 70/4 interaction.** Renko-family bars synthesise OHLC and the forming brick is provisional. A ZigZag reading `High[]`/`Low[]` on provisional bricks will place and retract pivots as the brick resolves. Rule it out with the bar-type A/B in §5 of the validation process.

### 6.4 The original ToS study is symmetric

Worth stating plainly, because it locates the defect: the ThinkScript source this was ported from has **no buy/sell asymmetry**. It is the community "Trend Reversal Indicator (with Signals)", origin unattributed, credited on useThinkScript to SkinnyFry and "Bayside of Enhanced Investor", and its two sides mirror exactly:

```
buy  = mov_avg9 > mov_avg14 and mov_avg14 > mov_avg21 and low  > mov_avg9;
sell = mov_avg9 < mov_avg14 and mov_avg14 < mov_avg21 and high < mov_avg9;
```

So the capture problem was introduced by the NT8 port's rendering choice — one brush for both sides — not inherited from the study. See §8 for where to get the original.

---

## 7. Recommendation

1. **Run [gbSignalProbe.cs](gbSignalProbe.cs) on the licence holder's chart first.** It is late-bound — no compile-time reference to the vendor DLL — so it also compiles on a machine without the licence. It prints `Values.Length` vs `Plots.Length` (the capturability gap), the live plot map, and flags repaint. Everything below depends on what it reports.
2. **Install [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs)** and repoint `DD_Entry1_SignalSource` at it. That resolves the magenta colour collision (F1), which is the actual reported problem.
3. **Treat it as a repainting signal source** (F6) and keep `ConfirmationBars ≥ 1`, exactly as `gbUaiWrapperStrategy` does for UltimateAI2. Never accept a marker on the bar it first appears.
4. **Sandbox before it goes near the live workspace** (validation process §1), because of F3 and F7.
5. **Accept that it is untunable** (F4). The evaluation question is binary — does the vendor's fixed configuration have an edge on this instrument and bar type, yes or no. Answer it with the process doc, on forward data only.

---

## 8. Getting the ToS original

The port's fields (`superfast_length`, `fast_length`, `slow_length`, `mov_avg9/14/21`, `buy`, `sell`, `buysignal`, `sellsignal`) are verbatim from a widely circulated ThinkScript study, and the ZigZag half is ToS's own built-in.

- **The signal study** — "Trend Reversal Indicator with Signals", useThinkScript. Origin unattributed; credited there to SkinnyFry and "Bayside of Enhanced Investor". Threads: [the original](https://usethinkscript.com/threads/trend-reversal-indicator-with-signals-for-thinkorswim.183), [enhanced version](https://usethinkscript.com/threads/enhanced-trend-reversal-indicator-for-thinkorswim.393), and [YungTrader's Ultimate Indicator](https://usethinkscript.com/threads/yungtraders-ultimate-indicator.2194) — the last is the closest match to this product's name.
- **The other three components are ToS built-ins** whose source is viewable in the platform itself, which is the authoritative copy and needs no web archaeology: open ThinkOrSwim → *Studies → Edit Studies*, then view/export the thinkScript for **ZigZagHighLow**, **MACD**, and **StochasticFull**. The DLL's own strings name them: `"TOS ZigZagHighLow"`, `"TOS Stochastic full"`, `"TOS Wilder MA"`.

Useful because it gives you the real parameter defaults the port hard-codes (F4) — which is the only way to know what this indicator is actually set to.

---

## Related documents

- [UltimateSignals_Signals.md](UltimateSignals_Signals.md) — plot-by-plot signal contract and consumption code
- [UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) — the repeatable acceptance process
- [gbSignalProbe.cs](gbSignalProbe.cs) — late-bound plot-map / capturability / repaint probe
- [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs) — makes the sell side capturable
