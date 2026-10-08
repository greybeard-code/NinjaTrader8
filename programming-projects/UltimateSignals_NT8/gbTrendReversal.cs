#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// Phase 1 of the port described in ToS_TrendReversal_Port_Plan.md: the base "Trend Reversal"
// thinkScript study (https://usethinkscript.com/threads/trend-reversal-for-thinkorswim.183/),
// three EMAs (9/14/21) driving a latched buy/sell state, ported to NinjaScript with a capturable
// signal contract. It exists to fix what UltimateSignals could not be fixed for: its BUY and SELL
// arrows share one brush, so PredatorX/Infinity (which key on drawing-object tag/colour, not
// plots) can never isolate the sell side. Here the two sides get distinct, user-set brushes and
// stable per-side tags by construction -- see UltimateSignals_Review.md §6.
//
// This base engine does NOT repaint. buy/sell/stopbuy/stopsell are pure functions of Close/High/
// Low up to and including the current (already-closed) bar, so once a bar closes under
// Calculate.OnBarClose its BuyTrigger/SellTrigger value is final -- no confirmation delay is
// needed here. That is a real difference from UltimateSignals, whose ZigZag layer is inherently
// forward-looking (a pivot isn't confirmed until price reverses past it) and does repaint. Ported
// ZigZag/Stochastic/MACD tiers (Phase 2/3) will need their own repaint handling when added.
namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public class gbTrendReversal : Indicator
	{
		private EMA superfastMa, fastMa, slowMa;

		// Latched state (1 = active, 0 = not), mirrors the source's CompoundValue: turns on when
		// the raw condition fires (and isn't already stopped), turns off only when the matching
		// stop condition clears it -- NOT simply when the raw condition goes false on its own.
		private Series<double> buyState, sellState;

		// Raw (unlatched) buy/sell conditions, kept as Series so the transition test reads a value
		// that rewinds with NT8's historical recalculation rather than a stale field.
		private Series<double> buyCondSeries, sellCondSeries;

		[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.0";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Superfast Length", Order = 0, GroupName = "1. Moving Averages")]
		public int SuperfastLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Fast Length", Order = 1, GroupName = "1. Moving Averages")]
		public int FastLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Slow Length", Order = 2, GroupName = "1. Moving Averages")]
		public int SlowLength
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Arrows", Order = 0, GroupName = "2. Signal Output")]
		public bool ShowArrows
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Arrow Offset (ticks)", Order = 1, GroupName = "2. Signal Output")]
		public int ArrowOffsetTicks
		{ get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Arrow Brush", Order = 2, GroupName = "2. Signal Output",
			Description = "MUST differ from Sell Arrow Brush -- that difference is what makes the sell side capturable by a colour-keyed consumer like PredatorX.")]
		public Brush BuyBrush
		{ get; set; }

		[Browsable(false)]
		public string BuyBrushSerialize
		{
			get { return Serialize.BrushToString(BuyBrush); }
			set { BuyBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Arrow Brush", Order = 3, GroupName = "2. Signal Output",
			Description = "MUST differ from Buy Arrow Brush.")]
		public Brush SellBrush
		{ get; set; }

		[Browsable(false)]
		public string SellBrushSerialize
		{
			get { return Serialize.BrushToString(SellBrush); }
			set { SellBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Stop Lines", Order = 0, GroupName = "3. Stop Lines",
			Description = "Not verified against the original thinkScript's exact stop-line formula (unavailable from the source thread) -- this rides Superfast (EMA 9) as a defensible stand-in for the level implied by the entry conditions (low > EMA9 for a buy, high < EMA9 for a sell). Treat as an approximation, not a faithful port.")]
		public bool ShowStopLines
		{ get; set; }

		[XmlIgnore]
		[Display(Name = "Stop Line (Long) Brush", Order = 1, GroupName = "3. Stop Lines")]
		public Brush StopLineLongBrush
		{ get; set; }

		[Browsable(false)]
		public string StopLineLongBrushSerialize
		{
			get { return Serialize.BrushToString(StopLineLongBrush); }
			set { StopLineLongBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Stop Line (Short) Brush", Order = 2, GroupName = "3. Stop Lines")]
		public Brush StopLineShortBrush
		{ get; set; }

		[Browsable(false)]
		public string StopLineShortBrushSerialize
		{
			get { return Serialize.BrushToString(StopLineShortBrush); }
			set { StopLineShortBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Color Bars", Order = 0, GroupName = "4. Bar Coloring",
			Description = "Matches the source: green while the long state is active, red while the short state is active, plum while neither.")]
		public bool ColorBars
		{ get; set; }

		[XmlIgnore]
		[Display(Name = "Neutral Bar Brush", Order = 1, GroupName = "4. Bar Coloring")]
		public Brush NeutralBarBrush
		{ get; set; }

		[Browsable(false)]
		public string NeutralBarBrushSerialize
		{
			get { return Serialize.BrushToString(NeutralBarBrush); }
			set { NeutralBarBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Alerts Enabled", Order = 0, GroupName = "5. Alerts")]
		public bool AlertsEnabled
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Buy Sound File", Order = 1, GroupName = "5. Alerts")]
		public string AlertSoundBuy
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Sell Sound File", Order = 2, GroupName = "5. Alerts")]
		public string AlertSoundSell
		{ get; set; }

		// The plots a condition builder should key on: 1 only on the bar the state transitions
		// from inactive to active (a fresh signal), 0 every other bar. This is the same 1/0-not-
		// NaN, single-brush-per-side contract gbUltimateSignalsBridge.cs re-emits UltimateSignals
		// as -- built in from the start here instead of bolted on afterward.
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BuyTrigger => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SellTrigger => Values[1];

		// Continuous state for bar coloring, external filters, or a strategy that wants "currently
		// long-biased" rather than "just fired" -- +1 while the long state is active, -1 while
		// short, 0 while neither.
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> TrendState => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StopLineLong => Values[3];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StopLineShort => Values[4];

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description								= @"GreyBeard port of the ToS Trend Reversal study (3-EMA latched buy/sell state), with a colour- and tag-capturable signal contract PredatorX/Infinity can act on. See ToS_TrendReversal_Port_Plan.md.";
				Name									= "gbTrendReversal";
				Calculate								= Calculate.OnBarClose;
				IsOverlay								= true;
				DisplayInDataBox						= true;
				DrawOnPricePanel						= true;
				PaintPriceMarkers						= false;
				IsSuspendedWhileInactive				= false;
				ShowTransparentPlotsInDataBox			= true;

				AddPlot(Brushes.Transparent, "BuyTrigger");
				AddPlot(Brushes.Transparent, "SellTrigger");
				AddPlot(Brushes.Transparent, "TrendState");
				AddPlot(Brushes.Transparent, "StopLineLong");
				AddPlot(Brushes.Transparent, "StopLineShort");

				SuperfastLength							= 9;
				FastLength								= 14;
				SlowLength								= 21;
				ShowArrows								= true;
				ArrowOffsetTicks						= 2;
				BuyBrush								= Brushes.Lime;
				SellBrush								= Brushes.Red;
				ShowStopLines							= true;
				StopLineLongBrush						= Brushes.LightGreen;
				StopLineShortBrush						= Brushes.Salmon;
				ColorBars								= true;
				NeutralBarBrush							= Brushes.Plum;
				AlertsEnabled							= false;
				AlertSoundBuy							= @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav";
				AlertSoundSell							= @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav";
			}
			else if (State == State.DataLoaded)
			{
				superfastMa	= EMA(Close, SuperfastLength);
				fastMa		= EMA(Close, FastLength);
				slowMa		= EMA(Close, SlowLength);

				buyState		= new Series<double>(this);
				sellState		= new Series<double>(this);
				buyCondSeries	= new Series<double>(this);
				sellCondSeries	= new Series<double>(this);

				if (!(SuperfastLength < FastLength && FastLength < SlowLength))
					Print(string.Format("[gbTrendReversal] WARNING: lengths are not strictly increasing (Superfast={0}, Fast={1}, Slow={2}) -- the buy/sell conditions require Superfast < Fast < Slow to ever fire.", SuperfastLength, FastLength, SlowLength));

				if (BuyBrush != null && SellBrush != null && BuyBrush.ToString() == SellBrush.ToString())
					Print("[gbTrendReversal] WARNING: Buy Arrow Brush and Sell Arrow Brush are the same colour -- a colour-keyed consumer (e.g. PredatorX ShortColorEntrySignal) will not be able to tell the two sides apart. This is the exact UltimateSignals defect this indicator exists to avoid.");
			}
		}

		protected override void OnBarUpdate()
		{
			// EMA seeding differs from ToS (NT8 seeds from the first bar), so early values diverge.
			// Wait for all three MAs to be past their own warm-up before evaluating conditions.
			if (CurrentBar < SlowLength)
			{
				// Seed every state series, not just the plots -- the first evaluated bar reads
				// [1] off all four and would otherwise hit an unset value.
				buyState[0] = 0;
				sellState[0] = 0;
				buyCondSeries[0] = 0;
				sellCondSeries[0] = 0;
				BuyTrigger[0] = 0;
				SellTrigger[0] = 0;
				TrendState[0] = 0;
				StopLineLong[0] = double.NaN;
				StopLineShort[0] = double.NaN;
				return;
			}

			double ma9 = superfastMa[0];
			double ma14 = fastMa[0];
			double ma21 = slowMa[0];

			bool buyCond	= ma9 > ma14 && ma14 > ma21 && Low[0]  > ma9;
			bool sellCond	= ma9 < ma14 && ma14 < ma21 && High[0] < ma9;
			bool stopBuy	= ma9 <= ma14;
			bool stopSell	= ma9 >= ma14;

			// Raw conditions are held in Series, not plain fields, so they rewind correctly when
			// NT8 recalculates historical bars.
			buyCondSeries[0]  = buyCond  ? 1 : 0;
			sellCondSeries[0] = sellCond ? 1 : 0;

			double prevBuy      = CurrentBar >= 1 ? buyState[1]      : 0;
			double prevSell     = CurrentBar >= 1 ? sellState[1]     : 0;
			double prevBuyCond  = CurrentBar >= 1 ? buyCondSeries[1]  : 0;
			double prevSellCond = CurrentBar >= 1 ? sellCondSeries[1] : 0;

			// The source latches on the TRANSITION into the condition (buynow = buy and not buy[1]),
			// not on the condition merely holding. Using the level instead would re-arm a state that
			// had been stopped out while the raw condition was still true -- reachable at warm-up,
			// where the state seeds to 0 while buy/sell may already be true.
			bool buyNow  = buyCond  && prevBuyCond  == 0;
			bool sellNow = sellCond && prevSellCond == 0;

			double newBuy  = buyNow  && !stopBuy  ? 1 : (prevBuy  == 1 && stopBuy  ? 0 : prevBuy);
			double newSell = sellNow && !stopSell ? 1 : (prevSell == 1 && stopSell ? 0 : prevSell);

			buyState[0]  = newBuy;
			sellState[0] = newSell;

			bool buyTriggered  = newBuy  == 1 && prevBuy  != 1;
			bool sellTriggered = newSell == 1 && prevSell != 1;

			BuyTrigger[0]  = buyTriggered  ? 1 : 0;
			SellTrigger[0] = sellTriggered ? 1 : 0;
			TrendState[0]  = newBuy == 1 ? 1 : (newSell == 1 ? -1 : 0);
			StopLineLong[0]  = ShowStopLines && newBuy  == 1 ? ma9 : double.NaN;
			StopLineShort[0] = ShowStopLines && newSell == 1 ? ma9 : double.NaN;

			if (ColorBars)
				BarBrush = newBuy == 1 ? BuyBrush : (newSell == 1 ? SellBrush : NeutralBarBrush);

			// Stable, side-distinct tags -- PredatorX's LongSignalTag1/ShortSignalTag1 (or
			// Infinity's tag rules) can match a fixed prefix, unlike UltimateSignals' churning
			// addText/removeText tags.
			if (ShowArrows)
			{
				if (buyTriggered)
					Draw.ArrowUp(this, "GTR_BUY_" + CurrentBar, false, 0, Low[0] - ArrowOffsetTicks * TickSize, BuyBrush);
				if (sellTriggered)
					Draw.ArrowDown(this, "GTR_SELL_" + CurrentBar, false, 0, High[0] + ArrowOffsetTicks * TickSize, SellBrush);
			}

			if (AlertsEnabled)
			{
				if (buyTriggered)
					Alert("gbTrendReversalBuy", Priority.Medium, "gbTrendReversal BUY", AlertSoundBuy, 10, Brushes.Black, BuyBrush);
				if (sellTriggered)
					Alert("gbTrendReversalSell", Priority.Medium, "gbTrendReversal SELL", AlertSoundSell, 10, Brushes.Black, SellBrush);
			}
		}
	}
}
