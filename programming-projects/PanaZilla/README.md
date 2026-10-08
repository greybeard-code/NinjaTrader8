# BreakSignalCombined

A NinjaTrader 8 indicator that combines two locked indicators —
**ninZaPANAKanal** (ninZa.co) and **RenkoKings ThunderZilla** — into a single
entry + optional exit signal system.

> **Important:** This version of `BreakSignalCombined` is built against the
> specific versions of `RenkoKings_ThunderZilla.cs` and `ninZaPANAKanal.cs`
> included in this repository. It will not compile against other versions of
> these indicators as the enum types and method signatures may differ.

---

## How It Works

### Entry Signals

A **Long** arrow fires when both conditions are true on the same bar:

| Indicator | Condition |
|-----------|-----------|
| PANA Kanal `Signal_Trade` | `= 2` (Break Up) |
| ThunderZilla `Signal_Trade` | `>= TZ_BullishThreshold` (default `3` = strongest bullish) |

A **Short** arrow fires when both conditions are true:

| Indicator | Condition |
|-----------|-----------|
| PANA Kanal `Signal_Trade` | `= -2` (Break Down) |
| ThunderZilla `Signal_Trade` | `<= TZ_BearishThreshold` (default `-3` = strongest bearish) |

#### PANA Kanal Signal_Trade values

| Value | Meaning |
|-------|---------|
| `2`  | Break Up ← **Long entry trigger** |
| `1`  | Uptrend Start |
| `3`  | Pullback Bullish |
| `-1` | Downtrend Start |
| `-2` | Break Down ← **Short entry trigger** |
| `-3` | Pullback Bearish |
| `0`  | No signal |

#### ThunderZilla Signal_Trade values

| Value | Meaning |
|-------|---------|
| `3`  | Bullish — Per Trend (strongest) |
| `2`  | Bullish — Per Flat |
| `1`  | Bullish |
| `-1` | Bearish |
| `-2` | Bearish — Per Flat |
| `-3` | Bearish — Per Trend (strongest) |

---

### Exit Signals (optional)

Enable via the **Enable Exit Signals** checkbox (Group 3 in the indicator panel).

After an entry fires, the indicator tracks the trade and fires an exit arrow
when the **first** of these conditions is met:

| Priority | Condition | Description |
|----------|-----------|-------------|
| A | **ATR Trail Breached** | Price closes through the trailing stop level |
| B | **MA Cross** *(optional)* | Price closes on the wrong side of the smoothed trend MA |
| C | **Color Flip** | An opposing-color bar appears after `MinBarsInTrade` bars |

The **ATR trailing stop** ratchets with price:
- Long: trails `highestHigh - (ATR × multiplier)`
- Short: trails `lowestLow + (ATR × multiplier)`

The trail level is plotted as a gray line on the chart while in a trade
(toggle with **Show Trail Line**).

---

## Parameters

### 1. ThunderZilla

| Parameter | Default | Description |
|-----------|---------|-------------|
| Trend: MA Type | SMA | Moving average type for trend detection |
| Trend: Period | 100 | MA period |
| Trend: Smoothing Enabled | false | Apply a secondary smoothing MA |
| Trend: Smoothing Method | EMA | Smoothing MA type |
| Trend: Smoothing Period | 10 | Smoothing MA period |
| Stop: Offset Multiplier (Ticks) | 60 | ThunderZilla internal stop offset |
| Signal: Qty Per Flat | 2 | Max signals allowed in flat conditions |
| Signal: Qty Per Trend | 999 | Max signals allowed in trending conditions |
| Bullish Threshold (>=) | 3 | Minimum TZ signal value to qualify as bullish |
| Bearish Threshold (<=) | -3 | Maximum TZ signal value to qualify as bearish |

### 2. PANA Kanal

| Parameter | Default | Description |
|-----------|---------|-------------|
| Period | 20 | Channel calculation period |
| Factor | 4 | Channel width multiplier |
| Middle Period | 14 | Middle line period |
| Signal Break Split (Bars) | 20 | Bars used to detect a break split |
| Signal Pullback Finding Period | 10 | Bars to look back for pullback detection |

### 3. Exit Signals

| Parameter | Default | Description |
|-----------|---------|-------------|
| Enable Exit Signals | true | Master toggle for all exit logic |
| ATR Period | 14 | ATR calculation period |
| ATR Multiplier | 2.0 | Trail distance = ATR × this value |
| Use MA Cross Exit | true | Exit when price crosses the smoothed trend MA |
| Min Bars In Trade | 3 | Minimum bars before a color-flip exit is allowed |

### 4. Visuals — Entry

| Parameter | Default | Description |
|-----------|---------|-------------|
| Long Arrow Color | Cyan | Color of the long entry arrow |
| Short Arrow Color | Magenta | Color of the short entry arrow |
| Arrow Offset (ticks) | 3 | Distance from bar high/low to arrow |
| Show Label | true | Display text label next to the arrow |
| Long Label Text | "Break Buy" | Label text for long entries |
| Short Label Text | "Break Sell" | Label text for short entries |
| Label Font Size | 10 | Font size for all labels |

### 5. Visuals — Exit

| Parameter | Default | Description |
|-----------|---------|-------------|
| Long Exit Color | Orange | Color of the long exit arrow |
| Short Exit Color | Orange | Color of the short exit arrow |
| Long Exit Label | "Exit Long" | Label text for long exits (reason appended automatically) |
| Short Exit Label | "Exit Short" | Label text for short exits (reason appended automatically) |
| Show Trail Line | true | Plot the ATR trail level on the chart |

---

## Output Plots

`EntrySignal` and `ExitSignal` are transparent on the chart but visible in the
NT8 data box and readable by strategies and Market Analyzer columns.
`TrailStop` is a gray line plotted on the chart while a trade is active.

| Plot | Values | Use |
|------|--------|-----|
| `EntrySignal` | `1` = long entry, `-1` = short entry, `0` = none | Strategy entry trigger |
| `ExitSignal` | `1` = long exit, `-1` = short exit, `0` = none | Strategy exit trigger |
| `TrailStop` | ATR trail price level | Visual only (gray line on chart) |

### Reading plots from a strategy

```csharp
private BreakSignalCombined bsc;

// In OnStateChange -> DataLoaded:
bsc = BreakSignalCombined(/* parameters */);

// In OnBarUpdate:
if (bsc.EntrySignal[0] == 1)
    EnterLong();
else if (bsc.EntrySignal[0] == -1)
    EnterShort();

if (bsc.ExitSignal[0] == 1)
    ExitLong();
else if (bsc.ExitSignal[0] == -1)
    ExitShort();
```

---

## Installation

1. Copy `BreakSignalCombined.cs` into:
   ```
   Documents\NinjaTrader 8\bin\Custom\Indicators\
   ```
2. The following locked indicators must already be installed:
   - **ninZaPANAKanal** — from [ninZa.co](https://ninza.co)
   - **RenkoKings_ThunderZilla** — from RenkoKings
3. In NinjaTrader: **NinjaScript Editor → Compile** (F5)

---

## Dependencies

| Indicator | Namespace | Source |
|-----------|-----------|--------|
| ninZaPANAKanal | `NinjaTrader.NinjaScript.Indicators` | ninZa.co |
| RenkoKings_ThunderZilla | `NinjaTrader.NinjaScript.Indicators.RenkoKings` | RenkoKings |
