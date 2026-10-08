# GodZilla Indicators — Technical Reference

All six indicators share a common namespace (`NinjaTrader.NinjaScript.Indicators.GreyBeard`), a consistent `Signal_Trade` series contract, and are consumed directly by GodZillaKilla and GodZuki via their factory methods at `State.DataLoaded`.

**Signal contract:** All `Signal_Trade` series output integer values. Positive = bullish, negative = bearish, 0 = no signal. The magnitude encodes signal type (e.g., 1 = Return, 2 = Breakout for KO). GodZillaKilla and GodZuki use configurable comparison operators to select which magnitude(s) to act on.

---

## gbKingOrderBlock — King Order Block

Identifies **institutional order blocks** (supply and demand zones) using market structure break analysis.

### How It Works

1. **Swing Point Detection** — scans for swing highs and lows using a configurable `SwingPointNeighborhood` period. Three detection algorithms handle flat-top/bottom plateaus and mixed-width structures.
2. **BOS / CHoCH Classification** — when price breaks a swing, the indicator classifies it as either a *Break of Structure* (BOS, trend continuation) or *Change of Character* (CHoCH, potential reversal) based on the sequence of prior breaks.
3. **Imbalance (Fair Value Gap) Detection** — looks for consecutive marubozu candles in the same direction to mark imbalanced price zones.
4. **Order Block Formation** — an Order Block is confirmed when a swing point is paired with a nearby imbalance and a BOS/CHoCH in the opposite direction, within `OrderBlockFindingBosChochPeriod` bars.
5. **Zone Lifecycle** — blocks remain active until price closes through their invalidation level. Age is capped by `OrderBlockAge` bars.
6. **Signals:**
   - **Return (±1)** — price wicks back into an active Order Block zone and closes on the correct side.
   - **Breakout (±2)** — price closes through the invalidation level of an active Order Block.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Return Bullish — price returned into a bullish order block |
| +2 | Breakout Bullish — price broke out through a bullish order block |
| -1 | Return Bearish |
| -2 | Breakout Bearish |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| SwingPointNeighborhood | 5 | Bars each side for swing high/low detection |
| ImbalanceQualifying | 3 | Minimum consecutive marubozu bars to qualify an imbalance |
| OrderBlockFindingBosChochPeriod | 50 | Look-back bars for BOS/CHoCH pairing |
| OrderBlockAge | 500 | Maximum bars an order block stays active |
| OrderBlocksSameDirectionOffset | 10 | Minimum tick separation between same-direction blocks |
| OrderBlocksDifferenceDirectionOffset | 10 | Minimum tick separation between opposite-direction blocks |
| SignalTradeQuantityPerOrderBlock | 3 | Maximum signals per block |
| SignalTradeSplitBars | 6 | Minimum bar spacing between signals from the same block |

---

## gbPANAKanal — PANA Kanal

A **Keltner-style adaptive channel** that defines trend direction and generates pullback and breakout trade signals.

### How It Works

1. **Channel Calculation** — uses a Wilder-style ATR multiplied by `Factor` to build dynamic upper and lower bands around the close. Once price closes through a band, the band locks in place (ratchet behaviour) and only moves in the trend direction.
2. **Middle Band** — a double-smoothed EMA over `MiddlePeriod` acts as the Keltner centre line.
3. **Trend Direction** — uptrend when close is above the lower band; downtrend when below the upper band. Direction flips emit a Trend Start signal.
4. **Fibonacci Pullback Zones** — at each trend flip the indicator places Fibonacci levels at 61.8% and 78.6% retracement as pullback target zones.
5. **Signals:**
   - **Trend Start (±1)** — candle that crosses and closes past the channel band.
   - **Break (±2)** — price crosses the Keltner middle band in trend direction after a pullback reset.
   - **Pullback (±3)** — inside the pullback-finding period, a reversal candle that wicks into the 61.8% Fibonacci zone.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Trend Start Up |
| +2 | Break Up |
| +3 | Pullback Bullish |
| -1 | Trend Start Down |
| -2 | Break Down |
| -3 | Pullback Bearish |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| Period | 20 | ATR period for channel width |
| Factor | 4.0 | ATR multiplier for band distance |
| MiddlePeriod | 14 | Double-EMA period for centre line |
| SignalBreakSplit | 20 | Minimum bars between Break signals |
| SignalPullbackFindingPeriod | 10 | Look-back window for pullback detection |

---

## gbThunderZilla — ThunderZilla

A **dual-system trend + pullback indicator** combining a SolarWind trailing stop, a Sumo multi-MA cloud pullback detector, and a multi-oscillator overbought/oversold slowdown filter.

### How It Works

1. **SolarWind (SW) System** — computes a trailing stop offset by `StopOffsetMultiplierStop × TickSize`. The trend flips when close crosses the stop. A secondary "trend vector" line tracks swing momentum.
2. **Sumo Pullback System** — aligns the configurable Trend MA plus EMA 14/30/45. A two-candle reversal pattern (prior candle opposite, current candle trend direction) with price entirely inside the MA stack fires a Sumo pullback. Signal spacing via `SignalQuantityPerFlat` and `SignalQuantityPerTrend`.
3. **Combined Trend State** — *uptrend* only when both SW and Sumo agree; *downtrend* when both point down; neutral when they disagree.
4. **Multi-Oscillator OBOS Slowdown** — MFI (14), RSI (14, smooth 3), and Stochastics (14/7/3) evaluated simultaneously. When all three are overbought (> 70) or oversold (< 30) a Slowdown signal fires on the exit bar from the overlap zone.
5. **Move Stop Signal** — fires when the trailing stop crosses the Trend MA, signalling an upgraded profit-protecting stop.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Trend Start Long |
| +2 | Slowdown (OBOS exit) — bearish reversal warning |
| +3 | Sumo Pullback — two-candle reversal inside MA cloud |
| +4 | Move Stop — trailing stop crossed Trend MA |
| Negative mirrors | Same signals in downtrend direction |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| TrendMAType | SMA | MA type for Trend MA (SMA/EMA/WMA/etc.) |
| TrendPeriod | 200 | Period for Trend MA |
| TrendSmoothingEnabled | false | Apply secondary smoothing pass to Trend MA |
| TrendSmoothingMethod | EMA | Smoothing MA type |
| TrendSmoothingPeriod | 10 | Smoothing period |
| StopOffsetMultiplierStop | 60.0 | Trailing stop distance in ticks |
| SignalQuantityPerFlat | 2 | Max Sumo signals when trend MA is flat |
| SignalQuantityPerTrend | 999 | Max Sumo signals when trend MA is sloping |

---

## gbSuperJumpBoost — Super JumpBoost

An **ATR-derived multi-level zone indicator** that builds supply/demand zones from four configurable trend vectors and tracks price interaction through the full zone lifecycle.

### How It Works

1. **Trend Vectors** — four independent instances each run a trend-flip algorithm using ATR-scaled offsets (`OffsetLevel1`–`OffsetLevel4` relative to `OffsetBase`). Each tracks its own trend state, trailing stop, and slowdown counter.
2. **Zone Formation** — when two or more adjacent trend vectors agree on direction, their price levels are sorted into a `ZoneInfo` with up to four lines: TopPrice, PriceLevel1, PriceLevel2, BottomPrice.
3. **Zone Lifecycle** — zones cycle through Active → Inactive → Broken lists based on price interaction.
4. **Signals:**
   - **Return (±1)** — a confirming close on the correct side of the zone's inner level, following a wick through it, within `SignalQuantityPerZone` limit and `SignalSplit` spacing.
   - **Zone Start (±2)** — fires immediately when a new zone is accepted.
5. **Extremum Tracking** — independent swing high/low tracking with `ExtremeNeighborhood`. Levels migrate from naked to tested.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Return Long — price returned into bullish zone |
| +2 | Zone Start Long — new bullish zone formed |
| -1 | Return Short |
| -2 | Zone Start Short |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| SensitiveModeEnabled | true | Increases sensitivity by widening slowdown windows |
| OffsetLevel1–4 | 1.0/2.0/3.0/4.0 | ATR multipliers for each trend vector |
| OffsetBase | 4.0 | Base ATR multiplier |
| ReferencePricePeriod | 2 | Bars used to compute reference price for zone validation |
| LineLevelsOffset | 100 | Tick offset for zone line rendering |
| ExtremeNeighborhood | 30 | Bars each side for extremum swing detection |
| SignalCloseThreshold | 70 | Percentage close proximity required for Return signal |
| SignalQuantityPerZone | 2 | Maximum Return signals per zone |
| SignalSplit | 20 | Minimum bars between signals |

---

## gbSumoPullback — Sumo Pullback

A **multi-MA cloud pullback indicator** that signals when a two-candle reversal pattern fires entirely inside a stacked moving average cloud.

### How It Works

1. **MA Cloud** — one slow MA (default SMA 60) and three fast MAs (default EMA 14/30/45). The cloud spans the full range across all four MAs.
2. **Trend Alignment Check** — the slow MA must be the minimum of all four (uptrend stack) or maximum (downtrend stack). This ensures the cloud is properly sorted before signalling.
3. **Candle Pattern** — the full price bar must sit inside the cloud (`low > cloudMin` and `high < cloudMax`) and a two-bar reversal: prior candle opposite-direction, current candle trend-direction.
4. **Signal Spacing** — first signal resets a counter. Second signal requires `SignalSplitFirst` bars. Subsequent signals require `SignalSplitSecond` bars.
5. **Fair Value Plot** — arithmetic mean of all four MAs plotted as a reference line.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Pullback Bullish — two-candle reversal inside uptrend MA cloud |
| -1 | Pullback Bearish — two-candle reversal inside downtrend MA cloud |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| SlowMAType | SMA | Slow MA type |
| SlowMAPeriod | 60 | Slow MA period |
| FastMA1Type/Period | EMA/14 | First fast MA |
| FastMA2Type/Period | EMA/30 | Second fast MA |
| FastMA3Type/Period | EMA/45 | Third fast MA |
| SignalSplitFirst | 15 | Bars required before second signal |
| SignalSplitSecond | 30 | Bars required before subsequent signals |

> **Note:** `SlowMASmoothingEnabled` and `FastMA*SmoothingEnabled` properties are present but the smoothing pass is not yet implemented — the raw MA value is always used.

---

## gbNobleCloud — Noble Cloud

A **kernel-envelope cloud indicator** that wraps a smoothed baseline MA in dynamic standard-deviation bands and fires trade signals on confirmed cloud re-entry. *Adapted from DDNobleCloud by DD.*

### How It Works

1. **Baseline** — configurable MA type and period with optional secondary smoothing pass. Plotted as a dashed line coloured by rising/falling direction.
2. **Kernel Bands** — a separate MA anchors a dynamic envelope. Upper and lower thresholds are set at `kernel ± (Sensitivity / 50) × StdDev(KernelPeriod)`. Optional EMA smoothing (`Smoothness`) reduces band noise.
3. **Cloud State Machine** — when the baseline crosses above the upper threshold, the indicator enters a bearish cloud state (−1); when it crosses below the lower threshold, a bullish cloud state (+1).
4. **Bar Filter** — signals are only eligible between `FilterBarMin` and `FilterBarMax` bars into a cloud run, filtering out early false signals and stale cloud entries.
5. **Trade Signals:**
   - **Bullish** — inside a bullish cloud, the low wicks below the lower threshold and the close re-enters above it; prior candle bearish, current candle bullish; within `SignalSplit` spacing.
   - **Bearish** — inside a bearish cloud, the high touches the upper threshold and the close falls back below it; prior candle bullish, current candle bearish.

### Signal_Trade Values
| Value | Meaning |
|---|---|
| +1 | Bullish re-entry signal |
| -1 | Bearish re-entry signal |

### Key Parameters
| Parameter | Default | Description |
|---|---|---|
| Sensitivity | 60.0 | Controls band width (higher = wider bands) |
| Smoothness | 1 | EMA smoothing passes on bands (1 = one pass) |
| BaselineMAType | SMA | Baseline MA type |
| BaselinePeriod | 60 | Baseline MA period |
| BaselineSmoothingEnabled | true | Apply secondary smoothing to baseline |
| BaselineSmoothingMethod | EMA | Baseline smoothing type |
| BaselineSmoothingPeriod | 60 | Baseline smoothing period |
| KernelMAType | SMA | Kernel MA type |
| KernelPeriod | 20 | Kernel MA period |
| KernelSmoothingEnabled | true | Apply secondary smoothing to kernel |
| KernelSmoothingMethod | EMA | Kernel smoothing type |
| KernelSmoothingPeriod | 5 | Kernel smoothing period |
| SignalSplit | 5 | Minimum bars between signals in same cloud run |
| FilterEnabled | true | Enable bar count filter |
| FilterBarMin | 10 | Minimum bars into cloud run before signals eligible |
| FilterBarMax | 300 | Maximum bars into cloud run for signal eligibility |

---

## Common Features

All six indicators share:

| Feature | Description |
|---|---|
| Toggle button | Draggable on-chart button to show/hide drawings without removing the indicator |
| Alert system | Popup, WAV sound (with rearm timer), and email alerts per signal condition |
| Bar colouring | Optional bias-based bar painting by trend state |
| Custom marker rendering | Text markers drawn via DirectWrite for DPI-aware rendering |
| Z-Order control | Configurable `IndicatorZOrder` for correct panel layering |

---

← [README.md](../README.md)
