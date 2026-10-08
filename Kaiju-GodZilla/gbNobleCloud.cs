#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

public enum gbNobleCloud_MAType
{
	DEMA,
	EMA,
	HMA,
	LinReg,
	SMA,
	TEMA,
	TMA,
	VWMA,
	WMA,
	WilderMA,
	ZLEMA
}

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	[CategoryOrder("Developer",  0)]
	[CategoryOrder("Parameters", 1000005)]
	[CategoryOrder("General",    1000010)]
	[CategoryOrder("Graphics",   1000020)]
	[CategoryOrder("Gradient",   1000030)]
	[CategoryOrder("Alerts",     1000040)]
	[CategoryOrder("Special",    1000060)]
	[CategoryOrder("Critical",   1000070)]
	public class gbNobleCloud : Indicator
	{
		// ── Alerts ────────────────────────────────────────────────────────────────

		[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Marker: Color Bullish", Order = 31, GroupName = "Alerts")]
		public Brush MarkerBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerBrushBullish_Serialize
		{
			get { return Serialize.BrushToString(this.MarkerBrushBullish); }
			set { this.MarkerBrushBullish = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Marker: Color Bearish", Order = 32, GroupName = "Alerts")]
		public Brush MarkerBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerBrushBearish_Serialize
		{
			get { return Serialize.BrushToString(this.MarkerBrushBearish); }
			set { this.MarkerBrushBearish = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Marker: String Bullish", Order = 33, GroupName = "Alerts")]
		public string MarkerStringBullish { get; set; }

		[Display(Name = "Marker: String Bearish", Order = 34, GroupName = "Alerts")]
		public string MarkerStringBearish { get; set; }

		[Display(Name = "Marker: Font", Order = 40, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }

		[Display(Name = "Marker: Offset", Order = 41, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }

		[Range(0, 2147483647)]
		[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
		public int AlertBlockingSeconds { get; set; }

		// ── Developer ─────────────────────────────────────────────────────────────
		// Thanks DD!

		[Display(Name = "Version", Order = 0, GroupName = "Developer")]
		public string Version => "1.1.1";

		[Display(Name = "Author", Order = 0, GroupName = "Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Website", Order = 5, GroupName = "Developer")]
		public string Website => "https://greybeardconsulting.net/";

		// ── General ───────────────────────────────────────────────────────────────

		[Range(99, 500)]
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		public int ScreenDPI { get; set; }

		// ── Graphics ──────────────────────────────────────────────────────────────

		[Display(Name = "Plot: Enabled", Order = 10, GroupName = "Graphics")]
		public bool PlotEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Plot: Rise", Order = 11, GroupName = "Graphics")]
		public Brush PlotRise { get; set; }
		[Browsable(false)]
		public string PlotRise_Serialize
		{
			get { return Serialize.BrushToString(this.PlotRise); }
			set { this.PlotRise = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Plot: Fall", Order = 12, GroupName = "Graphics")]
		public Brush PlotFall { get; set; }
		[Browsable(false)]
		public string PlotFall_Serialize
		{
			get { return Serialize.BrushToString(this.PlotFall); }
			set { this.PlotFall = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Bar: Enabled", Order = 20, GroupName = "Graphics")]
		public bool BarEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Bar: Bullish", Order = 21, GroupName = "Graphics")]
		public Brush BarBullish { get; set; }
		[Browsable(false)]
		public string BarBullish_Serialize
		{
			get { return Serialize.BrushToString(this.BarBullish); }
			set { this.BarBullish = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bar: Bearish", Order = 22, GroupName = "Graphics")]
		public Brush BarBearish { get; set; }
		[Browsable(false)]
		public string BarBearish_Serialize
		{
			get { return Serialize.BrushToString(this.BarBearish); }
			set { this.BarBearish = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Bar: Outline Enabled", Order = 23, GroupName = "Graphics")]
		public bool BarOutlineEnabled { get; set; }

		[Display(Name = "Bar: Bias Based", Order = 24, GroupName = "Graphics")]
		public bool BarBiasBased { get; set; }

		[Display(Name = "Cloud: Enabled", Order = 30, GroupName = "Graphics")]
		public bool CloudEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Cloud: Bullish", Order = 31, GroupName = "Graphics")]
		public Brush CloudBullish { get; set; }
		[Browsable(false)]
		public string CloudBullish_Serialize
		{
			get { return Serialize.BrushToString(this.CloudBullish); }
			set { this.CloudBullish = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Cloud: Bearish", Order = 32, GroupName = "Graphics")]
		public Brush CloudBearish { get; set; }
		[Browsable(false)]
		public string CloudBearish_Serialize
		{
			get { return Serialize.BrushToString(this.CloudBearish); }
			set { this.CloudBearish = Serialize.StringToBrush(value); }
		}

		[Range(0, 100)]
		[Display(Name = "Cloud: Opacity", Order = 33, GroupName = "Graphics")]
		public int CloudOpacity { get; set; }

		// ── Parameters ────────────────────────────────────────────────────────────

		[Range(0.0, 1.7976931348623157E+308)]
		[NinjaScriptProperty]
		[Display(Name = "Sensitivity", Order = 10, GroupName = "Parameters")]
		public double Sensitivity { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Smoothness", Order = 11, GroupName = "Parameters")]
		public int Smoothness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Baseline: MA Type", Order = 20, GroupName = "Parameters")]
		public gbNobleCloud_MAType BaselineMAType { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Baseline: Period", Order = 21, GroupName = "Parameters")]
		public int BaselinePeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Baseline: Smoothing Enabled", Order = 22, GroupName = "Parameters")]
		public bool BaselineSmoothingEnabled { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Baseline: Smoothing Method", Order = 23, GroupName = "Parameters")]
		public gbNobleCloud_MAType BaselineSmoothingMethod { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Baseline: Smoothing Period", Order = 24, GroupName = "Parameters")]
		public int BaselineSmoothingPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Kernel: MA Type", Order = 40, GroupName = "Parameters")]
		public gbNobleCloud_MAType KernelMAType { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Kernel: Period", Order = 41, GroupName = "Parameters")]
		public int KernelPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Kernel: Smoothing Enabled", Order = 42, GroupName = "Parameters")]
		public bool KernelSmoothingEnabled { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Kernel: Smoothing Method", Order = 43, GroupName = "Parameters")]
		public gbNobleCloud_MAType KernelSmoothingMethod { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Kernel: Smoothing Period", Order = 44, GroupName = "Parameters")]
		public int KernelSmoothingPeriod { get; set; }

		[Range(0, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Signal Split (Bars)", Order = 50, GroupName = "Parameters")]
		public int SignalSplit { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Filter: Enabled", Order = 60, GroupName = "Parameters")]
		public bool FilterEnabled { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Filter: Bar Min", Order = 61, GroupName = "Parameters")]
		public int FilterBarMin { get; set; }

		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Filter: Bar Max", Order = 62, GroupName = "Parameters")]
		public int FilterBarMax { get; set; }

		// ── Special ───────────────────────────────────────────────────────────────

		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }

		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }

		// ── Output series ─────────────────────────────────────────────────────────

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Baseline      { get { return base.Values[0]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal_Cloud  { get { return base.Values[1]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal_Trade  { get { return base.Values[2]; } }

		// ── DisplayName ───────────────────────────────────────────────────────────

		public override string DisplayName
		{
			get
			{
				if (base.Parent is MarketAnalyzerColumnBase)
					return base.DisplayName;
				return "Noble Cloud by GreyBeard" + GetUserNote();
			}
		}

		private string GetUserNote()
		{
			string text = UserNote.Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				text = text.ToLower();
				if (text.Contains("instrument") && Instrument != null)
					text = text.Replace("instrument", Instrument.FullName);
				if (text.Contains("period") && BarsPeriod != null)
					text = text.Replace("period", ((object)BarsPeriod).ToString());
				return " (" + text + ")";
			}
			return string.Empty;
		}

		// ── Lifecycle ─────────────────────────────────────────────────────────────

		protected override void OnStateChange()
		{
			try
			{
				if (base.State == State.SetDefaults)
				{
					base.Description                  = "Trend-following cloud indicator with baseline and kernel MAs.";
					base.Name                         = "gbNobleCloud";
					base.Calculate                    = Calculate.OnBarClose;
					base.IsOverlay                    = true;
					base.DisplayInDataBox             = true;
					base.DrawOnPricePanel             = true;
					base.DrawHorizontalGridLines      = true;
					base.DrawVerticalGridLines        = true;
					base.PaintPriceMarkers            = true;
					base.ScaleJustification           = ScaleJustification.Right;
					base.IsSuspendedWhileInactive     = false;
					base.BarsRequiredToPlot           = 0;
					base.ShowTransparentPlotsInDataBox = true;

					this.MarkerEnabled         = true;
					this.MarkerBrushBullish    = Brushes.Lime;
					this.MarkerBrushBearish    = Brushes.DarkOrange;
					this.MarkerStringBullish   = "⯭";
					this.MarkerStringBearish   = "⯯";
					this.MarkerFont            = new SimpleFont("Arial", 26);
					this.MarkerOffset          = 10;
					this.AlertBlockingSeconds  = 60;
					this.ScreenDPI             = 99;
					this.PlotEnabled           = true;
					this.PlotRise              = Brushes.DodgerBlue;
					this.PlotFall              = Brushes.DeepPink;
					this.BarEnabled            = true;
					this.BarBullish            = Brushes.DodgerBlue;
					this.BarBearish            = Brushes.HotPink;
					this.BarOutlineEnabled     = true;
					this.BarBiasBased          = true;
					this.CloudEnabled          = true;
					this.CloudBullish          = Brushes.Teal;
					this.CloudBearish          = Brushes.DarkSlateBlue;
					this.CloudOpacity          = 100;
					this.Sensitivity           = 60.0;
					this.Smoothness            = 1;
					this.BaselineMAType        = gbNobleCloud_MAType.SMA;
					this.BaselinePeriod        = 60;
					this.BaselineSmoothingEnabled = true;
					this.BaselineSmoothingMethod  = gbNobleCloud_MAType.EMA;
					this.BaselineSmoothingPeriod  = 60;
					this.KernelMAType          = gbNobleCloud_MAType.SMA;
					this.KernelPeriod          = 20;
					this.KernelSmoothingEnabled   = true;
					this.KernelSmoothingMethod    = gbNobleCloud_MAType.EMA;
					this.KernelSmoothingPeriod    = 5;
					this.SignalSplit            = 5;
					this.FilterEnabled         = true;
					this.FilterBarMin          = 10;
					this.FilterBarMax          = 300;
					this.IndicatorZOrder       = 0;
					this.UserNote              = "instrument (period)";

					base.AddPlot(new Stroke(Brushes.Orange,      DashStyleHelper.Dash,  2f), PlotStyle.Line, "Baseline");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Signal: Cloud");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Signal: Trade");
				}
				else if (base.State == State.Configure)
				{
					base.Calculate = Calculate.OnBarClose;

					this.seriesUpperThresholdRaw      = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesLowerThresholdRaw      = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesUpperThresholdSmoothed = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesLowerThresholdSmoothed = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdSmoothingEnabled    = this.Smoothness > 1;

					if (this.FilterBarMin > this.FilterBarMax)
					{
						int tmp         = this.FilterBarMin;
						this.FilterBarMin = this.FilterBarMax;
						this.FilterBarMax = tmp;
					}

					this.effectiveSensitivity    = this.Sensitivity / 50.0;
					this.nextAcceptedBar_Bearish = 0;
					this.nextAcceptedBar_Bullish = 0;
				}
				else if (base.State == State.DataLoaded)
				{
					this.kernelSmoothed   = this.GetMASeries_Smoothed(base.Input, this.KernelMAType,   this.KernelPeriod,   this.KernelSmoothingEnabled,   this.KernelSmoothingMethod,   this.KernelSmoothingPeriod);
					this.baselineSmoothed = this.GetMASeries_Smoothed(base.Input, this.BaselineMAType, this.BaselinePeriod, this.BaselineSmoothingEnabled, this.BaselineSmoothingMethod, this.BaselineSmoothingPeriod);
				}
				else if (base.State == State.Historical)
				{
					if (this.ScreenDPI < 100)
						this.ScreenDPI = this.GetDPI();
					if (this.IndicatorZOrder != 0)
						base.SetZOrder(this.IndicatorZOrder);
					this.isCharting = base.ChartControl != null;
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}

		protected override void OnBarUpdate()
		{
			try
			{
				double stdDev = StdDev(base.Input, this.KernelPeriod)[0];
				this.seriesUpperThresholdRaw[0]      = this.kernelSmoothed[0] + this.effectiveSensitivity * stdDev;
				this.seriesLowerThresholdRaw[0]      = this.kernelSmoothed[0] - this.effectiveSensitivity * stdDev;
				this.seriesUpperThresholdSmoothed[0] = this.thresholdSmoothingEnabled ? this.ComputeMAValue(this.seriesUpperThresholdRaw, gbNobleCloud_MAType.EMA, this.Smoothness) : this.seriesUpperThresholdRaw[0];
				this.seriesLowerThresholdSmoothed[0] = this.thresholdSmoothingEnabled ? this.ComputeMAValue(this.seriesLowerThresholdRaw, gbNobleCloud_MAType.EMA, this.Smoothness) : this.seriesLowerThresholdRaw[0];
				this.Baseline[0] = this.baselineSmoothed[0];

				// Track baseline direction
				if (base.CurrentBar == 1)
					this.isRisingBaseline = this.Baseline[0].ApproxCompare(this.Baseline[1]) > 0;
				else if (base.CurrentBar >= 2)
				{
					if (this.isRisingBaseline)
					{
						if (this.Baseline[0].ApproxCompare(this.Baseline[1]) < 0) this.isRisingBaseline = false;
					}
					else
					{
						if (this.Baseline[0].ApproxCompare(this.Baseline[1]) > 0) this.isRisingBaseline = true;
					}
				}

				// Cloud state machine
				if (base.CurrentBar == 0)
				{
					if      (this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) > 0) { this.barCount_Baseline = 1;  this.cloudState = -1; this.cloudIndex = base.CurrentBar; }
					else if (this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) < 0) { this.barCount_Baseline = 1;  this.cloudState =  1; this.cloudIndex = base.CurrentBar; }
					else                                                                                { this.barCount_Baseline = -1; this.cloudState =  0; this.cloudIndex = -1; }
				}
				else
				{
					if (this.cloudState != 0
						&& this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) <= 0
						&& this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) >= 0)
					{
						this.barCount_Baseline = -1; this.cloudState = 0; this.cloudIndex = -1;
					}
					else if (this.cloudState <= 0 && this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) < 0)
					{
						this.barCount_Baseline = 1; this.cloudState =  1; this.cloudIndex = base.CurrentBar;
					}
					else if (this.cloudState >= 0 && this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) > 0)
					{
						this.barCount_Baseline = 1; this.cloudState = -1; this.cloudIndex = base.CurrentBar;
					}

					if (this.Signal_Cloud[1] == (double)this.cloudState && this.barCount_Baseline >= 1)
						this.barCount_Baseline++;
				}

				// Signal detection
				int tradeSignal = 0;
				if (!this.FilterEnabled || (this.barCount_Baseline >= this.FilterBarMin && this.barCount_Baseline <= this.FilterBarMax))
				{
					if (this.cloudState > 0
						&& base.Low[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0])  <= 0
						&& base.Close[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) > 0
						&& base.Close[1].ApproxCompare(base.Open[1]) < 0
						&& base.Close[0].ApproxCompare(base.Open[0]) > 0
						&& base.CurrentBar >= this.nextAcceptedBar_Bullish)
					{
						tradeSignal = 1;
						this.nextAcceptedBar_Bullish = base.CurrentBar + this.SignalSplit;
						this.PaintBar(true);
						this.PrintMarker(true);
					}
					else if (this.cloudState < 0
						&& base.High[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0])  >= 0
						&& base.Close[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) < 0
						&& base.Close[1].ApproxCompare(base.Open[1]) > 0
						&& base.Close[0].ApproxCompare(base.Open[0]) < 0
						&& base.CurrentBar >= this.nextAcceptedBar_Bearish)
					{
						tradeSignal = -1;
						this.nextAcceptedBar_Bearish = base.CurrentBar + this.SignalSplit;
						this.PaintBar(false);
						this.PrintMarker(false);
					}
				}

				// Cloud region
				if (this.CloudEnabled && this.CloudOpacity > 0 && base.CurrentBar >= base.BarsRequiredToPlot && this.cloudState != 0)
				{
					bool bullish            = this.cloudState > 0;
					string regionTag        = string.Format("gbNobleCloud.{0}.{1}", bullish ? "cloud.bullish" : "cloud.bearish", this.cloudIndex);
					Brush regionBrush       = bullish ? this.CloudBullish : this.CloudBearish;
					ISeries<double> band    = bullish ? (ISeries<double>)this.seriesLowerThresholdSmoothed : this.seriesUpperThresholdSmoothed;
					int oldCloudBar = base.CurrentBar - DRAW_TAG_KEEP;
					if (oldCloudBar >= 0)
					{
						try { base.RemoveDrawObject("gbNobleCloud.cloud.bullish." + oldCloudBar); } catch { }
						try { base.RemoveDrawObject("gbNobleCloud.cloud.bearish." + oldCloudBar); } catch { }
					}
					Draw.Region(this, regionTag, base.CurrentBar - this.cloudIndex, 0, band, this.Baseline, Brushes.Transparent, regionBrush, this.CloudOpacity, 0);
				}

				// Plot colour
				if (this.PlotEnabled)
					base.PlotBrushes[0][0] = this.isRisingBaseline ? this.PlotRise : this.PlotFall;

				this.Signal_Cloud[0] = (double)this.cloudState;
				this.Signal_Trade[0] = (double)tradeSignal;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		private void PrintMarker(bool isBullish)
		{
			if (!this.isCharting || !this.MarkerEnabled || base.CurrentBar < base.BarsRequiredToPlot) return;

			string tag    = "gbNobleCloud.marker." + base.CurrentBar;
			Brush  brush  = isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish;
			double price  = isBullish ? base.Low[0] : base.High[0];
			string label  = this.FormatMarkerString(isBullish ? this.MarkerStringBullish : this.MarkerStringBearish);
			int    height = Convert.ToInt32(this.ComputeTextSize(label, this.MarkerFont, this.ScreenDPI).Height);
			int    dir    = isBullish ? -1 : 1;
			int    offset = dir * (this.MarkerOffset + height / 2);
			int oldBar = base.CurrentBar - DRAW_TAG_KEEP;
			if (oldBar >= 0)
			{
				try { base.RemoveDrawObject("gbNobleCloud.marker." + oldBar); } catch { }
			}
			Draw.Text(this, tag, base.IsAutoScale, label, 0, price, offset, brush, this.MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}

		private void PaintBar(bool isBullish)
		{
			if (!this.isCharting || !this.BarEnabled) return;

			Brush brush = isBullish ? this.BarBullish : this.BarBearish;
			int   dir   = isBullish ? 1 : -1;
			int   cmp   = base.Close[0].ApproxCompare(base.Open[0]);

			if (this.BarOutlineEnabled && !brush.IsTransparent())
				base.CandleOutlineBrush = brush;

			if (this.BarBiasBased)
			{
				if (brush.IsTransparent())
				{
					if (dir * cmp < 0) { base.BarBrush = Brushes.Transparent; return; }
				}
				else if (cmp != 0)
				{
					base.BarBrush = (dir * cmp > 0) ? brush : Brushes.Transparent;
					return;
				}
			}
			else if (cmp != 0)
			{
				base.BarBrush = brush;
			}
		}

		private void PrintException(Exception ex)
		{
			string msg = string.Concat("gbNobleCloud: ", ex.ToString(), " (", ex.StackTrace, ")");
			base.Print(msg);
			NinjaScript.Log(msg, LogLevel.Error);
		}

		public override string FormatPriceMarker(double price)
		{
			if (base.Instrument == null) return price.ToString();
			return base.Instrument.MasterInstrument.FormatPrice(base.Instrument.MasterInstrument.RoundToTickSize(price), true);
		}

		private ISeries<double> GetMASeries(ISeries<double> input, gbNobleCloud_MAType maType, int period)
		{
			if (period < 1) return null;
			switch (maType)
			{
				case gbNobleCloud_MAType.DEMA:     return DEMA(input, period);
				case gbNobleCloud_MAType.EMA:      return EMA(input, period);
				case gbNobleCloud_MAType.HMA:      return HMA(input, period);
				case gbNobleCloud_MAType.LinReg:   return LinReg(input, period);
				case gbNobleCloud_MAType.SMA:      return SMA(input, period);
				case gbNobleCloud_MAType.TEMA:     return TEMA(input, period);
				case gbNobleCloud_MAType.TMA:      return TMA(input, period);
				case gbNobleCloud_MAType.VWMA:     return VWMA(input, period);
				case gbNobleCloud_MAType.WMA:      return WMA(input, period);
				case gbNobleCloud_MAType.WilderMA: return EMA(input, 2 * period);
				case gbNobleCloud_MAType.ZLEMA:    return ZLEMA(input, period);
				default:                 return null;
			}
		}

		private ISeries<double> GetMASeries_Smoothed(ISeries<double> input, gbNobleCloud_MAType maType, int period, bool smoothingEnabled, gbNobleCloud_MAType smoothingMethod, int smoothingPeriod)
		{
			ISeries<double> series = GetMASeries(input, maType, period);
			if (smoothingEnabled && series != null) return GetMASeries(series, smoothingMethod, smoothingPeriod);
			return series;
		}

		private double ComputeMAValue(ISeries<double> input, gbNobleCloud_MAType maType, int period)
		{
			ISeries<double> series = GetMASeries(input, maType, period);
			return series != null ? series[0] : input[0];
		}

		private int GetDPI()
		{
			System.Reflection.PropertyInfo prop = typeof(SystemParameters).GetProperty("DpiX", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			if (prop == null) return 100;
			return Math.Max(100, Convert.ToInt32(prop.GetValue(null, null)) * 100 / 96);
		}

		private string FormatMarkerString(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return string.Empty;
			string[] parts = input.Trim().Split(new char[] { '+' });
			return string.Join("\n", parts.Select(x => x.Trim())).Trim();
		}

		private System.Windows.Size ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			double height = font != null ? font.Size * 1.5 : 12.0;
			double width  = (text != null && font != null) ? text.Length * font.Size * 0.7 : 0.0;
			return new System.Windows.Size(width, height);
		}

		// ── Private state ─────────────────────────────────────────────────────────

		private const string indicatorName = "Noble Cloud";
		private const string indicatorNameFull = "Noble Cloud by GreyBeard";

		private Series<double>   seriesUpperThresholdRaw;
		private Series<double>   seriesLowerThresholdRaw;
		private Series<double>   seriesUpperThresholdSmoothed;
		private Series<double>   seriesLowerThresholdSmoothed;
		private ISeries<double>  baselineSmoothed;
		private ISeries<double>  kernelSmoothed;
		private double           effectiveSensitivity;
		private int              nextAcceptedBar_Bearish;
		private int              nextAcceptedBar_Bullish;
		private int              cloudState;
		private int              cloudIndex;
		// Defense #4: rolling drawing-tag cleanup — per feedback_nt8_wpf_quota_prevention.md.
		private const int DRAW_TAG_KEEP = 250;
		private int              barCount_Baseline;
		private bool             isRisingBaseline;
		private bool             thresholdSmoothingEnabled;
		private bool             isCharting;
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbNobleCloud[] cachegbNobleCloud;

		public GreyBeard.gbNobleCloud gbNobleCloud(double sensitivity, int smoothness, gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return gbNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public GreyBeard.gbNobleCloud gbNobleCloud(ISeries<double> input, double sensitivity, int smoothness, gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			if (cachegbNobleCloud != null)
				for (int idx = 0; idx < cachegbNobleCloud.Length; idx++)
					if (cachegbNobleCloud[idx] != null
						&& cachegbNobleCloud[idx].Sensitivity              == sensitivity
						&& cachegbNobleCloud[idx].Smoothness               == smoothness
						&& cachegbNobleCloud[idx].BaselineMAType           == baselineMAType
						&& cachegbNobleCloud[idx].BaselinePeriod           == baselinePeriod
						&& cachegbNobleCloud[idx].BaselineSmoothingEnabled == baselineSmoothingEnabled
						&& cachegbNobleCloud[idx].BaselineSmoothingMethod  == baselineSmoothingMethod
						&& cachegbNobleCloud[idx].BaselineSmoothingPeriod  == baselineSmoothingPeriod
						&& cachegbNobleCloud[idx].KernelMAType             == kernelMAType
						&& cachegbNobleCloud[idx].KernelPeriod             == kernelPeriod
						&& cachegbNobleCloud[idx].KernelSmoothingEnabled   == kernelSmoothingEnabled
						&& cachegbNobleCloud[idx].KernelSmoothingMethod    == kernelSmoothingMethod
						&& cachegbNobleCloud[idx].KernelSmoothingPeriod    == kernelSmoothingPeriod
						&& cachegbNobleCloud[idx].SignalSplit               == signalSplit
						&& cachegbNobleCloud[idx].FilterEnabled             == filterEnabled
						&& cachegbNobleCloud[idx].FilterBarMin              == filterBarMin
						&& cachegbNobleCloud[idx].FilterBarMax              == filterBarMax
						&& cachegbNobleCloud[idx].EqualsInput(input))
						return cachegbNobleCloud[idx];
			return CacheIndicator<GreyBeard.gbNobleCloud>(new GreyBeard.gbNobleCloud()
			{
				Sensitivity              = sensitivity,
				Smoothness               = smoothness,
				BaselineMAType           = baselineMAType,
				BaselinePeriod           = baselinePeriod,
				BaselineSmoothingEnabled = baselineSmoothingEnabled,
				BaselineSmoothingMethod  = baselineSmoothingMethod,
				BaselineSmoothingPeriod  = baselineSmoothingPeriod,
				KernelMAType             = kernelMAType,
				KernelPeriod             = kernelPeriod,
				KernelSmoothingEnabled   = kernelSmoothingEnabled,
				KernelSmoothingMethod    = kernelSmoothingMethod,
				KernelSmoothingPeriod    = kernelSmoothingPeriod,
				SignalSplit              = signalSplit,
				FilterEnabled            = filterEnabled,
				FilterBarMin             = filterBarMin,
				FilterBarMax             = filterBarMax
			}, input, ref cachegbNobleCloud);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbNobleCloud gbNobleCloud(double sensitivity, int smoothness, Indicators.gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, Indicators.gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, Indicators.gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, Indicators.gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.gbNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public Indicators.GreyBeard.gbNobleCloud gbNobleCloud(ISeries<double> input, double sensitivity, int smoothness, Indicators.gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, Indicators.gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, Indicators.gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, Indicators.gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.gbNobleCloud(input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbNobleCloud gbNobleCloud(double sensitivity, int smoothness, Indicators.gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, Indicators.gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, Indicators.gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, Indicators.gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.gbNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public Indicators.GreyBeard.gbNobleCloud gbNobleCloud(ISeries<double> input, double sensitivity, int smoothness, Indicators.gbNobleCloud_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, Indicators.gbNobleCloud_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, Indicators.gbNobleCloud_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, Indicators.gbNobleCloud_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.gbNobleCloud(input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}
	}
}

#endregion
