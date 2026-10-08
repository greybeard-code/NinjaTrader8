// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.Strategies.Golden_Momentum
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Cbi;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.Strategies;

public class Golden_Momentum : Strategy
{
  private const Calculate HiddenCalcMode = (Calculate) 0;
  private const bool HiddenIncludeCommission = true;
  private const bool HiddenUseExitOnClose = true;
  private const int HiddenEntriesPerDirection = 3;
  private const EntryHandling HiddenEntryHandling = (EntryHandling) 1;
  private const bool HiddenTraceOrders = false;
  private const int HiddenBarsRequiredToTrade = 14;
  private const int HiddenExitOnSessionCloseSecs = 44;
  private const bool UseEMATrendFilter_Global = true;
  private const int LongEmaFastLen = 3;
  private const int LongEmaMidLen = 25;
  private const int LongEmaSlowLen = 120;
  private const int ShortEmaFastLen = 6;
  private const int ShortEmaMidLen = 25;
  private const int ShortEmaSlowLen = 153;
  private const bool UseVolumeFilter_Global = true;
  private const int VolSmaLen = 21;
  private const int MSSWindow = 2;
  private Golden_Momentum.EntryLogic EntryLogicMode;
  private Golden_Momentum.MRMode MeanReversionMode;
  private Golden_Momentum.MRPriceSource MRInputPrice;
  private int MRPeriod;
  private int MRStdDevPeriod;
  private double MRZEntry;
  private int RSI2Length;
  private int RSI2Smoothing;
  private int RSI2Buy;
  private int RSI2Sell;
  private bool UseEMATrendFilter_MR;
  private bool UseVolumeFilter_MR;
  private int MRMinBarsBetweenEntries;
  private NinjaTrader.NinjaScript.Indicators.EMA emaFastLong;
  private NinjaTrader.NinjaScript.Indicators.EMA emaMidLong;
  private NinjaTrader.NinjaScript.Indicators.EMA emaSlowLong;
  private NinjaTrader.NinjaScript.Indicators.EMA emaFastShort;
  private NinjaTrader.NinjaScript.Indicators.EMA emaMidShort;
  private NinjaTrader.NinjaScript.Indicators.EMA emaSlowShort;
  private NinjaTrader.NinjaScript.Indicators.SMA volSMA;
  private NinjaTrader.NinjaScript.Indicators.SMA mrSMA;
  private NinjaTrader.NinjaScript.Indicators.EMA mrEMA;
  private NinjaTrader.NinjaScript.Indicators.StdDev mrStd;
  private NinjaTrader.NinjaScript.Indicators.RSI rsi2;
  private ISeries<double> mrInput;
  private double longEntryPrice;
  private double shortEntryPrice;
  private double tp1Price;
  private double tp2Price;
  private bool nyClosedToday;
  private DateTime lastEstDate;
  private int lastMREntryBar;
  private int longTradesToday;
  private int shortTradesToday;
  private MarketPosition lastMarketPosition;
  private const string LongTp1Tag = "GA_GM_Long_TP1";
  private const string LongTp2Tag = "GA_GM_Long_TP2";
  private const string ShortTp1Tag = "GA_GM_Short_TP1";
  private const string ShortTp2Tag = "GA_GM_Short_TP2";
  private bool dailyProfitHaltActive;
  private double dayStartCumProfitUSD;
  private bool longTrailActive;
  private double longTrailHigh;
  private bool shortTrailActive;
  private double shortTrailLow;
  private const string LongTP1EntrySignal = "LongTP1Entry";
  private const string LongTP2EntrySignal = "LongTP2Entry";
  private const string ShortTP1EntrySignal = "ShortTP1Entry";
  private const string ShortTP2EntrySignal = "ShortTP2Entry";
  private int longTp1QtyPlanned;
  private int longTp2QtyPlanned;
  private int shortTp1QtyPlanned;
  private int shortTp2QtyPlanned;
  private double lastLongStopSent;
  private double lastShortStopSent;
  private TimeZoneInfo estTz;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public Golden_Momentum()
  {
  }

  [Display(Name = "Use Kill Switch", Order = 0, GroupName = "01. Risk - Global")]
  [NinjaScriptProperty]
  public bool UseKillSwitch { get; set; }

  [Display(Name = "Max Unrealized Loss (USD)", Order = 1, GroupName = "01. Risk - Global")]
  [Range(0.0, 1000000.0)]
  [NinjaScriptProperty]
  public double MaxUnrealizedLossUSD { get; set; }

  [NinjaScriptProperty]
  [Display(Name = "Long Reduced Target Multiplier", Order = 2, GroupName = "01. Risk - Global")]
  [Range(0.1, 10.0)]
  public double LongReducedTargetMultiplier { get; set; }

  [Display(Name = "Short Reduced Target Multiplier", Order = 3, GroupName = "01. Risk - Global")]
  [NinjaScriptProperty]
  [Range(0.1, 10.0)]
  public double ShortReducedTargetMultiplier { get; set; }

  [NinjaScriptProperty]
  [Display(Name = "Max Long Trades Per Day", Order = 4, GroupName = "01. Risk - Global")]
  [Range(0, 50)]
  public int MaxLongTradesPerDay { get; set; }

  [Range(0, 50)]
  [NinjaScriptProperty]
  [Display(Name = "Max Short Trades Per Day", Order = 5, GroupName = "01. Risk - Global")]
  public int MaxShortTradesPerDay { get; set; }

  [Display(Name = "Use Daily Profit Target (USD)", Order = 6, GroupName = "01. Risk - Global")]
  [NinjaScriptProperty]
  public bool UseDailyProfitTargetUSD { get; set; }

  [Range(1.0, 1000000.0)]
  [Display(Name = "Daily Profit Target (USD)", Order = 7, GroupName = "01. Risk - Global")]
  [NinjaScriptProperty]
  public double DailyProfitTargetUSD { get; set; }

  [Display(Name = "Direction Mode", Order = 8, GroupName = "01. Risk - Global")]
  [NinjaScriptProperty]
  public Golden_Momentum.TradeSide DirectionMode { get; set; }

  [Range(1, 100)]
  [NinjaScriptProperty]
  [Display(Name = "Long Contracts", Order = 0, GroupName = "02. Risk - Long")]
  public int LongContracts { get; set; }

  [Range(1, 100000)]
  [NinjaScriptProperty]
  [Display(Name = "Long Stop Loss (USD)", Order = 1, GroupName = "02. Risk - Long")]
  public double LongStopLossUSD { get; set; }

  [Range(0.1, 20.0)]
  [NinjaScriptProperty]
  [Display(Name = "Long TP1 Multiplier (x Stop)", Order = 2, GroupName = "02. Risk - Long")]
  public double LongTP1Multiplier { get; set; }

  [NinjaScriptProperty]
  [Display(Name = "Long TP2 Multiplier (x Stop)", Order = 3, GroupName = "02. Risk - Long")]
  [Range(0.1, 20.0)]
  public double LongTP2Multiplier { get; set; }

  [NinjaScriptProperty]
  [Display(Name = "Long TP1 Split (%)", Order = 4, GroupName = "02. Risk - Long")]
  [Range(1, 100)]
  public int LongTP1SplitPercent { get; set; }

  [Display(Name = "Short Contracts", Order = 0, GroupName = "03. Risk - Short")]
  [NinjaScriptProperty]
  [Range(1, 100)]
  public int ShortContracts { get; set; }

  [Display(Name = "Short Stop Loss (USD)", Order = 1, GroupName = "03. Risk - Short")]
  [Range(1, 100000)]
  [NinjaScriptProperty]
  public double ShortStopLossUSD { get; set; }

  [Display(Name = "Short TP1 Multiplier (x Stop)", Order = 2, GroupName = "03. Risk - Short")]
  [Range(0.1, 20.0)]
  [NinjaScriptProperty]
  public double ShortTP1Multiplier { get; set; }

  [Display(Name = "Short TP2 Multiplier (x Stop)", Order = 3, GroupName = "03. Risk - Short")]
  [Range(0.05, 20.0)]
  [NinjaScriptProperty]
  public double ShortTP2Multiplier { get; set; }

  [Display(Name = "Short TP1 Split (%)", Order = 4, GroupName = "03. Risk - Short")]
  [Range(1, 100)]
  [NinjaScriptProperty]
  public int ShortTP1SplitPercent { get; set; }

  [Display(Name = "Use Long Trailing Profit", Order = 0, GroupName = "04. Trailing Profit - Long")]
  [NinjaScriptProperty]
  public bool UseLongTrailingProfit { get; set; }

  [Display(Name = "Long Trail Start (% of TP1)", Order = 1, GroupName = "04. Trailing Profit - Long")]
  [Range(1, 100)]
  [NinjaScriptProperty]
  public int LongTrailStartPctOfTP1 { get; set; }

  [Display(Name = "Long Trail Distance (Ticks)", Order = 2, GroupName = "04. Trailing Profit - Long")]
  [Range(1, 2000)]
  [NinjaScriptProperty]
  public int LongTrailDistanceTicks { get; set; }

  [Display(Name = "Use Short Trailing Profit", Order = 0, GroupName = "05. Trailing Profit - Short")]
  [NinjaScriptProperty]
  public bool UseShortTrailingProfit { get; set; }

  [Display(Name = "Short Trail Start (% of TP1)", Order = 1, GroupName = "05. Trailing Profit - Short")]
  [Range(1, 100)]
  [NinjaScriptProperty]
  public int ShortTrailStartPctOfTP1 { get; set; }

  [Display(Name = "Short Trail Distance (Ticks)", Order = 2, GroupName = "05. Trailing Profit - Short")]
  [Range(1, 2000)]
  [NinjaScriptProperty]
  public int ShortTrailDistanceTicks { get; set; }

  [Display(Name = "Enforce NY Close Exit", Order = 0, GroupName = "06. Session - NY Close")]
  [NinjaScriptProperty]
  public bool EnforceNYCloseExit { get; set; }

  [Display(Name = "NY Close Hour (ET)", Order = 1, GroupName = "06. Session - NY Close")]
  [Range(0, 23)]
  [NinjaScriptProperty]
  public int NYCloseHourET { get; set; }

  [Display(Name = "NY Close Minute (ET)", Order = 2, GroupName = "06. Session - NY Close")]
  [Range(0, 59)]
  [NinjaScriptProperty]
  public int NYCloseMinuteET { get; set; }

  [Display(Name = "NY Close Buffer Minutes", Order = 3, GroupName = "06. Session - NY Close")]
  [Range(0, 240 /*0xF0*/)]
  [NinjaScriptProperty]
  public int NYCloseBufferMinutes { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnBarUpdate()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void FlattenAndReset(string reason)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void ResetTrailingState()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void ResetOrderStabilityState()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private double AlignToTick(double price, double tick) => 0.0;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private ISeries<double> SelectMRInputSeries() => (ISeries<double>) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private bool MSSlongSignal(int window) => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private bool MSSshortSignal(int window) => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private bool MR_Long(bool bullishEMA_global, bool volumeOK_global) => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private bool MR_Short(bool bearishEMA_global, bool volumeOK_global) => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void ClearTPLines()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static Golden_Momentum()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  public enum TradeSide
  {
    Both,
    LongOnly,
    ShortOnly,
  }

  private enum MRMode
  {
    Off,
    SMA_ZScore,
    EMA_ZScore,
    RSI2,
  }

  private enum EntryLogic
  {
    BreakoutOnly,
    MeanReversionOnly,
    Hybrid_Either,
    Hybrid_Both,
  }

  private enum MRPriceSource
  {
    Close,
    Open,
    High,
    Low,
    Median,
    Typical,
    Weighted,
  }
}
