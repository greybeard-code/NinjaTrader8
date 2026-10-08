#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//  gbUltimateAIPro v1.0.0
//  ---------------------------------------------------------------------------
//  GreyBeard rebuild of UltimateAIProV3. Requires gbUltimateSignalsEngines.cs (GbUsMaMode, GbUsMa)
//  and gbUltimateAIProEngines.cs (GbUaiZigZagHighLow, GbUaiPrice, GbUaiHtfType).
//
//  Signal mathematics preserved. Defects from UltimateAIProV3_Code_Review.md fixed:
//
//    [A1] BUY / SELL plots were registered, publicly exposed and NEVER ASSIGNED — always NaN — and
//         their four alert settings did nothing. Now implemented, using the same condition
//         UltimateSignals uses: a ZigZag flip while the 3-EMA trend state is neutral. This is the
//         subset of the dot signal that fires out of a non-trending state.
//    [A2] `isConf` computed every bar, never read. Removed.
//    [A3] `trIsBind` gated two loops and was never assigned. Removed.
//    [B1] The short arrow used pmBuffer_long_Offset and was gated by pmBuffer_long_Enable — a
//         copy-paste bug that made "short Offset" a no-op for the arrow. Each side now uses its own.
//    [B2] Small buy/sell line colours were inverted relative to the large pair (small buy = Red,
//         small sell = LimeGreen). Straightened out.
//    [B3] Four broken-line strokes and both WithDot strokes all defaulted to Magenta, so no
//         colour-keyed consumer could separate the sides. Every pair now defaults to distinct
//         colours, with a startup warning if any pair is set equal.
//    [B4] `CurrentBar - 10` was compared with `> 0` in some guards and `!= 0` in others, so the
//         reversal-line blocks ran during early bars while the dot blocks did not. One named
//         constant now, one comparison.
//    [B5] botLine/topLine were written one bar behind the value they came from and never reset on
//         the false branch. Same-index write, explicit reset.
//    [B6] MovingAverage's default branch used `Input` rather than its `input` argument.
//    [B7] Colorbars' third ternary branch was unreachable.
//    [B8] `num6 <= 1` where num6 is 0 or 1 — a tautology.
//    [C1] MRO scanned the entire chart every tick. Bar-boundary gated and depth-capped.
//    [C2] The double rewrite loop ran every tick over an unbounded span. Same treatment.
//    [C3] Update() resolved EMA/SMA/SUM/ATR indicator instances on every call inside that loop, and
//         read ATR(atrlength)[c] three times in one expression. All hoisted to State.DataLoaded.
//    [C4] glList_Tags was a List<string> with Contains() — O(n) and unbounded. Now a HashSet.
//    [C5] The HTF loop called GetBar per HTF bar, and rebuilt a 36-argument factory call (plus its
//         cache comparison) every HTF bar to fetch a SECOND instance of the indicator running on
//         BarsArray[1]. The loop is now bounded by Max Zone Levels and the day cutoff, and the
//         child instance is gone entirely — the zone pipeline runs in-instance against
//         BarsArray[1] (see ComputeHtfBar). Besides being cheaper, that removes a hard
//         NinjaScript constraint: NT8 only generates the wrapper region for types it has already
//         registered, so a self-referencing indicator cannot compile its own first build (CS1955),
//         and a hand-written region is duplicated as soon as NT8 does start generating one.
//    [D1] IsSuspendedWhileInactive was true, so the indicator stopped calculating when it was not
//         the active chart object — a correctness hazard for anything consuming it. Now false.
//    [D2] Every signal input was private and hard-coded. All exposed.
//    [D3] MACDLength assigned, never used — there is no MACD signal line. Removed.
//    [D4] Print on every state transition and every new day, plus Logger writing to both
//         NinjaScript.Log and Print from every alert path. Removed; alerts use NT8's Alert().
//    [D5] Random-generated instance id used only for log lines. Removed.
//    [B9] WiCurrentBar was always 0 — see gbUltimateAIProEngines.cs.
//
//  EMA STACK: V3 used EMA(8)/14/21; UltimateSignals and the public ToS study use 9/14/21. Exposed,
//  defaulting to 9. Set Superfast to 8 to reproduce V3 exactly.
//
//  CAPTURABILITY: buy and sell are emitted with distinct identity three ways — 1/0 trigger plots,
//  different brushes per side, and stable per-side draw tags GBUAI_BUY_* / GBUAI_SELL_* etc.
//
//  Plot indices 0-13 match UltimateAIProV3 exactly so existing chart templates map across; the
//  trigger plots are appended at 14-18.
//  ---------------------------------------------------------------------------

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public class gbUltimateAIPro : Indicator
	{
		#region Fields
		private ISeries<double> emaSuperfast, emaFast, emaSlow;
		private ISeries<double> macdFastMa, macdSlowMa;
		private Stochastics	stoch;
		private ISeries<double> atrSeries;
		private ISeries<double> zigPriceH, zigPriceL;
		private GbUaiZigZagHighLow zigZag;

		private Series<double>	macd;
		private Series<bool>	macdUp, macdDown;
		private Series<bool>	buyCond, sellCond, buyState, sellState;
		private Series<double>	eiSave, eiLow, eiHigh;
		private Series<int>		zigDir, zigSignal;
		private Series<double>	revLineTop, revLineBot;

		// Higher-timeframe pipeline. UltimateAIProV3 spawned a SECOND instance of itself on
		// BarsArray[1] via the NinjaScript-generated factory. That is unusable here: NinjaTrader
		// only generates the wrapper region for types it has already registered, so a brand-new
		// self-referencing indicator can never compile its own first build (CS1955), and a
		// hand-written region gets duplicated the moment NT8 does start generating. Running the
		// zone pipeline in-instance against BarsArray[1] removes the dependency entirely — and
		// costs one fewer indicator instance than the vendor's design.
		private GbUaiZigZagHighLow	htfZigZag;
		private ISeries<double>		htfPriceH, htfPriceL, htfEma1, htfEma2, htfEma3;
		private Series<double>		htfEiSave, htfEiLow, htfEiHigh, htfRevTop, htfRevBot;
		private Series<int>			htfDir, htfSignal, htfColor;
		private Series<bool>		htfBuyCond, htfSellCond, htfBuyState, htfSellState;

		private readonly HashSet<string> liveTags = new HashSet<string>();

		private int warmupBars;
		private int minMacdBars;

		// Trailing line levels. The vendor's four LineLevel classes reduce to two levels per side:
		// the large (signal-driven) and small (upSignal-driven) trails.
		private double buyTrailLevel, sellTrailLevel;
		private double buyTrailLevelSmall, sellTrailLevelSmall;

		#endregion

		#region Plots — indices 0-13 match UltimateAIProV3
		[Browsable(false)] [XmlIgnore] public Series<double> upSignal		=> Values[0];
		[Browsable(false)] [XmlIgnore] public Series<double> dnSignal		=> Values[1];
		[Browsable(false)] [XmlIgnore] public Series<double> Colorbars		=> Values[2];
		[Browsable(false)] [XmlIgnore] public Series<double> EnhancedLines	=> Values[3];
		[Browsable(false)] [XmlIgnore] public Series<double> longS			=> Values[4];
		[Browsable(false)] [XmlIgnore] public Series<double> shortS			=> Values[5];
		[Browsable(false)] [XmlIgnore] public Series<double> botLine		=> Values[6];
		[Browsable(false)] [XmlIgnore] public Series<double> topLine		=> Values[7];
		[Browsable(false)] [XmlIgnore] public Series<double> BUY			=> Values[8];
		[Browsable(false)] [XmlIgnore] public Series<double> SELL			=> Values[9];
		[Browsable(false)] [XmlIgnore] public Series<double> DotLong		=> Values[10];
		[Browsable(false)] [XmlIgnore] public Series<double> DotShort		=> Values[11];
		[Browsable(false)] [XmlIgnore] public Series<double> botLineHtf		=> Values[12];
		[Browsable(false)] [XmlIgnore] public Series<double> topLineHtf		=> Values[13];

		// Appended: always-numeric triggers for condition builders that cannot express IsNaN.
		[Browsable(false)] [XmlIgnore] public Series<double> BuyTrigger		=> Values[14];
		[Browsable(false)] [XmlIgnore] public Series<double> SellTrigger	=> Values[15];
		[Browsable(false)] [XmlIgnore] public Series<double> LongTrigger	=> Values[16];
		[Browsable(false)] [XmlIgnore] public Series<double> ShortTrigger	=> Values[17];
		[Browsable(false)] [XmlIgnore] public Series<double> TrendState		=> Values[18];
		#endregion

		#region Developer
		[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string GbVersion => "1.0.0";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		#region Signal parameters  [D2]
		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Superfast EMA", Description = "V3 hard-coded 8; the ToS source uses 9. Set to 8 to reproduce V3 exactly.", Order = 0, GroupName = "1. Trend")]
		public int SuperfastLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Fast EMA", Order = 1, GroupName = "1. Trend")]
		public int FastLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Slow EMA", Order = 2, GroupName = "1. Trend")]
		public int SlowLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "MACD Fast", Order = 0, GroupName = "2. MACD Tier")]
		public int MacdFastLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "MACD Slow", Order = 1, GroupName = "2. MACD Tier")]
		public int MacdSlowLength { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "MACD Average Type", Order = 2, GroupName = "2. MACD Tier")]
		public GbUsMaMode MacdAverageType { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Trend Length", Order = 3, GroupName = "2. MACD Tier")]
		public int TrendLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Sequential Length", Order = 4, GroupName = "2. MACD Tier")]
		public int SequentialLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "K Period", Order = 0, GroupName = "3. Stochastic")]
		public int KPeriod { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "D Period", Order = 1, GroupName = "3. Stochastic")]
		public int DPeriod { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Smooth", Order = 2, GroupName = "3. Stochastic")]
		public int Smooth { get; set; }

		[NinjaScriptProperty] [Range(0.0, 100.0)]
		[Display(Name = "Overbought", Order = 3, GroupName = "3. Stochastic")]
		public double Overbought { get; set; }

		[NinjaScriptProperty] [Range(0.0, 100.0)]
		[Display(Name = "Oversold", Order = 4, GroupName = "3. Stochastic")]
		public double Oversold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "ZigZag Price High", Order = 0, GroupName = "4. ZigZag")]
		public GbUaiPrice ZigZagPriceH { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "ZigZag Price Low", Order = 1, GroupName = "4. ZigZag")]
		public GbUaiPrice ZigZagPriceL { get; set; }

		[NinjaScriptProperty] [Range(0.0, 100.0)]
		[Display(Name = "Percentage Reversal", Order = 2, GroupName = "4. ZigZag")]
		public double PercentageReversal { get; set; }

		[NinjaScriptProperty] [Range(0.0, double.MaxValue)]
		[Display(Name = "Absolute Reversal", Order = 3, GroupName = "4. ZigZag")]
		public double AbsoluteReversal { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Tick Reversal", Order = 4, GroupName = "4. ZigZag")]
		public int TickReversal { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "ATR Length", Order = 5, GroupName = "4. ZigZag")]
		public int AtrLength { get; set; }

		[NinjaScriptProperty] [Range(0.0, double.MaxValue)]
		[Display(Name = "ATR Reversal", Order = 6, GroupName = "4. ZigZag")]
		public double AtrReversal { get; set; }
		#endregion

		#region Ultimate Zones (HTF)
		[NinjaScriptProperty]
		[Display(Name = "Enable Ultimate Zones", Order = 0, GroupName = "5. Ultimate Zones")]
		public bool HtfEnable { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Zone Type", Order = 1, GroupName = "5. Ultimate Zones")]
		public GbUaiHtfType HtfType { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Zone Period", Order = 2, GroupName = "5. Ultimate Zones")]
		public int HtfPeriod { get; set; }

		[NinjaScriptProperty] [Range(1, 30)]
		[Display(Name = "Zone Days To Load", Order = 3, GroupName = "5. Ultimate Zones")]
		public int HtfDaysToLoad { get; set; }

		[NinjaScriptProperty] [Range(1, 20)]
		[Display(Name = "Max Zone Levels", Description = "Stop scanning once this many distinct top and bottom levels have been found. The vendor hard-coded 3.", Order = 4, GroupName = "5. Ultimate Zones")]
		public int HtfMaxLevels { get; set; }

		[Display(Name = "Zone Up Stroke", Order = 10, GroupName = "5. Ultimate Zones")]
		public Stroke HtfUpStroke { get; set; }

		[Display(Name = "Zone Down Stroke", Order = 11, GroupName = "5. Ultimate Zones")]
		public Stroke HtfDnStroke { get; set; }
		#endregion

		#region Buffers — enables, offsets, strokes
		[Display(Name = "Enable upSignal", Order = 0, GroupName = "6. Buffers")]
		public bool UpSignalEnable { get; set; }

		[Display(Name = "upSignal Offset (ticks)", Order = 1, GroupName = "6. Buffers")]
		public int UpSignalOffset { get; set; }

		[Display(Name = "Enable dnSignal", Order = 2, GroupName = "6. Buffers")]
		public bool DnSignalEnable { get; set; }

		[Display(Name = "dnSignal Offset (ticks)", Order = 3, GroupName = "6. Buffers")]
		public int DnSignalOffset { get; set; }

		[Display(Name = "Enable long", Order = 4, GroupName = "6. Buffers")]
		public bool LongEnable { get; set; }

		[Display(Name = "long Offset (ticks)", Order = 5, GroupName = "6. Buffers")]
		public int LongOffset { get; set; }

		[Display(Name = "Enable short", Order = 6, GroupName = "6. Buffers")]
		public bool ShortEnable { get; set; }

		// [B1] The vendor read LongOffset here. Each side now uses its own.
		[Display(Name = "short Offset (ticks)", Order = 7, GroupName = "6. Buffers")]
		public int ShortOffset { get; set; }

		[Display(Name = "Enable longDot", Order = 8, GroupName = "6. Buffers")]
		public bool LongDotEnable { get; set; }

		[Display(Name = "Enable shortDot", Order = 9, GroupName = "6. Buffers")]
		public bool ShortDotEnable { get; set; }

		[Display(Name = "Enable botLine", Order = 10, GroupName = "6. Buffers")]
		public bool BotLineEnable { get; set; }

		[Display(Name = "Enable topLine", Order = 11, GroupName = "6. Buffers")]
		public bool TopLineEnable { get; set; }

		// [B3] Distinct per side — all six of these were Magenta in the vendor build.
		[Display(Name = "Long With Dot Stroke", Order = 20, GroupName = "6. Buffers")]
		public Stroke LongWithDotStroke { get; set; }

		[Display(Name = "Short With Dot Stroke", Order = 21, GroupName = "6. Buffers")]
		public Stroke ShortWithDotStroke { get; set; }
		#endregion

		#region Arrow line settings
		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Buy Line Buffer (ticks)", Order = 0, GroupName = "7. Large Arrow")]
		public int HighBufferTicks { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Sell Line Buffer (ticks)", Order = 1, GroupName = "7. Large Arrow")]
		public int LowBufferTicks { get; set; }

		[Display(Name = "Buy Line Stroke", Order = 2, GroupName = "7. Large Arrow")]
		public Stroke BuyTrailStroke { get; set; }

		[Display(Name = "Buy Broken Line Stroke", Order = 3, GroupName = "7. Large Arrow")]
		public Stroke BuyBrokenStroke { get; set; }

		[Display(Name = "Sell Line Stroke", Order = 4, GroupName = "7. Large Arrow")]
		public Stroke SellTrailStroke { get; set; }

		[Display(Name = "Sell Broken Line Stroke", Order = 5, GroupName = "7. Large Arrow")]
		public Stroke SellBrokenStroke { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Buy Line Buffer (ticks)", Order = 0, GroupName = "8. Small Arrow")]
		public int HighBufferTicksSmall { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Sell Line Buffer (ticks)", Order = 1, GroupName = "8. Small Arrow")]
		public int LowBufferTicksSmall { get; set; }

		// [B2] The vendor had these two inverted relative to the large pair.
		[Display(Name = "Buy Line Stroke", Order = 2, GroupName = "8. Small Arrow")]
		public Stroke BuyTrailStrokeSmall { get; set; }

		[Display(Name = "Buy Broken Line Stroke", Order = 3, GroupName = "8. Small Arrow")]
		public Stroke BuyBrokenStrokeSmall { get; set; }

		[Display(Name = "Sell Line Stroke", Order = 4, GroupName = "8. Small Arrow")]
		public Stroke SellTrailStrokeSmall { get; set; }

		[Display(Name = "Sell Broken Line Stroke", Order = 5, GroupName = "8. Small Arrow")]
		public Stroke SellBrokenStrokeSmall { get; set; }
		#endregion

		#region Signal output / performance / alerts
		[NinjaScriptProperty]
		[Display(Name = "Emit Draw Objects", Description = "Draw BUY/SELL/long/short arrows as NinjaTrader drawing objects with stable per-side tags. Required for tag-based capture by PredatorX or Infinity Algo Engine.", Order = 0, GroupName = "9. Signal Output")]
		public bool EmitDrawObjects { get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Marker Brush", Description = "MUST differ from the sell brush.", Order = 1, GroupName = "9. Signal Output")]
		public Brush BuyMarkerBrush { get; set; }

		[Browsable(false)]
		public string BuyMarkerBrushSerialize
		{
			get { return Serialize.BrushToString(BuyMarkerBrush); }
			set { BuyMarkerBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Marker Brush", Description = "MUST differ from the buy brush.", Order = 2, GroupName = "9. Signal Output")]
		public Brush SellMarkerBrush { get; set; }

		[Browsable(false)]
		public string SellMarkerBrushSerialize
		{
			get { return Serialize.BrushToString(SellMarkerBrush); }
			set { SellMarkerBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty] [Range(1, 5000)]
		[Display(Name = "Max Rewrite Bars", Description = "Cap on how far back a ZigZag revision may force recalculation. The vendor build was unbounded and re-ran the whole span every tick.", Order = 0, GroupName = "10. Performance")]
		public int MaxRewriteBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Alerts", Order = 0, GroupName = "11. Alerts")]
		public bool AlertsEnable { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Alert Rearm Seconds", Order = 1, GroupName = "11. Alerts")]
		public int AlertRearmSeconds { get; set; }

		[Display(Name = "Alert Sound File", Order = 2, GroupName = "11. Alerts")]
		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		public string AlertSoundFile { get; set; }
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "gbUltimateAIPro";
				Description						= @"GreyBeard rebuild of UltimateAIProV3: MACD/Stochastic reversal tier, ZigZag pivot dots, breakout-confirmed large arrows, BUY/SELL reversal signals (implemented — the vendor's were dead), and higher-timeframe Ultimate Zones.";
				IsOverlay						= true;
				IsSuspendedWhileInactive		= false;		// [D1] vendor had true
				ShowTransparentPlotsInDataBox	= true;
				// Calculate left at the NT8 default so OnBarClose is selectable.

				// Plots 0-13 in UltimateAIProV3 order.
				AddPlot(new Stroke(Brushes.LimeGreen,   DashStyleHelper.Solid, 3f), PlotStyle.TriangleUp,   "upSignal");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 3f), PlotStyle.Dot,          "dnSignal");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.TriangleDown, "Colorbars");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line,         "EnhancedLines");
				AddPlot(new Stroke(Brushes.LimeGreen,   DashStyleHelper.Solid, 8f), PlotStyle.TriangleUp,   "long");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 8f), PlotStyle.Dot,          "short");
				AddPlot(new Stroke(Brushes.LightGreen,  DashStyleHelper.Solid, 1f), PlotStyle.Line,         "botLine");
				AddPlot(new Stroke(Brushes.LightPink,   DashStyleHelper.Solid, 1f), PlotStyle.Line,         "topLine");
				// [A1] Were Transparent and never written. Now real signals with distinct colours.
				AddPlot(new Stroke(Brushes.Lime,        DashStyleHelper.Solid, 8f), PlotStyle.TriangleUp,   "BUY");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 8f), PlotStyle.Dot,          "SELL");
				AddPlot(new Stroke(Brushes.LimeGreen,   DashStyleHelper.Solid, 3f), PlotStyle.Hash,         "long Dot");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 3f), PlotStyle.Hash,         "short Dot");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Hash,         "botLine HTF");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Hash,         "topLine HTF");
				// Appended.
				AddPlot(Brushes.Transparent, "BuyTrigger");
				AddPlot(Brushes.Transparent, "SellTrigger");
				AddPlot(Brushes.Transparent, "LongTrigger");
				AddPlot(Brushes.Transparent, "ShortTrigger");
				AddPlot(Brushes.Transparent, "TrendState");

				SuperfastLength		= 9;			// V3 used 8 — see header
				FastLength			= 14;
				SlowLength			= 21;

				MacdFastLength		= 5;
				MacdSlowLength		= 26;
				MacdAverageType		= GbUsMaMode.EMA;
				TrendLength			= 5;
				SequentialLength	= 3;

				KPeriod				= 10;
				DPeriod				= 10;
				Smooth				= 3;
				Overbought			= 80.0;
				Oversold			= 20.0;

				ZigZagPriceH		= GbUaiPrice.High;
				ZigZagPriceL		= GbUaiPrice.Low;
				PercentageReversal	= 0.01;
				AbsoluteReversal	= 0.05;
				TickReversal		= 0;
				AtrLength			= 5;
				AtrReversal			= 2.0;

				HtfEnable			= true;
				HtfType				= GbUaiHtfType.Minute;
				HtfPeriod			= 15;
				HtfDaysToLoad		= 1;
				HtfMaxLevels		= 3;
				HtfUpStroke			= new Stroke(Brushes.Aqua);
				HtfDnStroke			= new Stroke(Brushes.RoyalBlue);

				UpSignalEnable		= true;		UpSignalOffset	= 4;
				DnSignalEnable		= true;		DnSignalOffset	= 4;
				LongEnable			= true;		LongOffset		= 6;
				ShortEnable			= true;		ShortOffset		= 6;
				LongDotEnable		= true;
				ShortDotEnable		= true;
				BotLineEnable		= true;
				TopLineEnable		= true;
				LongWithDotStroke	= new Stroke(Brushes.Cyan,   DashStyleHelper.Solid, 3f);
				ShortWithDotStroke	= new Stroke(Brushes.Orange, DashStyleHelper.Solid, 3f);

				HighBufferTicks		= 1;	LowBufferTicks		= 1;
				BuyTrailStroke		= new Stroke(Brushes.LimeGreen);
				BuyBrokenStroke		= new Stroke(Brushes.SeaGreen);
				SellTrailStroke		= new Stroke(Brushes.Red);
				SellBrokenStroke	= new Stroke(Brushes.IndianRed);

				HighBufferTicksSmall	= 1;	LowBufferTicksSmall	= 1;
				BuyTrailStrokeSmall		= new Stroke(Brushes.LimeGreen);
				BuyBrokenStrokeSmall	= new Stroke(Brushes.SeaGreen);
				SellTrailStrokeSmall	= new Stroke(Brushes.Red);
				SellBrokenStrokeSmall	= new Stroke(Brushes.IndianRed);

				EmitDrawObjects		= true;
				BuyMarkerBrush		= Brushes.Lime;
				SellMarkerBrush		= Brushes.Red;

				MaxRewriteBars		= 250;
				AlertsEnable		= false;
				AlertRearmSeconds	= 10;
				AlertSoundFile		= @"Alert2.wav";
			}
			else if (State == State.Configure)
			{
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;

				// The child instance runs on the HTF series itself, so it must not add another.
				if (HtfEnable)
					AddDataSeries((BarsPeriodType)HtfType, HtfPeriod);
			}
			else if (State == State.DataLoaded)
			{
				// [C3] Everything resolved once here, never inside the rewrite loop.
				emaSuperfast	= EMA(Close, SuperfastLength);
				emaFast			= EMA(Close, FastLength);
				emaSlow			= EMA(Close, SlowLength);

				macdFastMa		= GbUsMa.Create(this, MacdAverageType, Close, MacdFastLength);
				macdSlowMa		= GbUsMa.Create(this, MacdAverageType, Close, MacdSlowLength);
				stoch			= Stochastics(DPeriod, KPeriod, Smooth);
				atrSeries		= ATR(AtrLength);

				zigPriceH		= GbUaiPriceSeries.Get(this, ZigZagPriceH);
				zigPriceL		= GbUaiPriceSeries.Get(this, ZigZagPriceL);

				zigZag = new GbUaiZigZagHighLow(this, zigPriceH, zigPriceL, atrSeries)
				{
					PercentageReversal	= PercentageReversal,
					AbsoluteReversal	= AbsoluteReversal,
					TickReversal		= TickReversal,
					AtrReversal			= AtrReversal
				};

				macd		= NewD();	macdUp = NewB();	macdDown = NewB();
				buyCond		= NewB();	sellCond = NewB();	buyState = NewB();	sellState = NewB();
				eiSave		= NewD();	eiLow = NewD();		eiHigh = NewD();
				zigDir		= NewI();	zigSignal = NewI();
				revLineTop	= NewD();	revLineBot = NewD();

				minMacdBars	= MacdSlowLength + Math.Max(TrendLength, SequentialLength) + 1;
				warmupBars	= Math.Max(SlowLength + 1, AtrLength + 1);
				BarsRequiredToPlot = Math.Max(minMacdBars, warmupBars);

				if (HtfEnable)
				{
					Bars htfBars	= BarsArray[1];
					htfPriceH		= GbUaiPriceSeries.Get(this, ZigZagPriceH, 1);
					htfPriceL		= GbUaiPriceSeries.Get(this, ZigZagPriceL, 1);
					htfEma1			= EMA(Closes[1], SuperfastLength);
					htfEma2			= EMA(Closes[1], FastLength);
					htfEma3			= EMA(Closes[1], SlowLength);

					htfZigZag = new GbUaiZigZagHighLow(this, htfPriceH, htfPriceL, ATR(BarsArray[1], AtrLength), 1)
					{
						PercentageReversal	= PercentageReversal,
						AbsoluteReversal	= AbsoluteReversal,
						TickReversal		= TickReversal,
						AtrReversal			= AtrReversal
					};

					htfEiSave	= new Series<double>(htfBars, MaximumBarsLookBack.Infinite);
					htfEiLow	= new Series<double>(htfBars, MaximumBarsLookBack.Infinite);
					htfEiHigh	= new Series<double>(htfBars, MaximumBarsLookBack.Infinite);
					htfRevTop	= new Series<double>(htfBars, MaximumBarsLookBack.Infinite);
					htfRevBot	= new Series<double>(htfBars, MaximumBarsLookBack.Infinite);
					htfDir		= new Series<int>(htfBars, MaximumBarsLookBack.Infinite);
					htfSignal	= new Series<int>(htfBars, MaximumBarsLookBack.Infinite);
					htfColor	= new Series<int>(htfBars, MaximumBarsLookBack.Infinite);
					htfBuyCond	= new Series<bool>(htfBars, MaximumBarsLookBack.Infinite);
					htfSellCond	= new Series<bool>(htfBars, MaximumBarsLookBack.Infinite);
					htfBuyState	= new Series<bool>(htfBars, MaximumBarsLookBack.Infinite);
					htfSellState = new Series<bool>(htfBars, MaximumBarsLookBack.Infinite);
				}

				WarnIfSameBrush(BuyMarkerBrush, SellMarkerBrush, "Buy/Sell Marker Brush");
				WarnIfSameStroke(LongWithDotStroke, ShortWithDotStroke, "Long/Short With Dot Stroke");
				WarnIfSameStroke(BuyBrokenStroke, SellBrokenStroke, "Buy/Sell Broken Line Stroke");
			}
		}

		private Series<double> NewD() => new Series<double>(this, MaximumBarsLookBack.Infinite);
		private Series<bool>   NewB() => new Series<bool>(this, MaximumBarsLookBack.Infinite);
		private Series<int>    NewI() => new Series<int>(this, MaximumBarsLookBack.Infinite);

		private void WarnIfSameBrush(Brush a, Brush b, string what)
		{
			if (a != null && b != null && a.ToString() == b.ToString())
				Print("[gbUltimateAIPro] WARNING: " + what + " are the same colour — a colour-keyed consumer cannot separate the sides. This is the defect this rebuild exists to remove.");
		}

		private void WarnIfSameStroke(Stroke a, Stroke b, string what)
		{
			if (a != null && b != null)
				WarnIfSameBrush(a.Brush, b.Brush, what);
		}

		protected override void OnBarUpdate()
		{
			if (HtfEnable && BarsInProgress == 1)
			{
				UpdateHtfZones();
				return;
			}

			if (BarsInProgress != 0)
				return;

			if (CurrentBar < warmupBars)
			{
				SeedWarmup();
				return;
			}

			zigZag.OnBarUpdate();
			UpdateMacdTier();
			UpdateTrendState();

			// [C1][C2] The vendor ran an MRO scan over the whole chart plus a double rewrite loop on
			// every tick. The ZigZag only revises on a bar boundary, so intra-bar work is the
			// forming bar alone, and the span is capped.
			if (IsFirstTickOfBar || Calculate == Calculate.OnBarClose)
			{
				int floor = Math.Max(1, CurrentBar - MaxRewriteBars);
				for (int absBar = floor; absBar <= CurrentBar; absBar++)
					CalculateBar(CurrentBar - absBar);
			}
			else
			{
				CalculateBar(0);
			}

			ConfirmBreakouts();
		}

		private void SeedWarmup()
		{
			macd[0] = 0;  macdUp[0] = false;  macdDown[0] = false;
			buyCond[0] = sellCond[0] = buyState[0] = sellState[0] = false;
			eiSave[0] = Close[0];  eiLow[0] = Low[0];  eiHigh[0] = High[0];
			zigDir[0] = 0;  zigSignal[0] = 0;
			revLineTop[0] = 0;  revLineBot[0] = 0;

			for (int i = 0; i <= 13; i++)
				Values[i].Reset(0);

			Colorbars[0]	= 3;
			BuyTrigger[0]	= 0;	SellTrigger[0]	= 0;
			LongTrigger[0]	= 0;	ShortTrigger[0]	= 0;
			TrendState[0]	= 0;
		}

		// ------------------------------------------------------------------
		//  Tier 1 — MACD reversal gated by Stochastic
		// ------------------------------------------------------------------
		private void UpdateMacdTier()
		{
			macd[0] = macdFastMa[0] - macdSlowMa[0];

			if (CurrentBar < minMacdBars)
			{
				macdUp[0] = macdDown[0] = false;
				upSignal.Reset(0);
				dnSignal.Reset(0);
				return;
			}

			// [C3] The vendor built two 0/1 series and ran SUM() indicators over them inside the
			// per-bar loop. A direct count is identical and allocates nothing.
			int rises = 0, falls = 0;
			for (int i = 0; i < SequentialLength; i++)
			{
				if (macd[i] >= macd[i + 1]) rises++;
				else                        falls++;
			}

			macdUp[0]	= rises == SequentialLength && macd[0] >= macd[TrendLength];
			macdDown[0]	= falls == SequentialLength && macd[0] <  macd[TrendLength];

			bool up   = macdDown[1] && macd[0] > macd[1] && stoch.K[0] < Overbought;
			bool down = macdUp[1]   && macd[0] < macd[1] && stoch.K[0] > Oversold;

			if (up && UpSignalEnable)	upSignal[0] = Low[0]  - UpSignalOffset * TickSize;
			else						upSignal.Reset(0);

			if (down && DnSignalEnable)	dnSignal[0] = High[0] + DnSignalOffset * TickSize;
			else						dnSignal.Reset(0);

			if (AlertsEnable && IsFirstTickOfBar)
			{
				if (up)   FireAlert("gbUaiUp",   "upSignal", Plots[0].Brush);
				if (down) FireAlert("gbUaiDown", "dnSignal", Plots[1].Brush);
			}
		}

		// ------------------------------------------------------------------
		//  3-EMA trend latch
		// ------------------------------------------------------------------
		private void UpdateTrendState()
		{
			double ma1 = emaSuperfast[0], ma2 = emaFast[0], ma3 = emaSlow[0];

			buyCond[0]  = ma1 > ma2 && ma2 > ma3 && Low[0]  > ma1;
			sellCond[0] = ma1 < ma2 && ma2 < ma3 && High[0] < ma1;

			bool stopBuy  = ma1 <= ma2;
			bool stopSell = ma1 >= ma2;

			bool buyEntry  = !buyCond[1]  && buyCond[0];
			bool sellEntry = !sellCond[1] && sellCond[0];

			buyState[0]  = (buyEntry  && !stopBuy)  || (buyState[1]  && !stopBuy);
			sellState[0] = (sellEntry && !stopSell) || (sellState[1] && !stopSell);

			// [B7] The vendor's third ternary branch was unreachable.
			Colorbars[0]  = buyState[0] ? 1 : sellState[0] ? 2 : 3;
			TrendState[0] = buyState[0] ? 1 : sellState[0] ? -1 : 0;
		}

		// ------------------------------------------------------------------
		//  ZigZag bookkeeping, dots, reversal lines, BUY/SELL
		// ------------------------------------------------------------------
		private void CalculateBar(int b)
		{
			if (b < 0 || b + 2 > CurrentBar)
				return;

			bool havePivot = zigZag.IsValidDataPoint(b) && zigZag[b] != 0.0;

			eiSave[b] = havePivot ? zigZag[b] : eiSave[b + 1];

			// Tolerance rather than the vendor's exact double ==; both operands trace to the same
			// source on the same bar, so this only hardens against last-bit drift.
			bool pivotIsHigh = Math.Abs(eiSave[b] - zigPriceH[b]) <= TickSize * 0.5;
			bool rising      = (pivotIsHigh ? zigPriceH[b] : zigPriceL[b]) - eiSave[b + 1] >= 0.0;

			// [B8] The vendor guarded this with a tautology.
			if (havePivot)	EnhancedLines[b] = zigZag[b];
			else			EnhancedLines.Reset(b);

			eiLow[b]  = (!havePivot || rising) ? eiLow[b + 1]  : zigPriceL[b];
			eiHigh[b] = (!havePivot || !rising) ? eiHigh[b + 1] : zigPriceH[b];

			zigDir[b] =
				(eiLow[b] != eiLow[b + 1] || (zigPriceL[b] == eiLow[b + 1] && zigPriceL[b] == eiSave[b]))    ?  1 :
				(eiHigh[b] != eiHigh[b + 1] || (zigPriceH[b] == eiHigh[b + 1] && zigPriceH[b] == eiSave[b])) ? -1 :
				zigDir[b + 1];

			zigSignal[b] =
				(zigDir[b] > 0 && !(zigPriceL[b] <= eiLow[b]))  ? (zigSignal[b + 1] <= 0 ?  1 : zigSignal[b + 1]) :
				(zigDir[b] < 0 && zigPriceH[b] < eiHigh[b])     ? (zigSignal[b + 1] >= 0 ? -1 : zigSignal[b + 1]) :
				zigSignal[b + 1];

			bool flipUp   = zigSignal[b] > 0 && zigSignal[b + 1] <= 0;
			bool flipDown = zigSignal[b] < 0 && zigSignal[b + 1] >= 0;

			bool inTrend = Colorbars[b] != 3;

			UpdateReversalLines(b, flipUp, flipDown);

			// Dots fire on a flip regardless of trend state — the vendor set them in both the
			// in-trend and neutral branches. Preserved.
			bool dotLong  = flipUp   && !longS.IsValidDataPoint(b);
			bool dotShort = flipDown && !shortS.IsValidDataPoint(b);

			if (dotLong && LongDotEnable)
			{
				DotLong[b] = Low[b] - LongOffset * TickSize;
				PlotBrushes[10][b] = upSignal.IsValidDataPoint(b) ? LongWithDotStroke.Brush : Plots[10].Brush;
				if (b == 0) buyTrailLevel = High[1] + HighBufferTicks * TickSize;
			}
			else if (!dotLong) DotLong.Reset(b);

			if (dotShort && ShortDotEnable)
			{
				// [B1] ShortOffset, not LongOffset.
				DotShort[b] = High[b] + ShortOffset * TickSize;
				PlotBrushes[11][b] = dnSignal.IsValidDataPoint(b) ? ShortWithDotStroke.Brush : Plots[11].Brush;
				if (b == 0) sellTrailLevel = Low[1] - LowBufferTicks * TickSize;
			}
			else if (!dotShort) DotShort.Reset(b);

			// [A1] BUY/SELL — never written by the vendor. Same condition UltimateSignals uses:
			// a flip out of a neutral (non-trending) state.
			bool buySignal  = flipUp   && !inTrend;
			bool sellSignal = flipDown && !inTrend;

			EmitBuySell(b, buySignal, sellSignal);
		}

		private void UpdateReversalLines(int b, bool flipUp, bool flipDown)
		{
			// [B4] One consistent guard, instead of `> 0` in some places and `!= 0` in others.
			if (flipDown)
			{
				revLineBot[b] = 0;
				revLineTop[b] = High[b + 1];
			}
			else if (flipUp)
			{
				revLineTop[b] = 0;
				revLineBot[b] = Low[b + 1];
			}
			else if (revLineBot[b + 1] != 0.0 && (Colorbars[b + 2] == 2 || Colorbars[b + 1] == 2))
			{
				revLineBot[b] = revLineBot[b + 1];
				revLineTop[b] = 0;
			}
			else if (revLineTop[b + 1] != 0.0 && (Colorbars[b + 2] == 1 || Colorbars[b + 1] == 1))
			{
				revLineTop[b] = revLineTop[b + 1];
				revLineBot[b] = 0;
			}
			else
			{
				revLineTop[b] = 0;
				revLineBot[b] = 0;
			}

			// [B5] Same index as the value, and an explicit reset on the false branch — the vendor
			// wrote botLine[b+1] and never reset.
			if (revLineBot[b] != 0.0 && BotLineEnable)	botLine[b] = revLineBot[b];
			else										botLine.Reset(b);

			if (revLineTop[b] != 0.0 && TopLineEnable)	topLine[b] = revLineTop[b];
			else										topLine.Reset(b);
		}

		private void EmitBuySell(int b, bool buySignal, bool sellSignal)
		{
			int absBar = CurrentBar - b;

			if (buySignal)
			{
				BUY[b]			= Low[b] - TickSize;
				BuyTrigger[b]	= 1;
				DrawMarker(true, b, absBar);
				if (AlertsEnable && b == 0 && IsFirstTickOfBar)
					FireAlert("gbUaiBuy", "BUY", BuyMarkerBrush);
			}
			else
			{
				BUY.Reset(b);
				BuyTrigger[b] = 0;
				ClearMarker(true, absBar);
			}

			if (sellSignal)
			{
				SELL[b]			= High[b] + TickSize;
				SellTrigger[b]	= 1;
				DrawMarker(false, b, absBar);
				if (AlertsEnable && b == 0 && IsFirstTickOfBar)
					FireAlert("gbUaiSell", "SELL", SellMarkerBrush);
			}
			else
			{
				SELL.Reset(b);
				SellTrigger[b] = 0;
				ClearMarker(false, absBar);
			}
		}

		// ------------------------------------------------------------------
		//  Breakout confirmation — the large arrows
		// ------------------------------------------------------------------
		private void ConfirmBreakouts()
		{
			LongTrigger[0]  = 0;
			ShortTrigger[0] = 0;

			if (CurrentBar < 2)
				return;

			// A pending long dot is confirmed when price trades through the buffer above the prior
			// bar's high. Non-repainting: it depends only on closed-bar extremes.
			if (DotLong.IsValidDataPoint(1) && !longS.IsValidDataPoint(0) && buyTrailLevel > 0.0)
			{
				if (High[0] >= buyTrailLevel)
				{
					longS[0]		= LongEnable ? DotLong[1] : double.NaN;
					LongTrigger[0]	= 1;
					PlotBrushes[4][0] = upSignal.IsValidDataPoint(0) ? LongWithDotStroke.Brush : Plots[4].Brush;
					DrawTrailBreak(true, buyTrailLevel);
					buyTrailLevel = 0.0;
					if (AlertsEnable && IsFirstTickOfBar)
						FireAlert("gbUaiLong", "long", Plots[4].Brush);
				}
			}

			// [B1] Own offset, own enable — the vendor used the long side's for both.
			if (DotShort.IsValidDataPoint(1) && !shortS.IsValidDataPoint(0) && sellTrailLevel > 0.0)
			{
				if (Low[0] <= sellTrailLevel)
				{
					shortS[0]		= ShortEnable ? DotShort[1] : double.NaN;
					ShortTrigger[0]	= 1;
					PlotBrushes[5][0] = dnSignal.IsValidDataPoint(0) ? ShortWithDotStroke.Brush : Plots[5].Brush;
					DrawTrailBreak(false, sellTrailLevel);
					sellTrailLevel = 0.0;
					if (AlertsEnable && IsFirstTickOfBar)
						FireAlert("gbUaiShort", "short", Plots[5].Brush);
				}
			}
		}

		// ------------------------------------------------------------------
		//  Ultimate Zones (HTF)
		// ------------------------------------------------------------------
		private void UpdateHtfZones()
		{
			if (CurrentBars[1] < Math.Max(SlowLength + 1, AtrLength + 1) || CurrentBars[0] < 1)
				return;

			ComputeHtfBar();

			DateTime cutoff = Times[0][0].Date.AddDays(-HtfDaysToLoad);
			int tops = 0, bots = 0;
			double lastTop = double.NaN, lastBot = double.NaN;

			for (int i = 0; i < CurrentBars[1] && (tops <= HtfMaxLevels || bots <= HtfMaxLevels); i++)
			{
				DateTime htfTime = Times[1][i];
				if (htfTime < cutoff)
					break;

				int mappedBar = Math.Max(0, CurrentBars[0] - BarsArray[0].GetBar(htfTime));
				DateTime mappedTime = Times[0][mappedBar];

				TryZoneLine(htfRevTop, i, mappedBar, mappedTime, cutoff, "GBUAI_ZONE_TOP_",
					HtfUpStroke, topLineHtf, ref tops, ref lastTop);

				TryZoneLine(htfRevBot, i, mappedBar, mappedTime, cutoff, "GBUAI_ZONE_BOT_",
					HtfDnStroke, botLineHtf, ref bots, ref lastBot);
			}

			if (Times[0][0].Date != Times[0][1].Date)
				PurgeExpiredZones(Times[0][0].Date);
		}

		/// <summary>
		/// The same pivot/trend/reversal-line recursion as CalculateBar, run against BarsArray[1].
		/// Deliberately a separate copy rather than a shared generic: every read here is series-1
		/// relative (Highs[1], Lows[1], htf* state), and with no way to compile-test a generalised
		/// version, an explicit duplicate is the safer trade. Only the current HTF bar is computed —
		/// zones do not need the retro-rewrite the primary series does.
		/// </summary>
		private void ComputeHtfBar()
		{
			htfZigZag.OnBarUpdate();

			double m1 = htfEma1[0], m2 = htfEma2[0], m3 = htfEma3[0];

			htfBuyCond[0]  = m1 > m2 && m2 > m3 && Lows[1][0]  > m1;
			htfSellCond[0] = m1 < m2 && m2 < m3 && Highs[1][0] < m1;

			bool stopBuy  = m1 <= m2;
			bool stopSell = m1 >= m2;

			htfBuyState[0]  = ((!htfBuyCond[1]  && htfBuyCond[0])  && !stopBuy)  || (htfBuyState[1]  && !stopBuy);
			htfSellState[0] = ((!htfSellCond[1] && htfSellCond[0]) && !stopSell) || (htfSellState[1] && !stopSell);

			htfColor[0] = htfBuyState[0] ? 1 : htfSellState[0] ? 2 : 3;

			bool havePivot = htfZigZag.IsValidDataPoint(0) && htfZigZag[0] != 0.0;

			htfEiSave[0] = havePivot ? htfZigZag[0] : htfEiSave[1];

			bool pivotIsHigh = Math.Abs(htfEiSave[0] - htfPriceH[0]) <= TickSize * 0.5;
			bool rising      = (pivotIsHigh ? htfPriceH[0] : htfPriceL[0]) - htfEiSave[1] >= 0.0;

			htfEiLow[0]  = (!havePivot || rising)  ? htfEiLow[1]  : htfPriceL[0];
			htfEiHigh[0] = (!havePivot || !rising) ? htfEiHigh[1] : htfPriceH[0];

			htfDir[0] =
				(htfEiLow[0] != htfEiLow[1] || (htfPriceL[0] == htfEiLow[1] && htfPriceL[0] == htfEiSave[0]))    ?  1 :
				(htfEiHigh[0] != htfEiHigh[1] || (htfPriceH[0] == htfEiHigh[1] && htfPriceH[0] == htfEiSave[0])) ? -1 :
				htfDir[1];

			htfSignal[0] =
				(htfDir[0] > 0 && !(htfPriceL[0] <= htfEiLow[0]))  ? (htfSignal[1] <= 0 ?  1 : htfSignal[1]) :
				(htfDir[0] < 0 && htfPriceH[0] < htfEiHigh[0])     ? (htfSignal[1] >= 0 ? -1 : htfSignal[1]) :
				htfSignal[1];

			bool flipUp   = htfSignal[0] > 0 && htfSignal[1] <= 0;
			bool flipDown = htfSignal[0] < 0 && htfSignal[1] >= 0;

			if (flipDown)
			{
				htfRevBot.Reset(0);
				htfRevTop[0] = Highs[1][1];
			}
			else if (flipUp)
			{
				htfRevTop.Reset(0);
				htfRevBot[0] = Lows[1][1];
			}
			else if (htfRevBot.IsValidDataPoint(1) && (htfColor[2] == 2 || htfColor[1] == 2))
			{
				htfRevBot[0] = htfRevBot[1];
				htfRevTop.Reset(0);
			}
			else if (htfRevTop.IsValidDataPoint(1) && (htfColor[2] == 1 || htfColor[1] == 1))
			{
				htfRevTop[0] = htfRevTop[1];
				htfRevBot.Reset(0);
			}
			else
			{
				htfRevTop.Reset(0);
				htfRevBot.Reset(0);
			}
		}

		private void TryZoneLine(Series<double> source, int htfBar, int mappedBar, DateTime mappedTime,
			DateTime cutoff, string tagPrefix, Stroke stroke, Series<double> target, ref int count, ref double last)
		{
			string tag = tagPrefix + mappedTime.ToBinary();

			if (source.IsValidDataPoint(htfBar))
			{
				double level = source[htfBar];
				if (level != last)
				{
					count++;
					last = level;
				}

				target[mappedBar] = level;

				if (mappedTime >= cutoff)
				{
					Draw.HorizontalLine(this, tag, level, stroke.Brush).Stroke = stroke;
					liveTags.Add(tag);
				}
			}
			else if (target.IsValidDataPoint(mappedBar))
			{
				target.Reset(mappedBar);
				if (liveTags.Remove(tag))
					RemoveDrawObject(tag);
			}
		}

		// [C4] HashSet, and only tags we know we drew.
		private void PurgeExpiredZones(DateTime today)
		{
			DateTime cutoff = today.AddDays(-HtfDaysToLoad);
			List<string> expired = new List<string>();

			foreach (string tag in liveTags)
			{
				if (!tag.StartsWith("GBUAI_ZONE_"))
					continue;

				int idx = tag.LastIndexOf('_');
				if (idx < 0 || idx + 1 >= tag.Length)
					continue;

				long binary;
				if (!long.TryParse(tag.Substring(idx + 1), out binary))
					continue;

				try
				{
					if (DateTime.FromBinary(binary) <= cutoff)
						expired.Add(tag);
				}
				catch { }
			}

			foreach (string tag in expired)
			{
				liveTags.Remove(tag);
				RemoveDrawObject(tag);
			}

			if (expired.Count > 0)
				ForceRefresh();
		}

		// ------------------------------------------------------------------
		//  Draw objects and alerts
		// ------------------------------------------------------------------
		private string MarkerTag(bool isBuy, int absBar)	=> (isBuy ? "GBUAI_BUY_"   : "GBUAI_SELL_")  + absBar;
		private string BreakTag (bool isBuy, int absBar)	=> (isBuy ? "GBUAI_LONG_"  : "GBUAI_SHORT_") + absBar;

		private void DrawMarker(bool isBuy, int b, int absBar)
		{
			if (!EmitDrawObjects)
				return;

			string tag = MarkerTag(isBuy, absBar);
			Brush brush = isBuy ? BuyMarkerBrush : SellMarkerBrush;

			if (isBuy)	Draw.ArrowUp  (this, tag, true, b, Low[b]  - 2 * TickSize, brush);
			else		Draw.ArrowDown(this, tag, true, b, High[b] + 2 * TickSize, brush);

			liveTags.Add(tag);
		}

		private void ClearMarker(bool isBuy, int absBar)
		{
			string tag = MarkerTag(isBuy, absBar);
			if (liveTags.Remove(tag))
				RemoveDrawObject(tag);
		}

		private void DrawTrailBreak(bool isBuy, double level)
		{
			if (!EmitDrawObjects)
				return;

			string tag = BreakTag(isBuy, CurrentBar);
			Stroke stroke = isBuy ? BuyBrokenStroke : SellBrokenStroke;

			Draw.Line(this, tag, 1, level, 0, level, stroke.Brush).Stroke = stroke;
			liveTags.Add(tag);
		}

		private void FireAlert(string id, string label, Brush brush)
		{
			Alert(id, Priority.Medium, "gbUltimateAIPro: " + label, AlertSoundFile,
				AlertRearmSeconds, Brushes.Black, brush);
		}
	}
}

// NOTE — do NOT hand-write a "#region NinjaScript generated code" block here.
// NinjaTrader regenerates and APPENDS that region on every compile once the type is registered,
// so a hand-written copy becomes a duplicate (CS0102 / CS0111 / CS0121 / CS0229). Registration
// happens on the first successful compile, which is why a brand-new self-referencing indicator can
// need one temporary pass with a hand-written region before NT8 takes over.
//
// Any enum used as a [NinjaScriptProperty] type MUST live in the global namespace — NT8 emits those
// types unqualified into namespace NinjaTrader.NinjaScript.Indicators. See gbUltimateSignalsEngines.cs.
