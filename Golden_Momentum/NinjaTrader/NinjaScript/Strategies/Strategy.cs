// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.Strategies.Strategy
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.Strategies;

public class Strategy : StrategyRenderBase
{
  private readonly Indicator indicator;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public Strategy()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WoodiesCCI WoodiesCCI(
    int chopIndicatorWidth,
    int neutralBars,
    int period,
    int periodEma,
    int periodLinReg,
    int periodTurbo,
    int sideWinderLimit0,
    int sideWinderLimit1,
    int sideWinderWidth)
  {
    return (NinjaTrader.NinjaScript.Indicators.WoodiesCCI) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WoodiesCCI WoodiesCCI(
    ISeries<double> input,
    int chopIndicatorWidth,
    int neutralBars,
    int period,
    int periodEma,
    int periodLinReg,
    int periodTurbo,
    int sideWinderLimit0,
    int sideWinderLimit1,
    int sideWinderWidth)
  {
    return (NinjaTrader.NinjaScript.Indicators.WoodiesCCI) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WoodiesPivots WoodiesPivots(
    HLCCalculationModeWoodie priorDayHlc,
    int width)
  {
    return (NinjaTrader.NinjaScript.Indicators.WoodiesPivots) null;
  }

  public NinjaTrader.NinjaScript.Indicators.WoodiesPivots WoodiesPivots(
    ISeries<double> input,
    HLCCalculationModeWoodie priorDayHlc,
    int width)
  {
    return this.indicator.WoodiesPivots(input, priorDayHlc, width);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WisemanAlligator WisemanAlligator(
    int jawPeriod,
    int teethPeriod,
    int lipsPeriod,
    int jawOffset,
    int teethOffset,
    int lipsOffset)
  {
    return (NinjaTrader.NinjaScript.Indicators.WisemanAlligator) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WisemanAlligator WisemanAlligator(
    ISeries<double> input,
    int jawPeriod,
    int teethPeriod,
    int lipsPeriod,
    int jawOffset,
    int teethOffset,
    int lipsOffset)
  {
    return (NinjaTrader.NinjaScript.Indicators.WisemanAlligator) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WisemanAwesomeOscillator WisemanAwesomeOscillator()
  {
    return (NinjaTrader.NinjaScript.Indicators.WisemanAwesomeOscillator) null;
  }

  public NinjaTrader.NinjaScript.Indicators.WisemanAwesomeOscillator WisemanAwesomeOscillator(
    ISeries<double> input)
  {
    return this.indicator.WisemanAwesomeOscillator(input);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.WisemanFractal WisemanFractal(
    int strength,
    int triangleOffset)
  {
    return (NinjaTrader.NinjaScript.Indicators.WisemanFractal) null;
  }

  public NinjaTrader.NinjaScript.Indicators.WisemanFractal WisemanFractal(
    ISeries<double> input,
    int strength,
    int triangleOffset)
  {
    return this.indicator.WisemanFractal(input, strength, triangleOffset);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowCumulativeDelta OrderFlowCumulativeDelta(
    CumulativeDeltaType deltaType,
    CumulativeDeltaPeriod period,
    int sizeFilter)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowCumulativeDelta) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowCumulativeDelta OrderFlowCumulativeDelta(
    ISeries<double> input,
    CumulativeDeltaType deltaType,
    CumulativeDeltaPeriod period,
    int sizeFilter)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowCumulativeDelta) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowMarketDepthMap OrderFlowMarketDepthMap(
    BaseVolumeRange baseRange,
    int maxRange,
    int minRange,
    OpacityDistribution opacityDistribution,
    int depthMargin,
    bool extendLastKnown,
    bool showBidAskLine)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowMarketDepthMap) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowMarketDepthMap OrderFlowMarketDepthMap(
    ISeries<double> input,
    BaseVolumeRange baseRange,
    int maxRange,
    int minRange,
    OpacityDistribution opacityDistribution,
    int depthMargin,
    bool extendLastKnown,
    bool showBidAskLine)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowMarketDepthMap) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile OrderFlowVolumeProfile(
    MarketProfileType profileType,
    MarketProfilePeriod profilePeriod,
    int sessions,
    TradingHours tradingHoursInstance,
    MarketProfileResolution resolution,
    int valueAreaPercent,
    int initialBalanceMinutes)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile OrderFlowVolumeProfile(
    ISeries<double> input,
    MarketProfileType profileType,
    MarketProfilePeriod profilePeriod,
    int sessions,
    TradingHours tradingHoursInstance,
    MarketProfileResolution resolution,
    int valueAreaPercent,
    int initialBalanceMinutes)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowVWAP OrderFlowVWAP(
    VWAPResolution resolution,
    TradingHours tradingHoursInstance,
    VWAPStandardDeviations numStandardDeviations,
    double sD1Multiplier,
    double sD2Multiplier,
    double sD3Multiplier)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowVWAP) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowVWAP OrderFlowVWAP(
    ISeries<double> input,
    VWAPResolution resolution,
    TradingHours tradingHoursInstance,
    VWAPStandardDeviations numStandardDeviations,
    double sD1Multiplier,
    double sD2Multiplier,
    double sD3Multiplier)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowVWAP) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowTradeDetector OrderFlowTradeDetector(
    TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn,
    int minimumVolumeForMarker,
    int maximumMarkerSize,
    TradeDetectorSizeBase baseMarkerSizeOn,
    bool hoverValues)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowTradeDetector) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.OrderFlowTradeDetector OrderFlowTradeDetector(
    ISeries<double> input,
    TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn,
    int minimumVolumeForMarker,
    int maximumMarkerSize,
    TradeDetectorSizeBase baseMarkerSizeOn,
    bool hoverValues)
  {
    return (NinjaTrader.NinjaScript.Indicators.OrderFlowTradeDetector) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.StdDev StdDev(int period) => (NinjaTrader.NinjaScript.Indicators.StdDev) null;

  public NinjaTrader.NinjaScript.Indicators.StdDev StdDev(ISeries<double> input, int period)
  {
    return this.indicator.StdDev(input, period);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.RSI RSI(int period, int smooth) => (NinjaTrader.NinjaScript.Indicators.RSI) null;

  public NinjaTrader.NinjaScript.Indicators.RSI RSI(ISeries<double> input, int period, int smooth)
  {
    return this.indicator.RSI(input, period, smooth);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.EMA EMA(int period) => (NinjaTrader.NinjaScript.Indicators.EMA) null;

  public NinjaTrader.NinjaScript.Indicators.EMA EMA(ISeries<double> input, int period)
  {
    return this.indicator.EMA(input, period);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public NinjaTrader.NinjaScript.Indicators.SMA SMA(int period) => (NinjaTrader.NinjaScript.Indicators.SMA) null;

  public NinjaTrader.NinjaScript.Indicators.SMA SMA(ISeries<double> input, int period)
  {
    return this.indicator.SMA(input, period);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static Strategy()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
