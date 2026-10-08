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

//  gbUltimateSignalsBridge v1.0.0
//  ---------------------------------------------------------------------------
//  Makes an uncapturable vendor signal capturable.
//
//  THE PROBLEM
//  PredatorX Order Entry matches an external indicator's signals by drawing-object
//  TAG and/or COLOUR (LongSignalTag1 / ShortSignalTag1, LongColorEntrySignal1 /
//  ShortColorEntrySignal1). UltimateSignals paints its BUY arrow and its SELL
//  arrow in the SAME brush — magenta #FF00FF, measured on both — so a colour rule
//  for the short side also matches every long arrow. The two sides cannot be told
//  apart by any consumer that keys on colour. Same story for a condition builder
//  keying on a plot the vendor never registered.
//
//  THE FIX
//  Read the two sides where they ARE distinct — the vendor's separate Series — and
//  re-emit them as:
//    * two clean PLOTS, BuySignal / SellSignal, carrying 1 or 0 (never NaN, which
//      most condition builders cannot express), and
//    * two arrows in DISTINCT, configurable colours with STABLE, side-distinct
//      tags (GB_US_BUY_* / GB_US_SELL_*) that PredatorX can lock onto.
//
//  Point PredatorX at THIS indicator instead of at UltimateSignals:
//    DD_Entry1_SignalSource = gbUltimateSignalsBridge
//    LongSignalTag1  = GB_US_BUY        ShortSignalTag1  = GB_US_SELL
//    LongColorEntrySignal1 = <Buy Arrow Brush>   ShortColorEntrySignal1 = <Sell Arrow Brush>
//  ...and keep the two brushes different. That is the whole point.
//
//  LATE-BOUND: no compile-time reference to the vendor assembly, so this compiles
//  and loads on an install where UltimateSignals is absent or unlicensed. It
//  resolves the source through ChartControl.Indicators by name.
//
//  SETUP: add UltimateSignals to the chart FIRST, then this bridge on the same
//  chart and series. Run gbSignalProbe once to confirm Buy/Sell Plot Index — the
//  defaults (7 and 8) are inferred from assembly metadata, NOT verified at runtime.
//  ---------------------------------------------------------------------------

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public class gbUltimateSignalsBridge : Indicator
	{
		private NinjaTrader.Gui.NinjaScript.IndicatorRenderBase	source;
		private bool	resolveFailureLogged;
		private int		lastBuyBar	= -1;
		private int		lastSellBar	= -1;

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Source Indicator", Description = "Substring of the source indicator's name on this chart.", GroupName = "1. Source", Order = 0)]
		public string SourceIndicator { get; set; }

		[NinjaScriptProperty]
		[Range(0, 63)]
		[Display(Name = "Buy Plot Index", Description = "Index into the source indicator's Values[] that carries the BUY signal. Confirm with gbSignalProbe before trading.", GroupName = "1. Source", Order = 1)]
		public int BuyPlotIndex { get; set; }

		[NinjaScriptProperty]
		[Range(0, 63)]
		[Display(Name = "Sell Plot Index", Description = "Index into the source indicator's Values[] that carries the SELL signal. Confirm with gbSignalProbe before trading.", GroupName = "1. Source", Order = 2)]
		public int SellPlotIndex { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Idle Is NaN", Description = "ON: the source series is NaN when idle (the usual NT8 idiom). OFF: it is 0 when idle. gbSignalProbe reports which.", GroupName = "1. Source", Order = 3)]
		public bool IdleIsNaN { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Confirmation Bars", Description = "Re-read the signal this many bars after it first printed and only emit if it is still there. Defends against the source revising signals away. 0 = emit immediately.", GroupName = "2. Signal", Order = 0)]
		public int ConfirmationBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Draw Arrows", Description = "Draw the re-coloured, stably-tagged arrows. Required for PredatorX tag/colour capture; turn off if you only consume the plots.", GroupName = "2. Signal", Order = 1)]
		public bool DrawArrows { get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Arrow Brush", Description = "MUST differ from the sell brush — that difference is what makes the side capturable.", GroupName = "3. Colours", Order = 0)]
		public Brush BuyBrush { get; set; }

		[Browsable(false)]
		public string BuyBrushSerialize
		{
			get { return Serialize.BrushToString(BuyBrush); }
			set { BuyBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Arrow Brush", Description = "MUST differ from the buy brush.", GroupName = "3. Colours", Order = 1)]
		public Brush SellBrush { get; set; }

		[Browsable(false)]
		public string SellBrushSerialize
		{
			get { return Serialize.BrushToString(SellBrush); }
			set { SellBrush = Serialize.StringToBrush(value); }
		}

		// GreyBeard developer block — read-only, informational, never serialized.
		[Display(Name = "Author", GroupName = "0. Developer", Order = 0)]
		public string Author => "GreyBeard";

		[Display(Name = "Version", GroupName = "0. Developer", Order = 1)]
		public string Version => "1.0.0";

		[Display(Name = "Website", GroupName = "0. Developer", Order = 2)]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		// Clean, named, always-numeric plots. 1 = signal, 0 = no signal.
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BuySignal  { get { return Values[0]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SellSignal { get { return Values[1]; } }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "gbUltimateSignalsBridge";
				Description						= "Re-emits a vendor indicator's buy/sell signals as distinctly coloured, stably tagged, capturable signals.";
				Calculate						= Calculate.OnBarClose;
				IsOverlay						= true;
				DisplayInDataBox				= true;
				DrawOnPricePanel				= true;
				IsSuspendedWhileInactive		= false;
				ShowTransparentPlotsInDataBox	= true;
				PaintPriceMarkers				= false;

				SourceIndicator					= "UltimateSignals";
				BuyPlotIndex					= 7;		// buyTextMarker  (metadata order — CONFIRM with gbSignalProbe)
				SellPlotIndex					= 8;		// sellTextMarker (metadata order — CONFIRM with gbSignalProbe)
				IdleIsNaN						= true;
				ConfirmationBars				= 1;
				DrawArrows						= true;

				BuyBrush						= Brushes.Lime;
				SellBrush						= Brushes.Red;

				// Plot names are what a condition builder lists. Keep them unmistakable.
				AddPlot(Brushes.Transparent, "BuySignal");
				AddPlot(Brushes.Transparent, "SellSignal");
			}
		}

		protected override void OnBarUpdate()
		{
			BuySignal[0]  = 0;
			SellSignal[0] = 0;

			if (CurrentBar < ConfirmationBars + 1)
				return;

			if (source == null)
			{
				ResolveSource();
				if (source == null)
					return;
			}

			// Re-read the signal ConfirmationBars after it first printed. If the source
			// revised it away in the meantime, this reads idle and nothing is emitted.
			int barsAgo = ConfirmationBars;

			bool buy  = IsActive(BuyPlotIndex,  barsAgo);
			bool sell = IsActive(SellPlotIndex, barsAgo);

			// A source that somehow asserts both sides on one bar is telling us the plot
			// map is wrong. Emit neither rather than a coin-flip.
			if (buy && sell)
			{
				buy = sell = false;
				Print(string.Format("gbUltimateSignalsBridge: bar {0} asserted BOTH sides — check Buy/Sell Plot Index against gbSignalProbe.", CurrentBar - barsAgo));
			}

			if (buy)
			{
				BuySignal[0] = 1;
				EmitArrow(true, barsAgo);
			}

			if (sell)
			{
				SellSignal[0] = 1;
				EmitArrow(false, barsAgo);
			}
		}

		private bool IsActive(int plotIndex, int barsAgo)
		{
			try
			{
				if (source.Values == null || plotIndex >= source.Values.Length)
					return false;

				Series<double> s = source.Values[plotIndex];
				int bar = CurrentBar - barsAgo;

				if (s == null || bar < 0 || !s.IsValidDataPointAt(bar))
					return false;

				double v = s.GetValueAt(bar);

				return IdleIsNaN ? !double.IsNaN(v)
								 : (!double.IsNaN(v) && Math.Abs(v) > 1e-9);
			}
			catch
			{
				return false;
			}
		}

		// Stable, side-distinct tags. PredatorX matches on the tag string, so the
		// prefix must never change and must never be shared between the two sides.
		private void EmitArrow(bool isBuy, int barsAgo)
		{
			if (!DrawArrows)
				return;

			int bar = CurrentBar - barsAgo;

			if (isBuy)
			{
				if (bar == lastBuyBar) return;
				lastBuyBar = bar;
				Draw.ArrowUp(this, "GB_US_BUY_" + bar, true, barsAgo,
					Low[barsAgo] - 2 * TickSize, BuyBrush);
			}
			else
			{
				if (bar == lastSellBar) return;
				lastSellBar = bar;
				Draw.ArrowDown(this, "GB_US_SELL_" + bar, true, barsAgo,
					High[barsAgo] + 2 * TickSize, SellBrush);
			}
		}

		private void ResolveSource()
		{
			if (string.IsNullOrEmpty(SourceIndicator) || ChartControl == null)
				return;

			try
			{
				foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase ind in ChartControl.Indicators)
				{
					if (ind == null || object.ReferenceEquals(ind, this))
						continue;

					string n = ind.Name ?? string.Empty;
					if (n.IndexOf(SourceIndicator, StringComparison.OrdinalIgnoreCase) < 0)
						continue;

					source = ind;
					Print("gbUltimateSignalsBridge: source resolved -> " + n);
					return;
				}
			}
			catch { }

			if (!resolveFailureLogged && CurrentBar > 10)
			{
				resolveFailureLogged = true;
				Print("gbUltimateSignalsBridge: no indicator on this chart matches '" + SourceIndicator
					+ "'. Add the source indicator to this chart first.");
			}
		}
	}
}
