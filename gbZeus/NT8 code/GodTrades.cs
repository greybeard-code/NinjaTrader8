#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum GodTradesEarlyTouchHandling
	{
		StopLineImmediately,
		IgnoreTouchesUntilValid
	}

	public enum GodTradesValidTouchBehavior
	{
		StopLineOnly,
		StopLineAndMarkContinuation
	}

	public enum GodTradesContinuationConfirmationMode
	{
		TouchOnly,
		RequireCloseBeyondLine,
		RequireCloseBeyondFullZone
	}

	public enum GodTradesLinePriceMode
	{
		Midpoint,
		PreviousCloseEdge,
		CurrentOpenEdge
	}

	public enum GodTradesTargetMode
	{
		None,
		OppositeBollingerBand,
		FixedTicks
	}

	public enum GodTradesFcBollingerLocationSource
	{
		Close,
		WickExtreme,
		HLC3,
		BodyMidpoint
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class GodTrades : Indicator
	{
		private ATR rangeFilterAtr;
		// v16.6: bug fix — IsSuspendedWhileInactive now false. With true, NT8 pauses
		//        the indicator while its window is inactive/minimized, stalling live
		//        signals (VPS failure mode). Property help added to all sections.
		// v16.5: ATR-relative huge-candle filter (BG). When enabled, the max gap-bar
		//        range = ATR(period) x multiple, self-scaling across chart types and
		//        volatility. Overrides the fixed-tick cap while on.
		// v16.4: zoom-independent label spacing via pixel offset (tick offsets
		//        scale with vertical zoom; pixels do not). Label = tick offset
		//        anchor + SignalLabelPixelOffset pixels beyond, per direction.
		// v16.3: marker/label spacing fixed (defaults 6 / 20 ticks), label font size
		//        configurable, touch-dot offset option. Colors were already per-type
		//        in "08. Brushes"; labels keep using their signal's brush.
		// v16.2: optional on-chart version label, bottom-right (toggle in "00. About").
		// v16.1: renamed to plain "GodTrades"; version now shown as a read-only
		//        field at the top of the Properties dialog instead of on-chart.
		// v16.0 changes vs GodTrades15:
		//   1. MinimumBodyTicks / MaximumGapBarRangeTicks moved from gap-line
		//      creation to the Bollinger Gap signal only. Dojis and huge candles
		//      are a Trade-1 entry ban, not a reason to skip tracking the gap —
		//      the line must still exist for Fill & Reverse / Fill & Continue.
		//   2. BullishGapTouched / BearishGapTouched now emit ONLY on valid
		//      (>= MinimumBarsBeforeValid) touches. Early fills emit solely on
		//      InvalidGapTouched, so no execution wire can trade them.
		//   3. Default MinimumBodyTicks = 4 (doji guard on by default).
		private const string VERSION = "v16.6";

		private class GapInfo
		{
			public int		Direction;		// 1 = bullish gap-up, -1 = bearish gap-down
			public int		CreationBar;
			public double	PreviousClose;
			public double	CurrentOpen;
			public double	ZoneLow;
			public double	ZoneHigh;
			public double	LinePrice;
			public bool		IsValid;
			public bool		Touched;
			public bool		InvalidTooEarly;
			public string	TagBase;
			public string	LineTag;
			public string	ZoneTag;
		}

		private class PendingContinuation
		{
			public GapInfo	Gap;
			public int		TouchBar;
		}

		private List<GapInfo> activeGaps;
		private List<PendingContinuation> pendingContinuations;
		private Bollinger bollinger;
		private int signalDrawCounter;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "GodTrades";
				Description					= "GodTrades gap engine " + VERSION + ": gap lines, BG signals (doji + ATR huge-candle filtered), confirmed FC signals, valid-only touch plots, spiderweb warning, per-section property help.";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DrawOnPricePanel			= true;
				DisplayInDataBox			= true;
				ShowTransparentPlotsInDataBox = true;
				PaintPriceMarkers			= false;
				IsAutoScale					= false;
				IsSuspendedWhileInactive	= false;	// v16.6: signal engine must keep calculating when the window is inactive
				BarsRequiredToPlot			= 20;

				ShowVersionLabel			= true;

				MinimumGapSizeTicks			= 1;
				MinimumBarsBeforeValid		= 3;
				MinimumBodyTicks			= 4;
				MaximumGapBarRangeTicks		= 0;
				UseAtrRangeFilter			= true;
				RangeFilterAtrPeriod		= 14;
				RangeFilterAtrMultiple		= 2.5;
				MaximumActiveGapsToTrack	= 300;

				EarlyTouchHandling			= GodTradesEarlyTouchHandling.StopLineImmediately;
				ValidTouchBehavior			= GodTradesValidTouchBehavior.StopLineAndMarkContinuation;

				EnableContinuationSignals	= true;
				UseBollingerMidpointFilterForContinuation = true;
				FcBollingerLocationSource	= GodTradesFcBollingerLocationSource.WickExtreme;
				FcLongBelowMidpointPercent	= 50.0;
				FcShortAboveMidpointPercent	= 50.0;
				ContinuationConfirmationMode = GodTradesContinuationConfirmationMode.RequireCloseBeyondFullZone;
				ConfirmationBarsAfterTouch	= 2;
				RequireSignalCandleDirection = true;
				RequireCorrectContinuationApproach = true;

				LinePriceMode				= GodTradesLinePriceMode.Midpoint;
				ShowGapLine					= true;
				ShowGapZone					= false;
				ShowTouchMarker				= true;
				ShowContinuationMarkers		= true;
				ShowBollingerGapMarkers		= true;
				ShowSignalLabels			= true;
				UseTouchedLineColor			= false;
				GapLineWidth				= 2;
				GapLineStyle				= DashStyleHelper.Solid;
				ZoneOpacity					= 12;
				SignalMarkerOffsetTicks		= 6;
				SignalLabelOffsetTicks		= 20;
				SignalLabelFontSize			= 12;
				SignalLabelPixelOffset		= 18;
				TouchMarkerOffsetTicks		= 0;

				EnableBollingerGapSignals	= true;
				BollingerPeriod				= 20;
				BollingerStdDev				= 2.0;
				BollingerBandProximityTicks	= 8;

				EnableSpiderwebWarning		= true;
				ShowSpiderwebWarningText	= true;
				SpiderwebDistanceTicks		= 100;
				SpiderwebLineCount			= 5;
				SpiderwebTextFontSize		= 15;

				UseSignalTimeFilter			= false;
				SignalStartTime				= 101500;
				SignalEndTime				= 150000;

				SuggestedStopOffsetTicks		= 0;
				TargetMode					= GodTradesTargetMode.OppositeBollingerBand;
				FixedTargetTicks			= 40;

				BullishGapLineBrush			= Brushes.White;
				BearishGapLineBrush			= Brushes.White;
				TouchedGapLineBrush			= Brushes.Gray;
				InvalidGapBrush				= Brushes.Red;
				BullishZoneBrush			= Brushes.DodgerBlue;
				BearishZoneBrush			= Brushes.Magenta;
				TouchMarkerBrush			= Brushes.Gold;

				BollingerLongBrush			= Brushes.Lime;
				BollingerShortBrush			= Brushes.Red;
				ContinuationLongBrush		= Brushes.LimeGreen;
				ContinuationShortBrush		= Brushes.OrangeRed;
				SpiderwebWarningBrush		= Brushes.Orange;

				AddPlot(Brushes.Transparent, "BullishGapFormed");
				AddPlot(Brushes.Transparent, "BearishGapFormed");
				AddPlot(Brushes.Transparent, "BullishGapTouched");
				AddPlot(Brushes.Transparent, "BearishGapTouched");
				AddPlot(Brushes.Transparent, "InvalidGapTouched");

				AddPlot(Brushes.Transparent, "ContinuationLong");
				AddPlot(Brushes.Transparent, "ContinuationShort");

				AddPlot(Brushes.Transparent, "BollingerGapLong");
				AddPlot(Brushes.Transparent, "BollingerGapShort");

				AddPlot(Brushes.Transparent, "ActiveGapCount");
				AddPlot(Brushes.Transparent, "ValidActiveGapCount");
				AddPlot(Brushes.Transparent, "PendingContinuationCount");
				AddPlot(Brushes.Transparent, "SpiderwebCount");
				AddPlot(Brushes.Transparent, "SpiderwebWarning");
				AddPlot(Brushes.Transparent, "NearestGapPrice");

				AddPlot(Brushes.Transparent, "SuggestedEntryPrice");
				AddPlot(Brushes.Transparent, "SuggestedStopPrice");
				AddPlot(Brushes.Transparent, "SuggestedTargetPrice");

				AddPlot(Brushes.Transparent, "SignalDirection");
				AddPlot(Brushes.Transparent, "SignalCode");
				AddPlot(Brushes.Transparent, "MasterLongSignal");
				AddPlot(Brushes.Transparent, "MasterShortSignal");
			}
			else if (State == State.DataLoaded)
			{
				rangeFilterAtr = ATR(RangeFilterAtrPeriod);
				activeGaps = new List<GapInfo>();
				pendingContinuations = new List<PendingContinuation>();
				bollinger = Bollinger(BollingerStdDev, BollingerPeriod);
				signalDrawCounter = 0;
			}
		}

		protected override void OnBarUpdate()
		{
			ResetPlotValues();

			if (CurrentBar < 1)
				return;

			UpdatePendingContinuations();
			UpdateActiveGaps();
			DetectNewGap();
			TrimOldestActiveGaps();
			UpdateContextPlots();

			if (ShowVersionLabel)
				Draw.TextFixed(this, "GodTrades_Ver", "GodTrades " + VERSION, TextPosition.BottomRight);
			else
				RemoveDrawObject("GodTrades_Ver");
		}

		private void ResetPlotValues()
		{
			BullishGapFormed[0]			= 0;
			BearishGapFormed[0]			= 0;
			BullishGapTouched[0]		= 0;
			BearishGapTouched[0]		= 0;
			InvalidGapTouched[0]		= 0;

			ContinuationLong[0]			= 0;
			ContinuationShort[0]		= 0;

			BollingerGapLong[0]			= 0;
			BollingerGapShort[0]		= 0;

			ActiveGapCount[0]			= 0;
			ValidActiveGapCount[0]		= 0;
			PendingContinuationCount[0]	= 0;
			SpiderwebCount[0]			= 0;
			SpiderwebWarning[0]			= 0;
			NearestGapPrice[0]			= 0;

			SuggestedEntryPrice[0]		= 0;
			SuggestedStopPrice[0]		= 0;
			SuggestedTargetPrice[0]		= 0;

			SignalDirection[0]			= 0;
			SignalCode[0]				= 0;
			MasterLongSignal[0]			= 0;
			MasterShortSignal[0]		= 0;
		}

		private void DetectNewGap()
		{
			bool previousBullish = Close[1] > Open[1];
			bool currentBullish  = Close[0] > Open[0];

			bool previousBearish = Close[1] < Open[1];
			bool currentBearish  = Close[0] < Open[0];

			// v16: body/range filters no longer gate gap-line creation. They are
			// Bollinger Gap (Trade 1) entry rules only — a doji or huge candle
			// still leaves a valid gap line behind for Fill & Reverse / Continue.

			bool bullishGap =
				previousBullish &&
				currentBullish &&
				Open[0] > Close[1] &&
				((Open[0] - Close[1]) / TickSize) >= MinimumGapSizeTicks;

			bool bearishGap =
				previousBearish &&
				currentBearish &&
				Open[0] < Close[1] &&
				((Close[1] - Open[0]) / TickSize) >= MinimumGapSizeTicks;

			if (bullishGap)
			{
				CreateGap(1, Close[1], Open[0]);
				BullishGapFormed[0] = 1;
				MarkBollingerGapIfNeeded(1);
			}
			else if (bearishGap)
			{
				CreateGap(-1, Open[0], Close[1]);
				BearishGapFormed[0] = -1;
				MarkBollingerGapIfNeeded(-1);
			}
		}

		private bool PassesBodyFilter()
		{
			if (MinimumBodyTicks <= 0)
				return true;

			double previousBodyTicks = Math.Abs(Close[1] - Open[1]) / TickSize;
			double currentBodyTicks  = Math.Abs(Close[0] - Open[0]) / TickSize;

			return previousBodyTicks >= MinimumBodyTicks && currentBodyTicks >= MinimumBodyTicks;
		}

		private bool PassesGapBarRangeFilter()
		{
			double currentRangeTicks = (High[0] - Low[0]) / TickSize;

			if (UseAtrRangeFilter)
			{
				if (rangeFilterAtr == null || CurrentBar < RangeFilterAtrPeriod)
					return true;

				double limitTicks = rangeFilterAtr[0] * RangeFilterAtrMultiple / TickSize;
				return currentRangeTicks <= limitTicks;
			}

			if (MaximumGapBarRangeTicks <= 0)
				return true;

			return currentRangeTicks <= MaximumGapBarRangeTicks;
		}

		private void CreateGap(int direction, double zoneLow, double zoneHigh)
		{
			GapInfo gap = new GapInfo();

			gap.Direction		= direction;
			gap.CreationBar		= CurrentBar;
			gap.PreviousClose	= Close[1];
			gap.CurrentOpen		= Open[0];
			gap.ZoneLow			= Math.Min(zoneLow, zoneHigh);
			gap.ZoneHigh		= Math.Max(zoneLow, zoneHigh);
			gap.LinePrice		= GetLinePrice(gap);
			gap.IsValid			= false;
			gap.Touched			= false;
			gap.InvalidTooEarly	= false;
			gap.TagBase			= "GodTrades_Gap_" + CurrentBar + "_" + (direction > 0 ? "Bull" : "Bear");
			gap.LineTag			= gap.TagBase + "_Line";
			gap.ZoneTag			= gap.TagBase + "_Zone";

			activeGaps.Add(gap);
			DrawGap(gap, false, false);
		}

		private double GetLinePrice(GapInfo gap)
		{
			if (LinePriceMode == GodTradesLinePriceMode.PreviousCloseEdge)
				return gap.PreviousClose;

			if (LinePriceMode == GodTradesLinePriceMode.CurrentOpenEdge)
				return gap.CurrentOpen;

			return (gap.ZoneLow + gap.ZoneHigh) * 0.5;
		}

		private void UpdateActiveGaps()
		{
			if (activeGaps == null || activeGaps.Count == 0)
				return;

			for (int i = activeGaps.Count - 1; i >= 0; i--)
			{
				GapInfo gap = activeGaps[i];

				int age = CurrentBar - gap.CreationBar;
				gap.IsValid = age >= MinimumBarsBeforeValid;

				bool shouldCheckTouch = CurrentBar > gap.CreationBar;

				if (EarlyTouchHandling == GodTradesEarlyTouchHandling.IgnoreTouchesUntilValid && !gap.IsValid)
					shouldCheckTouch = false;

				bool touched = shouldCheckTouch && BarOverlapsZone(gap);

				if (touched)
				{
					gap.Touched = true;
					gap.InvalidTooEarly = !gap.IsValid;

					if (gap.InvalidTooEarly)
					{
						// v16: too-early fills no longer emit on the touch plots.
						// They only mark InvalidGapTouched, so nothing wired to
						// BullishGapTouched / BearishGapTouched can trade them.
						InvalidGapTouched[0] = gap.Direction;
						DrawGap(gap, true, true);
					}
					else
					{
						if (gap.Direction > 0)
							BullishGapTouched[0] = 1;
						else
							BearishGapTouched[0] = -1;

						DrawGap(gap, true, false);
					}

					if (ShowTouchMarker)
					{
						double dotPrice = gap.LinePrice + (gap.Direction > 0 ? -1 : 1) * TouchMarkerOffsetTicks * TickSize;
						Draw.Dot(this, gap.TagBase + "_Touch_" + CurrentBar, false, 0, dotPrice, TouchMarkerBrush);
					}

					if (gap.IsValid)
						CreatePendingContinuationAndEvaluate(gap);

					activeGaps.RemoveAt(i);
				}
				else
				{
					DrawGap(gap, false, false);
				}
			}
		}

		private bool BarOverlapsZone(GapInfo gap)
		{
			return High[0] >= gap.ZoneLow && Low[0] <= gap.ZoneHigh;
		}

		private void DrawGap(GapInfo gap, bool touched, bool invalid)
		{
			int startBarsAgo = CurrentBar - gap.CreationBar;
			Brush lineBrush = GetLineBrush(gap, touched, invalid);

			if (ShowGapLine)
			{
				Draw.Line(
					this,
					gap.LineTag,
					false,
					startBarsAgo,
					gap.LinePrice,
					0,
					gap.LinePrice,
					lineBrush,
					GapLineStyle,
					GapLineWidth);
			}
			else
			{
				RemoveDrawObject(gap.LineTag);
			}

			if (ShowGapZone)
			{
				Draw.Rectangle(
					this,
					gap.ZoneTag,
					false,
					startBarsAgo,
					gap.ZoneHigh,
					0,
					gap.ZoneLow,
					Brushes.Transparent,
					invalid ? InvalidGapBrush : GetZoneBrush(gap),
					ZoneOpacity);
			}
			else
			{
				RemoveDrawObject(gap.ZoneTag);
			}
		}

		private Brush GetLineBrush(GapInfo gap, bool touched, bool invalid)
		{
			if (invalid)
				return InvalidGapBrush;

			if (touched && UseTouchedLineColor)
				return TouchedGapLineBrush;

			return gap.Direction > 0 ? BullishGapLineBrush : BearishGapLineBrush;
		}

		private Brush GetZoneBrush(GapInfo gap)
		{
			return gap.Direction > 0 ? BullishZoneBrush : BearishZoneBrush;
		}

		private void CreatePendingContinuationAndEvaluate(GapInfo gap)
		{
			if (!EnableContinuationSignals)
				return;

			if (ValidTouchBehavior != GodTradesValidTouchBehavior.StopLineAndMarkContinuation)
				return;

			if (!IsSignalTimeAllowed())
				return;

			if (!PassesContinuationApproach(gap))
				return;

			PendingContinuation pending = new PendingContinuation();
			pending.Gap = gap;
			pending.TouchBar = CurrentBar;

			bool signaled = EvaluatePendingContinuation(pending);

			if (!signaled && ConfirmationBarsAfterTouch > 0)
				pendingContinuations.Add(pending);
		}

		private bool PassesContinuationApproach(GapInfo gap)
		{
			if (!RequireCorrectContinuationApproach)
				return true;

			if (gap.Direction > 0)
				return Close[1] >= gap.ZoneHigh || Open[0] >= gap.ZoneHigh;

			return Close[1] <= gap.ZoneLow || Open[0] <= gap.ZoneLow;
		}

		private void UpdatePendingContinuations()
		{
			if (pendingContinuations == null || pendingContinuations.Count == 0)
				return;

			for (int i = pendingContinuations.Count - 1; i >= 0; i--)
			{
				PendingContinuation pending = pendingContinuations[i];
				int barsSinceTouch = CurrentBar - pending.TouchBar;

				if (barsSinceTouch <= 0)
					continue;

				if (barsSinceTouch > ConfirmationBarsAfterTouch)
				{
					pendingContinuations.RemoveAt(i);
					continue;
				}

				bool signaled = EvaluatePendingContinuation(pending);

				if (signaled)
					pendingContinuations.RemoveAt(i);
			}
		}

		private bool EvaluatePendingContinuation(PendingContinuation pending)
		{
			if (pending == null || pending.Gap == null)
				return false;

			if (!IsSignalTimeAllowed())
				return false;

			if (pending.Gap.Direction > 0)
			{
				if (IsLongContinuation(pending.Gap))
				{
					SetLongContinuationSignal();
					return true;
				}
			}
			else
			{
				if (IsShortContinuation(pending.Gap))
				{
					SetShortContinuationSignal();
					return true;
				}
			}

			return false;
		}

		private double GetFcBollingerTestPrice(int direction)
		{
			if (FcBollingerLocationSource == GodTradesFcBollingerLocationSource.Close)
				return Close[0];

			if (FcBollingerLocationSource == GodTradesFcBollingerLocationSource.HLC3)
				return (High[0] + Low[0] + Close[0]) / 3.0;

			if (FcBollingerLocationSource == GodTradesFcBollingerLocationSource.BodyMidpoint)
				return (Open[0] + Close[0]) / 2.0;

			// WickExtreme default:
			// Long FC checks Low. Short FC checks High.
			return direction > 0 ? Low[0] : High[0];
		}

		private bool PassesBollingerMidpointContinuationFilter(int direction)
		{
			if (!UseBollingerMidpointFilterForContinuation)
				return true;

			if (bollinger == null || CurrentBar < BollingerPeriod)
				return false;

			double middle = bollinger.Middle[0];
			double upper  = bollinger.Upper[0];
			double lower  = bollinger.Lower[0];
			double testPrice = GetFcBollingerTestPrice(direction);

			if (direction > 0)
			{
				double pct = FcLongBelowMidpointPercent / 100.0;
				double threshold = middle - ((middle - lower) * pct);

				return testPrice <= threshold;
			}
			else
			{
				double pct = FcShortAboveMidpointPercent / 100.0;
				double threshold = middle + ((upper - middle) * pct);

				return testPrice >= threshold;
			}
		}

		private bool IsLongContinuation(GapInfo gap)
		{
			if (RequireSignalCandleDirection && Close[0] <= Open[0])
				return false;

			if (!PassesBollingerMidpointContinuationFilter(1))
				return false;

			if (ContinuationConfirmationMode == GodTradesContinuationConfirmationMode.TouchOnly)
				return true;

			if (ContinuationConfirmationMode == GodTradesContinuationConfirmationMode.RequireCloseBeyondLine)
				return Close[0] >= gap.LinePrice;

			return Close[0] >= gap.ZoneHigh;
		}

		private bool IsShortContinuation(GapInfo gap)
		{
			if (RequireSignalCandleDirection && Close[0] >= Open[0])
				return false;

			if (!PassesBollingerMidpointContinuationFilter(-1))
				return false;

			if (ContinuationConfirmationMode == GodTradesContinuationConfirmationMode.TouchOnly)
				return true;

			if (ContinuationConfirmationMode == GodTradesContinuationConfirmationMode.RequireCloseBeyondLine)
				return Close[0] <= gap.LinePrice;

			return Close[0] <= gap.ZoneLow;
		}

		private void SetLongContinuationSignal()
		{
			ContinuationLong[0] = 1;
			SignalDirection[0] = 1;
			SignalCode[0] = 2;
			MasterLongSignal[0] = 1;
			SetSuggestedPrices(1);

			if (ShowContinuationMarkers)
			{
				string tag = "GodTrades_CONT_Long_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = Low[0] - SignalMarkerOffsetTicks * TickSize;

				Draw.TriangleUp(this, tag, false, 0, markerPrice, ContinuationLongBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", false, "FC", 0, Low[0] - SignalLabelOffsetTicks * TickSize, SignalLabelPixelOffset, ContinuationLongBrush, new Gui.Tools.SimpleFont("Arial", SignalLabelFontSize), System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}
		}

		private void SetShortContinuationSignal()
		{
			ContinuationShort[0] = -1;
			SignalDirection[0] = -1;
			SignalCode[0] = -2;
			MasterShortSignal[0] = -1;
			SetSuggestedPrices(-1);

			if (ShowContinuationMarkers)
			{
				string tag = "GodTrades_CONT_Short_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = High[0] + SignalMarkerOffsetTicks * TickSize;

				Draw.TriangleDown(this, tag, false, 0, markerPrice, ContinuationShortBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", false, "FC", 0, High[0] + SignalLabelOffsetTicks * TickSize, -SignalLabelPixelOffset, ContinuationShortBrush, new Gui.Tools.SimpleFont("Arial", SignalLabelFontSize), System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}
		}

		private void MarkBollingerGapIfNeeded(int direction)
		{
			if (!EnableBollingerGapSignals)
				return;

			if (!IsSignalTimeAllowed())
				return;

			// v16: doji / huge-candle rules apply here (BG entries), not to line creation.
			if (!PassesBodyFilter())
				return;

			if (!PassesGapBarRangeFilter())
				return;

			if (bollinger == null || CurrentBar < BollingerPeriod + 1)
				return;

			double proximity = BollingerBandProximityTicks * TickSize;

			if (direction > 0)
			{
				bool nearLowerBand =
					Low[1] <= bollinger.Lower[1] + proximity &&
					Low[0] <= bollinger.Lower[0] + proximity;

				if (nearLowerBand)
				{
					BollingerGapLong[0] = 1;
					MasterLongSignal[0] = 1;
					SignalDirection[0] = 1;
					SignalCode[0] = 1;
					SetSuggestedPrices(1);

					if (ShowBollingerGapMarkers)
						DrawBollingerMarker(1);
				}
			}
			else
			{
				bool nearUpperBand =
					High[1] >= bollinger.Upper[1] - proximity &&
					High[0] >= bollinger.Upper[0] - proximity;

				if (nearUpperBand)
				{
					BollingerGapShort[0] = -1;
					MasterShortSignal[0] = -1;
					SignalDirection[0] = -1;
					SignalCode[0] = -1;
					SetSuggestedPrices(-1);

					if (ShowBollingerGapMarkers)
						DrawBollingerMarker(-1);
				}
			}
		}

		private void DrawBollingerMarker(int direction)
		{
			if (direction > 0)
			{
				string tag = "GodTrades_BG_Long_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = Low[0] - SignalMarkerOffsetTicks * TickSize;

				Draw.ArrowUp(this, tag, false, 0, markerPrice, BollingerLongBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", false, "BG", 0, Low[0] - SignalLabelOffsetTicks * TickSize, SignalLabelPixelOffset, BollingerLongBrush, new Gui.Tools.SimpleFont("Arial", SignalLabelFontSize), System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}
			else
			{
				string tag = "GodTrades_BG_Short_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = High[0] + SignalMarkerOffsetTicks * TickSize;

				Draw.ArrowDown(this, tag, false, 0, markerPrice, BollingerShortBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", false, "BG", 0, High[0] + SignalLabelOffsetTicks * TickSize, -SignalLabelPixelOffset, BollingerShortBrush, new Gui.Tools.SimpleFont("Arial", SignalLabelFontSize), System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}
		}

		private void SetSuggestedPrices(int direction)
		{
			SuggestedEntryPrice[0] = Close[0];

			if (direction > 0)
			{
				SuggestedStopPrice[0] = Low[0] - SuggestedStopOffsetTicks * TickSize;

				if (TargetMode == GodTradesTargetMode.OppositeBollingerBand && bollinger != null && CurrentBar >= BollingerPeriod)
					SuggestedTargetPrice[0] = bollinger.Upper[0];
				else if (TargetMode == GodTradesTargetMode.FixedTicks)
					SuggestedTargetPrice[0] = Close[0] + FixedTargetTicks * TickSize;
				else
					SuggestedTargetPrice[0] = 0;
			}
			else
			{
				SuggestedStopPrice[0] = High[0] + SuggestedStopOffsetTicks * TickSize;

				if (TargetMode == GodTradesTargetMode.OppositeBollingerBand && bollinger != null && CurrentBar >= BollingerPeriod)
					SuggestedTargetPrice[0] = bollinger.Lower[0];
				else if (TargetMode == GodTradesTargetMode.FixedTicks)
					SuggestedTargetPrice[0] = Close[0] - FixedTargetTicks * TickSize;
				else
					SuggestedTargetPrice[0] = 0;
			}
		}

		private bool IsSignalTimeAllowed()
		{
			if (!UseSignalTimeFilter)
				return true;

			int t = ToTime(Time[0]);

			if (SignalStartTime <= SignalEndTime)
				return t >= SignalStartTime && t <= SignalEndTime;

			return t >= SignalStartTime || t <= SignalEndTime;
		}

		private void TrimOldestActiveGaps()
		{
			if (activeGaps == null)
				return;

			while (activeGaps.Count > MaximumActiveGapsToTrack)
			{
				GapInfo oldest = activeGaps[0];
				DrawGap(oldest, false, false);
				activeGaps.RemoveAt(0);
			}
		}

		private void UpdateContextPlots()
		{
			int activeCount = activeGaps == null ? 0 : activeGaps.Count;
			int pendingCount = pendingContinuations == null ? 0 : pendingContinuations.Count;

			ActiveGapCount[0] = activeCount;
			PendingContinuationCount[0] = pendingCount;

			if (activeGaps == null || activeGaps.Count == 0)
			{
				ValidActiveGapCount[0] = 0;
				SpiderwebCount[0] = 0;
				SpiderwebWarning[0] = 0;
				NearestGapPrice[0] = 0;
				RemoveDrawObject("GodTrades_SpiderwebWarning");
				return;
			}

			double nearestDistance = double.MaxValue;
			double nearestPrice = 0;
			int validActiveCount = 0;
			int spiderwebCount = 0;

			foreach (GapInfo gap in activeGaps)
			{
				int age = CurrentBar - gap.CreationBar;
				bool valid = age >= MinimumBarsBeforeValid;

				if (valid)
					validActiveCount++;

				double distance = DistanceFromPriceToZone(Close[0], gap);
				double distanceTicks = distance / TickSize;

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestPrice = gap.LinePrice;
				}

				if (valid && distanceTicks <= SpiderwebDistanceTicks)
					spiderwebCount++;
			}

			ValidActiveGapCount[0] = validActiveCount;
			SpiderwebCount[0] = spiderwebCount;
			NearestGapPrice[0] = nearestPrice;

			bool warning = EnableSpiderwebWarning && spiderwebCount >= SpiderwebLineCount;
			SpiderwebWarning[0] = warning ? 1 : 0;

			if (warning && ShowSpiderwebWarningText)
			{
				string msg = "SPIDERWEB WARNING\n" + spiderwebCount + " valid gaps within " + SpiderwebDistanceTicks + " ticks";

				Draw.TextFixed(
					this,
					"GodTrades_SpiderwebWarning",
					msg,
					TextPosition.TopRight,
					SpiderwebWarningBrush,
					new SimpleFont("Arial", SpiderwebTextFontSize),
					Brushes.Transparent,
					Brushes.Transparent,
					0);
			}
			else
			{
				RemoveDrawObject("GodTrades_SpiderwebWarning");
			}
		}

		private double DistanceFromPriceToZone(double price, GapInfo gap)
		{
			if (price >= gap.ZoneLow && price <= gap.ZoneHigh)
				return 0;

			if (price < gap.ZoneLow)
				return gap.ZoneLow - price;

			return price - gap.ZoneHigh;
		}

		#region Inputs

		[XmlIgnore]
		[ReadOnly(true)]
		[Display(Name = "Version", Description = "GodTrades build version.", GroupName = "00. About", Order = 1)]
		public string Version
		{ get { return VERSION; } set { } }

		[Display(Name = "Show version label on chart", Description = "Draws the GodTrades version at the bottom-right of the chart.", GroupName = "00. About", Order = 2)]
		public bool ShowVersionLabel { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Gap Size Ticks", Description = "Smallest close-to-open gap (in ticks) that creates a gap line. Raise on minute charts to skip clock-boundary micro-gaps (tick charts: 1, 3-min: 3).", GroupName = "01. Gap Detection", Order = 1)]
		public int MinimumGapSizeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Bars Before Valid", Description = "A line must survive this many candles unfilled before a fill counts. Course rule: 3. Earlier fills are marked invalid (red) and never signal.", GroupName = "01. Gap Detection", Order = 2)]
		public int MinimumBarsBeforeValid { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Body Ticks (BG doji filter)", Description = "Bollinger Gap entries only: both candles of the pattern need at least this body. 0 disables. Gap lines are always created regardless.", GroupName = "01. Gap Detection", Order = 3)]
		public int MinimumBodyTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Maximum Gap Bar Range Ticks (BG huge-candle filter)", Description = "Bollinger Gap entries only: skip the entry if the signal candle range exceeds this. 0 disables. Gap lines are always created regardless.", GroupName = "01. Gap Detection", Order = 4)]
		public int MaximumGapBarRangeTicks { get; set; }

		[Display(Name = "Use ATR Range Filter (BG)", Description = "ON: huge-candle cap = ATR x multiple, self-scaling with the chart. Overrides the fixed tick cap above.", GroupName = "01. Gap Detection", Order = 5)]
		public bool UseAtrRangeFilter { get; set; }

		[Range(2, 100)]
		[Display(Name = "Range Filter ATR Period", Description = "ATR lookback used by the ATR range filter.", GroupName = "01. Gap Detection", Order = 6)]
		public int RangeFilterAtrPeriod { get; set; }

		[Range(0.5, 10.0)]
		[Display(Name = "Range Filter ATR Multiple", Description = "Signal candle range above ATR x this = huge candle, BG entry skipped. Course-typical: 2.0 - 3.0.", GroupName = "01. Gap Detection", Order = 7)]
		public double RangeFilterAtrMultiple { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3000)]
		[Display(Name = "Maximum Active Gaps To Track", Description = "Oldest lines are dropped beyond this count. Performance guard; rarely needs changing.", GroupName = "01. Gap Detection", Order = 5)]
		public int MaximumActiveGapsToTrack { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Early Touch Handling", Description = "What happens when a line is filled BEFORE it is valid (younger than Minimum Bars Before Valid). StopLineImmediately kills the line and marks it invalid.", GroupName = "02. Touch Logic", Order = 1)]
		public GodTradesEarlyTouchHandling EarlyTouchHandling { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Valid Touch Behavior", Description = "What happens when a VALID line is filled. StopLineAndMarkContinuation kills the line and opens the FC confirmation window.", GroupName = "02. Touch Logic", Order = 2)]
		public GodTradesValidTouchBehavior ValidTouchBehavior { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Continuation Signals", Description = "Master switch for FC (gap-fill) signals. OFF = lines still draw and touch plots still fire, but no FC entries.", GroupName = "03. Continuation Signals", Order = 1)]
		public bool EnableContinuationSignals { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Bollinger Midpoint Filter For Continuation", Description = "ON: an FC only confirms if the candle sits in the correct part of the Bollinger envelope (see the two percent settings). OFF: any location confirms.", GroupName = "03. Continuation Signals", Order = 2)]
		public bool UseBollingerMidpointFilterForContinuation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "FC Bollinger Location Source", Description = "Which price is tested against the envelope: WickExtreme = the candle's low (longs) / high (shorts); alternatives use close or body.", GroupName = "03. Continuation Signals", Order = 3)]
		public GodTradesFcBollingerLocationSource FcBollingerLocationSource { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Long Below Midpoint Percent", Description = "Long FC requires selected price source below midpoint by this percent of the distance from middle to lower band.", GroupName = "03. Continuation Signals", Order = 4)]
		public double FcLongBelowMidpointPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Short Above Midpoint Percent", Description = "Short FC requires selected price source above midpoint by this percent of the distance from middle to upper band.", GroupName = "03. Continuation Signals", Order = 5)]
		public double FcShortAboveMidpointPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Continuation Confirmation Mode", Description = "How decisively the candle must reject the gap zone. RequireCloseBeyondFullZone = close past the entire zone (course reading of 'fill then reverse').", GroupName = "03. Continuation Signals", Order = 6)]
		public GodTradesContinuationConfirmationMode ContinuationConfirmationMode { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Confirmation Bars After Touch", Description = "After a valid fill, the touch bar plus this many candles may confirm. No qualifying close in the window = setup silently expires. Course: 2.", GroupName = "03. Continuation Signals", Order = 7)]
		public int ConfirmationBarsAfterTouch { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Signal Candle Direction", Description = "Long signals require bullish candle; short signals require bearish candle.", GroupName = "03. Continuation Signals", Order = 8)]
		public bool RequireSignalCandleDirection { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Correct Continuation Approach", Description = "Bullish gaps must be touched from above; bearish gaps must be touched from below.", GroupName = "03. Continuation Signals", Order = 9)]
		public bool RequireCorrectContinuationApproach { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Signal Time Filter", Description = "Gates BG/FC signals to a time window inside the indicator. Leave OFF when PredatorX owns the session window (single source of truth).", GroupName = "03. Continuation Signals", Order = 10)]
		public bool UseSignalTimeFilter { get; set; }

		[NinjaScriptProperty]
		[Range(0, 235959)]
		[Display(Name = "Signal Start Time HHmmss", Description = "Window start (chart timezone, HHmmss: 101500 = 10:15:00). Only used when the signal time filter is ON.", GroupName = "03. Continuation Signals", Order = 11)]
		public int SignalStartTime { get; set; }

		[NinjaScriptProperty]
		[Range(0, 235959)]
		[Display(Name = "Signal End Time HHmmss", Description = "Window end (chart timezone, HHmmss: 150000 = 15:00:00). Only used when the signal time filter is ON.", GroupName = "03. Continuation Signals", Order = 12)]
		public int SignalEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Bollinger Gap Signals", Description = "Master switch for BG (Trade 1) signals: two same-color candles with a gap between them, at the outer band.", GroupName = "04. Bollinger Gap", Order = 1)]
		public bool EnableBollingerGapSignals { get; set; }

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Bollinger Period", Description = "Period of the bands used by the BG proximity test and FC location filter. Keep identical to PredatorBollinger (exit indicator).", GroupName = "04. Bollinger Gap", Order = 2)]
		public int BollingerPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Bollinger Std Dev", Description = "Standard deviations of the bands. Course: 20 / 2. Keep identical to PredatorBollinger.", GroupName = "04. Bollinger Gap", Order = 3)]
		public double BollingerStdDev { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Bollinger Band Proximity Ticks", Description = "Both BG candles' extremes must be within this many ticks of the outer band ('touching or VERY NEAR'). 1000-tick: 8; 3-min: ~14.", GroupName = "04. Bollinger Gap", Order = 4)]
		public int BollingerBandProximityTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Spiderweb Warning", Description = "Counts gap lines clustered near price and raises SpiderwebWarning when too many stack ('use EXTREME CAUTION').", GroupName = "05. Spiderweb", Order = 1)]
		public bool EnableSpiderwebWarning { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Spiderweb Warning Text", Description = "Draws the warning text on the chart when a spiderweb is detected.", GroupName = "05. Spiderweb", Order = 2)]
		public bool ShowSpiderwebWarningText { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Distance Ticks", Description = "Lines within this distance of price count toward the spiderweb.", GroupName = "05. Spiderweb", Order = 3)]
		public int SpiderwebDistanceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Line Count", Description = "This many lines inside the distance = spiderweb (warning plot goes to 1).", GroupName = "05. Spiderweb", Order = 4)]
		public int SpiderwebLineCount { get; set; }

		[NinjaScriptProperty]
		[Range(6, 60)]
		[Display(Name = "Spiderweb Text Font Size", Description = "Font size of the on-chart spiderweb warning.", GroupName = "05. Spiderweb", Order = 5)]
		public int SpiderwebTextFontSize { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Suggested Stop Offset Ticks", Description = "Extra ticks added beyond the signal candle extreme in the SuggestedStopPrice plot. Data Box information only - PredatorX does not read it.", GroupName = "06. Trade Prices", Order = 1)]
		public int SuggestedStopOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Target Mode", Description = "How SuggestedTargetPrice is computed (opposite band or fixed ticks). Data Box information only.", GroupName = "06. Trade Prices", Order = 2)]
		public GodTradesTargetMode TargetMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fixed Target Ticks", Description = "Target distance when Target Mode = fixed ticks. Data Box information only.", GroupName = "06. Trade Prices", Order = 3)]
		public int FixedTargetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Line Price Mode", Description = "Where the gap line is drawn inside the gap zone (midpoint or edge).", GroupName = "07. Visuals", Order = 1)]
		public GodTradesLinePriceMode LinePriceMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Gap Line", Description = "Draws the extending white line for each tracked gap.", GroupName = "07. Visuals", Order = 2)]
		public bool ShowGapLine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Gap Zone", Description = "Shades the full gap zone (between the two candles) instead of just the line.", GroupName = "07. Visuals", Order = 3)]
		public bool ShowGapZone { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Touch Marker", Description = "Draws the dot where a line gets filled.", GroupName = "07. Visuals", Order = 4)]
		public bool ShowTouchMarker { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Continuation Markers", Description = "Draws the FC triangles.", GroupName = "07. Visuals", Order = 5)]
		public bool ShowContinuationMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Bollinger Gap Markers", Description = "Draws the BG arrows.", GroupName = "07. Visuals", Order = 6)]
		public bool ShowBollingerGapMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Signal Labels", Description = "Draws the FC / BG text next to the markers.", GroupName = "07. Visuals", Order = 7)]
		public bool ShowSignalLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Touched Line Color", Description = "Recolors a line with the touched brush once filled, instead of leaving the original color.", GroupName = "07. Visuals", Order = 8)]
		public bool UseTouchedLineColor { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Gap Line Width", Description = "Line width in pixels.", GroupName = "07. Visuals", Order = 9)]
		public int GapLineWidth { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Gap Line Style", Description = "Solid / dash style of gap lines.", GroupName = "07. Visuals", Order = 10)]
		public DashStyleHelper GapLineStyle { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Zone Opacity", Description = "Opacity (0-100) of the shaded gap zone.", GroupName = "07. Visuals", Order = 11)]
		public int ZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Signal Marker Offset Ticks", Description = "Distance of the triangle/arrow from the candle extreme, in ticks (price-based, scales with zoom).", GroupName = "07. Visuals", Order = 12)]
		public int SignalMarkerOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Signal Label Offset Ticks", Description = "Tick-based anchor distance of the FC/BG text. The pixel offset below is added on top and does not scale with zoom.", GroupName = "07. Visuals", Order = 13)]
		public int SignalLabelOffsetTicks { get; set; }

		[Range(6, 40)]
		[Display(Name = "Signal Label Font Size", Description = "Font size of the FC / BG text labels.", GroupName = "07. Visuals", Order = 22)]
		public int SignalLabelFontSize { get; set; }

		[Range(-200, 200)]
		[Display(Name = "Signal Label Pixel Offset", Description = "Extra label distance in SCREEN PIXELS (zoom-independent). Applied beyond the tick offset, away from the candle. If labels land on the wrong side on your setup, flip the sign.", GroupName = "07. Visuals", Order = 24)]
		public int SignalLabelPixelOffset { get; set; }

		[Range(0, 100)]
		[Display(Name = "Touch Marker Offset Ticks", Description = "Moves the touch dot away from the line (0 = on the line, current behavior).", GroupName = "07. Visuals", Order = 23)]
		public int TouchMarkerOffsetTicks { get; set; }

		#endregion

		#region Brushes

		[XmlIgnore]
		[Display(Name = "Bullish Gap Line Brush", Description = "Line color for bullish (gap-up) lines.", GroupName = "08. Brushes", Order = 1)]
		public Brush BullishGapLineBrush { get; set; }

		[Browsable(false)]
		public string BullishGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(BullishGapLineBrush); }
			set { BullishGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bearish Gap Line Brush", Description = "Line color for bearish (gap-down) lines.", GroupName = "08. Brushes", Order = 2)]
		public Brush BearishGapLineBrush { get; set; }

		[Browsable(false)]
		public string BearishGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(BearishGapLineBrush); }
			set { BearishGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Touched Gap Line Brush", Description = "Line color after a valid fill (when Use Touched Line Color is ON).", GroupName = "08. Brushes", Order = 3)]
		public Brush TouchedGapLineBrush { get; set; }

		[Browsable(false)]
		public string TouchedGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(TouchedGapLineBrush); }
			set { TouchedGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Invalid Gap Color", Description = "Line color for gaps filled too early (before Minimum Bars Before Valid).", GroupName = "08. Brushes", Order = 4)]
		public Brush InvalidGapBrush { get; set; }

		[Browsable(false)]
		public string InvalidGapBrushSerializable
		{
			get { return Serialize.BrushToString(InvalidGapBrush); }
			set { InvalidGapBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bullish Zone Brush", Description = "Zone shading for bullish gaps.", GroupName = "08. Brushes", Order = 5)]
		public Brush BullishZoneBrush { get; set; }

		[Browsable(false)]
		public string BullishZoneBrushSerializable
		{
			get { return Serialize.BrushToString(BullishZoneBrush); }
			set { BullishZoneBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bearish Zone Brush", Description = "Zone shading for bearish gaps.", GroupName = "08. Brushes", Order = 6)]
		public Brush BearishZoneBrush { get; set; }

		[Browsable(false)]
		public string BearishZoneBrushSerializable
		{
			get { return Serialize.BrushToString(BearishZoneBrush); }
			set { BearishZoneBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Touch Marker Brush", Description = "Color of the fill dot.", GroupName = "08. Brushes", Order = 7)]
		public Brush TouchMarkerBrush { get; set; }

		[Browsable(false)]
		public string TouchMarkerBrushSerializable
		{
			get { return Serialize.BrushToString(TouchMarkerBrush); }
			set { TouchMarkerBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bollinger Long Brush", Description = "BG long arrow + label color.", GroupName = "08. Brushes", Order = 8)]
		public Brush BollingerLongBrush { get; set; }

		[Browsable(false)]
		public string BollingerLongBrushSerializable
		{
			get { return Serialize.BrushToString(BollingerLongBrush); }
			set { BollingerLongBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bollinger Short Brush", Description = "BG short arrow + label color.", GroupName = "08. Brushes", Order = 9)]
		public Brush BollingerShortBrush { get; set; }

		[Browsable(false)]
		public string BollingerShortBrushSerializable
		{
			get { return Serialize.BrushToString(BollingerShortBrush); }
			set { BollingerShortBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Continuation Long Brush", Description = "FC long triangle + label color.", GroupName = "08. Brushes", Order = 10)]
		public Brush ContinuationLongBrush { get; set; }

		[Browsable(false)]
		public string ContinuationLongBrushSerializable
		{
			get { return Serialize.BrushToString(ContinuationLongBrush); }
			set { ContinuationLongBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Continuation Short Brush", Description = "FC short triangle + label color.", GroupName = "08. Brushes", Order = 11)]
		public Brush ContinuationShortBrush { get; set; }

		[Browsable(false)]
		public string ContinuationShortBrushSerializable
		{
			get { return Serialize.BrushToString(ContinuationShortBrush); }
			set { ContinuationShortBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Spiderweb Warning Brush", Description = "Color of the spiderweb warning text.", GroupName = "08. Brushes", Order = 12)]
		public Brush SpiderwebWarningBrush { get; set; }

		[Browsable(false)]
		public string SpiderwebWarningBrushSerializable
		{
			get { return Serialize.BrushToString(SpiderwebWarningBrush); }
			set { SpiderwebWarningBrush = Serialize.StringToBrush(value); }
		}

		#endregion

		#region Data Box Outputs

		[Browsable(false)] [XmlIgnore] public Series<double> BullishGapFormed			{ get { return Values[0]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BearishGapFormed			{ get { return Values[1]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BullishGapTouched			{ get { return Values[2]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BearishGapTouched			{ get { return Values[3]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> InvalidGapTouched			{ get { return Values[4]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> ContinuationLong			{ get { return Values[5]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> ContinuationShort			{ get { return Values[6]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> BollingerGapLong			{ get { return Values[7]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BollingerGapShort			{ get { return Values[8]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> ActiveGapCount				{ get { return Values[9]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> ValidActiveGapCount		{ get { return Values[10]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PendingContinuationCount	{ get { return Values[11]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SpiderwebCount				{ get { return Values[12]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SpiderwebWarning			{ get { return Values[13]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> NearestGapPrice			{ get { return Values[14]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedEntryPrice		{ get { return Values[15]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedStopPrice			{ get { return Values[16]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedTargetPrice		{ get { return Values[17]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> SignalDirection			{ get { return Values[18]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SignalCode					{ get { return Values[19]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> MasterLongSignal			{ get { return Values[20]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> MasterShortSignal			{ get { return Values[21]; } }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GodTrades[] cacheGodTrades;
		public GodTrades GodTrades(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return GodTrades(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public GodTrades GodTrades(ISeries<double> input, int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			if (cacheGodTrades != null)
				for (int idx = 0; idx < cacheGodTrades.Length; idx++)
					if (cacheGodTrades[idx] != null && cacheGodTrades[idx].MinimumGapSizeTicks == minimumGapSizeTicks && cacheGodTrades[idx].MinimumBarsBeforeValid == minimumBarsBeforeValid && cacheGodTrades[idx].MinimumBodyTicks == minimumBodyTicks && cacheGodTrades[idx].MaximumGapBarRangeTicks == maximumGapBarRangeTicks && cacheGodTrades[idx].MaximumActiveGapsToTrack == maximumActiveGapsToTrack && cacheGodTrades[idx].EarlyTouchHandling == earlyTouchHandling && cacheGodTrades[idx].ValidTouchBehavior == validTouchBehavior && cacheGodTrades[idx].EnableContinuationSignals == enableContinuationSignals && cacheGodTrades[idx].UseBollingerMidpointFilterForContinuation == useBollingerMidpointFilterForContinuation && cacheGodTrades[idx].FcBollingerLocationSource == fcBollingerLocationSource && cacheGodTrades[idx].FcLongBelowMidpointPercent == fcLongBelowMidpointPercent && cacheGodTrades[idx].FcShortAboveMidpointPercent == fcShortAboveMidpointPercent && cacheGodTrades[idx].ContinuationConfirmationMode == continuationConfirmationMode && cacheGodTrades[idx].ConfirmationBarsAfterTouch == confirmationBarsAfterTouch && cacheGodTrades[idx].RequireSignalCandleDirection == requireSignalCandleDirection && cacheGodTrades[idx].RequireCorrectContinuationApproach == requireCorrectContinuationApproach && cacheGodTrades[idx].UseSignalTimeFilter == useSignalTimeFilter && cacheGodTrades[idx].SignalStartTime == signalStartTime && cacheGodTrades[idx].SignalEndTime == signalEndTime && cacheGodTrades[idx].EnableBollingerGapSignals == enableBollingerGapSignals && cacheGodTrades[idx].BollingerPeriod == bollingerPeriod && cacheGodTrades[idx].BollingerStdDev == bollingerStdDev && cacheGodTrades[idx].BollingerBandProximityTicks == bollingerBandProximityTicks && cacheGodTrades[idx].EnableSpiderwebWarning == enableSpiderwebWarning && cacheGodTrades[idx].ShowSpiderwebWarningText == showSpiderwebWarningText && cacheGodTrades[idx].SpiderwebDistanceTicks == spiderwebDistanceTicks && cacheGodTrades[idx].SpiderwebLineCount == spiderwebLineCount && cacheGodTrades[idx].SpiderwebTextFontSize == spiderwebTextFontSize && cacheGodTrades[idx].SuggestedStopOffsetTicks == suggestedStopOffsetTicks && cacheGodTrades[idx].TargetMode == targetMode && cacheGodTrades[idx].FixedTargetTicks == fixedTargetTicks && cacheGodTrades[idx].LinePriceMode == linePriceMode && cacheGodTrades[idx].ShowGapLine == showGapLine && cacheGodTrades[idx].ShowGapZone == showGapZone && cacheGodTrades[idx].ShowTouchMarker == showTouchMarker && cacheGodTrades[idx].ShowContinuationMarkers == showContinuationMarkers && cacheGodTrades[idx].ShowBollingerGapMarkers == showBollingerGapMarkers && cacheGodTrades[idx].ShowSignalLabels == showSignalLabels && cacheGodTrades[idx].UseTouchedLineColor == useTouchedLineColor && cacheGodTrades[idx].GapLineWidth == gapLineWidth && cacheGodTrades[idx].GapLineStyle == gapLineStyle && cacheGodTrades[idx].ZoneOpacity == zoneOpacity && cacheGodTrades[idx].SignalMarkerOffsetTicks == signalMarkerOffsetTicks && cacheGodTrades[idx].SignalLabelOffsetTicks == signalLabelOffsetTicks && cacheGodTrades[idx].EqualsInput(input))
						return cacheGodTrades[idx];
			return CacheIndicator<GodTrades>(new GodTrades(){ MinimumGapSizeTicks = minimumGapSizeTicks, MinimumBarsBeforeValid = minimumBarsBeforeValid, MinimumBodyTicks = minimumBodyTicks, MaximumGapBarRangeTicks = maximumGapBarRangeTicks, MaximumActiveGapsToTrack = maximumActiveGapsToTrack, EarlyTouchHandling = earlyTouchHandling, ValidTouchBehavior = validTouchBehavior, EnableContinuationSignals = enableContinuationSignals, UseBollingerMidpointFilterForContinuation = useBollingerMidpointFilterForContinuation, FcBollingerLocationSource = fcBollingerLocationSource, FcLongBelowMidpointPercent = fcLongBelowMidpointPercent, FcShortAboveMidpointPercent = fcShortAboveMidpointPercent, ContinuationConfirmationMode = continuationConfirmationMode, ConfirmationBarsAfterTouch = confirmationBarsAfterTouch, RequireSignalCandleDirection = requireSignalCandleDirection, RequireCorrectContinuationApproach = requireCorrectContinuationApproach, UseSignalTimeFilter = useSignalTimeFilter, SignalStartTime = signalStartTime, SignalEndTime = signalEndTime, EnableBollingerGapSignals = enableBollingerGapSignals, BollingerPeriod = bollingerPeriod, BollingerStdDev = bollingerStdDev, BollingerBandProximityTicks = bollingerBandProximityTicks, EnableSpiderwebWarning = enableSpiderwebWarning, ShowSpiderwebWarningText = showSpiderwebWarningText, SpiderwebDistanceTicks = spiderwebDistanceTicks, SpiderwebLineCount = spiderwebLineCount, SpiderwebTextFontSize = spiderwebTextFontSize, SuggestedStopOffsetTicks = suggestedStopOffsetTicks, TargetMode = targetMode, FixedTargetTicks = fixedTargetTicks, LinePriceMode = linePriceMode, ShowGapLine = showGapLine, ShowGapZone = showGapZone, ShowTouchMarker = showTouchMarker, ShowContinuationMarkers = showContinuationMarkers, ShowBollingerGapMarkers = showBollingerGapMarkers, ShowSignalLabels = showSignalLabels, UseTouchedLineColor = useTouchedLineColor, GapLineWidth = gapLineWidth, GapLineStyle = gapLineStyle, ZoneOpacity = zoneOpacity, SignalMarkerOffsetTicks = signalMarkerOffsetTicks, SignalLabelOffsetTicks = signalLabelOffsetTicks }, input, ref cacheGodTrades);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GodTrades GodTrades(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public Indicators.GodTrades GodTrades(ISeries<double> input , int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades(input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GodTrades GodTrades(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public Indicators.GodTrades GodTrades(ISeries<double> input , int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTradesEarlyTouchHandling earlyTouchHandling, GodTradesValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTradesFcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTradesContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTradesTargetMode targetMode, int fixedTargetTicks, GodTradesLinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades(input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}
	}
}

#endregion
