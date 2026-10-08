#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
#endregion

public enum gbThunderZilla_MAType
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
public enum gbThunderZillaTextPosition
{
	BottomLeft = 0,
	BottomRight = 1,
	Center = 2,
	TopLeft = 3,
	TopRight = 4
}

public enum gbThunderZilla_RenderingMethod
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
public class gbThunderZilla : Indicator
{
	private enum SignalTradeInfo
	{
		NoSignal = 0,
		UptrendStart = 1,
		DowntrendStart = -1,
		DowntrendSlowdown = 2,
		UptrendSlowdown = -2,
		UptrendPullback = 3,
		DowntrendPullback = -3,
		MoveStopUp = 4,
		MoveStopDown = -4
	}

	private struct MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalInfo SignalInfo { get; set; }

		}

	private class CountInfo
	{
		public byte CountSW { get; set; }

		public byte CountSM { get; set; }

		public byte Total => (byte)(CountSW + CountSM);

		}

	private enum SignalInfo
	{
		Trend,
		Slowdown,
		Pullback,
		MoveStop
	}

	private const double offsetMultiplierTrend = 30.0;

	private const gbThunderZilla_MAType sumoFastMA1Type = gbThunderZilla_MAType.EMA;

	private const int sumoFastMA1Period = 14;

	private const bool sumoFastMA1SmoothingEnabled = false;

	private const gbThunderZilla_MAType sumoFastMA1SmoothingMethod = gbThunderZilla_MAType.SMA;

	private const int sumoFastMA1SmoothingPeriod = 5;

	private const gbThunderZilla_MAType sumoFastMA2Type = gbThunderZilla_MAType.EMA;

	private const int sumoFastMA2Period = 30;

	private const bool sumoFastMA2SmoothingEnabled = false;

	private const gbThunderZilla_MAType sumoFastMA2SmoothingMethod = gbThunderZilla_MAType.SMA;

	private const int sumoFastMA2SmoothingPeriod = 10;

	private const gbThunderZilla_MAType sumoFastMA3Type = gbThunderZilla_MAType.EMA;

	private const int sumoFastMA3Period = 45;

	private const bool sumoFastMA3SmoothingEnabled = false;

	private const gbThunderZilla_MAType sumoFastMA3SmoothingMethod = gbThunderZilla_MAType.SMA;

	private const int sumoFastMA3SmoothingPeriod = 15;

	private const int sumoSignalSplitFirst = 15;

	private const int sumoSignalSplitSecond = 30;

	private const int oBOSMFIPeriod = 14;

	private const int oBOSMFIThresholdHigh = 70;

	private const int oBOSMFIThresholdLow = 30;

	private const int oBOSRSIPeriod = 14;

	private const int oBOSRSISmooth = 3;

	private const int oBOSRSIThresholdHigh = 70;

	private const int oBOSRSIThresholdLow = 30;

	private const int oBOSStochPeriodD = 7;

	private const int oBOSStochPeriodK = 14;

	private const gbThunderZilla_MAType oBOSStochSmoothingMethod = gbThunderZilla_MAType.SMA;

	private const int oBOSStochSmoothingPeriod = 3;

	private const int oBOSStochThresholdHigh = 70;

	private const int oBOSStochThresholdLow = 30;

	private const int oBOSSafeReversalPeriod = 3;

	private const int defaultMargin = 5;

	private const string toolTipSpace = "  ";

	private string tag;

	private Dictionary<int, MarkerInfo> dictMarkers;

	private Series<double> seriesSWTrendVector;

	private Series<double> seriesSumoMax;

	private Series<double> seriesSumoMin;

	private Series<double> seriesSumoFair;

	private Series<int> seriesOBOSSignalState;

	private Series<double> seriesMinMax;

	private Brush cloudMixBrush;

	private bool swIsUptrend;

	private bool sumoIsUptrend;

	private SignalTradeInfo trendState;

	private Window alertWindow;

	private const string prefix = "gbThunderZilla";

	private const string indicatorName = "ThunderZilla";

	private const string indicatorNameFull = "ThunderZilla by GreyBeard";

	private bool isCharting;

	private bool isCustomRenderingMethod;

	private CountInfo countInfo;

	private bool isTrendOrSwTrendChanged;

	private int sumoSignalTrade;

	private int obosSignalTrade;

	private int crossIndex;

	private double countSignalPerFlat;

	private double countSignalPerTrend;

	private int sumoNextBar = -1;

	private int sumoCountSignalBars;

	private int obosOverlapExitBarIndex = -1;

	private int obosLastSignalState = -1;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	[Display(Name = "Condition: Trend Start", Order = 0, GroupName = "Alerts")]
	public bool ConditionTrendStart { get; set; }

	[Display(Name = "Condition: Trend Slowdown", Order = 1, GroupName = "Alerts")]
	public bool ConditionSlowdown { get; set; }

	[Display(Name = "Condition: Trend Pullback", Order = 2, GroupName = "Alerts")]
	public bool ConditionTrendPullback { get; set; }

	[Display(Name = "Condition: Move Stop", Order = 3, GroupName = "Alerts")]
	public bool ConditionMoveStop { get; set; }

	[Display(Name = "Popup: Enabled", Order = 10, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Background Color", Order = 11, GroupName = "Alerts")]
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
	[Display(Name = "Popup: Background Opacity", Order = 12, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[Display(Name = "Popup: Text Color", Order = 13, GroupName = "Alerts")]
	[XmlIgnore]
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
	[Display(Name = "Popup: Text Size", Order = 14, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 15, GroupName = "Alerts")]
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

	[Display(Name = "Sound: Enabled", Order = 20, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	[Display(Name = "Sound: Uptrend Start", Order = 21, GroupName = "Alerts")]
	public string SoundUptrendStart { get; set; }

	[Display(Name = "Sound: Uptrend Slowdown", Order = 22, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundUptrendSlowdown { get; set; }

	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	[Display(Name = "Sound: Uptrend Pullback", Order = 23, GroupName = "Alerts")]
	public string SoundUptrendPullback { get; set; }

	[Display(Name = "Sound: Downtrend Start", Order = 24, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendStart { get; set; }

	[Display(Name = "Sound: Downtrend Slowdown", Order = 25, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendSlowdown { get; set; }

	[Display(Name = "Sound: Dowtrend Pullback", Order = 26, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendPullback { get; set; }

	[Display(Name = "Sound: Move Stop Up", Order = 27, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundMoveStopUp { get; set; }

	[Display(Name = "Sound: Move Stop Down", Order = 28, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundMoveStopDown { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 29, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 30, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 31, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 32, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 40, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 41, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbThunderZilla_RenderingMethod MarkerRenderingMethod { get; set; }

	[Display(Name = "Marker: Color Bullish", Order = 42, GroupName = "Alerts")]
	[XmlIgnore]
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

	[Display(Name = "Marker: Color Bearish", Order = 43, GroupName = "Alerts")]
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

	[Display(Name = "Marker: String Uptrend Start", Order = 44, GroupName = "Alerts")]
	public string MarkerStringUptrendStart { get; set; }

	[Display(Name = "Marker: String Uptrend Slowdown", Order = 45, GroupName = "Alerts")]
	public string MarkerStringUptrendSlowdown { get; set; }

	[Display(Name = "Marker: String Uptrend Pullback", Order = 46, GroupName = "Alerts")]
	public string MarkerStringUptrendPullback { get; set; }

	[Display(Name = "Marker: String Downtrend Start", Order = 47, GroupName = "Alerts")]
	public string MarkerStringDowntrendStart { get; set; }

	[Display(Name = "Marker: String Downtrend Slowdown", Order = 48, GroupName = "Alerts")]
	public string MarkerStringDowntrendSlowndown { get; set; }

	[Display(Name = "Marker: String Downtrend Pullback", Order = 49, GroupName = "Alerts")]
	public string MarkerStringDowntrendPullback { get; set; }

	[Display(Name = "Marker: String Move Stop Up", Order = 50, GroupName = "Alerts")]
	public string MarkerStringMoveStopUp { get; set; }

	[Display(Name = "Marker: String Move Stop Down", Order = 51, GroupName = "Alerts")]
	public string MarkerStringMoveStopDown { get; set; }

	[Display(Name = "Marker: Font", Order = 60, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 61, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Display(Name = "Alert Blocking (Seconds)", Order = 70, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
	public bool SwitchedOn { get; set; }

	[Display(Name = "Author",  Order = 0,  GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Version", Order = 1,  GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Website", Order = 5,  GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Plot Trend: Enabled", Order = 0, GroupName = "Graphics")]
	public bool PlotSlowEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot Trend: Uptrend", Order = 1, GroupName = "Graphics")]
	public Brush PlotTrendUptrend { get; set; }

	[Browsable(false)]
	public string PlotTrendUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendUptrend);
		}
		set
		{
			PlotTrendUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Trend: Downtrend", Order = 2, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotTrendDowntrend { get; set; }

	[Browsable(false)]
	public string PlotTrendDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendDowntrend);
		}
		set
		{
			PlotTrendDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Trend: Neutral", Order = 3, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotTrendNeutral { get; set; }

	[Browsable(false)]
	public string PlotTrendNeutral_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendNeutral);
		}
		set
		{
			PlotTrendNeutral = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Stop: Enabled", Order = 10, GroupName = "Graphics")]
	public bool PlotStopEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot Stop: Uptrend", Order = 11, GroupName = "Graphics")]
	public Brush PlotStopUptrend { get; set; }

	[Browsable(false)]
	public string PlotStopUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotStopUptrend);
		}
		set
		{
			PlotStopUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Stop: Downtrend", Order = 12, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotStopDowntrend { get; set; }

	[Browsable(false)]
	public string PlotStopDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotStopDowntrend);
		}
		set
		{
			PlotStopDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Enabled", Order = 20, GroupName = "Graphics")]
	public bool BarEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Bar: Uptrend", Order = 21, GroupName = "Graphics")]
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

	[XmlIgnore]
	[Display(Name = "Bar: Downtrend", Order = 22, GroupName = "Graphics")]
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

	[XmlIgnore]
	[Display(Name = "Bar: Neutral", Order = 23, GroupName = "Graphics")]
	public Brush BarNeutral { get; set; }

	[Browsable(false)]
	public string BarNoTrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarNeutral);
		}
		set
		{
			BarNeutral = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Outline Enabled", Order = 70, GroupName = "Graphics")]
	public bool BarOutlineEnabled { get; set; }

	[Display(Name = "Bar: Bias Based", Order = 71, GroupName = "Graphics")]
	public bool BarBiasBased { get; set; }

	[Display(Name = "Cloud: Enabled", Order = 90, GroupName = "Graphics")]
	public bool CloudEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Cloud: Uptrend", Order = 91, GroupName = "Graphics")]
	public Brush CloudUptrend { get; set; }

	[Browsable(false)]
	public string CloudUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudUptrend);
		}
		set
		{
			CloudUptrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Cloud: Downtrend", Order = 92, GroupName = "Graphics")]
	public Brush CloudDowntrend { get; set; }

	[Browsable(false)]
	public string CloudDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudDowntrend);
		}
		set
		{
			CloudDowntrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Cloud: Neutral", Order = 93, GroupName = "Graphics")]
	public Brush CloudNeutral { get; set; }

	[Browsable(false)]
	public string CloudNeutral_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudNeutral);
		}
		set
		{
			CloudNeutral = Serialize.StringToBrush(value);
		}
	}
	[Display(Name = "Cloud: Opacity", Order = 94, GroupName = "Graphics")]
	public int CloudOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: MA Type", Order = 0, GroupName = "Parameters")]
	public gbThunderZilla_MAType TrendMAType { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: Period", Order = 1, GroupName = "Parameters")]
	public int TrendPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: Smoothing Enabled", Order = 2, GroupName = "Parameters")]
	public bool TrendSmoothingEnabled { get; set; }

	[Display(Name = "Trend: Smoothing Method", Order = 3, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbThunderZilla_MAType TrendSmoothingMethod { get; set; }
	[Display(Name = "Trend: Smoothing Period", Order = 4, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int TrendSmoothingPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Stop: Offset Multiplier (Ticks)", Order = 10, GroupName = "Parameters")]
	public double StopOffsetMultiplierStop { get; set; }

	[Display(Name = "Signal: Quantity Per Flat", Order = 20, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalQuantityPerFlat { get; set; }

	[Display(Name = "Signal: Quantity Per Trend", Order = 22, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalQuantityPerTrend { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Trend => Values[0];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Stop => Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Trend => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => Values[3];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "ThunderZilla by GreyBeard" + GetUserNote();
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

	private double GetMA(ISeries<double> input, gbThunderZilla_MAType maType, int period)
	{
		switch (maType)
		{
			case gbThunderZilla_MAType.EMA: return EMA(input, period)[0];
			case gbThunderZilla_MAType.SMA: return SMA(input, period)[0];
			case gbThunderZilla_MAType.WMA: return WMA(input, period)[0];
			case gbThunderZilla_MAType.HMA: return HMA(input, period)[0];
			case gbThunderZilla_MAType.DEMA: return DEMA(input, period)[0];
			case gbThunderZilla_MAType.TEMA: return TEMA(input, period)[0];
			case gbThunderZilla_MAType.TMA: return TMA(input, period)[0];
			case gbThunderZilla_MAType.LinReg: return LinReg(input, period)[0];
			case gbThunderZilla_MAType.VWMA: return VWMA(input, period)[0];
			case gbThunderZilla_MAType.WilderMA: return EMA(input, 2 * period - 1)[0];
			case gbThunderZilla_MAType.ZLEMA: return ZLEMA(input, period)[0];
			default: return SMA(input, period)[0];
		}
	}

	private double GetMASmoothed(ISeries<double> input, gbThunderZilla_MAType maType, int period, bool smoothEnabled, gbThunderZilla_MAType smoothMethod, int smoothPeriod)
	{
		double val = GetMA(input, maType, period);
		return val;
	}

	private static Brush CreateAverageBrush(Brush a, Brush b)
	{
		var ca = ((SolidColorBrush)a).Color;
		var cb = ((SolidColorBrush)b).Color;
		var avg = System.Windows.Media.Color.FromArgb(255, (byte)((ca.R + cb.R) / 2), (byte)((ca.G + cb.G) / 2), (byte)((ca.B + cb.B) / 2));
		var brush = new SolidColorBrush(avg);
		brush.Freeze();
		return brush;
	}

	private string FormatMarkerString(string text)
	{
		return text.Replace(" + ", "\n");
	}

	private int ComputeTextHeight(string text, SimpleFont font)
	{
		int lines = 1;
		foreach (char c in text)
			if (c == '\n') lines++;
		return (int)(font.Size * 1.4 * lines);
	}

	private void DrawTextOnChart(string text, SimpleFont font, float x, float y, int xOffset, int direction, Brush wpfBrush, int dpi, SharpDX.Direct2D1.RenderTarget rt)
	{
		if (rt == null || string.IsNullOrEmpty(text)) return;

		using (var dxBrush = wpfBrush.ToDxBrush(rt))
		{
			using (var format = font.ToDirectWriteTextFormat())
			{
				format.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, format, 200f, 200f))
				{
					float textH = layout.Metrics.Height;
					float drawX = x - 100f + xOffset;
					// direction > 0 = bullish (below bar): text starts at anchor going down
					// direction < 0 = bearish (above bar): text ends at anchor
					float drawY = (direction < 0) ? y - textH : y;

					rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), layout, dxBrush);
				}
			}
		}
	}

	protected override void OnStateChange()
	{
		switch (State)
		{
			case State.SetDefaults:
				Description = "Trend strength indicator with configurable MA smoothing.";
				Name = "gbThunderZilla";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				BarsRequiredToPlot = 0;
				ShowTransparentPlotsInDataBox = true;
				ConditionTrendStart = false;
				ConditionSlowdown = true;
				ConditionTrendPullback = true;
				ConditionMoveStop = true;
				PopupEnabled = false;
				PopupBackgroundBrush = Brushes.Gold;
				PopupBackgroundOpacity = 60;
				PopupTextBrush = Brushes.DarkSlateGray;
				PopupTextSize = 16;
				PopupButtonBrush = Brushes.Transparent;
				SoundEnabled = false;
				SoundUptrendStart = "Alert4.wav";
				SoundUptrendSlowdown = "Alert1.wav";
				SoundUptrendPullback = "Alert4.wav";
				SoundDowntrendStart = "Alert3.wav";
				SoundDowntrendSlowdown = "Alert2.wav";
				SoundDowntrendPullback = "Alert3.wav";
				SoundMoveStopUp = "Alert1.wav";
				SoundMoveStopDown = "Alert2.wav";
				SoundRearmEnabled = true;
				SoundRearmSeconds = 5;
				EmailEnabled = false;
				EmailReceiver = "receiver@example.com";
				MarkerEnabled = true;
				MarkerRenderingMethod = gbThunderZilla_RenderingMethod.Custom;
				MarkerBrushBullish = Brushes.DodgerBlue;
				MarkerBrushBearish = Brushes.HotPink;
				MarkerStringUptrendStart = "\u25b2 + Trend";
				MarkerStringUptrendSlowdown = "\u2b18";
				MarkerStringUptrendPullback = "\u2b06 + Buy";
				MarkerStringDowntrendStart = "Trend + \u25bc";
				MarkerStringDowntrendSlowndown = "\u2b19";
				MarkerStringDowntrendPullback = "Sell + \u2b07";
				MarkerStringMoveStopUp = "\u235f";
				MarkerStringMoveStopDown = "\u235f";
				MarkerFont = new SimpleFont("Arial", 20);
				MarkerOffset = 10;
				AlertBlockingSeconds = 60;
				SwitchedOn = true;
				ScreenDPI = 100;
				PlotSlowEnabled = true;
				PlotTrendUptrend = Brushes.DodgerBlue;
				PlotTrendDowntrend = Brushes.HotPink;
				PlotTrendNeutral = Brushes.Gold;
				PlotStopEnabled = true;
				PlotStopUptrend = Brushes.DodgerBlue;
				PlotStopDowntrend = Brushes.HotPink;
				BarEnabled = true;
				BarUptrend = Brushes.LimeGreen;
				BarDowntrend = Brushes.Red;
				BarNeutral = Brushes.Goldenrod;
				BarOutlineEnabled = true;
				BarBiasBased = true;
				CloudEnabled = true;
				CloudUptrend = Brushes.DodgerBlue;
				CloudDowntrend = Brushes.HotPink;
				CloudNeutral = Brushes.DarkGoldenrod;
				CloudOpacity = 30;
				TrendMAType = gbThunderZilla_MAType.SMA;
				TrendPeriod = 100;
				TrendSmoothingEnabled = false;
				TrendSmoothingMethod = gbThunderZilla_MAType.EMA;
				TrendSmoothingPeriod = 10;
				StopOffsetMultiplierStop = 60.0;
				SignalQuantityPerFlat = 2;
				SignalQuantityPerTrend = 999;
				IndicatorZOrder = 0;
				UserNote = "instrument (period)";
				AddPlot(new Stroke(Brushes.Gold, DashStyleHelper.Solid, 5f), PlotStyle.Line, "Trend");
				AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Dot, 2f), PlotStyle.Line, "Stop");
				AddPlot(new Stroke(Brushes.Transparent, 1f), PlotStyle.Line, "Signal: Trend");
				AddPlot(new Stroke(Brushes.Transparent, 1f), PlotStyle.Line, "Signal: Trade");
				break;

			case State.Configure:
				seriesMinMax = new Series<double>(this, MaximumBarsLookBack.Infinite);
				seriesSWTrendVector = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoMax = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoMin = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoFair = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesOBOSSignalState = new Series<int>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				rearmTimer = new DispatcherTimer();
				rearmTimer.Interval = TimeSpan.FromMilliseconds(100.0);
				rearmTimer.Tick += OnRearmTimerTick;
				cloudMixBrush = CreateAverageBrush(CloudUptrend, CloudDowntrend);
				isCustomRenderingMethod = MarkerRenderingMethod == gbThunderZilla_RenderingMethod.Custom;
				if (isCustomRenderingMethod)
				{
					dictMarkers = new Dictionary<int, MarkerInfo>();
				}
				break;

			case State.DataLoaded:
				if (ScreenDPI < 100)
				{
					try
					{
						ScreenDPI = (int)(96 * System.Windows.PresentationSource.FromVisual(ChartControl)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
					}
					catch
					{
						ScreenDPI = 96;
					}
				}
				if (IndicatorZOrder != 0)
				{
					SetZOrder(IndicatorZOrder);
				}
				isCharting = ChartControl != null;
				break;

			case State.Historical:
				break;

			case State.Terminated:
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (alertWindow != null)
						{
							alertWindow.Close();
							alertWindow = null;
						}
					});
				}
				if (rearmTimer != null)
				{
					rearmTimer.Stop();
					rearmTimer.Tick -= OnRearmTimerTick;
					rearmTimer = null;
				}
				break;
		}
	}

	protected override void OnBarUpdate()
	{
		double num = Open[0];
		double num2 = Close[0];
		double num3 = High[0];
		double num4 = Low[0];
		isTrendOrSwTrendChanged = false;
		ComputeSolarWindRK(num, num2);
		ComputeSumoPullback(num, num3, num4, num2);
		ComputeMultiOscOBOSOverlap();
		if (CurrentBar == 0)
		{
			return;
		}
		SignalTradeInfo signalTradeInfo = SignalTradeInfo.UptrendStart;
		SignalTradeInfo signalTradeInfo2 = SignalTradeInfo.DowntrendStart;
		SignalTradeInfo signalTradeInfo3 = SignalTradeInfo.NoSignal;
		if (CurrentBar != 1)
		{
			SignalTradeInfo signalTradeInfo4 = signalTradeInfo3;
			if (trendState != signalTradeInfo3)
			{
				if (trendState != signalTradeInfo)
				{
					if (!swIsUptrend || !sumoIsUptrend)
					{
						if (!swIsUptrend || sumoIsUptrend)
						{
							if (!swIsUptrend && sumoIsUptrend)
							{
								countInfo.CountSM = 0;
							}
						}
						else
						{
							countInfo.CountSW = 0;
						}
					}
					else
					{
						trendState = signalTradeInfo;
						countInfo.CountSW = 1;
						countInfo.CountSM = 1;
						isTrendOrSwTrendChanged = true;
						crossIndex = CurrentBar;
					}
					if (countInfo.Total == 0)
					{
						trendState = signalTradeInfo3;
						countInfo.CountSW = 0;
						countInfo.CountSM = 0;
						crossIndex = CurrentBar;
					}
				}
				else
				{
					if (swIsUptrend || sumoIsUptrend)
					{
						if (!swIsUptrend || sumoIsUptrend)
						{
							if (!swIsUptrend && sumoIsUptrend)
							{
								countInfo.CountSW = 0;
							}
						}
						else
						{
							countInfo.CountSM = 0;
						}
					}
					else
					{
						trendState = signalTradeInfo2;
						countInfo.CountSW = 1;
						countInfo.CountSM = 1;
						isTrendOrSwTrendChanged = true;
						crossIndex = CurrentBar;
					}
					if (countInfo.Total == 0)
					{
						trendState = signalTradeInfo3;
						countInfo.CountSW = 0;
						countInfo.CountSM = 0;
						crossIndex = CurrentBar;
					}
				}
			}
			else if (!swIsUptrend || !sumoIsUptrend)
			{
				if (!swIsUptrend && !sumoIsUptrend)
				{
					trendState = signalTradeInfo2;
					countInfo.CountSW = 1;
					countInfo.CountSM = 1;
					isTrendOrSwTrendChanged = true;
					crossIndex = CurrentBar;
				}
			}
			else
			{
				trendState = signalTradeInfo;
				countInfo.CountSW = 1;
				countInfo.CountSM = 1;
				isTrendOrSwTrendChanged = true;
				crossIndex = CurrentBar;
			}
			Signal_Trend[0] = (double)trendState;
			if (Signal_Trend[0] != Signal_Trend[1] && Math.Abs(Signal_Trend[0]) == 1.0)
			{
				countSignalPerTrend = 0.0;
			}
			double num5 = Instrument.MasterInstrument.RoundToTickSize(Trend[0]);
			double num6 = Instrument.MasterInstrument.RoundToTickSize(Trend[1]);
			double num7 = Instrument.MasterInstrument.RoundToTickSize(Stop[0]);
			double num8 = Instrument.MasterInstrument.RoundToTickSize(Stop[1]);
			if (MathExtentions.ApproxCompare(num7, num8) != 0)
			{
				countSignalPerFlat = 0.0;
			}
			int barIndex = CurrentBars[0];
			int num9 = Convert.ToInt32(Signal_Trend[1]);
			if (trendState != SignalTradeInfo.NoSignal)
			{
				bool flag = trendState == signalTradeInfo;
				if (trendState == (SignalTradeInfo)num9)
				{
					int num10 = (flag ? 1 : (-1));
					bool flag2 = MathExtentions.ApproxCompare(num2, num) * num10 > 0 && MathExtentions.ApproxCompare(Close[1], Open[1]) * num10 < 0;
					if (!(countSignalPerTrend >= (double)SignalQuantityPerTrend) && countSignalPerFlat < (double)SignalQuantityPerFlat && flag2)
					{
						if (sumoSignalTrade == 0)
						{
							double num11 = Instrument.MasterInstrument.RoundToTickSize(seriesSWTrendVector[0]);
							double num12 = Instrument.MasterInstrument.RoundToTickSize(seriesSumoFair[0]);
							bool flag3 = MathExtentions.ApproxCompare(num3, num12) >= 0 && MathExtentions.ApproxCompare(num4, num12) <= 0;
							bool flag4 = MathExtentions.ApproxCompare(num3, num5) >= 0 && MathExtentions.ApproxCompare(num4, num5) <= 0;
							bool flag5 = MathExtentions.ApproxCompare(num3, num11) >= 0 && MathExtentions.ApproxCompare(num4, num11) <= 0;
							if ((flag3 && flag5) || (flag3 && flag4))
							{
								if (ConditionTrendPullback)
								{
									if (!isCustomRenderingMethod)
									{
										PrintMarker(SignalInfo.Pullback, flag);
									}
									else
									{
										AddMarker(barIndex, flag, SignalInfo.Pullback);
									}
									TriggerAlerts(SignalInfo.Pullback, flag);
								}
								countSignalPerTrend += 1.0;
								countSignalPerFlat += 1.0;
								signalTradeInfo4 = ((!flag) ? SignalTradeInfo.DowntrendPullback : SignalTradeInfo.UptrendPullback);
							}
						}
						else
						{
							if (ConditionTrendPullback && ((sumoSignalTrade == 1 && flag) || (sumoSignalTrade == -1 && !flag)))
							{
								if (!isCustomRenderingMethod)
								{
									PrintMarker(SignalInfo.Pullback, flag);
								}
								else
								{
									AddMarker(barIndex, flag, SignalInfo.Pullback);
								}
								TriggerAlerts(SignalInfo.Pullback, flag);
							}
							countSignalPerTrend += 1.0;
							countSignalPerFlat += 1.0;
							signalTradeInfo4 = ((!flag) ? SignalTradeInfo.DowntrendPullback : SignalTradeInfo.UptrendPullback);
						}
					}
					if (swIsUptrend == flag && MathExtentions.ApproxCompare(num7, num5) * num10 > 0 && MathExtentions.ApproxCompare(num8, num6) * num10 <= 0)
					{
						if (ConditionMoveStop)
						{
							if (!isCustomRenderingMethod)
							{
								PrintMarker(SignalInfo.MoveStop, flag);
							}
							else
							{
								AddMarker(barIndex, flag, SignalInfo.MoveStop);
							}
							TriggerAlerts(SignalInfo.MoveStop, flag);
						}
						signalTradeInfo4 = ((!flag) ? SignalTradeInfo.MoveStopDown : SignalTradeInfo.MoveStopUp);
					}
				}
				else
				{
					if (ConditionTrendStart)
					{
						if (!isCustomRenderingMethod)
						{
							PrintMarker(SignalInfo.Trend, flag);
						}
						else
						{
							AddMarker(barIndex, trendState > SignalTradeInfo.NoSignal, SignalInfo.Trend);
						}
						TriggerAlerts(SignalInfo.Trend, flag);
					}
					countSignalPerFlat = 0.0;
					signalTradeInfo4 = ((!flag) ? signalTradeInfo2 : signalTradeInfo);
				}
			}
			if (obosSignalTrade != 0)
			{
				if (ConditionSlowdown)
				{
					if (!isCustomRenderingMethod)
					{
						PrintMarker(SignalInfo.Slowdown, obosSignalTrade > 0);
					}
					else
					{
						AddMarker(barIndex, obosSignalTrade > 0, SignalInfo.Slowdown);
					}
					TriggerAlerts(SignalInfo.Slowdown, obosSignalTrade > 0);
				}
				signalTradeInfo4 = ((obosSignalTrade != 1) ? SignalTradeInfo.UptrendSlowdown : SignalTradeInfo.DowntrendSlowdown);
			}
			Signal_Trade[0] = (double)signalTradeInfo4;
			if (isCharting)
			{
				if (PlotSlowEnabled)
				{
					Brush brush = ((trendState == SignalTradeInfo.NoSignal) ? PlotTrendNeutral : ((trendState <= SignalTradeInfo.NoSignal) ? PlotTrendDowntrend : PlotTrendUptrend));
					if (!BrushExtensions.IsTransparent(brush))
					{
						PlotBrushes[0][0] = brush;
					}
				}
				if (PlotStopEnabled)
				{
					Brush brush2 = ((trendState == SignalTradeInfo.NoSignal) ? Brushes.Transparent : ((trendState == signalTradeInfo && swIsUptrend) ? PlotStopUptrend : ((trendState == signalTradeInfo2 && !swIsUptrend) ? PlotStopDowntrend : Brushes.Transparent)));
					if (!isTrendOrSwTrendChanged)
					{
						PlotBrushes[1][0] = brush2;
					}
					else if ((int)Plots[1].PlotStyle == 6 || (int)Plots[1].PlotStyle == 7)
					{
						PlotBrushes[1][0] = Brushes.Transparent;
					}
					else
					{
						PlotBrushes[1][0] = brush2;
					}
				}
			}
			if (SwitchedOn)
			{
				PaintBar(trendState);
			}
			if (!isCharting || !CloudEnabled || CloudOpacity <= 0)
			{
				return;
			}
			double num13 = 0.4 * Math.Abs(num - num2);
			double num14 = num - num5;
			bool flag6 = MathExtentions.ApproxCompare(num, num5) >= 0 && MathExtentions.ApproxCompare(num2, num5) >= 0;
			bool flag7 = MathExtentions.ApproxCompare(num, num5) < 0 && MathExtentions.ApproxCompare(num2, num5) < 0;
			double num15 = Math.Max(num, num2);
			double num16 = Math.Min(num, num2);
			double num17 = ((trendState == SignalTradeInfo.NoSignal) ? ((MathExtentions.ApproxCompare(num15, num5) <= 0) ? num16 : num15) : (flag6 ? num15 : (flag7 ? num16 : ((MathExtentions.ApproxCompare(num14, num13) <= 0) ? num16 : num15))));
			seriesMinMax[0] = num17;
			bool flag8;
			Brush brush3 = ((flag8 = crossIndex == CurrentBar) ? cloudMixBrush : ((trendState == signalTradeInfo) ? CloudUptrend : ((trendState != signalTradeInfo2) ? CloudNeutral : CloudDowntrend)));
			if (!BrushExtensions.IsTransparent(brush3))
			{
				int startBarsAgo = (flag8 ? 1 : (CurrentBar - crossIndex));
				tag = "gbThunderZilla.cloud." + ((!flag8) ? crossIndex : (CurrentBar - 1));
				Region region = Draw.Region(this, tag, startBarsAgo, 0, Trend, seriesMinMax, null, brush3, CloudOpacity);
				if (!SwitchedOn)
				{
					((NinjaScript)region).IsVisible = false;
				}
			}
		}
		else
		{
			trendState = ((swIsUptrend && sumoIsUptrend) ? signalTradeInfo : ((!swIsUptrend && !sumoIsUptrend) ? signalTradeInfo2 : signalTradeInfo3));
			Series<double> signal_Trend = Signal_Trend;
			double num18 = (Signal_Trade[0] = (double)trendState);
			signal_Trend[0] = num18;
			countInfo = new CountInfo();
			if (trendState != signalTradeInfo3)
			{
				countInfo.CountSW = 1;
				countInfo.CountSM = 1;
				isTrendOrSwTrendChanged = true;
				crossIndex = CurrentBar;
			}
			else
			{
				countInfo.CountSW = 0;
				countInfo.CountSM = 0;
			}
		}
	}

	private void ComputeSolarWindRK(double open, double close)
	{
		double num = Instrument.MasterInstrument.RoundToTickSize(StopOffsetMultiplierStop * TickSize);
		double num2 = Instrument.MasterInstrument.RoundToTickSize(close - num);
		double num3 = Instrument.MasterInstrument.RoundToTickSize(close + num);
		double num4 = Instrument.MasterInstrument.RoundToTickSize(30.0 * TickSize);
		double num5 = close - num4;
		double num6 = close + num4;
		if (CurrentBar != 0)
		{
			double num7 = seriesSWTrendVector[1];
			double num8 = Stop[1];
			if (!swIsUptrend)
			{
				if (MathExtentions.ApproxCompare(close, num8) <= 0)
				{
					if (MathExtentions.ApproxCompare(num6, num7) >= 0)
					{
						seriesSWTrendVector[0] = num7;
					}
					else
					{
						seriesSWTrendVector[0] = num6;
					}
					Stop[0] = Math.Min(num3, num8);
					return;
				}
				swIsUptrend = true;
				isTrendOrSwTrendChanged = true;
				if (MathExtentions.ApproxCompare(num5, num7) <= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num5;
				}
				Stop[0] = num2;
			}
			else if (MathExtentions.ApproxCompare(close, num8) >= 0)
			{
				if (MathExtentions.ApproxCompare(num5, num7) <= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num5;
				}
				Stop[0] = Math.Max(num2, num8);
			}
			else
			{
				swIsUptrend = false;
				isTrendOrSwTrendChanged = true;
				if (MathExtentions.ApproxCompare(num6, num7) >= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num6;
				}
				Stop[0] = num3;
			}
		}
		else
		{
			swIsUptrend = MathExtentions.ApproxCompare(close, open) > 0;
			if (!swIsUptrend)
			{
				seriesSWTrendVector[0] = num6;
				Stop[0] = num3;
			}
			else
			{
				seriesSWTrendVector[0] = num5;
				Stop[0] = num2;
			}
		}
	}

	private void ComputeSumoPullback(double open0, double high0, double low0, double close0)
	{
		if (CurrentBar == 0)
		{
			return;
		}
		sumoSignalTrade = 0;
		double num = GetMASmoothed(Input, TrendMAType, TrendPeriod, TrendSmoothingEnabled, TrendSmoothingMethod, TrendSmoothingPeriod);
		double num2 = EMA(Input, 14)[0];
		double num3 = EMA(Input, 30)[0];
		double num4 = EMA(Input, 45)[0];
		double[] source = new double[4] { num, num2, num3, num4 };
		double num5 = source.Max();
		double num6 = source.Min();
		if (MathExtentions.ApproxCompare(close0, open0) <= 0 || MathExtentions.ApproxCompare(Close[1], Open[1]) >= 0)
		{
			if (MathExtentions.ApproxCompare(close0, open0) < 0 && MathExtentions.ApproxCompare(Close[1], Open[1]) > 0 && MathExtentions.ApproxCompare(num6, low0) > 0 && MathExtentions.ApproxCompare(num5, high0) < 0 && MathExtentions.ApproxCompare(num, num5) == 0)
			{
				if (sumoCountSignalBars != 0 || sumoNextBar >= 0)
				{
					if (sumoCountSignalBars != 1)
					{
						if (sumoCountSignalBars == 2 && CurrentBar > sumoNextBar)
						{
							sumoSignalTrade = -1;
						}
					}
					else if (CurrentBar >= sumoNextBar)
					{
						sumoSignalTrade = -1;
						sumoCountSignalBars++;
						sumoNextBar = CurrentBar + 30;
					}
				}
				else
				{
					sumoSignalTrade = -1;
					sumoCountSignalBars++;
					sumoNextBar = CurrentBar + 15;
				}
			}
		}
		else if (MathExtentions.ApproxCompare(num6, low0) > 0 && MathExtentions.ApproxCompare(num5, high0) < 0 && MathExtentions.ApproxCompare(num, num6) == 0)
		{
			if (sumoCountSignalBars != 0 || sumoNextBar >= 0)
			{
				if (sumoCountSignalBars != 1)
				{
					if (sumoCountSignalBars == 2 && CurrentBar > sumoNextBar)
					{
						sumoSignalTrade = 1;
					}
				}
				else if (CurrentBar >= sumoNextBar)
				{
					sumoSignalTrade = 1;
					sumoCountSignalBars++;
					sumoNextBar = CurrentBar + 30;
				}
			}
			else
			{
				sumoSignalTrade = 1;
				sumoCountSignalBars++;
				sumoNextBar = CurrentBar + 15;
			}
		}
		if (CurrentBar > sumoNextBar && sumoCountSignalBars != 0)
		{
			sumoCountSignalBars = 0;
			sumoNextBar = -1;
		}
		Trend[0] = num;
		seriesSumoMax[0] = num5;
		seriesSumoMin[0] = num6;
		double num7 = Trend[0];
		double num8 = Trend[1];
		if (CurrentBar == 1)
		{
			sumoIsUptrend = MathExtentions.ApproxCompare(num5, num6) > 0;
		}
		if (!sumoIsUptrend || MathExtentions.ApproxCompare(seriesSumoMin[0], num7) >= 0 || (MathExtentions.ApproxCompare(seriesSumoMax[0], num7) != 0 && MathExtentions.ApproxCompare(seriesSumoMin[1], num8) != 0))
		{
			if (!sumoIsUptrend && MathExtentions.ApproxCompare(seriesSumoMax[0], num7) > 0 && (MathExtentions.ApproxCompare(seriesSumoMin[0], num7) == 0 || MathExtentions.ApproxCompare(seriesSumoMax[1], num8) == 0))
			{
				sumoIsUptrend = true;
			}
		}
		else
		{
			sumoIsUptrend = false;
		}
		seriesSumoFair[0] = source.Average();
	}

	private void ComputeMultiOscOBOSOverlap()
	{
		if (CurrentBar == 0)
		{
			return;
		}
		double oscValue = MFI(Input, 14)[0];
		double oscValue2 = RSI(Input, 14, 3)[0];
		double oscValue3 = Stochastics(Input, 14, 7, 3).D[0];
		int stateSimple = GetStateSimple(oscValue, 70.0, 30.0);
		int stateSimple2 = GetStateSimple(oscValue2, 70.0, 30.0);
		int stateSimple3 = GetStateSimple(oscValue3, 70.0, 30.0);
		int num = 0;
		if (stateSimple <= 0 || stateSimple2 <= 0 || stateSimple3 <= 0)
		{
			if (stateSimple < 0 && stateSimple2 < 0 && stateSimple3 < 0)
			{
				num = -1;
			}
		}
		else
		{
			num = 1;
		}
		seriesOBOSSignalState[0] = num;
		if (seriesOBOSSignalState[0] == 0 && seriesOBOSSignalState[1] != 0)
		{
			obosOverlapExitBarIndex = CurrentBar;
			obosLastSignalState = Convert.ToInt32(seriesOBOSSignalState[1]);
		}
		obosSignalTrade = 0;
		if (obosOverlapExitBarIndex > 0 && num == 0 && CurrentBar - obosOverlapExitBarIndex < 3)
		{
			if (obosLastSignalState > 0 && MathExtentions.ApproxCompare(Close[0], Close[1]) < 0)
			{
				obosOverlapExitBarIndex = -1;
				obosSignalTrade = -1;
			}
			if (obosLastSignalState < 0 && MathExtentions.ApproxCompare(Close[0], Close[1]) > 0)
			{
				obosOverlapExitBarIndex = -1;
				obosSignalTrade = 1;
			}
		}
	}

	private int GetStateSimple(double oscValue, double thresholdHigh, double thresholdLow)
	{
		if (MathExtentions.ApproxCompare(oscValue, thresholdHigh) < 0)
		{
			if (MathExtentions.ApproxCompare(oscValue, thresholdLow) > 0)
			{
				return 0;
			}
			return -1;
		}
		return 1;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (isCharting && SwitchedOn && ChartControl != null && !IsInHitTest)
		{
			if (MarkerRenderingMethod == gbThunderZilla_RenderingMethod.Custom)
			{
				DrawMarkers(chartScale);
			}
			base.OnRender(chartControl, chartScale);
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
		if (!MarkerEnabled || dictMarkers == null || dictMarkers.Count <= 0)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= Math.Min(CurrentBars[0], ChartBars.ToIndex); i++)
		{
			if (dictMarkers.ContainsKey(i))
			{
				DrawAMarker(chartScale, dictMarkers[i]);
			}
		}
	}

	private void DrawAMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		bool isBullish = markerInfo.IsBullish;
		SignalInfo signalInfo = markerInfo.SignalInfo;
		string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Slowdown) ? ((!isBullish) ? MarkerStringDowntrendSlowndown : MarkerStringUptrendSlowdown) : ((signalInfo == SignalInfo.Pullback) ? ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback) : ((!isBullish) ? MarkerStringMoveStopDown : MarkerStringMoveStopUp))));
		if (string.IsNullOrWhiteSpace(text) || (!isBullish && MathExtentions.ApproxCompare(High.GetValueAt(markerInfo.BarIndex), chartScale.MaxValue) >= 0) || (isBullish && MathExtentions.ApproxCompare(Low.GetValueAt(markerInfo.BarIndex), chartScale.MinValue) <= 0))
		{
			return;
		}
		Brush brush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
		if (!BrushExtensions.IsTransparent(brush))
		{
			text = FormatMarkerString(text);
			int num = (isBullish ? 1 : (-1));
			float num2 = ChartControl.GetXByBarIndex(ChartBars, markerInfo.BarIndex);
			object obj;
			if (signalInfo == SignalInfo.Trend || signalInfo == SignalInfo.MoveStop)
			{
				ISeries<double> trend = Trend;
				obj = trend;
			}
			else
			{
				obj = ((!isBullish) ? High : Low);
			}
			float num3 = chartScale.GetYByValue(((ISeries<double>)obj).GetValueAt(markerInfo.BarIndex)) + num * MarkerOffset;
			DrawTextOnChart(text, MarkerFont, num2, num3, 0, num, brush, ScreenDPI, RenderTarget);
		}
	}

	private void PrintMarker(SignalInfo signalInfo, bool isBullish)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string text = ((signalInfo == SignalInfo.Trend) ? "trend" : ((signalInfo == SignalInfo.Slowdown) ? "slowdown" : ((signalInfo == SignalInfo.Pullback) ? "pullback" : "move-stop")));
			tag = "gbThunderZilla.marker." + text + "." + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((signalInfo != SignalInfo.MoveStop && signalInfo != SignalInfo.Trend) ? ((!isBullish) ? High[0] : Low[0]) : Trend[0]);
			string text2 = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Slowdown) ? ((!isBullish) ? MarkerStringDowntrendSlowndown : MarkerStringUptrendSlowdown) : ((signalInfo == SignalInfo.Pullback) ? ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback) : ((!isBullish) ? MarkerStringMoveStopDown : MarkerStringMoveStopUp))));
			text2 = FormatMarkerString(text2);
			int num = ComputeTextHeight(text2, MarkerFont);
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text2, 0, y, yPixelOffset, textBrush, MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private void PaintBar(SignalTradeInfo trendState, bool isToggleClickEvent = false, int barIndex = 0)
	{
		if (!isCharting || !BarEnabled)
		{
			return;
		}
		Brush brush = ((trendState == SignalTradeInfo.NoSignal) ? BarNeutral : ((trendState != SignalTradeInfo.UptrendStart) ? BarDowntrend : BarUptrend));
		int num = ((!isToggleClickEvent) ? MathExtentions.ApproxCompare(Close[0], Open[0]) : MathExtentions.ApproxCompare(Close.GetValueAt(barIndex), Open.GetValueAt(barIndex)));
		int num2 = ((trendState != SignalTradeInfo.NoSignal) ? ((trendState == SignalTradeInfo.UptrendStart) ? 1 : (-1)) : 0);
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

	private void TriggerAlerts(SignalInfo signalInfo, bool isBullish)
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
		string text3 = Instrument.FullName + " (" + ((object)BarsPeriod)?.ToString() + ")";
		bool flag = signalInfo == SignalInfo.Trend;
		bool flag2 = signalInfo == SignalInfo.Slowdown;
		bool flag3 = signalInfo == SignalInfo.Pullback;
		string arg = (flag ? "TREND-START" : (flag2 ? "TREND-SLOWDOWN" : (flag3 ? "TREND-PULLBACK" : "MOVE-STOP")));
		string text4 = string.Format("{0}: {1} alert on {2} at ", "ThunderZilla", arg, text3) + text;
		string text5 = (flag ? string.Format("The market has entered {0}.", (!isBullish) ? "a DOWNTREND" : "an UPTREND") : (flag2 ? string.Format("The {0} has slowed down.", (!isBullish) ? "DOWNTREND" : "UPTREND") : (flag3 ? string.Format("The {0} has had a PULLBACK.", (!isBullish) ? "downtrend" : "uptrend") : string.Format("The {0} has had a MOVE STOP.", (!isBullish) ? "downtrend" : "uptrend"))));
		string popupMessage = text5 + "\n\nAlert chart: " + text3 + "\nAlert time: " + text2;
		string text6 = "\n_______________________\n\n";
		string text7 = popupMessage + text6 + "ThunderZilla by GreyBeard\nWebsite: https://greybeardconsulting.net/";
		if (PopupEnabled && isCharting)
		{
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
				{
					alertWindow.Close();
				}
				alertWindow = new Window
				{
					Title = "ThunderZilla by GreyBeard",
					Width = 400,
					Height = 300,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
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
			string text8 = "alert @ " + text2;
			soundPath = string.Concat(str2: flag ? ((!isBullish) ? SoundDowntrendStart : SoundUptrendStart) : (flag2 ? ((!isBullish) ? SoundUptrendSlowdown : SoundDowntrendSlowdown) : (flag3 ? ((!isBullish) ? SoundDowntrendPullback : SoundUptrendPullback) : ((!isBullish) ? SoundMoveStopDown : SoundMoveStopUp))), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(text8, Priority.Low, text4, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text4, text7);
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
					if (alertWindow != null && !alertWindow.IsVisible)
					{
						rearmTimer.Stop();
					}
					else
					{
						if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					}
				});
			}
		}, (object)e);
	}

}
} // end namespace GreyBeard

public class gbThunderZilla_SoundConverter : TypeConverter
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

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbThunderZilla[] cachegbThunderZilla;
		public GreyBeard.gbThunderZilla gbThunderZilla(gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}

		public GreyBeard.gbThunderZilla gbThunderZilla(ISeries<double> input, gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			if (cachegbThunderZilla != null)
				for (int idx = 0; idx < cachegbThunderZilla.Length; idx++)
					if (cachegbThunderZilla[idx] != null && cachegbThunderZilla[idx].TrendMAType == trendMAType && cachegbThunderZilla[idx].TrendPeriod == trendPeriod && cachegbThunderZilla[idx].TrendSmoothingEnabled == trendSmoothingEnabled && cachegbThunderZilla[idx].TrendSmoothingMethod == trendSmoothingMethod && cachegbThunderZilla[idx].TrendSmoothingPeriod == trendSmoothingPeriod && cachegbThunderZilla[idx].StopOffsetMultiplierStop == stopOffsetMultiplierStop && cachegbThunderZilla[idx].SignalQuantityPerFlat == signalQuantityPerFlat && cachegbThunderZilla[idx].SignalQuantityPerTrend == signalQuantityPerTrend && cachegbThunderZilla[idx].EqualsInput(input))
						return cachegbThunderZilla[idx];
			return CacheIndicator<GreyBeard.gbThunderZilla>(new GreyBeard.gbThunderZilla(){ TrendMAType = trendMAType, TrendPeriod = trendPeriod, TrendSmoothingEnabled = trendSmoothingEnabled, TrendSmoothingMethod = trendSmoothingMethod, TrendSmoothingPeriod = trendSmoothingPeriod, StopOffsetMultiplierStop = stopOffsetMultiplierStop, SignalQuantityPerFlat = signalQuantityPerFlat, SignalQuantityPerTrend = signalQuantityPerTrend }, input, ref cachegbThunderZilla);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbThunderZilla gbThunderZilla(gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}

		public Indicators.GreyBeard.gbThunderZilla gbThunderZilla(ISeries<double> input , gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbThunderZilla gbThunderZilla(gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}

		public Indicators.GreyBeard.gbThunderZilla gbThunderZilla(ISeries<double> input , gbThunderZilla_MAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, gbThunderZilla_MAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}
	}
}

#endregion
