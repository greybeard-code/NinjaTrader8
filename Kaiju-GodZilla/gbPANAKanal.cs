#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
public enum gbPANAKanalTextPosition
{
	BottomLeft,
	BottomRight,
	Center,
	TopLeft,
	TopRight
}

public enum gbPANAKanal_MarkerRenderingMethod
{
	Custom,
	Builtin
}

[CategoryOrder("Developer", 0)]
[CategoryOrder("General",   1000010)]
[CategoryOrder("Graphics",  1000020)]
[CategoryOrder("Gradient",  1000030)]
[CategoryOrder("Alerts",    1000040)]
[CategoryOrder("Special",   1000060)]
[CategoryOrder("Critical",  1000070)]
public class gbPANAKanal : Indicator
{
	private struct MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalInfo SignalInfo { get; set; }

		}

	private class LineInfo
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		public bool IsBroken { get; set; }

		}

	private struct TraillingStopAndFibonacciInfo
	{
		public double TraillingStop { get; set; }

		public double Fibonacci1 { get; set; }

		public double Fibonacci2 { get; set; }

		}

	private enum SignalInfo
	{
		Trend,
		Break,
		Pullback
	}

	private const int defaultMargin = 5;

	private Series<double> seriesDiffHighLow;

	private Series<double> seriesTrueRange;

	private Series<double> seriesWildMA;

	private Series<double> seriesUp;

	private Series<double> seriesDown;

	private Series<double> seriesFibonacci1;

	private Series<double> seriesFibonacci2;

	private Series<double> seriesSK;

	// SortedList (keyed by bar index) instead of Dictionary: "last line" is the
	// highest-bar entry by contract rather than by fragile insertion order, and
	// the render loop gets O(1) Values[i] access — Dictionary forced O(n)
	// ElementAt() per index, making DrawLines O(n²) per frame.
	private SortedList<int, LineInfo> dictLineInfo;

	private Dictionary<int, MarkerInfo> dictMarkers;

	private bool isCustomMarkerRenderingMethod;

	private bool isOnBarCloseMode;

	private bool isUptrend;

	private Window alertWindow;

	private const string prefix = "gbPANAKanal";

	private const string indicatorName = "PANA Kanal";

	private const string indicatorNameFull = "PANA Kanal by GreyBeard";

	private bool isCharting;

	private const double swingArmFibonacciLevel1 = 0.618;

	private const double swingArmFibonacciLevel2 = 0.786;

	private int signalStateKeltner;

	private int prevSignalStateKeltner;

	private bool hasPbSignal;

	private bool prevHasPbSignal;

	private int pbSignalIndex;

	private int prevPbSignalIndex;

	private bool isResetConditionPb;

	private bool hasBreakSignal;

	private bool prevHasBreakSignal;

	private int breakUpSignalIndex;

	private int breakDownSignalIndex;

	private int prevBreakSignalIndex;

	private bool isCheckPriceInZone = true;

	private bool isPriceInZone;

	private bool prevIsPriceInZone;

	private bool isResetConditionBreak;

	private int crossUpIndex;

	private int crossDownIndex;

	private bool hasBreakSignalOnEachTick;

	private bool hasPbSignalOnEachTick;

	private bool isLineBrokenByCandleUp;

	private bool isLineBrokenByCandleDown;

	private bool isBreakSignalSplitOk;

	private int signalTrend1;

	private double extremum1;

	private SignalInfo signalInfo;

	private string tagRegion1 = string.Empty;

	private string tagRegion2 = string.Empty;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;


	[Display(Name = "Condition: Trend Start", Order = 0, GroupName = "Alerts")]
	public bool ConditionTrendStart { get; set; }

	[Display(Name = "Condition: Trend Pullback", Order = 1, GroupName = "Alerts")]
	public bool ConditionTrendPullback { get; set; }

	[Display(Name = "Condition: Break", Order = 2, GroupName = "Alerts")]
	public bool ConditionBreak { get; set; }

	[Display(Name = "Popup: Enabled", Order = 3, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Background Color", Order = 4, GroupName = "Alerts")]
	public Brush PopupBackgroundBrush { get; set; }

	[Browsable(false)]
	public string PopupBackgroundBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupBackgroundBrush);
		}
		set
		{
			PopupBackgroundBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Popup: Background Opacity", Order = 5, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 6, GroupName = "Alerts")]
	public Brush PopupTextBrush { get; set; }

	[Browsable(false)]
	public string PopupTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupTextBrush);
		}
		set
		{
			PopupTextBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Popup: Text Size", Order = 7, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[Display(Name = "Popup: Button Color", Order = 8, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush PopupButtonBrush { get; set; }

	[Browsable(false)]
	public string PopupButtonBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupButtonBrush);
		}
		set
		{
			PopupButtonBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Sound: Enabled", Order = 10, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Uptrend Start", Order = 11, GroupName = "Alerts")]
	public string SoundUptrendStart { get; set; }

	[Display(Name = "Sound: Downtrend Start", Order = 12, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundDowntrendStart { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Uptrend Pullback", Order = 13, GroupName = "Alerts")]
	public string SoundUptrendPullback { get; set; }

	[Display(Name = "Sound: Downtrend Pullback", Order = 14, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundDowntrendPullback { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Break Up", Order = 15, GroupName = "Alerts")]
	public string SoundBreakUp { get; set; }

	[Display(Name = "Sound: Break Down", Order = 16, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundBreakDown { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 17, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 18, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 20, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 21, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 31, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbPANAKanal_MarkerRenderingMethod MarkerRenderingMethod { get; set; }

	[XmlIgnore]
	[Display(Name = "Marker: Color Bullish", Order = 32, GroupName = "Alerts")]
	public Brush MarkerBrushBullish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBullish);
		}
		set
		{
			MarkerBrushBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Bearish", Order = 33, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush MarkerBrushBearish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBearish);
		}
		set
		{
			MarkerBrushBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: String Uptrend Start", Order = 34, GroupName = "Alerts")]
	public string MarkerStringUptrendStart { get; set; }

	[Display(Name = "Marker: String Downtrend Start", Order = 35, GroupName = "Alerts")]
	public string MarkerStringDowntrendStart { get; set; }

	[Display(Name = "Marker: String Uptrend Pullback", Order = 36, GroupName = "Alerts")]
	public string MarkerStringUptrendPullback { get; set; }

	[Display(Name = "Marker: String Downtrend Pullback", Order = 37, GroupName = "Alerts")]
	public string MarkerStringDowntrendPullback { get; set; }

	[Display(Name = "Marker: String Break Up", Order = 38, GroupName = "Alerts")]
	public string MarkerStringBreakUp { get; set; }

	[Display(Name = "Marker: String Break Down", Order = 39, GroupName = "Alerts")]
	public string MarkerStringBreakDown { get; set; }

	[Display(Name = "Marker: Font", Order = 40, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 41, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }
	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
	public bool SwitchedOn { get; set; }

	[Display(Name = "Author",   Order = 0,  GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Version",  Order = 1,  GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Website", Order = 5, GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Plot: Enabled", Order = 0, GroupName = "Graphics")]
	public bool PlotEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot: Uptrend", Order = 3, GroupName = "Graphics")]
	public Brush PlotUptrend { get; set; }

	[Browsable(false)]
	public string PlotUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotUptrend);
		}
		set
		{
			PlotUptrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Plot: Downtrend", Order = 4, GroupName = "Graphics")]
	public Brush PlotDowntrend { get; set; }

	[Browsable(false)]
	public string PlotDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotDowntrend);
		}
		set
		{
			PlotDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Enabled", Order = 10, GroupName = "Graphics")]
	public bool BarEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Bar: Uptrend", Order = 20, GroupName = "Graphics")]
	public Brush BarUptrend { get; set; }

	[Browsable(false)]
	public string BarUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarUptrend);
		}
		set
		{
			BarUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Downtrend", Order = 21, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush BarDowntrend { get; set; }

	[Browsable(false)]
	public string BarDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarDowntrend);
		}
		set
		{
			BarDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Outline Enabled", Order = 70, GroupName = "Graphics")]
	public bool BarOutlineEnabled { get; set; }

	[Display(Name = "Bar: Bias Based", Order = 71, GroupName = "Graphics")]
	public bool BarBiasBased { get; set; }

	[Display(Name = "Region: Enabled", Order = 80, GroupName = "Graphics")]
	public bool RegionEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Region: Uptrend", Order = 82, GroupName = "Graphics")]
	public Brush RegionUptrend { get; set; }

	[Browsable(false)]
	public string RegionUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(RegionUptrend);
		}
		set
		{
			RegionUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Region: Downtrend", Order = 84, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush RegionDowntrend { get; set; }

	[Browsable(false)]
	public string RegionDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(RegionDowntrend);
		}
		set
		{
			RegionDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Region: Opacity #1", Order = 86, GroupName = "Graphics")]
	public int RegionOpacity1 { get; set; }
	[Display(Name = "Region: Opacity #2", Order = 88, GroupName = "Graphics")]
	public int RegionOpacity2 { get; set; }

	[Display(Name = "Line: Active Top", Order = 90, GroupName = "Graphics")]
	public Stroke LineActiveTop { get; set; }

	[Display(Name = "Line: Active Bottom", Order = 92, GroupName = "Graphics")]
	public Stroke LineActiveBottom { get; set; }

	[Display(Name = "Line: Inactive Enabled", Order = 94, GroupName = "Graphics")]
	public bool LineInactiveEnabled { get; set; }

	[Display(Name = "Line: Inactive Top", Order = 96, GroupName = "Graphics")]
	public Stroke LineInactiveTop { get; set; }

	[Display(Name = "Line: Inactive Bottom", Order = 98, GroupName = "Graphics")]
	public Stroke LineInactiveBottom { get; set; }

	[Display(Name = "Period", Order = 2, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int Period { get; set; }
	[Display(Name = "Factor", Order = 4, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public double Factor { get; set; }
	[NinjaScriptProperty]
	[Display(Name = "Middle Period", Order = 6, GroupName = "Parameters")]
	public int MiddlePeriod { get; set; }
	[Display(Name = "Signal Break Split (Bars)", Order = 8, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalBreakSplit { get; set; }
	[Display(Name = "Signal Pullback Finding Period", Order = 10, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalPullbackFindingPeriod { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Extremum => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> TrailingStop => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trend => Values[3];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Trade => Values[4];

	[XmlIgnore]
	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "PANA Kanal by GreyBeard" + GetUserNote();
			}
			return DisplayName;
		}
	}

	private string GetUserNote()
	{
		string text = UserNote.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.ToLower();
			string text2 = "instrument";
			string text3 = "period";
			if (text.Contains(text2) && Instruments[0] != null)
			{
				text = text.Replace(text2, Instruments[0].FullName);
			}
			if (text.Contains(text3) && BarsPeriods[0] != null)
			{
				text = text.Replace(text3, ((object)BarsPeriods[0]).ToString());
			}
			return " (" + text + ")";
		}
		return string.Empty;
	}

	protected override void OnStateChange()
	{
		switch (State)
		{
		case State.SetDefaults:
			Name = "gbPANAKanal";
			Description = "Keltner Channel with break and pullback trade signals.";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			IsSuspendedWhileInactive = true;
			IsAutoScale = true;
			BarsRequiredToPlot = 0;
			ShowTransparentPlotsInDataBox = true;

			// Parameters
			Period = 20;
			Factor = 4.0;
			MiddlePeriod = 14;
			SignalBreakSplit = 20;
			SignalPullbackFindingPeriod = 10;

			// Critical
			SwitchedOn = true;

			// General
			ScreenDPI = 100;

			// Graphics
			PlotEnabled = true;
			PlotUptrend = Brushes.DodgerBlue;             // #FF1E90FF
			PlotDowntrend = Brushes.Magenta;               // #FFFF00FF
			BarEnabled = true;
			BarUptrend = Brushes.DodgerBlue;               // #FF1E90FF
			BarDowntrend = Brushes.Plum;                   // #FFDDA0DD
			BarOutlineEnabled = true;
			BarBiasBased = true;
			RegionEnabled = true;
			RegionUptrend = Brushes.LimeGreen;             // #FF32CD32
			RegionDowntrend = Brushes.HotPink;             // #FFFF69B4
			RegionOpacity1 = 30;
			RegionOpacity2 = 50;
			LineActiveTop = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 5);       // #FF32CD32
			LineActiveBottom = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 5);      // #FFDC143C
			LineInactiveEnabled = true;
			LineInactiveTop = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 5, 40);
			LineInactiveBottom = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 5, 40);

			// Alerts
			ConditionTrendStart = true;
			ConditionTrendPullback = true;
			ConditionBreak = true;
			PopupEnabled = false;
			PopupBackgroundBrush = Brushes.Gold;           // #FFFFD700
			PopupBackgroundOpacity = 60;
			PopupTextBrush = Brushes.DarkSlateGray;        // #FF2F4F4F
			PopupTextSize = 16;
			PopupButtonBrush = Brushes.Transparent;        // #00FFFFFF
			SoundEnabled = false;
			SoundUptrendStart = "Alert4.wav";
			SoundDowntrendStart = "Alert3.wav";
			SoundUptrendPullback = "Alert4.wav";
			SoundDowntrendPullback = "Alert3.wav";
			SoundBreakUp = "Alert1.wav";
			SoundBreakDown = "Alert2.wav";
			SoundRearmEnabled = true;
			SoundRearmSeconds = 5;
			EmailEnabled = false;
			EmailReceiver = "receiver@example.com";
			MarkerEnabled = true;
			MarkerRenderingMethod = gbPANAKanal_MarkerRenderingMethod.Custom;
			MarkerBrushBullish = Brushes.LimeGreen;        // #FF32CD32
			MarkerBrushBearish = Brushes.OrangeRed;        // #FFFF4500
			MarkerStringUptrendStart = "\u25b2 + Trend";
			MarkerStringDowntrendStart = "Trend + \u25bc";
			MarkerStringUptrendPullback = "\ud83e\udc31 + Pb";
			MarkerStringDowntrendPullback = "Pb + \ud83e\udc33";
			MarkerStringBreakUp = "\ud83e\udc75 + Break";
			MarkerStringBreakDown = "Break + \ud83e\udc76";
			MarkerFont = new SimpleFont("Arial", 20);
			MarkerOffset = 10;
			AlertBlockingSeconds = 60;

			// Special
			IndicatorZOrder = 0;
			UserNote = "instrument (period)";

			// Plots
			AddPlot(new Stroke(Brushes.Yellow, 1), PlotStyle.Dot, "Extremum");                              // #FFFFFF00
			AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Dot, 1), PlotStyle.Dot, "Middle");         // #FFDAA520
			AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Solid, 3), PlotStyle.Line, "Trailing Stop"); // #FFDAA520
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Signal: Trend");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Signal: Trade");
			break;

		case State.DataLoaded:
			seriesDiffHighLow = new Series<double>(this);
			seriesTrueRange = new Series<double>(this);
			seriesWildMA = new Series<double>(this);
			seriesUp = new Series<double>(this);
			seriesDown = new Series<double>(this);
			seriesFibonacci1 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesFibonacci2 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesSK = new Series<double>(this);
			dictLineInfo = new SortedList<int, LineInfo>();
			dictMarkers = new Dictionary<int, MarkerInfo>();
			isCustomMarkerRenderingMethod = MarkerRenderingMethod == gbPANAKanal_MarkerRenderingMethod.Custom;
			isOnBarCloseMode = Calculate == Calculate.OnBarClose;
			rearmTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			rearmTimer.Tick += OnRearmTimerTick;
			break;

		case State.Historical:
			isCharting = ChartControl != null;
			break;

		case State.Terminated:
			if (isCharting)
			{
				if (alertWindow != null)
					ChartControl.Dispatcher.InvokeAsync(delegate { alertWindow.Close(); });
			}
			if (rearmTimer != null)
			{
				rearmTimer.Stop();
				rearmTimer = null;
			}
			break;
		}
	}

	protected override void OnBarUpdate()
	{
		ComputeKeltner();
		if (IsFirstTickOfBar)
		{
			prevSignalStateKeltner = signalStateKeltner;
		}
		signalStateKeltner = GetStateKeltner(prevSignalStateKeltner, Middle[0]);
		if (CurrentBar != 0)
		{
			double num = High[0];
			double num2 = High[1];
			double num3 = Low[0];
			double num4 = Low[1];
			double num5 = Close[0];
			double num6 = Close[1];
			seriesDiffHighLow[0] = num - num3;
			double val = Math.Min(seriesDiffHighLow[0], 1.5 * SMA(seriesDiffHighLow, Period)[0]);
			double val2 = ((MathExtentions.ApproxCompare(num3, num2) > 0) ? (num - num6 - 0.5 * (num3 - num2)) : (num - num6));
			double val3 = ((MathExtentions.ApproxCompare(num, num4) < 0) ? (num6 - num3 - 0.5 * (num4 - num)) : (num6 - num3));
			seriesTrueRange[0] = Math.Max(val, Math.Max(val2, val3));
			seriesWildMA[0] = seriesWildMA[1] + (seriesTrueRange[0] - seriesWildMA[1]) / (double)Period;
			double num7 = Factor * seriesWildMA[0];
			double num8 = num5 - num7;
			double num9 = num5 + num7;
			double num10 = seriesUp[1];
			double num11 = seriesDown[1];
			seriesUp[0] = ((MathExtentions.ApproxCompare(num6, num10) <= 0) ? num8 : Math.Max(num8, num10));
			seriesDown[0] = ((MathExtentions.ApproxCompare(num6, num11) >= 0) ? num9 : Math.Min(num9, num11));
			if (IsFirstTickOfBar)
			{
				hasPbSignalOnEachTick = false;
				hasBreakSignalOnEachTick = false;
				isPriceInZone = !isResetConditionBreak && isCheckPriceInZone;
				isBreakSignalSplitOk = !hasBreakSignal || CurrentBar - (((!isUptrend) ? breakDownSignalIndex : breakUpSignalIndex) - 1) >= SignalBreakSplit;
				if (!hasPbSignal && isResetConditionPb)
				{
					hasPbSignal = true;
					isResetConditionPb = false;
				}
				signalTrend1 = Convert.ToInt32(Signal_Trend[1]);
				extremum1 = Extremum[1];
			}
			int num12 = 0;
			TraillingStopAndFibonacciInfo traillingStopAndFibonacciInfo = default(TraillingStopAndFibonacciInfo);
			double num13;
			if (!isUptrend)
			{
				if (isOnBarCloseMode || signalTrend1 != 1)
				{
					if (MathExtentions.ApproxCompare(num5, num11) <= 0)
					{
						num13 = Math.Min(extremum1, num3);
						traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
						num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
						CheckLineBrokenAndAddNewLine();
					}
					else
					{
						isUptrend = true;
						num12 = 1;
						prevIsPriceInZone = isCheckPriceInZone;
						isCheckPriceInZone = true;
						isResetConditionBreak = false;
						prevHasPbSignal = hasPbSignal;
						hasPbSignal = false;
						prevPbSignalIndex = pbSignalIndex;
						pbSignalIndex = 0;
						prevHasBreakSignal = hasBreakSignal;
						hasBreakSignal = false;
						breakUpSignalIndex = CurrentBar;
						crossUpIndex = CurrentBar;
						isLineBrokenByCandleUp = false;
						num13 = num;
						traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
						if (!isOnBarCloseMode)
						{
							if (hasBreakSignalOnEachTick)
							{
								if (ConditionBreak)
								{
									RemoveMarker();
									RemovePopupAndSound();
								}
								breakDownSignalIndex = prevBreakSignalIndex;
								hasBreakSignalOnEachTick = false;
							}
							if (hasPbSignalOnEachTick)
							{
								if (ConditionTrendPullback)
								{
									RemoveMarker();
									RemovePopupAndSound();
								}
								isResetConditionPb = false;
								hasPbSignalOnEachTick = false;
							}
						}
						CheckLineBrokenAndAddNewLine(isTrendSwitched: true);
					}
				}
				else if (MathExtentions.ApproxCompare(num5, num10) < 0)
				{
					AddNewLine();
					num13 = num3;
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
				}
				else
				{
					isUptrend = true;
					hasPbSignal = prevHasPbSignal;
					pbSignalIndex = prevPbSignalIndex;
					hasBreakSignal = prevHasBreakSignal;
					isCheckPriceInZone = prevIsPriceInZone;
					num13 = Math.Max(extremum1, num);
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					if (ConditionTrendStart)
					{
						RemoveMarker();
						RemovePopupAndSound();
					}
					num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
					if (dictLineInfo.ContainsKey(CurrentBar))
					{
						dictLineInfo.Remove(CurrentBar);
						CheckLineBrokenAndAddNewLine();
					}
				}
			}
			else if (isOnBarCloseMode || signalTrend1 != -1)
			{
				if (MathExtentions.ApproxCompare(num5, num10) >= 0)
				{
					num13 = Math.Max(extremum1, num);
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
					CheckLineBrokenAndAddNewLine();
				}
				else
				{
					isUptrend = false;
					num12 = -1;
					prevIsPriceInZone = isCheckPriceInZone;
					isCheckPriceInZone = true;
					isResetConditionBreak = false;
					prevHasPbSignal = hasPbSignal;
					hasPbSignal = false;
					prevPbSignalIndex = pbSignalIndex;
					pbSignalIndex = 0;
					prevHasBreakSignal = hasBreakSignal;
					hasBreakSignal = false;
					breakDownSignalIndex = CurrentBar;
					crossDownIndex = CurrentBar;
					isLineBrokenByCandleDown = false;
					num13 = num3;
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					if (!isOnBarCloseMode)
					{
						if (hasBreakSignalOnEachTick)
						{
							if (ConditionBreak)
							{
								RemoveMarker();
								RemovePopupAndSound();
							}
							breakUpSignalIndex = prevBreakSignalIndex;
							hasBreakSignalOnEachTick = false;
						}
						if (hasPbSignalOnEachTick)
						{
							if (ConditionTrendPullback)
							{
								RemoveMarker();
								RemovePopupAndSound();
							}
							isResetConditionPb = false;
							hasPbSignalOnEachTick = false;
						}
					}
					CheckLineBrokenAndAddNewLine(isTrendSwitched: true);
				}
			}
			else if (MathExtentions.ApproxCompare(num5, num11) > 0)
			{
				AddNewLine();
				num13 = num;
				traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
			}
			else
			{
				isUptrend = false;
				hasPbSignal = prevHasPbSignal;
				pbSignalIndex = prevPbSignalIndex;
				hasBreakSignal = prevHasBreakSignal;
				isCheckPriceInZone = prevIsPriceInZone;
				num13 = Math.Min(extremum1, num3);
				traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
				if (ConditionTrendStart)
				{
					RemoveMarker();
					RemovePopupAndSound();
				}
				num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
				if (dictLineInfo.ContainsKey(CurrentBar))
				{
					dictLineInfo.Remove(CurrentBar);
					CheckLineBrokenAndAddNewLine();
				}
			}
			Extremum[0] = num13;
			TrailingStop[0] = traillingStopAndFibonacciInfo.TraillingStop;
			seriesFibonacci1[0] = traillingStopAndFibonacciInfo.Fibonacci1;
			seriesFibonacci2[0] = traillingStopAndFibonacciInfo.Fibonacci2;
			Signal_Trend[0] = (isUptrend ? 1 : (-1));
			Signal_Trade[0] = num12;
			if (ConditionTrendStart && Signal_Trend[0] * (double)signalTrend1 < 0.0)
			{
				signalInfo = SignalInfo.Trend;
				if (!isCustomMarkerRenderingMethod)
				{
					PrintMarker(isUptrend, signalInfo);
				}
				else
				{
					AddMarker(CurrentBar, isUptrend, signalInfo);
				}
				TriggerAlerts(isUptrend, signalInfo);
			}
			if (isCharting)
			{
				Brush brush = ((!isUptrend) ? PlotDowntrend : PlotUptrend);
				if (!BrushExtensions.IsTransparent(brush))
				{
					if (MathExtentions.ApproxCompare(Signal_Trend[0], (double)signalTrend1) == 0)
					{
						if (PlotEnabled)
						{
							BrushSeries obj = PlotBrushes[0];
							Brush brush2 = (PlotBrushes[2][0] = brush);
							obj[0] = brush2;
						}
					}
					else
					{
						BrushSeries obj2 = PlotBrushes[0];
						Brush brush2 = (PlotBrushes[2][0] = Brushes.Transparent);
						obj2[0] = brush2;
					}
					if (PlotEnabled)
					{
						PlotBrushes[1][0] = brush;
					}
				}
			}
			if (SwitchedOn)
			{
				PaintBar(isUptrend);
			}
			if (!isCharting || !RegionEnabled)
			{
				return;
			}
			Brush brush4 = ((!isUptrend) ? RegionDowntrend : RegionUptrend);
			if (BrushExtensions.IsTransparent(brush4))
			{
				return;
			}
			if (!((!isUptrend) ? (crossDownIndex == CurrentBar) : (crossUpIndex == CurrentBar)))
			{
				int barsAgo = CurrentBar - ((!isUptrend) ? crossDownIndex : crossUpIndex);
				if (RegionOpacity1 > 0)
				{
					FillRegion(seriesFibonacci1, seriesFibonacci2, brush4, barsAgo, 1, updateTag: true, RegionOpacity1);
				}
				if (RegionOpacity2 > 0)
				{
					FillRegion(seriesFibonacci2, TrailingStop, brush4, barsAgo, 2, updateTag: true, RegionOpacity2);
				}
			}
			else if (!isOnBarCloseMode)
			{
				brush4 = ((!isUptrend) ? RegionUptrend : RegionDowntrend);
				int barsAgo2 = CurrentBar - ((!isUptrend) ? crossUpIndex : crossDownIndex);
				if (RegionOpacity1 > 0)
				{
					FillRegion(seriesFibonacci1, seriesFibonacci2, brush4, barsAgo2, 1, updateTag: false, RegionOpacity1);
				}
				if (RegionOpacity2 > 0)
				{
					FillRegion(seriesFibonacci2, TrailingStop, brush4, barsAgo2, 2, updateTag: false, RegionOpacity2);
				}
			}
		}
		else
		{
			seriesWildMA[0] = 0.0;
			seriesTrueRange[0] = 0.0;
			Series<double> obj3 = seriesUp;
			seriesDown[0] = 0.0;
			obj3[0] = 0.0;
			isUptrend = false;
			Signal_Trend[0] = (isUptrend ? 1 : (-1));
			AddNewLine();
		}
	}

	private void FillRegion(Series<double> series1, Series<double> series2, Brush regionBrush, int barsAgo, int tagIndex, bool updateTag, int regionOpacity)
	{
		if (updateTag)
		{
			if (tagIndex == 1)
			{
				tagRegion1 = "gbPANAKanal.cloud1." + ((!isUptrend) ? crossDownIndex : crossUpIndex);
			}
			if (tagIndex == 2)
			{
				tagRegion2 = "gbPANAKanal.cloud2." + ((!isUptrend) ? crossDownIndex : crossUpIndex);
			}
		}
		Region region = Draw.Region(this, (tagIndex != 1) ? tagRegion2 : tagRegion1, barsAgo, (!updateTag) ? 1 : 0, series1, series2, Brushes.Transparent, regionBrush, regionOpacity);
		if (!SwitchedOn)
		{
			((NinjaScript)region).IsVisible = false;
		}
	}

	private TraillingStopAndFibonacciInfo ComputeTraillingStopAndFibonacci(bool isUptrend, double extremum)
	{
		TraillingStopAndFibonacciInfo result = new TraillingStopAndFibonacciInfo
		{
			TraillingStop = ((!isUptrend) ? seriesDown[0] : seriesUp[0])
		};
		double num = result.TraillingStop - extremum;
		result.Fibonacci1 = extremum + num * swingArmFibonacciLevel1;
		result.Fibonacci2 = extremum + num * swingArmFibonacciLevel2;
		return result;
	}

	private int CheckBreakoutAndPullbackSignal(double fibonacci1)
	{
		if (!isPriceInZone)
		{
			if (!isUptrend || MathExtentions.ApproxCompare(Close[0], fibonacci1) >= 0)
			{
				if (isUptrend || MathExtentions.ApproxCompare(Close[0], fibonacci1) <= 0)
				{
					isCheckPriceInZone = false;
				}
				else
				{
					isCheckPriceInZone = true;
				}
			}
			else
			{
				isCheckPriceInZone = true;
			}
		}
		int result = 0;
		if (!hasPbSignal)
		{
			if (IsFirstTickOfBar)
			{
				pbSignalIndex++;
			}
			if (pbSignalIndex < SignalPullbackFindingPeriod)
			{
				double num = Close[0];
				double num2 = Close[1];
				double num3 = Open[0];
				double num4 = Open[1];
				int num5 = MathExtentions.ApproxCompare(num, num3);
				int num6 = MathExtentions.ApproxCompare(num2, num4);
				if (!((isUptrend ? (num6 < 0 && num5 > 0) : (num6 > 0 && num5 < 0)) & ((isUptrend && MathExtentions.ApproxCompare(Low[0], fibonacci1) <= 0) || (!isUptrend && MathExtentions.ApproxCompare(High[0], fibonacci1) >= 0))))
				{
					if (hasPbSignalOnEachTick)
					{
						if (ConditionTrendPullback)
						{
							RemoveMarker();
						}
						isResetConditionPb = false;
						hasPbSignalOnEachTick = false;
					}
				}
				else
				{
					if (hasBreakSignalOnEachTick)
					{
						if (ConditionBreak)
						{
							RemoveMarker();
						}
						if (!isUptrend)
						{
							breakDownSignalIndex = prevBreakSignalIndex;
						}
						else
						{
							breakUpSignalIndex = prevBreakSignalIndex;
						}
						hasBreakSignalOnEachTick = false;
					}
					result = ((!isUptrend) ? (-3) : 3);
					if (!hasPbSignalOnEachTick)
					{
						if (ConditionTrendPullback)
						{
							signalInfo = SignalInfo.Pullback;
							if (!isCustomMarkerRenderingMethod)
							{
								PrintMarker(isUptrend, signalInfo);
							}
							else
							{
								AddMarker(CurrentBar, isUptrend, signalInfo);
							}
							TriggerAlerts(isUptrend, signalInfo);
						}
						isResetConditionPb = true;
						hasPbSignalOnEachTick = true;
					}
				}
			}
		}
		if (!isResetConditionPb)
		{
			if (!isPriceInZone || !isBreakSignalSplitOk || !((!isUptrend) ? (signalStateKeltner - prevSignalStateKeltner < 0) : (signalStateKeltner - prevSignalStateKeltner > 0)))
			{
				isResetConditionBreak = false;
				if (hasBreakSignalOnEachTick)
				{
					if (ConditionBreak)
					{
						RemoveMarker();
					}
					if (!isUptrend)
					{
						breakDownSignalIndex = prevBreakSignalIndex;
					}
					else
					{
						breakUpSignalIndex = prevBreakSignalIndex;
					}
					hasBreakSignal = false;
					hasBreakSignalOnEachTick = false;
				}
			}
			else
			{
				result = ((!isUptrend) ? (-2) : 2);
				if (!hasBreakSignalOnEachTick)
				{
					if (!isUptrend)
					{
						prevBreakSignalIndex = breakDownSignalIndex;
						breakDownSignalIndex = CurrentBar;
					}
					else
					{
						prevBreakSignalIndex = breakUpSignalIndex;
						breakUpSignalIndex = CurrentBar;
					}
					if (ConditionBreak)
					{
						signalInfo = SignalInfo.Break;
						if (!isCustomMarkerRenderingMethod)
						{
							PrintMarker(isUptrend, signalInfo);
						}
						else
						{
							AddMarker(CurrentBar, isUptrend, signalInfo);
						}
						TriggerAlerts(isUptrend, signalInfo);
					}
					hasBreakSignal = true;
					isResetConditionBreak = true;
					hasBreakSignalOnEachTick = true;
				}
			}
			return result;
		}
		return result;
	}

	private void CheckLineBrokenAndAddNewLine(bool isTrendSwitched = false)
	{
		if (dictLineInfo.Count <= 0)
		{
			return;
		}
		LineInfo lineInfo = dictLineInfo.Values[dictLineInfo.Count - 1];
		bool isTop = lineInfo.IsTop;
		if (lineInfo.IsBroken)
		{
			if (!isTop || isLineBrokenByCandleUp)
			{
				if (!isTop && !isLineBrokenByCandleDown)
				{
					lineInfo.IsBroken = false;
				}
			}
			else
			{
				lineInfo.IsBroken = false;
			}
		}
		if (!lineInfo.IsBroken)
		{
			if (!isTrendSwitched)
			{
				bool flag;
				if (flag = ((!isTop) ? (MathExtentions.ApproxCompare(Low[0], lineInfo.Price) <= 0) : (MathExtentions.ApproxCompare(High[0], lineInfo.Price) >= 0)))
				{
					lineInfo.IsBroken = true;
					if (!isTop)
					{
						isLineBrokenByCandleDown = true;
					}
					else
					{
						isLineBrokenByCandleUp = true;
					}
				}
				lineInfo.BarEnd = CurrentBar - (flag ? 1 : 0);
			}
			else
			{
				lineInfo.IsBroken = true;
				lineInfo.BarEnd = CurrentBar - 1;
			}
		}
		if (isTrendSwitched)
		{
			AddNewLine();
		}
	}

	private void RemoveMarker()
	{
		if (MarkerEnabled)
		{
			if (!isCustomMarkerRenderingMethod)
			{
				string text = "gbPANAKanal.marker." + CurrentBar;
				RemoveDrawObject(text);
			}
			else if (dictMarkers.ContainsKey(CurrentBar))
			{
				dictMarkers.Remove(CurrentBar);
			}
		}
	}

	private void RemovePopupAndSound()
	{
		if (!PopupEnabled && !SoundEnabled)
		{
			return;
		}
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (PopupEnabled && alertWindow != null)
					{
						alertWindow.Close();
					}
					if (SoundEnabled && alertWindow != null && !alertWindow.IsVisible)
					{
						rearmTimer.Stop();
					}
				});
			}
		}, (object)null);
	}

	private void AddNewLine()
	{
		double num = 0.0;
		if (CurrentBar != 0)
		{
			num = Math.Abs(TrailingStop[1] - seriesFibonacci1[1]);
		}
		LineInfo value = new LineInfo
		{
			Price = ((!isUptrend) ? (Low[0] - num) : (High[0] + num)),
			BarStart = CurrentBar,
			BarEnd = CurrentBar,
			IsTop = isUptrend,
			IsBroken = false
		};
		if (dictLineInfo.ContainsKey(CurrentBar))
		{
			dictLineInfo[CurrentBar] = value;
		}
		else
		{
			dictLineInfo.Add(CurrentBar, value);
		}
	}

	private void ComputeKeltner()
	{
		seriesSK[0] = EMA(Input, MiddlePeriod)[0];
		double num = EMA(seriesSK, 2)[0];
		Middle[0] = Instrument.MasterInstrument.RoundToTickSize(num);
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (ChartControl != null && SwitchedOn && !IsInHitTest)
		{
			DrawLines(chartScale);
			DrawMarkers(chartScale);
			base.OnRender(chartControl, chartScale);
		}
	}

	private void DrawLines(ChartScale chartScale)
	{
		if (dictLineInfo.Count <= 0)
		{
			return;
		}
		if (!LineInactiveEnabled)
		{
			LineInfo value = dictLineInfo.Values[dictLineInfo.Count - 1];
			if (!value.IsBroken)
			{
				DrawOneLine(chartScale, value);
			}
		}
		else
		{
			for (int num = dictLineInfo.Count - 1; num >= 0; num--)
			{
				DrawOneLine(chartScale, dictLineInfo.Values[num]);
			}
		}
	}

	private void DrawOneLine(ChartScale chartScale, LineInfo lineInfo)
	{
		if (lineInfo == null)
		{
			return;
		}
		int barStart = lineInfo.BarStart;
		bool isTop = lineInfo.IsTop;
		bool isBroken = lineInfo.IsBroken;
		int barEnd = lineInfo.BarEnd;
		int fromIndex = ChartBars.FromIndex;
		int toIndex = ChartBars.ToIndex;
		if (barStart >= barEnd || barStart > toIndex || barEnd < fromIndex)
		{
			return;
		}
		double price = lineInfo.Price;
		if (!isTop)
		{
			if (MathExtentions.ApproxCompare(price, chartScale.MinValue) <= 0)
			{
				return;
			}
		}
		else if (MathExtentions.ApproxCompare(price, chartScale.MaxValue) >= 0)
		{
			return;
		}
		Stroke val = (isBroken ? ((!isTop) ? LineInactiveBottom : LineInactiveTop) : ((!isTop) ? LineActiveBottom : LineActiveTop));
		Brush brush = val.Brush;
		if (!BrushExtensions.IsTransparent(brush))
		{
			float width = val.Width;
			SharpDX.Direct2D1.StrokeStyle strokeStyle = val.StrokeStyle;
			float num = ChartControl.GetXByBarIndex(ChartBars, barStart);
			float num2 = ChartControl.GetXByBarIndex(ChartBars, barEnd);
			float num3 = chartScale.GetYByValue(price);
			Vector2 val2 = new Vector2(num, num3);
			Vector2 val3 = new Vector2(num2, num3);
			RenderTarget.AntialiasMode = (SharpDX.Direct2D1.AntialiasMode)0;
			using (SharpDX.Direct2D1.Brush dxBrush = DxExtensions.ToDxBrush(brush, RenderTarget))
				RenderTarget.DrawLine(val2, val3, dxBrush, width, strokeStyle);
		}
	}

	private void AddMarker(int barIndex, bool isBullish, SignalInfo signalInfo)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo
			{
				BarIndex = barIndex,
				IsBullish = isBullish,
				SignalInfo = signalInfo
			};
			if (dictMarkers.ContainsKey(barIndex))
			{
				dictMarkers[barIndex] = value;
			}
			else
			{
				dictMarkers.Add(barIndex, value);
			}
		}
	}

	private void DrawMarkers(ChartScale chartScale)
	{
		if (!MarkerEnabled || !isCustomMarkerRenderingMethod || dictMarkers == null || dictMarkers.Count <= 0)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= Math.Min(CurrentBar, ChartBars.ToIndex); i++)
		{
			if (dictMarkers.ContainsKey(i))
			{
				DrawOneMarker(chartScale, dictMarkers[i]);
			}
		}
	}

	private void DrawOneMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		bool isBullish = markerInfo.IsBullish;
		SignalInfo signalInfo = markerInfo.SignalInfo;
		int barIndex = markerInfo.BarIndex;
		if ((!isBullish && MathExtentions.ApproxCompare(High.GetValueAt(barIndex), chartScale.MaxValue) >= 0) || (isBullish && MathExtentions.ApproxCompare(Low.GetValueAt(barIndex), chartScale.MinValue) <= 0))
		{
			return;
		}
		Brush brush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
		if (BrushExtensions.IsTransparent(brush))
		{
			return;
		}
		string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Break) ? ((!isBullish) ? MarkerStringBreakDown : MarkerStringBreakUp) : ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback)));
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.Replace(" + ", "\n");
			int num = (isBullish ? 1 : (-1));
			float num2 = ChartControl.GetXByBarIndex(ChartBars, barIndex);
			double num3;
			if (signalInfo != SignalInfo.Trend)
			{
				num3 = chartScale.GetYByValue(((!isBullish) ? High : Low).GetValueAt(barIndex)) + num * MarkerOffset;
			}
			else
			{
				double num4 = Math.Min(Low.GetValueAt(barIndex), TrailingStop.GetValueAt(barIndex));
				double num5 = Math.Max(High.GetValueAt(barIndex), TrailingStop.GetValueAt(barIndex));
				num3 = chartScale.GetYByValue((!isBullish) ? num5 : num4) + num * MarkerOffset;
			}
			using (var textFormat = MarkerFont.ToDirectWriteTextFormat())
			{
				textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var textLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, textFormat, 200, 200))
				{
					float textHeight = textLayout.Metrics.Height;
					float x = num2 - 100;
					float y = (num < 0) ? (float)num3 - textHeight : (float)num3;
					using (var dxBrush = DxExtensions.ToDxBrush(brush, RenderTarget))
					{
						RenderTarget.DrawTextLayout(new SharpDX.Vector2(x, y), textLayout, dxBrush);
					}
				}
			}
		}
	}

	private void PrintMarker(bool isBullish, SignalInfo signalInfo)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string tag = "gbPANAKanal.marker." + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((signalInfo != SignalInfo.Trend) ? ((!isBullish) ? High[0] : Low[0]) : ((!isBullish) ? Math.Max(High[0], TrailingStop[0]) : Math.Min(Low[0], TrailingStop[0])));
			string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Break) ? ((!isBullish) ? MarkerStringBreakDown : MarkerStringBreakUp) : ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback)));
			text = text.Replace(" + ", "\n");
			int num = (int)(MarkerFont.Size * 1.4 * text.Split('\n').Length);
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text, 0, y, yPixelOffset, textBrush, MarkerFont, System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private int GetStateKeltner(int prevState, double middleBandValue)
	{
		int num = MathExtentions.ApproxCompare(Close[0], middleBandValue);
		if (num <= 0)
		{
			if (num != 0)
			{
				return -1;
			}
			if (prevState == 1)
			{
				return 1;
			}
			return -1;
		}
		return 1;
	}

	private void PaintBar(bool isUptrend, bool isToggleClickEvent = false, int barIndex = 0)
	{
		if (!isCharting || !BarEnabled)
		{
			return;
		}
		Brush brush = ((!isUptrend) ? BarDowntrend : BarUptrend);
		int num = ((!isToggleClickEvent) ? MathExtentions.ApproxCompare(Close[0], Open[0]) : MathExtentions.ApproxCompare(Close.GetValueAt(barIndex), Open.GetValueAt(barIndex)));
		int num2 = (isUptrend ? 1 : (-1));
		int num3 = CurrentBar - barIndex;
		if (BarOutlineEnabled && !BrushExtensions.IsTransparent(brush))
		{
			if (!isToggleClickEvent)
			{
				CandleOutlineBrush = brush;
			}
			else
			{
				CandleOutlineBrushes[num3] = brush;
			}
		}
		if (!BarBiasBased)
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = brush;
				}
				else
				{
					BarBrushes[num3] = brush;
				}
			}
		}
		else if (!BrushExtensions.IsTransparent(brush))
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
				else
				{
					BarBrushes[num3] = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
			}
		}
		else if (num2 * num < 0)
		{
			if (!isToggleClickEvent)
			{
				BarBrush = Brushes.Transparent;
			}
			else
			{
				BarBrushes[num3] = Brushes.Transparent;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void TriggerAlerts(bool isUptrend, SignalInfo signalInfo)
	{
		if (State == State.Historical || (!PopupEnabled && !SoundEnabled && !EmailEnabled))
		{
			return;
		}
		DateTime now = DateTime.Now;
		if (now < nextAlert)
		{
			return;
		}
		string text = now.ToString("HH:mm");
		string text2 = now.ToString("HH:mm:ss, dd MMM yyyy");
		nextAlert = now + TimeSpan.FromSeconds(AlertBlockingSeconds);
		string text3 = $"{Instrument.FullName} ({BarsPeriod})";
		bool flag = signalInfo == SignalInfo.Trend;
		bool flag2 = signalInfo == SignalInfo.Break;
		string arg = (flag ? "TREND-START" : (flag2 ? ((!isUptrend) ? "BREAK-DOWN" : ": BREAK-UP") : "TREND-PULLBACK"));
		string text4 = string.Format("{0}: {1} alert on {2} at ", "PANA Kanal", arg, text3) + text;
		string popupMessage;
		if (!flag)
		{
			if (!flag2)
			{
				popupMessage = string.Format("The {0} has had a PULLBACK.", (!isUptrend) ? "downtrend" : "uptrend");
			}
			else
			{
				popupMessage = string.Format("The market has broken {0} the {1}.", (!isUptrend) ? "BELOW" : "ABOVE", (!isUptrend) ? "Lower" : "Upper");
			}
		}
		else
		{
			popupMessage = string.Format("The market has entered {0}.", (!isUptrend) ? "a DOWNTREND" : "an UPTREND");
		}
		popupMessage = $"{popupMessage}\n\nAlert chart: {text3}\nAlert time: {text2}";
		string text5 = "\n_______________________\n\n";
		string text6 = popupMessage + text5 + "PANA Kanal by GreyBeard\nWebsite: https://greybeardconsulting.net/";
		if (PopupEnabled && isCharting)
		{
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
					alertWindow.Close();
				alertWindow = new Window
				{
					Title = "PANA Kanal by GreyBeard",
					Width = 400, Height = 250,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Background = PopupBackgroundBrush,
					Opacity = PopupBackgroundOpacity / 100.0,
					Topmost = true,
					Content = new TextBlock
					{
						Text = popupMessage,
						Foreground = PopupTextBrush,
						FontSize = PopupTextSize,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(15)
					}
				};
				alertWindow.Show();
			});
		}
		if (SoundEnabled)
		{
			string text7 = "alert @ " + text2;
			soundPath = string.Concat(str2: flag ? ((!isUptrend) ? SoundDowntrendStart : SoundUptrendStart) : (flag2 ? ((!isUptrend) ? SoundBreakDown : SoundBreakUp) : ((!isUptrend) ? SoundDowntrendPullback : SoundUptrendPullback)), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(text7, (Priority)0, text4, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text4, text6);
		}
	}

	private void OnRearmTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (alertWindow == null || alertWindow.IsVisible)
					{
						if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					}
					else
					{
						rearmTimer.Stop();
					}
				});
			}
		}, (object)e);
	}


}

public class gbPANAKanal_SoundConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (context != null)
		{
			List<string> list = new List<string>();
			FileInfo[] files = new DirectoryInfo(Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (FileInfo fileInfo in files)
			{
				list.Add(fileInfo.Name);
			}
			return new StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbPANAKanal[] cachegbPANAKanal;
		public GreyBeard.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}

		public GreyBeard.gbPANAKanal gbPANAKanal(ISeries<double> input, int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			if (cachegbPANAKanal != null)
				for (int idx = 0; idx < cachegbPANAKanal.Length; idx++)
					if (cachegbPANAKanal[idx] != null && cachegbPANAKanal[idx].Period == period && cachegbPANAKanal[idx].Factor == factor && cachegbPANAKanal[idx].MiddlePeriod == middlePeriod && cachegbPANAKanal[idx].SignalBreakSplit == signalBreakSplit && cachegbPANAKanal[idx].SignalPullbackFindingPeriod == signalPullbackFindingPeriod && cachegbPANAKanal[idx].EqualsInput(input))
						return cachegbPANAKanal[idx];
			return CacheIndicator<GreyBeard.gbPANAKanal>(new GreyBeard.gbPANAKanal(){ Period = period, Factor = factor, MiddlePeriod = middlePeriod, SignalBreakSplit = signalBreakSplit, SignalPullbackFindingPeriod = signalPullbackFindingPeriod }, input, ref cachegbPANAKanal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}

		public Indicators.GreyBeard.gbPANAKanal gbPANAKanal(ISeries<double> input , int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}

		public Indicators.GreyBeard.gbPANAKanal gbPANAKanal(ISeries<double> input , int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}
	}
}

#endregion
