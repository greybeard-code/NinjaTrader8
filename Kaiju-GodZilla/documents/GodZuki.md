# GodZuki — Signal Indicator

**Version:** 1.4.1
**Namespace:** `NinjaTrader.NinjaScript.Indicators.GreyBeard`

GodZuki is the signal indicator layer of the GodZilla Suite. It reads the same six sub-indicators and evaluates the same confluence logic as GodZillaKilla, but executes no trades. Its primary purpose is to feed signals into third-party trade management systems — such as Predator or Infinity Algo — that supply their own entry execution and order management. It is also used for fully manual trading, where the trader takes entries based on GodZuki's visual arrows and Data Box values. Signal history can be audited on any chart, audio alerts trigger on group fires, and all signals are logged to CSV.

---

## Version History

| Version | Summary |
|---|---|
| **1.4.1** | **Performance pass (no signal output change).** Child sub-indicators are now instantiated only when their signal is enabled in Set 1 or Set 2 — hosted indicators are calculated every bar whether or not they're read, so skipping unused ones avoids running heavy sub-indicators (ThunderZilla, SumoPullback, NobleCloud, etc.) for signals the user has turned off. The `OnBarUpdate` null-guard now only fails on a *needed* indicator. Signal reads use a series-based `SafeSignalRead` overload (no per-read closure allocation, null-safe for disabled indicators). HUD title now shows the indicator's instance name instead of a hardcoded literal. CSV log opens lazily on the first logged bar using `Time[0]` so Market Replay/Playback files are dated by session, not wall-clock. |
| **1.4** | **Confirmation Bars** — new `Confirmation Bars` property (range 0–25, default 0) at the top of the Signals section. When set to N, the arrow and Data Box signal are suppressed after the group trigger fires. On bar N, if price has moved in the signal direction (close higher than signal bar for long, lower for short), the arrow fires and Set1/Set2 values publish. If not, the setup is dropped and the window closes. A new signal on any bar during the wait restarts the clock. Default 0 = immediate signal, identical to prior behavior. Applies independently to Set 1 and Set 2. |
| **1.3** | Data Box expanded from 11 to 18 plots. Both Signal added (1 when S1=1 AND S2=1, -1 when S1=-1 AND S2=-1, 0 otherwise) at position 5. Individual signals split into S1_KO–S1_NC (Set 1 thresholds, raw) and S2_KO–S2_NC (Set 2 thresholds, raw; 0 when Set 2 disabled). EMA Dir moved to position 6. Public accessor properties renamed to `S1KOSignal`…`S1NCSignal`, `S2KOSignal`…`S2NCSignal`, and `BothSignal` added. Default comparison operators changed from `Equal` to `GreaterOrEqual` (long) / `LessOrEqual` (short) for all 12 indicator pairs in Set 1 and Set 2, matching GodZillaKilla defaults. |
| **1.2.1** | All 6 signals enabled by default in both Set 1 and Set 2. Set 1 `RequiredCount` default raised 2 → 3 (avoids signal flood with 6/6 on). HUD Set1/Set2 rows now color by live trigger state: green = long firing, red = short firing, white = watching, dim = off, yellow `[!]` = `RequiredCount` exceeds enabled-signal count. Set1/Set2 lines expanded to show full enabled signal list (`R:3/6: KO, PA, TH, SJ, SU, NC`). EMA label renamed to "EMA Cross:". HUD box widths widened to fit longer rows. |
| **1.2** | Per-indicator **Require** flags added for both Set 1 and Set 2 (`RequireKOSignal`…`RequireNCSignal`, `G2_RequireKOSignal`…`G2_RequireNCSignal`). A required indicator must appear among the signals that fired in the trigger direction; meeting Required Count without it vetoes the trigger. All default to false. Debug print updated: `Signals=[...]` replaced with `Set1=[...]` and `Set2=[...]`; required indicators prefixed with `+`. |
| 1.1 | Internal release. |
| **1.0.3** | Indicator null diagnostic — one-time print at realtime with per-indicator load status. Signal reads hardened: outer null guards removed in favor of unified SafeSignalRead error handling across all six signals. |
| 1.0.2 | Audio alert sound file properties now use NT8 file picker (browse for .wav). |
| 1.0.1 | Fixed nested enum compile errors — `GodZukiSignalOperator`, `GodZukiHudCorner`, `GodZukiHudSize` moved to namespace level. Set 1 and Set 2 now draw independently on the same bar. Set 2 arrow offset increased (22 ticks vs Set 1 at 12 ticks). Group arrow labels changed from numeric suffix to `-S1` / `-S2`. |
| 1.0.0 | Initial release. |

---

## How It Differs from GodZillaKilla

| Feature | GodZillaKilla | GodZuki |
|---|---|---|
| Type | Strategy | Indicator |
| Trades | Yes (ATM or Fixed Ticks) | No |
| Session filters | Yes | No |
| News filter | Yes | No |
| Daily PnL limits | Yes | No |
| Martingale recovery | Yes | No |
| Signal visualization | Yes | Yes |
| Audio alerts | Yes | Yes |
| CSV logging | Trade log | Signal log |
| Data Box outputs | No | Yes (18 plots) |
| Public Series outputs | No | Yes |
| Control panel | Yes | No |

---

## Signal System

GodZuki uses the same signal configuration as GodZillaKilla: six sub-indicators, two independent trigger sets, configurable operators and threshold values per indicator. Default operators are `GreaterOrEqual` (long) / `LessOrEqual` (short); default thresholds are KO/SJ/SU/NC = ±1, PA/TH Set 1 = ±2, PA/TH Set 2 = ±3.

### Sub-Indicator Signals
Each of the six indicators exposes a `Signal_Trade` series. GodZuki reads `Signal_Trade[0]` each bar and computes a normalized −1 / 0 / +1 output using the configured comparison operator and value.

### Required Count
With N indicators enabled and Required Count = R, the trigger fires when at least R signals agree in the same direction. Flat signals (0) do not count toward either side. If both long and short counts both reach R on the same bar, the conflict guard suppresses the trigger — no signal fires. Setting Required Count = N requires all enabled signals to agree.

### Require Flags
Each indicator in Set 1 and Set 2 has a **Require** flag (`Set 1 Require KingOrderBlock`, `Set 2 Require PANAKanal`, etc.). All default to `false`.

When enabled, that indicator must be one of the signals that actually fired in the trigger direction. Meeting Required Count without it vetoes the trigger. Multiple indicators may be required simultaneously — all must be present in the agreeing set.

### EMA Filter
Optional short/long EMA filter. When enabled, signals that conflict with the EMA direction are suppressed from visuals, arrows, audio, and the Set1/Set2 output plots. Individual sub-indicator signal values (KO–NC) in the Data Box reflect pre-filter raw values so near-misses remain visible.

### Trigger Sets — Independent Signals
Set 1 and Set 2 are evaluated and drawn **independently on every bar.** Both can produce arrows on the same bar simultaneously. This differs from GodZillaKilla where Set 2 is used only as a trading fallback when Set 1 does not trigger.

---

## Visual Output

### EMA Lines
When `EnableEmaFilter = true`, two EMA lines are plotted directly on the price panel:
- **EMA Short** — DodgerBlue, 2px
- **EMA Long** — HotPink, 2px

### Signal Arrows
Per-indicator and group trigger arrows are drawn on the price panel at configurable tick offsets above (short) or below (long) each signal bar. Rolling 250-bar cleanup prevents draw object pool exhaustion.

### Group Trigger Arrows
Set 1 and Set 2 arrows draw independently at different distances from the bar:

| Set | Arrow offset | Label format |
|---|---|---|
| Set 1 | ArrowOffset + 12 ticks | `GODZUKI-S1` (or custom text `-S1`) |
| Set 2 | ArrowOffset + 22 ticks | `GODZUKI-S2` (or custom text `-S2`) |

The 10-tick gap between Set 1 and Set 2 ensures the arrows and labels are visually distinct when both fire on the same bar.

### Group Trigger Back-Brush
When either Set 1 or Set 2 fires, the bar background is highlighted with a configurable semi-transparent brush. Set 1 takes priority when both fire.

---

## HUD (Dashboard)

The SharpDX overlay panel shows four fixed rows:

```
GodZuki  v1.4
─────────────────────────────────────────────────────────────
EMA Cross: ON   21=19843.50 / 50=19856.25   ← green=bullish, red=bearish, dim=off
Set1 Enabled R:3/6: KO, PA, TH, SJ, SU, NC ← green=long, red=short, white=watching, dim=off
Set2: OFF                                    ← dim when disabled
```

- **EMA row** — shows `ON/OFF` status and live price values for each EMA when enabled; green (bullish) or red (bearish)
- **Set1/Set2 rows** — show full enabled-signal list and required count (`R:required/enabled`); live color reflects current trigger state: green = long firing, red = short firing, white = watching (enabled but not triggered), dim = off. Yellow `[!]` prefix when `RequiredCount` exceeds the number of enabled signals.

The box height is fixed at 4 rows — no layout shifting as signals change.

Position: `TopLeft` / `TopRight` / `BottomLeft` / `BottomRight` / `Center` / `Hidden`.
Size: `Tiny` / `Small` / `Normal` / `Large` / `Huge`.

---

## Data Box

GodZuki registers 18 `AddPlot` entries visible when hovering over any bar (`ShowTransparentPlotsInDataBox = true`; signal plots draw no visible chart line):

| # | Plot | Index | Value | Notes |
|---|---|---|---|---|
| 1 | EMA Short | Values[0] | Short EMA price | Chart line — DodgerBlue |
| 2 | EMA Long | Values[1] | Long EMA price | Chart line — HotPink |
| 3 | EMA Dir | Values[2] | 1=bullish / −1=bearish / 0=off | — |
| 4 | Set1 Signal | Values[3] | −1 / 0 / 1 | EMA-filtered group result |
| 5 | Set2 Signal | Values[4] | −1 / 0 / 1 | EMA-filtered group result |
| 6 | **Both Signal** | Values[5] | −1 / 0 / 1 | 1 when S1=1 AND S2=1; −1 when S1=−1 AND S2=−1; 0 otherwise |
| 7 | S1_KO Signal | Values[6] | −1 / 0 / 1 | Set 1 threshold, raw (0 if KO disabled) |
| 8 | S1_PA Signal | Values[7] | −1 / 0 / 1 | Set 1 threshold, raw (0 if PA disabled) |
| 9 | S1_TH Signal | Values[8] | −1 / 0 / 1 | Set 1 threshold, raw (0 if TH disabled) |
| 10 | S1_SJ Signal | Values[9] | −1 / 0 / 1 | Set 1 threshold, raw (0 if SJ disabled) |
| 11 | S1_SU Signal | Values[10] | −1 / 0 / 1 | Set 1 threshold, raw (0 if SU disabled) |
| 12 | S1_NC Signal | Values[11] | −1 / 0 / 1 | Set 1 threshold, raw (0 if NC disabled) |
| 13 | S2_KO Signal | Values[12] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or KO disabled) |
| 14 | S2_PA Signal | Values[13] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or PA disabled) |
| 15 | S2_TH Signal | Values[14] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or TH disabled) |
| 16 | S2_SJ Signal | Values[15] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or SJ disabled) |
| 17 | S2_SU Signal | Values[16] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or SU disabled) |
| 18 | S2_NC Signal | Values[17] | −1 / 0 / 1 | Set 2 threshold, raw (0 if Set 2 or NC disabled) |

**Set1/Set2 vs individual signals:** Set1 and Set2 reflect the EMA-filtered result — 0 when the EMA filter blocks the group trigger. Individual S1_/S2_ values are always raw computed signals so near-misses remain visible even when the EMA filter suppressed the group trigger.

**Both Signal:** Requires both group triggers to agree. With Set 2 disabled, Both Signal is always 0 (there is no second set to agree with).

**Note:** If Set1 or Set2 show non-zero in the Data Box but no arrow appears on the chart, check that `Group: Show Trigger Arrows = true` in the Display properties and that `GroupTriggerBrush` is not set to a transparent colour.

---

## Public Series Outputs

All signal plots are exposed as typed public properties for use by external strategies or indicators:

```csharp
Series<double> EmaSignal    // Values[2]  — EMA filter direction (1=bullish, −1=bearish, 0=off)
Series<double> Set1Signal   // Values[3]  — EMA-filtered Set 1 group result
Series<double> Set2Signal   // Values[4]  — EMA-filtered Set 2 group result
Series<double> BothSignal   // Values[5]  — 1 when S1=1 AND S2=1; −1 when S1=−1 AND S2=−1

// Set 1 individual signals — Set 1 operator/threshold, raw pre-filter (0 if disabled)
Series<double> S1KOSignal   // Values[6]  — KingOrderBlock
Series<double> S1PASignal   // Values[7]  — PANAKanal
Series<double> S1THSignal   // Values[8]  — ThunderZilla
Series<double> S1SJSignal   // Values[9]  — SuperJumpBoost
Series<double> S1SUSignal   // Values[10] — SumoPullback
Series<double> S1NCSignal   // Values[11] — NobleCloud

// Set 2 individual signals — Set 2 (G2_) operator/threshold, raw pre-filter (0 if Set 2 or signal disabled)
Series<double> S2KOSignal   // Values[12] — KingOrderBlock
Series<double> S2PASignal   // Values[13] — PANAKanal
Series<double> S2THSignal   // Values[14] — ThunderZilla
Series<double> S2SJSignal   // Values[15] — SuperJumpBoost
Series<double> S2SUSignal   // Values[16] — SumoPullback
Series<double> S2NCSignal   // Values[17] — NobleCloud
```

**Usage from a consuming strategy:**
```csharp
var gz = GodZuki(/* params */);
if (gz.Set1Signal[0] == 1)    // Set 1 long trigger this bar
if (gz.Set2Signal[0] == -1)   // Set 2 short trigger this bar
if (gz.BothSignal[0] == 1)    // both sets agree long this bar
if (gz.EmaSignal[0]  == 1)    // EMA filter is bullish
if (gz.S1PASignal[1] == -1)   // PANAKanal (Set 1 threshold) was short last bar
if (gz.S2PASignal[0] == -1)   // PANAKanal (Set 2 threshold) is short this bar
```

All series support historical lookback via `[n]` indexing.

---

## Audio Alerts

When `EnableSignalAudioAlerts = true`:
- **Individual alerts** — fires when a single sub-indicator signal passes the EMA filter, deduped to once per bar per direction per indicator
- **Group alerts** — fires independently for Set 1 and Set 2; both can alert on the same bar

Both have independent WAV file selection via the NT8 file picker. Deduplication uses a `CurrentBar:DIRECTION` stamp per alert key.

---

## CSV Signal Log

When `LogEnabled = true`, a CSV file is created at `State.DataLoaded`:

**Filename:** `GodZuki_[AccountName]_YYYYMMDD_HHmmss.csv`

Account name is read from the chart's ChartTrader account at load time. Falls back to `NoAccount` if unavailable.

**Columns:**
`DateTime, Instrument, Set1, Set2, EMA, KO, PA, TH, SJ, SU, NC`

**Write trigger:** One row per bar when any signal other than EMA fires (KO, PA, TH, SJ, SU, NC, Set1, or Set2 is non-zero). EMA column is always included as a status field showing filter direction at time of signal.

**DateTime** uses `Time[0]` (bar time) for correct timestamps during Market Replay and playback.

**Example:**
```
DateTime,Instrument,Set1,Set2,EMA,KO,PA,TH,SJ,SU,NC
2026-05-17 09:14:00,NQ 06-26,1,0,1,0,1,1,0,0,0
2026-05-17 09:35:00,NQ 06-26,-1,-1,-1,0,-1,-1,0,0,0
```

---

## Debug Output

Enable `EnableDebug = true` to see Output window diagnostics:

```
[GodZuki] DataLoaded | Instr=NQ 06-26 | Set1=[PA,+TH,SJ] | Set2=OFF | Set1Req=2 | EMA=ON (21/50) | Log=ON
[GodZuki] CSV log opened | Acct=Sim101 | C:\...\GodZuki_Sim101_20260517_143022.csv
[GodZuki] Bar=1842 09:14:00 | KO=0 PA=1 TH=1 SJ=0 SU=0 NC=0
[GodZuki] Bar=1842 | Set1=LONG[PA+TH] OK | Set2=FLAT
[GodZuki] Bar=1843 | EMA filter BLOCKED signal(s) | EMA=BEARISH (19821.50/19844.25)
[GodZuki] Bar=1842 | AUDIO | PANAKanal LONG | Alert1.wav
[GodZuki] Bar=1842 | AUDIO | Group Trigger Set1 LONG | Alert2.wav
[GodZuki] Bar=1842 | CSV | Set1=1 Set2=0 EMA=1 KO=0 PA=1 TH=1 SJ=0 SU=0 NC=0
```

---

## Indicator Settings

All six sub-indicator parameters are exposed in the **Indicator Settings** section (hidden by default — toggle `Show Indicator Settings = true`). Parameter names and grouping exactly match GodZillaKilla for cross-reference.

See [Indicators.md](Indicators.md) for full parameter documentation.

---

## Compile Notes

The following types are defined at **namespace level** (not nested inside the class) to avoid type resolution errors when compiled alongside other GodZilla Suite indicators:

- `GodZukiSignalOperator` — comparison operator enum for signal thresholds
- `GodZukiHudCorner` — HUD position enum
- `GodZukiHudSize` — HUD size enum

GodZuki has **no dependency on GodZillaKilla**. It requires only the six GodZilla Suite sub-indicators (`gbKingOrderBlock`, `gbPANAKanal`, `gbThunderZilla`, `gbSuperJumpBoost`, `gbSumoPullback`, `gbNobleCloud`) and standard NT8/SharpDX framework types.

---

← [README.md](../README.md)
