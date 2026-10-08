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

//  gbUltimateSignalsIndicator v1.0.0
//  ---------------------------------------------------------------------------
//  GreyBeard rebuild of the vendor "Ultimate Signals" indicator. Signal maths is preserved; the
//  defects catalogued in UltimateSignalsIndicator_Code_Review.md are fixed and every hard-coded
//  input is exposed.
//
//  FIXES APPLIED (review reference in brackets)
//    [A1] Alerts could never fire — the vendor tested WaitForNextBar.isNewBar but never called
//         check(), the only thing that assigns it, so both alert branches were unreachable.
//    [A2] macd[] was read 5 bars back but only written from bar 6, so early MACD signals were
//         computed against unwritten history. MACD is now written from bar 0 and the consumers
//         are guarded separately.
//    [A3] Tier 2/3 evaluated from bar 1 while EMA 9/14/21 were still seed-dominated. Now gated on
//         a real warm-up.
//    [A4] Buy and sell markers shared one hard-coded Brushes.Magenta, which is why no colour-keyed
//         consumer could tell the sides apart. Now Lime / Red by default and user-settable.
//    [B]  UPTICKBrush / DOWNTICKBrush were unreachable — the colour was chosen on Colorbars != 3
//         while the guard required == 3, so BUY/SELL text was always white. Text is now genuinely
//         side-coloured, which is what the vendor evidently intended.
//    [C1] The rewrite loop ran on every tick across an unbounded span. It now runs only when the
//         bar changes, and is depth-capped by MaxRewriteBars.
//    [C2] RemoveDrawObject was called for tags that mostly did not exist. Live tags are tracked.
//    [D1] Exact float equality decided high-vs-low pivot handling; now tolerance-based.
//    [D2] buysignal was seeded at warm-up but sellsignal was not. Both sides now seed identically.
//    [D3] Dead unreachable CurrentBar < 1 branch removed.
//    [E1] Every named constant was shadowed by a magic number at the call site, so changing a
//         constant did nothing. All values now flow from properties.
//    [E2] Dead members removed: MACDLength (no signal line exists), bubbleoffset, showarrows,
//         XPlotBotLine/XPlotTopLine, OnRenderZigzag, IsPriceGreater.
//    [E3] LineDraw read the same bar for both endpoints, drawing top/bot lines as a staircase.
//         Replaced by ordinary NT8 line plots.
//    [F1] Zero parameters were exposed. All inputs are now [NinjaScriptProperty].
//    [F2] Calculate was forced to OnEachTick. Left at the NT8 default so OnBarClose is selectable.
//    [F3] Plots renamed so the three tiers are self-describing in a condition builder.
//
//  CAPTURABILITY — the reason this rebuild exists. Buy and sell are emitted three ways, each with
//  a distinct per-side identity, so a consumer can key on whichever it supports:
//    * plots        — BuyTrigger / SellTrigger carry 1 or 0 (never NaN, which most condition
//                     builders cannot express)
//    * colour       — every tier uses a different brush per side, all user-settable
//    * draw objects — stable, side-distinct tags GBUS_BUY_<bar> / GBUS_SELL_<bar>
//
//  DELIBERATE DEVIATIONS from the vendor build
//    * NT8 built-ins replace the private EMA/SMA/HMA/WMA/ATR/MIN/MAX engines (formula-identical),
//      and NT8's Stochastics replaces TosStochastics — the same calculation at the vendor's
//      hard-coded High/Low/Close + SMA settings. This removes ~900 lines of re-derived code.
//    * Custom SharpDX arrow rendering is replaced by plots plus NT8 drawing objects. SharpDX
//      geometry is invisible to tag-based consumers; drawing objects are not.
//  Both are recorded in UltimateSignals_Decoded_Technical_Summary.md.
//
//  Plot indices 0-8 are unchanged from the vendor build so existing configurations map across.
//  ---------------------------------------------------------------------------

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public class gbUltimateSignalsIndicator : Indicator
	{
		#region Fields
		private ISeries<double>	movAvgFast;			// EMA 9  (superfast)
		private ISeries<double>	movAvgMid;			// EMA 14 (fast)
		private ISeries<double>	movAvgSlow;			// EMA 21 (slow)
		private ISeries<double>	macdFastMa;
		private ISeries<double>	macdSlowMa;
		private Stochastics		stoch;

		private ISeries<double>	zigHigh;			// EMA(High, n) or raw High
		private ISeries<double>	zigLow;
		private GbUsZigZagHighLow zigZag;

		private Series<double>	macd;
		private Series<bool>	macdUp, macdDown;

		private Series<bool>	buyCond, sellCond;	// raw, unlatched
		private Series<bool>	buyState, sellState;	// latched
		private Series<int>		colorState;			// 1 = long, 2 = short, 3 = neutral

		private Series<double>	pivotSave, pivotLow, pivotHigh;
		private Series<int>		zigDir, zigSignal;
		private Series<double>	revLineTop, revLineBot;

		private int minMacdBars;
		private int warmupBars;

		// [C2] Only tags actually drawn are candidates for removal.
		private readonly HashSet<string> liveTags = new HashSet<string>();
		#endregion

		#region Plots
		[Browsable(false)] [XmlIgnore] public Series<double> upSignalPlot		=> Values[0];
		[Browsable(false)] [XmlIgnore] public Series<double> dnSignalPlot		=> Values[1];
		[Browsable(false)] [XmlIgnore] public Series<double> EnhancedLines		=> Values[2];
		[Browsable(false)] [XmlIgnore] public Series<double> long_				=> Values[3];
		[Browsable(false)] [XmlIgnore] public Series<double> short_				=> Values[4];
		[Browsable(false)] [XmlIgnore] public Series<double> botLine			=> Values[5];
		[Browsable(false)] [XmlIgnore] public Series<double> topLine			=> Values[6];
		[Browsable(false)] [XmlIgnore] public Series<double> buyTextMarker		=> Values[7];
		[Browsable(false)] [XmlIgnore] public Series<double> sellTextMarker		=> Values[8];

		// New: 1/0 triggers for condition builders that cannot express IsNaN.
		[Browsable(false)] [XmlIgnore] public Series<double> BuyTrigger			=> Values[9];
		[Browsable(false)] [XmlIgnore] public Series<double> SellTrigger		=> Values[10];
		[Browsable(false)] [XmlIgnore] public Series<double> TrendState			=> Values[11];
		#endregion

		#region Properties
		[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.0";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";

		// --- 1. Trend (the 3-EMA state machine) ---
		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Superfast EMA", Order = 0, GroupName = "1. Trend")]
		public int SuperfastLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Fast EMA", Order = 1, GroupName = "1. Trend")]
		public int FastLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Slow EMA", Order = 2, GroupName = "1. Trend")]
		public int SlowLength { get; set; }

		// --- 2. MACD tier ---
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
		[Display(Name = "Trend Length", Description = "MACD must beat its value this many bars back.", Order = 3, GroupName = "2. MACD Tier")]
		public int TrendLength { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Sequential Length", Description = "Consecutive bars MACD must rise or fall.", Order = 4, GroupName = "2. MACD Tier")]
		public int SequentialLength { get; set; }

		// --- 3. Stochastic gate ---
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

		// --- 4. ZigZag ---
		[NinjaScriptProperty]
		[Display(Name = "Price Method", Description = "Average = ZigZag tracks EMA(High)/EMA(Low), the vendor default. HighLow = raw extremes.", Order = 0, GroupName = "4. ZigZag")]
		public GbUsZigZagPriceMethod ZigZagPriceMethod { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "Smoothing Length", Description = "EMA length for the High/Low series when Price Method is Average.", Order = 1, GroupName = "4. ZigZag")]
		public int ZigZagSmoothingLength { get; set; }

		[NinjaScriptProperty] [Range(0.0, 100.0)]
		[Display(Name = "Percentage Reversal", Order = 2, GroupName = "4. ZigZag")]
		public double PercentageReversal { get; set; }

		[NinjaScriptProperty] [Range(0.0, double.MaxValue)]
		[Display(Name = "Absolute Reversal", Order = 3, GroupName = "4. ZigZag")]
		public double AbsoluteReversal { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Tick Reversal", Order = 4, GroupName = "4. ZigZag")]
		public double TickReversal { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)]
		[Display(Name = "ATR Length", Order = 5, GroupName = "4. ZigZag")]
		public int AtrLength { get; set; }

		[NinjaScriptProperty] [Range(0.0, double.MaxValue)]
		[Display(Name = "ATR Reversal", Order = 6, GroupName = "4. ZigZag")]
		public double AtrReversal { get; set; }

		// --- 5. Signal output ---
		[NinjaScriptProperty]
		[Display(Name = "Emit Draw Objects", Description = "Draw BUY/SELL arrows as NinjaTrader drawing objects with stable per-side tags (GBUS_BUY_*, GBUS_SELL_*). Required for tag-based capture by PredatorX or Infinity Algo Engine.", Order = 0, GroupName = "5. Signal Output")]
		public bool EmitDrawObjects { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show BUY/SELL Text", Order = 1, GroupName = "5. Signal Output")]
		public bool ShowText { get; set; }

		[NinjaScriptProperty] [Range(0, 500)]
		[Display(Name = "Text Pixel Offset", Order = 2, GroupName = "5. Signal Output")]
		public int TextPixelOffset { get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Text Brush", Description = "MUST differ from the sell brush — that difference is what lets a colour-keyed consumer tell the sides apart.", Order = 3, GroupName = "5. Signal Output")]
		public Brush BuyTextBrush { get; set; }

		[Browsable(false)]
		public string BuyTextBrushSerialize
		{
			get { return Serialize.BrushToString(BuyTextBrush); }
			set { BuyTextBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Text Brush", Description = "MUST differ from the buy brush.", Order = 4, GroupName = "5. Signal Output")]
		public Brush SellTextBrush { get; set; }

		[Browsable(false)]
		public string SellTextBrushSerialize
		{
			get { return Serialize.BrushToString(SellTextBrush); }
			set { SellTextBrush = Serialize.StringToBrush(value); }
		}

		// --- 6. Performance ---
		[NinjaScriptProperty] [Range(1, 5000)]
		[Display(Name = "Max Rewrite Bars", Description = "Cap on how far back a retracted ZigZag pivot may force a recalculation. The vendor build was unbounded and re-ran the whole span on every tick.", Order = 0, GroupName = "6. Performance")]
		public int MaxRewriteBars { get; set; }

		// --- 7. Alerts ---
		[NinjaScriptProperty]
		[Display(Name = "Enable Alerts", Order = 0, GroupName = "7. Alerts")]
		public bool UseAlerts { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)]
		[Display(Name = "Alert Rearm Seconds", Order = 1, GroupName = "7. Alerts")]
		public int AlertRearmSeconds { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Alert Sound File", Order = 2, GroupName = "7. Alerts")]
		public string AlertSoundFile { get; set; }
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "gbUltimateSignalsIndicator";
				Description						= @"GreyBeard rebuild of Ultimate Signals: MACD/Stochastic reversal tier, ZigZag pivot tier, and 3-EMA trend BUY/SELL markers, with per-side colours and stable per-side draw tags so the sell signal is capturable.";
				IsOverlay						= true;
				IsAutoScale						= true;
				IsSuspendedWhileInactive		= false;
				ShowTransparentPlotsInDataBox	= true;
				// [F2] Calculate deliberately left at the NT8 default so OnBarClose is selectable.

				// Plot order 0-8 matches the vendor build. [F3] names describe the tier.
				AddPlot(new Stroke(Brushes.Lime,        DashStyleHelper.Solid, 3f), PlotStyle.TriangleUp, "MACD Up");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 3f), PlotStyle.Dot,        "MACD Down");
				AddPlot(new Stroke(Brushes.DodgerBlue,  DashStyleHelper.Solid, 1f), PlotStyle.Line,       "ZigZag Line");
				AddPlot(new Stroke(Brushes.Lime,        DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "ZigZag Long");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 5f), PlotStyle.Dot,        "ZigZag Short");
				AddPlot(new Stroke(Brushes.LightGreen,  DashStyleHelper.Solid, 2f), PlotStyle.Line,       "Stop Line Long");
				AddPlot(new Stroke(Brushes.Salmon,      DashStyleHelper.Solid, 2f), PlotStyle.Line,       "Stop Line Short");
				// [A4] Distinct brushes. These two were both Brushes.Magenta in the vendor build.
				AddPlot(new Stroke(Brushes.Lime,        DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "BUY Signal");
				AddPlot(new Stroke(Brushes.Red,         DashStyleHelper.Solid, 5f), PlotStyle.Dot,        "SELL Signal");
				AddPlot(Brushes.Transparent, "BuyTrigger");
				AddPlot(Brushes.Transparent, "SellTrigger");
				AddPlot(Brushes.Transparent, "TrendState");

				SuperfastLength			= 9;
				FastLength				= 14;
				SlowLength				= 21;

				MacdFastLength			= 5;
				MacdSlowLength			= 26;
				MacdAverageType			= GbUsMaMode.EMA;
				TrendLength				= 5;
				SequentialLength		= 3;

				KPeriod					= 10;
				DPeriod					= 10;
				Smooth					= 3;
				Overbought				= 80.0;
				Oversold				= 20.0;

				ZigZagPriceMethod		= GbUsZigZagPriceMethod.Average;
				ZigZagSmoothingLength	= 5;
				PercentageReversal		= 0.01;
				AbsoluteReversal		= 0.05;
				TickReversal			= 0.0;
				AtrLength				= 5;
				AtrReversal				= 2.0;

				EmitDrawObjects			= true;
				ShowText				= true;
				TextPixelOffset			= 50;
				BuyTextBrush			= Brushes.Lime;
				SellTextBrush			= Brushes.Red;

				MaxRewriteBars			= 250;

				UseAlerts				= false;
				AlertRearmSeconds		= 10;
				AlertSoundFile			= @"Alert1.wav";
			}
			else if (State == State.Configure)
			{
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
			}
			else if (State == State.DataLoaded)
			{
				// [E1] Every length now comes from a property rather than a shadowed literal.
				movAvgFast	= EMA(Close, SuperfastLength);
				movAvgMid	= EMA(Close, FastLength);
				movAvgSlow	= EMA(Close, SlowLength);

				macdFastMa	= GbUsMa.Create(this, MacdAverageType, Close, MacdFastLength);
				macdSlowMa	= GbUsMa.Create(this, MacdAverageType, Close, MacdSlowLength);
				stoch		= Stochastics(DPeriod, KPeriod, Smooth);

				zigHigh		= ZigZagPriceMethod == GbUsZigZagPriceMethod.Average ? EMA(High, ZigZagSmoothingLength) : (ISeries<double>)High;
				zigLow		= ZigZagPriceMethod == GbUsZigZagPriceMethod.Average ? EMA(Low,  ZigZagSmoothingLength) : (ISeries<double>)Low;

				zigZag = new GbUsZigZagHighLow(this, zigHigh, zigLow, ATR(AtrLength))
				{
					PercentageReversal	= PercentageReversal,
					AbsoluteReversal	= AbsoluteReversal,
					TickReversal		= TickReversal,
					AtrReversal			= AtrReversal
				};

				macd		= new Series<double>(this, MaximumBarsLookBack.Infinite);
				macdUp		= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				macdDown	= new Series<bool>(this, MaximumBarsLookBack.Infinite);

				buyCond		= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				sellCond	= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				buyState	= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				sellState	= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				colorState	= new Series<int>(this, MaximumBarsLookBack.Infinite);

				pivotSave	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				pivotLow	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				pivotHigh	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				zigDir		= new Series<int>(this, MaximumBarsLookBack.Infinite);
				zigSignal	= new Series<int>(this, MaximumBarsLookBack.Infinite);
				revLineTop	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				revLineBot	= new Series<double>(this, MaximumBarsLookBack.Infinite);

				// [A2] The deepest MACD read is macd[TrendLength], and macd itself is only
				// meaningful once its slow MA is warm.
				minMacdBars	= MacdSlowLength + Math.Max(TrendLength, SequentialLength) + 1;
				// [A3] Tier 2/3 need the 21-EMA warm, not merely one bar of history.
				warmupBars	= Math.Max(SlowLength + 1, AtrLength + 1);

				BarsRequiredToPlot = Math.Max(minMacdBars, warmupBars);

				if (BuyTextBrush != null && SellTextBrush != null && BuyTextBrush.ToString() == SellTextBrush.ToString())
					Print("[gbUltimateSignalsIndicator] WARNING: Buy and Sell text brushes are identical — a colour-keyed consumer cannot separate the sides. This is the exact defect this rebuild exists to remove.");
			}
		}

		protected override void OnBarUpdate()
		{
			zigZag.OnBarUpdate();
			UpdateMacdTier();
			UpdateTrendAndPivotTiers();
		}

		// ------------------------------------------------------------------
		//  Tier 1 — MACD reversal, gated by Stochastic
		// ------------------------------------------------------------------
		private void UpdateMacdTier()
		{
			// [A2] Written unconditionally so the lookbacks below always read real history.
			macd[0] = macdFastMa[0] - macdSlowMa[0];

			if (CurrentBar < minMacdBars)
			{
				macdUp[0]	= false;
				macdDown[0]	= false;
				upSignalPlot.Reset(0);
				dnSignalPlot.Reset(0);
				return;
			}

			int rises = 0;
			int falls = 0;
			for (int i = 0; i < SequentialLength; i++)
			{
				if (macd[i] >= macd[i + 1]) rises++;
				else                        falls++;
			}

			macdUp[0]	= rises == SequentialLength && macd[0] >= macd[TrendLength];
			macdDown[0]	= falls == SequentialLength && macd[0] <  macd[TrendLength];

			// Exhaustion reversal: momentum ran one way, turned, and the oscillator is not yet
			// stretched in the direction of the new move.
			bool up   = macdDown[1] && macd[0] > macd[1] && stoch.K[0] < Overbought;
			bool down = macdUp[1]   && macd[0] < macd[1] && stoch.K[0] > Oversold;

			if (up)		upSignalPlot[0] = Low[0]  - TickSize;
			else		upSignalPlot.Reset(0);

			if (down)	dnSignalPlot[0] = High[0] + TickSize;
			else		dnSignalPlot.Reset(0);

			// [A1] Alert() self-rearms via AlertRearmSeconds; the vendor's WaitForNextBar gate was
			// unreachable because check() — the only assignment to isNewBar — was never called.
			if (UseAlerts && IsFirstTickOfBar)
			{
				if (up)
					Alert("gbUsUp", Priority.Medium, "gbUltimateSignals: MACD up", AlertSoundFile, AlertRearmSeconds, Brushes.Black, Plots[0].Brush);
				if (down)
					Alert("gbUsDown", Priority.Medium, "gbUltimateSignals: MACD down", AlertSoundFile, AlertRearmSeconds, Brushes.Black, Plots[1].Brush);
			}
		}

		// ------------------------------------------------------------------
		//  Tiers 2 and 3 — 3-EMA trend state, then ZigZag pivot flips
		// ------------------------------------------------------------------
		private void UpdateTrendAndPivotTiers()
		{
			// [A3] / [D2] Seed BOTH sides symmetrically during warm-up. The vendor seeded
			// buysignal only, and started evaluating from bar 1.
			if (CurrentBar < warmupBars)
			{
				buyCond[0] = sellCond[0] = false;
				buyState[0] = sellState[0] = false;
				colorState[0] = 3;
				zigDir[0] = 0;
				zigSignal[0] = 0;
				pivotSave[0] = Close[0];
				pivotLow[0] = Low[0];
				pivotHigh[0] = High[0];
				revLineTop.Reset(0);
				revLineBot.Reset(0);
				ResetTierPlots(0);
				TrendState[0] = 0;
				return;
			}

			double ma9  = movAvgFast[0];
			double ma14 = movAvgMid[0];
			double ma21 = movAvgSlow[0];

			buyCond[0]  = ma9 > ma14 && ma14 > ma21 && Low[0]  > ma9;
			sellCond[0] = ma9 < ma14 && ma14 < ma21 && High[0] < ma9;

			bool stopBuy  = ma9 <= ma14;
			bool stopSell = ma9 >= ma14;

			// Latches on the transition into the condition, clears on the MA cross. [D3] the
			// vendor's unreachable CurrentBar < 1 branch is gone.
			bool buyEntry  = !buyCond[1]  && buyCond[0];
			bool sellEntry = !sellCond[1] && sellCond[0];

			buyState[0]  = (buyEntry  && !stopBuy)  || (buyState[1]  && !stopBuy);
			sellState[0] = (sellEntry && !stopSell) || (sellState[1] && !stopSell);

			// [E2] The vendor's third ternary branch was unreachable — by that point both states
			// are false, so it always yielded 3.
			colorState[0] = buyState[0] ? 1 : sellState[0] ? 2 : 3;

			// [C1] The vendor ran this span on every tick. The ZigZag can only confirm or retract a
			// pivot on a bar boundary, so intra-bar work is limited to the forming bar.
			bool barChanged = IsFirstTickOfBar || Calculate == Calculate.OnBarClose;

			if (barChanged)
			{
				int floor = Math.Max(zigZag.XLastChangedBar, CurrentBar - MaxRewriteBars);
				if (floor < 1)
					floor = 1;

				for (int absBar = floor; absBar <= CurrentBar; absBar++)
					CalculateZigZagBar(absBar);
			}
			else
			{
				CalculateZigZagBar(CurrentBar);
			}

			TrendState[0] = buyState[0] ? 1 : sellState[0] ? -1 : 0;
		}

		private void CalculateZigZagBar(int absBar)
		{
			if (absBar < 1)
				return;

			int b = CurrentBar - absBar;		// barsAgo
			if (b < 0 || b + 2 > CurrentBar)
				return;

			bool havePivot = zigZag.IsValidDataPoint(b);

			pivotSave[b] = havePivot ? zigZag[b] : pivotSave[b + 1];

			// [D1] The vendor compared two doubles with == to decide whether this pivot was a high
			// or a low. Exact equality is unnecessary here — a tick-scaled tolerance is safe and
			// cannot be defeated by a last-bit difference.
			bool pivotIsHigh = Math.Abs(pivotSave[b] - zigHigh[b]) <= TickSize * 0.5;
			bool rising      = (pivotIsHigh ? zigHigh[b] : zigLow[b]) - pivotSave[b + 1] >= 0.0;

			if (havePivot)	EnhancedLines[b] = zigZag[b];
			else			EnhancedLines.Reset(b);

			pivotLow[b]  = (!havePivot || rising) ? pivotLow[b + 1]  : zigLow[b];
			pivotHigh[b] = (havePivot && rising)  ? zigHigh[b]       : pivotHigh[b + 1];

			zigDir[b] =
				(pivotLow[b] != pivotLow[b + 1] || (zigLow[b] == pivotLow[b + 1] && zigLow[b] == pivotSave[b]))   ?  1 :
				(pivotHigh[b] != pivotHigh[b + 1] || (zigHigh[b] == pivotHigh[b + 1] && zigHigh[b] == pivotSave[b])) ? -1 :
				zigDir[b + 1];

			zigSignal[b] =
				(zigDir[b] > 0 && !(zigLow[b] <= pivotLow[b]))  ? (zigSignal[b + 1] <= 0 ?  1 : zigSignal[b + 1]) :
				(zigDir[b] < 0 && zigHigh[b] < pivotHigh[b])    ? (zigSignal[b + 1] >= 0 ? -1 : zigSignal[b + 1]) :
				zigSignal[b + 1];

			bool flipUp   = zigSignal[b] > 0 && zigSignal[b + 1] <= 0;
			bool flipDown = zigSignal[b] < 0 && zigSignal[b + 1] >= 0;

			bool trendEstablished = colorState[b] != 3;

			// Tier 2 fires while a 3-EMA trend is running; tier 3 fires only when it is not.
			// The two are mutually exclusive by construction — that is the vendor's design.
			if (flipUp && trendEstablished)		long_[b]  = Low[b];
			else								long_.Reset(b);

			if (flipDown && trendEstablished)	short_[b] = High[b];
			else								short_.Reset(b);

			UpdateReversalLines(b, flipUp, flipDown);

			bool buySignal  = flipUp   && !trendEstablished && zigSignal[b] > 0 && zigSignal[b + 1] <= 0;
			bool sellSignal = flipDown && !trendEstablished && zigSignal[b] < 0 && zigSignal[b + 1] >= 0;

			EmitTierThree(b, buySignal, sellSignal, rising);
		}

		private void UpdateReversalLines(int b, bool flipUp, bool flipDown)
		{
			if (flipDown)
			{
				revLineBot.Reset(b);
				revLineTop[b] = High[b + 1];
			}
			else if (flipUp)
			{
				revLineTop.Reset(b);
				revLineBot[b] = Low[b + 1];
			}
			else if (revLineBot.IsValidDataPoint(b + 1) && (colorState[b + 2] == 2 || colorState[b + 1] == 2))
			{
				revLineBot[b] = revLineBot[b + 1];
				revLineTop.Reset(b);
			}
			else if (revLineTop.IsValidDataPoint(b + 1) && (colorState[b + 2] == 1 || colorState[b + 1] == 1))
			{
				revLineTop[b] = revLineTop[b + 1];
				revLineBot.Reset(b);
			}
			else
			{
				revLineTop.Reset(b);
				revLineBot.Reset(b);
			}

			if (revLineBot.IsValidDataPoint(b))	botLine[b] = revLineBot[b];
			else								botLine.Reset(b);

			if (revLineTop.IsValidDataPoint(b))	topLine[b] = revLineTop[b];
			else								topLine.Reset(b);
		}

		private void EmitTierThree(int b, bool buySignal, bool sellSignal, bool rising)
		{
			int absBar = CurrentBar - b;

			if (buySignal)
			{
				buyTextMarker[b]	= Low[b] - TickSize;
				BuyTrigger[b]		= 1;
				DrawSignal(true, b, absBar, rising);
			}
			else
			{
				buyTextMarker.Reset(b);
				BuyTrigger[b] = 0;
				ClearSignal(true, absBar);
			}

			if (sellSignal)
			{
				sellTextMarker[b]	= High[b] + TickSize;
				SellTrigger[b]		= 1;
				DrawSignal(false, b, absBar, rising);
			}
			else
			{
				sellTextMarker.Reset(b);
				SellTrigger[b] = 0;
				ClearSignal(false, absBar);
			}
		}

		private void ResetTierPlots(int b)
		{
			EnhancedLines.Reset(b);
			long_.Reset(b);
			short_.Reset(b);
			botLine.Reset(b);
			topLine.Reset(b);
			buyTextMarker.Reset(b);
			sellTextMarker.Reset(b);
			BuyTrigger[b] = 0;
			SellTrigger[b] = 0;
		}

		// ------------------------------------------------------------------
		//  Draw objects — stable, side-distinct tags so tag-keyed consumers can bind
		// ------------------------------------------------------------------
		private string ArrowTag(bool isBuy, int absBar)	=> (isBuy ? "GBUS_BUY_"      : "GBUS_SELL_")      + absBar;
		private string TextTag (bool isBuy, int absBar)	=> (isBuy ? "GBUS_BUYTEXT_"  : "GBUS_SELLTEXT_")  + absBar;

		private void DrawSignal(bool isBuy, int b, int absBar, bool rising)
		{
			if (!EmitDrawObjects && !ShowText)
				return;

			Brush brush = isBuy ? BuyTextBrush : SellTextBrush;

			if (EmitDrawObjects)
			{
				string tag = ArrowTag(isBuy, absBar);
				if (isBuy)	Draw.ArrowUp  (this, tag, true, b, Low[b]  - 2 * TickSize, brush);
				else		Draw.ArrowDown(this, tag, true, b, High[b] + 2 * TickSize, brush);
				liveTags.Add(tag);
			}

			if (ShowText)
			{
				// [B] The vendor selected these brushes on `colorState != 3` inside a branch that
				// required `== 3`, so the text was always white and UPTICK/DOWNTICK were dead.
				string tag = TextTag(isBuy, absBar);
				double y   = rising ? Low[b] : High[b];
				Draw.Text(this, tag, isBuy ? "BUY" : "SELL", b, y, brush).YPixelOffset =
					TextPixelOffset * (isBuy ? -1 : 1);
				liveTags.Add(tag);
			}
		}

		// [C2] The vendor called RemoveDrawObject on every bar of the rewrite span regardless of
		// whether anything had been drawn there. Only remove what we know exists.
		private void ClearSignal(bool isBuy, int absBar)
		{
			string arrow = ArrowTag(isBuy, absBar);
			if (liveTags.Remove(arrow))
				RemoveDrawObject(arrow);

			string text = TextTag(isBuy, absBar);
			if (liveTags.Remove(text))
				RemoveDrawObject(text);
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
