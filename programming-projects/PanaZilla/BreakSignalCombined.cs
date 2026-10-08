#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// ============================================================
//  BreakSignalCombined
//  Combines ThunderZilla (RenkoKings) + PANA Kanal (ninZa.co)
//  Optionally fires ATR-trailed exit signals after each entry.
//
//  PANA Kanal Signal_Trade plot values (ninZa.co official docs):
//    2  = Break Up      â† BULLISH BREAK  âœ"
//    3  = Pullback Bullish
//    1  = Uptrend Start
//   -1  = Downtrend Start
//   -2  = Break Down    â† BEARISH BREAK  âœ"
//   -3  = Pullback Bearish
//    0  = No signal
//
//  ThunderZilla Signal_Trade plot values (RenkoKings):
//    3  = Bullish signal (Per Trend)
//    2  = Bullish signal (Per Flat)
//    1  = Bullish signal
//   -1  = Bearish signal
//   -2  = Bearish signal (Per Flat)
//   -3  = Bearish signal (Per Trend)
//
//  DEFAULT COMBINED SIGNALS:
//  LONG  â†' PANA Kanal = 2 (Break Up)    AND ThunderZilla >= 1 (any bullish)
//  SHORT â†' PANA Kanal = -2 (Break Down) AND ThunderZilla <= -1 (any bearish)
//
//  EXIT (when EnableExitSignals = true):
//  A. ATR trailing stop breached         (primary)
//  B. Price crosses smoothed MA          (optional, UseMAExit)
//  C. Opposing color bar after MinBarsInTrade
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class BreakSignalCombined : Indicator
    {
        // ---- Indicator references ----
        private Indicators.RenkoKings.RenkoKings_ThunderZilla thunderZilla;
        private Indicators.ninZaPANAKanal                     panaKanal;

        // ---- MA series for exit logic ----
        private Series<double> trendMA;
        private Series<double> smoothedMA;

        // ---- Exit state machine ----
        private int    tradeDirection = 0;          //  1 = long, -1 = short, 0 = flat
        private double trailLevel     = 0;
        private double highestHigh    = 0;
        private double lowestLow      = double.MaxValue;
        private int    entryBar       = -999;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Fires Long/Short arrows when PANA Kanal Break signal aligns with ThunderZilla. "
                                            + "Optional exit signals via ATR trailing stop, MA cross, or color flip.";
                Name                        = "BreakSignalCombined";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                PaintPriceMarkers           = true;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive    = true;
                ShowTransparentPlotsInDataBox = true;

                // ---- ThunderZilla Parameters ----
                TZ_TrendMAType              = ThunderZillaMAType.SMA;
                TZ_TrendPeriod              = 100;
                TZ_TrendSmoothingEnabled    = false;
                TZ_TrendSmoothingMethod     = ThunderZillaMAType.EMA;
                TZ_TrendSmoothingPeriod     = 10;
                TZ_StopOffsetMultiplier     = 60;
                TZ_SignalQtyPerFlat         = 2;
                TZ_SignalQtyPerTrend        = 999;
                TZ_BullishThreshold         = 3;
                TZ_BearishThreshold         = -3;

                // ---- PANA Kanal Parameters ----
                PK_Period                   = 20;
                PK_Factor                   = 4;
                PK_MiddlePeriod             = 14;
                PK_SignalBreakSplitBars     = 20;
                PK_SignalPullbackFindPeriod = 10;

                // ---- Exit Signal Settings ----
                EnableExitSignals           = true;
                ATR_Period                  = 14;
                ATR_Multiplier              = 2.0;
                UseMAExit                   = true;
                MinBarsInTrade              = 3;

                // ---- Visuals - Entry ----
                LongArrowColor              = Brushes.Cyan;
                ShortArrowColor             = Brushes.Magenta;
                ArrowOffset                 = 3;
                ShowLabel                   = true;
                LongLabelText               = "Break Buy";
                ShortLabelText              = "Break Sell";
                LabelFontSize               = 10;

                // ---- Visuals - Exit ----
                LongExitColor               = Brushes.Orange;
                ShortExitColor              = Brushes.Orange;
                LongExitLabel               = "Exit Long";
                ShortExitLabel              = "Exit Short";
                ShowTrailLine               = true;
            }
            else if (State == State.Configure)
            {
                // Entry signal: 1 = long, -1 = short, 0 = none
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot,  "EntrySignal");

                // Exit signal: 1 = long exit, -1 = short exit, 0 = none
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot,  "ExitSignal");

                // Trail stop line
                AddPlot(new Stroke(Brushes.Gray, 1), PlotStyle.Line, "TrailStop");
            }
            else if (State == State.DataLoaded)
            {
                thunderZilla = RenkoKings_ThunderZilla(
                    TZ_TrendMAType,
                    TZ_TrendPeriod,
                    TZ_TrendSmoothingEnabled,
                    TZ_TrendSmoothingMethod,
                    TZ_TrendSmoothingPeriod,
                    TZ_StopOffsetMultiplier,
                    TZ_SignalQtyPerFlat,
                    TZ_SignalQtyPerTrend
                );

                panaKanal = ninZaPANAKanal(
                    PK_Period,
                    PK_Factor,
                    PK_MiddlePeriod,
                    PK_SignalBreakSplitBars,
                    PK_SignalPullbackFindPeriod
                );

                trendMA    = new Series<double>(this);
                smoothedMA = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(TZ_TrendPeriod, PK_Period) + 20)
                return;

            // Reset all plots
            Values[0][0] = 0;   // EntrySignal
            Values[1][0] = 0;   // ExitSignal
            // TrailStop — transparent brush hides it cleanly without NaN interpolation
            Values[2][0]      = Close[0];
            PlotBrushes[2][0] = Brushes.Transparent;

            // MA for exit smoothing (always calculated, low cost)
            trendMA[0]    = SMA(Close, TZ_TrendPeriod)[0];
            smoothedMA[0] = EMA(trendMA, TZ_TrendSmoothingPeriod)[0];

            double pkSignal = panaKanal.Signal_Trade[0];
            double tzSignal = thunderZilla.Signal_Trade[0];

            // ================================================================
            //  ENTRY LOGIC
            // ================================================================
            bool longFired  = false;
            bool shortFired = false;

            if (pkSignal == 2 && tzSignal >= TZ_BullishThreshold)
            {
                Draw.ArrowUp(this,
                    "Long_" + CurrentBar, false,
                    0, Low[0] - (ArrowOffset * TickSize),
                    LongArrowColor);

                if (ShowLabel)
                    Draw.Text(this,
                        "LongTxt_" + CurrentBar, false,
                        LongLabelText,
                        0, Low[0] - (ArrowOffset * TickSize * 4),
                        0, LongArrowColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[0][0] = 1;   // EntrySignal = long
                longFired    = true;
            }
            else if (pkSignal == -2 && tzSignal <= TZ_BearishThreshold)
            {
                Draw.ArrowDown(this,
                    "Short_" + CurrentBar, false,
                    0, High[0] + (ArrowOffset * TickSize),
                    ShortArrowColor);

                if (ShowLabel)
                    Draw.Text(this,
                        "ShortTxt_" + CurrentBar, false,
                        ShortLabelText,
                        0, High[0] + (ArrowOffset * TickSize * 4),
                        0, ShortArrowColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[0][0] = -1;  // EntrySignal = short
                shortFired   = true;
            }

            // ================================================================
            //  EXIT LOGIC  (only when EnableExitSignals is true)
            // ================================================================
            if (!EnableExitSignals)
                return;

            double atr         = ATR(ATR_Period)[0];
            int    barsInTrade = CurrentBar - entryBar;

            // ---- Detect new entry ----
            if (tradeDirection == 0)
            {
                if (longFired)
                {
                    tradeDirection = 1;
                    entryBar       = CurrentBar;
                    highestHigh    = High[0];
                    trailLevel     = High[0] - (atr * ATR_Multiplier);
                }
                else if (shortFired)
                {
                    tradeDirection = -1;
                    entryBar       = CurrentBar;
                    lowestLow      = Low[0];
                    trailLevel     = Low[0] + (atr * ATR_Multiplier);
                }
            }

            // ---- Update trail and check exit ----
            bool   longExit   = false;
            bool   shortExit  = false;
            string exitReason = "";

            if (tradeDirection == 1)
            {
                highestHigh = Math.Max(highestHigh, High[0]);
                trailLevel  = highestHigh - (atr * ATR_Multiplier);
                // Skip entry bar (barsInTrade == 0) to prevent vertical connecting line from flat period
                if (ShowTrailLine && barsInTrade > 0)
                {
                    Values[2][0]      = trailLevel;
                    PlotBrushes[2][0] = Brushes.Gray;
                }

                if (Close[0] < trailLevel)
                    { longExit = true; exitReason = "Trail"; }
                else if (UseMAExit && Close[0] < smoothedMA[0] && Close[1] >= smoothedMA[1])
                    { longExit = true; exitReason = "MA Cross"; }
                else if (barsInTrade >= MinBarsInTrade && Close[0] < Open[0] && Close[1] > Open[1])
                    { longExit = true; exitReason = "Flip"; }
            }
            else if (tradeDirection == -1)
            {
                lowestLow  = Math.Min(lowestLow, Low[0]);
                trailLevel = lowestLow + (atr * ATR_Multiplier);
                // Skip entry bar (barsInTrade == 0) to prevent vertical connecting line from flat period
                if (ShowTrailLine && barsInTrade > 0)
                {
                    Values[2][0]      = trailLevel;
                    PlotBrushes[2][0] = Brushes.Gray;
                }

                if (Close[0] > trailLevel)
                    { shortExit = true; exitReason = "Trail"; }
                else if (UseMAExit && Close[0] > smoothedMA[0] && Close[1] <= smoothedMA[1])
                    { shortExit = true; exitReason = "MA Cross"; }
                else if (barsInTrade >= MinBarsInTrade && Close[0] > Open[0] && Close[1] < Open[1])
                    { shortExit = true; exitReason = "Flip"; }
            }

            // ---- Fire exit signals ----
            if (longExit)
            {
                Draw.ArrowDown(this,
                    "LongExit_" + CurrentBar, false,
                    0, High[0] + (ArrowOffset * TickSize),
                    LongExitColor);

                if (ShowLabel)
                    Draw.Text(this,
                        "LongExitTxt_" + CurrentBar, false,
                        LongExitLabel + " (" + exitReason + ")",
                        0, High[0] + (ArrowOffset * TickSize * 4),
                        0, LongExitColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[1][0]   = 1;     // ExitSignal = long exit
                tradeDirection = 0;
                highestHigh    = 0;
            }

            if (shortExit)
            {
                Draw.ArrowUp(this,
                    "ShortExit_" + CurrentBar, false,
                    0, Low[0] - (ArrowOffset * TickSize),
                    ShortExitColor);

                if (ShowLabel)
                    Draw.Text(this,
                        "ShortExitTxt_" + CurrentBar, false,
                        ShortExitLabel + " (" + exitReason + ")",
                        0, Low[0] - (ArrowOffset * TickSize * 4),
                        0, ShortExitColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[1][0]   = -1;    // ExitSignal = short exit
                tradeDirection = 0;
                lowestLow      = double.MaxValue;
            }
        }

        #region Properties

        // =====================================================================
        // GROUP 1: ThunderZilla Parameters
        // =====================================================================

        [NinjaScriptProperty]
        [Display(Name = "Trend: MA Type", GroupName = "1. ThunderZilla", Order = 1)]
        public ThunderZillaMAType TZ_TrendMAType { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Period", GroupName = "1. ThunderZilla", Order = 2)]
        public int TZ_TrendPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Enabled", GroupName = "1. ThunderZilla", Order = 3)]
        public bool TZ_TrendSmoothingEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Method", GroupName = "1. ThunderZilla", Order = 4)]
        public ThunderZillaMAType TZ_TrendSmoothingMethod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Smoothing Period", GroupName = "1. ThunderZilla", Order = 5)]
        public int TZ_TrendSmoothingPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Stop: Offset Multiplier (Ticks)", GroupName = "1. ThunderZilla", Order = 6)]
        public double TZ_StopOffsetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Flat", GroupName = "1. ThunderZilla", Order = 7)]
        public int TZ_SignalQtyPerFlat { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Trend", GroupName = "1. ThunderZilla", Order = 8)]
        public int TZ_SignalQtyPerTrend { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Threshold (>=)", GroupName = "1. ThunderZilla", Order = 9,
            Description = "TZ Signal_Trade must be >= this to count as bullish. 1 = any bullish, 3 = strongest only.")]
        public double TZ_BullishThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Threshold (<=)", GroupName = "1. ThunderZilla", Order = 10,
            Description = "TZ Signal_Trade must be <= this to count as bearish. -1 = any bearish, -3 = strongest only.")]
        public double TZ_BearishThreshold { get; set; }

        // =====================================================================
        // GROUP 2: PANA Kanal Parameters
        // =====================================================================

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Period", GroupName = "2. PANA Kanal", Order = 1)]
        public int PK_Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Factor", GroupName = "2. PANA Kanal", Order = 2)]
        public double PK_Factor { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Middle Period", GroupName = "2. PANA Kanal", Order = 3)]
        public int PK_MiddlePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Break Split (Bars)", GroupName = "2. PANA Kanal", Order = 4)]
        public int PK_SignalBreakSplitBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Pullback Finding Period", GroupName = "2. PANA Kanal", Order = 5)]
        public int PK_SignalPullbackFindPeriod { get; set; }

        // =====================================================================
        // GROUP 3: Exit Signal Settings
        // =====================================================================

        [NinjaScriptProperty]
        [Display(Name = "Enable Exit Signals", GroupName = "3. Exit Signals", Order = 1,
            Description = "When enabled, fires exit arrows after each entry using ATR trailing stop, MA cross, or color flip.")]
        public bool EnableExitSignals { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period", GroupName = "3. Exit Signals", Order = 2)]
        public int ATR_Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "ATR Multiplier", GroupName = "3. Exit Signals", Order = 3,
            Description = "Trail distance = ATR x this value. Default = 2.0.")]
        public double ATR_Multiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use MA Cross Exit", GroupName = "3. Exit Signals", Order = 4,
            Description = "Exit when price crosses back through the smoothed trend MA.")]
        public bool UseMAExit { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars In Trade", GroupName = "3. Exit Signals", Order = 5,
            Description = "Minimum bars before a color-flip exit is allowed. Default = 3.")]
        public int MinBarsInTrade { get; set; }

        // =====================================================================
        // GROUP 4: Visuals - Entry
        // =====================================================================

        [XmlIgnore]
        [Display(Name = "Long Arrow Color", GroupName = "4. Visuals - Entry", Order = 1)]
        public Brush LongArrowColor { get; set; }

        [Browsable(false)]
        public string LongArrowColorSerializable
        {
            get { return Serialize.BrushToString(LongArrowColor); }
            set { LongArrowColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Arrow Color", GroupName = "4. Visuals - Entry", Order = 2)]
        public Brush ShortArrowColor { get; set; }

        [Browsable(false)]
        public string ShortArrowColorSerializable
        {
            get { return Serialize.BrushToString(ShortArrowColor); }
            set { ShortArrowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Arrow Offset (ticks)", GroupName = "4. Visuals - Entry", Order = 3)]
        public int ArrowOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Label", GroupName = "4. Visuals - Entry", Order = 4)]
        public bool ShowLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Long Label Text", GroupName = "4. Visuals - Entry", Order = 5)]
        public string LongLabelText { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Label Text", GroupName = "4. Visuals - Entry", Order = 6)]
        public string ShortLabelText { get; set; }

        [NinjaScriptProperty]
        [Range(6, 30)]
        [Display(Name = "Label Font Size", GroupName = "4. Visuals - Entry", Order = 7)]
        public int LabelFontSize { get; set; }

        // =====================================================================
        // GROUP 5: Visuals - Exit
        // =====================================================================

        [XmlIgnore]
        [Display(Name = "Long Exit Color", GroupName = "5. Visuals - Exit", Order = 1)]
        public Brush LongExitColor { get; set; }

        [Browsable(false)]
        public string LongExitColorSerializable
        {
            get { return Serialize.BrushToString(LongExitColor); }
            set { LongExitColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Exit Color", GroupName = "5. Visuals - Exit", Order = 2)]
        public Brush ShortExitColor { get; set; }

        [Browsable(false)]
        public string ShortExitColorSerializable
        {
            get { return Serialize.BrushToString(ShortExitColor); }
            set { ShortExitColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Long Exit Label", GroupName = "5. Visuals - Exit", Order = 3)]
        public string LongExitLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Exit Label", GroupName = "5. Visuals - Exit", Order = 4)]
        public string ShortExitLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Trail Line", GroupName = "5. Visuals - Exit", Order = 5,
            Description = "Plots the ATR trailing stop level on the chart while in a trade.")]
        public bool ShowTrailLine { get; set; }

        // =====================================================================
        // Output plots (for use by strategies / Market Analyzer)
        // =====================================================================

        [Browsable(false)]
        [XmlIgnore]
        [Display(Name = "Entry Signal", Description = "1 = long entry, -1 = short entry, 0 = none")]
        public Series<double> EntrySignal { get { return Values[0]; } }

        [Browsable(false)]
        [XmlIgnore]
        [Display(Name = "Exit Signal", Description = "1 = long exit, -1 = short exit, 0 = none")]
        public Series<double> ExitSignal  { get { return Values[1]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TrailStop   { get { return Values[2]; } }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BreakSignalCombined[] cacheBreakSignalCombined;
		public BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			return BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, enableExitSignals, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize, longExitLabel, shortExitLabel, showTrailLine);
		}

		public BreakSignalCombined BreakSignalCombined(ISeries<double> input, ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			if (cacheBreakSignalCombined != null)
				for (int idx = 0; idx < cacheBreakSignalCombined.Length; idx++)
					if (cacheBreakSignalCombined[idx] != null && cacheBreakSignalCombined[idx].TZ_TrendMAType == tZ_TrendMAType && cacheBreakSignalCombined[idx].TZ_TrendPeriod == tZ_TrendPeriod && cacheBreakSignalCombined[idx].TZ_TrendSmoothingEnabled == tZ_TrendSmoothingEnabled && cacheBreakSignalCombined[idx].TZ_TrendSmoothingMethod == tZ_TrendSmoothingMethod && cacheBreakSignalCombined[idx].TZ_TrendSmoothingPeriod == tZ_TrendSmoothingPeriod && cacheBreakSignalCombined[idx].TZ_StopOffsetMultiplier == tZ_StopOffsetMultiplier && cacheBreakSignalCombined[idx].TZ_SignalQtyPerFlat == tZ_SignalQtyPerFlat && cacheBreakSignalCombined[idx].TZ_SignalQtyPerTrend == tZ_SignalQtyPerTrend && cacheBreakSignalCombined[idx].TZ_BullishThreshold == tZ_BullishThreshold && cacheBreakSignalCombined[idx].TZ_BearishThreshold == tZ_BearishThreshold && cacheBreakSignalCombined[idx].PK_Period == pK_Period && cacheBreakSignalCombined[idx].PK_Factor == pK_Factor && cacheBreakSignalCombined[idx].PK_MiddlePeriod == pK_MiddlePeriod && cacheBreakSignalCombined[idx].PK_SignalBreakSplitBars == pK_SignalBreakSplitBars && cacheBreakSignalCombined[idx].PK_SignalPullbackFindPeriod == pK_SignalPullbackFindPeriod && cacheBreakSignalCombined[idx].EnableExitSignals == enableExitSignals && cacheBreakSignalCombined[idx].ATR_Period == aTR_Period && cacheBreakSignalCombined[idx].ATR_Multiplier == aTR_Multiplier && cacheBreakSignalCombined[idx].UseMAExit == useMAExit && cacheBreakSignalCombined[idx].MinBarsInTrade == minBarsInTrade && cacheBreakSignalCombined[idx].ArrowOffset == arrowOffset && cacheBreakSignalCombined[idx].ShowLabel == showLabel && cacheBreakSignalCombined[idx].LongLabelText == longLabelText && cacheBreakSignalCombined[idx].ShortLabelText == shortLabelText && cacheBreakSignalCombined[idx].LabelFontSize == labelFontSize && cacheBreakSignalCombined[idx].LongExitLabel == longExitLabel && cacheBreakSignalCombined[idx].ShortExitLabel == shortExitLabel && cacheBreakSignalCombined[idx].ShowTrailLine == showTrailLine && cacheBreakSignalCombined[idx].EqualsInput(input))
						return cacheBreakSignalCombined[idx];
			return CacheIndicator<BreakSignalCombined>(new BreakSignalCombined(){ TZ_TrendMAType = tZ_TrendMAType, TZ_TrendPeriod = tZ_TrendPeriod, TZ_TrendSmoothingEnabled = tZ_TrendSmoothingEnabled, TZ_TrendSmoothingMethod = tZ_TrendSmoothingMethod, TZ_TrendSmoothingPeriod = tZ_TrendSmoothingPeriod, TZ_StopOffsetMultiplier = tZ_StopOffsetMultiplier, TZ_SignalQtyPerFlat = tZ_SignalQtyPerFlat, TZ_SignalQtyPerTrend = tZ_SignalQtyPerTrend, TZ_BullishThreshold = tZ_BullishThreshold, TZ_BearishThreshold = tZ_BearishThreshold, PK_Period = pK_Period, PK_Factor = pK_Factor, PK_MiddlePeriod = pK_MiddlePeriod, PK_SignalBreakSplitBars = pK_SignalBreakSplitBars, PK_SignalPullbackFindPeriod = pK_SignalPullbackFindPeriod, EnableExitSignals = enableExitSignals, ATR_Period = aTR_Period, ATR_Multiplier = aTR_Multiplier, UseMAExit = useMAExit, MinBarsInTrade = minBarsInTrade, ArrowOffset = arrowOffset, ShowLabel = showLabel, LongLabelText = longLabelText, ShortLabelText = shortLabelText, LabelFontSize = labelFontSize, LongExitLabel = longExitLabel, ShortExitLabel = shortExitLabel, ShowTrailLine = showTrailLine }, input, ref cacheBreakSignalCombined);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			return indicator.BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, enableExitSignals, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize, longExitLabel, shortExitLabel, showTrailLine);
		}

		public Indicators.BreakSignalCombined BreakSignalCombined(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			return indicator.BreakSignalCombined(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, enableExitSignals, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize, longExitLabel, shortExitLabel, showTrailLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			return indicator.BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, enableExitSignals, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize, longExitLabel, shortExitLabel, showTrailLine);
		}

		public Indicators.BreakSignalCombined BreakSignalCombined(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, bool enableExitSignals, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize, string longExitLabel, string shortExitLabel, bool showTrailLine)
		{
			return indicator.BreakSignalCombined(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, enableExitSignals, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize, longExitLabel, shortExitLabel, showTrailLine);
		}
	}
}

#endregion
