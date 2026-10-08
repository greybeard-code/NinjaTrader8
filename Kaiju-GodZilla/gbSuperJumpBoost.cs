#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
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
using SharpDX.Direct2D1;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
public enum gbSuperJumpBoostTextPosition
{
	BottomLeft = 0,
	BottomRight = 1,
	Center = 2,
	TopLeft = 3,
	TopRight = 4
}

public enum gbSuperJumpBoost_RenderingMethod
{
	Builtin,
	Custom
}

[CategoryOrder("Developer", 0)]
[CategoryOrder("General",   1000010)]
[CategoryOrder("Graphics",  1000020)]
[CategoryOrder("Gradient",  1000030)]
[CategoryOrder("Alerts",    1000040)]
[CategoryOrder("Special",   1000060)]
[CategoryOrder("Critical",  1000070)]
public class gbSuperJumpBoost : Indicator
{
	private class ZoneInfo
	{
		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		public double TopPrice { get; set; }

		public double PriceLevel1 { get; set; }

		public double PriceLevel2 { get; set; }

		public double BottomPrice { get; set; }

		public int CountLevel { get; set; }

		public bool IsBullish { get; set; }

		public int CountSignalReturn { get; set; }

		public int Sign
		{
			get
			{
				if (IsBullish)
				{
					return 1;
				}
				return -1;
			}
		}

			public ZoneInfo(int barStart, int barEnd, double topPrice, double priceLevel1, double priceLevel2, double bottomPrice, bool isBullish)
		{
			BarStart = barStart;
			BarEnd = barEnd;
			TopPrice = topPrice;
			PriceLevel1 = priceLevel1;
			PriceLevel2 = priceLevel2;
			BottomPrice = bottomPrice;
			IsBullish = isBullish;
		}

		}

	private class JumpBoostInfo
	{
		public bool IsUptrend { get; set; }

		public double StopCurrentValue { get; set; }

		public int CountSlowdown { get; set; }

		public int SlowdownScan { get; set; }

		public int WeakWeakSplit { get; set; }

		public int NextWeakTrendBar { get; set; }

		public double OffsetBase { get; set; }

		public double OffsetLevel { get; set; }

		public Series<double> SeriesTrendVector { get; set; }

		public Series<int> SeriesSignalTrend { get; set; }

		public JumpBoostInfo(double offsetBase, double offsetLevel, int nextWeakTrendBar, int slowdownScan, int weakWeakSplit, Series<double> seriesTrendVector, Series<int> seriesSignalTrend)
		{
			OffsetBase = offsetBase;
			OffsetLevel = offsetLevel;
			NextWeakTrendBar = nextWeakTrendBar;
			SlowdownScan = slowdownScan;
			WeakWeakSplit = weakWeakSplit;
			SeriesTrendVector = seriesTrendVector;
			SeriesSignalTrend = seriesSignalTrend;
		}

		}

	private class MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalType SignalType { get; set; }

		public MarkerInfo(int barIndex, bool isBullish, SignalType signalType)
		{
			BarIndex = barIndex;
			IsBullish = isBullish;
			SignalType = signalType;
		}

		}

	internal class ExtremumLevel
	{
		internal int BarStart { get; set; }

		internal int BarEnd { get; set; }

		internal double Price { get; set; }

		internal ExtremumLevel(double price, int barStart, int barEnd)
		{
			BarStart = barStart;
			BarEnd = barEnd;
			Price = price;
		}

		}

	private enum SignalType
	{
		BullishBearish = 1,
		ZoneStart
	}

	private const int defaultMargin = 5;

	private int SlowdownScan;

	private int WeakWeakSplit;

	private int OffsetATRPeriod;

	private int ReferencePriceCloseWeight;

	private int TrendMultiplierStop;

	private bool isOnBarClose;

	private bool isMarkerCustomRenderingMethod;

	private int lastBullishReturnSignalIndex = -1;

	private int lastBearishReturnSignalIndex = -1;

	private double openPriceUpBar;

	private double openPriceDownBar;

	private double lineLevelsOffset;

	private float signalCloseThreshold;

	private Series<double> seriesTrendVector;

	private Series<double> seriesTrendVector1;

	private Series<double> seriesTrendVector2;

	private Series<double> seriesTrendVector3;

	private Series<double> seriesTrendVector4;

	private Series<int> seriesSignalTrend;

	private Series<int> seriesSignalTrend1;

	private Series<int> seriesSignalTrend2;

	private Series<int> seriesSignalTrend3;

	private Series<int> seriesSignalTrend4;

	private SortedList<int, ZoneInfo> listZoneInfoActive;

	private SortedList<int, ZoneInfo> listZoneInfoInactive;

	private SortedList<int, ZoneInfo> listZoneInfoBroken;

	private List<JumpBoostInfo> listJumpBoostInfo;

	private JumpBoostInfo jumpBoostInfo;

	private Brush backgroundBullish;

	private Brush backgroundBearish;

	private Brush zoneBullishActive;

	private Brush zoneBearishActive;

	private Brush zoneBullishInactive;

	private Brush zoneBearishInactive;

	private Brush brushNakedLevelMaximum;

	private Brush brushTestedLevelMaximum;

	private Brush brushNakedLevelMinimum;

	private Brush brushTestedLevelMinimum;

	private Brush brushBarHighlightBackgroundBullish;

	private Brush brushBarHighlightBackgroundBearish;

	private Brush brushBarHighlightCoreLineBullish;

	private Brush brushBarHighlightCoreLineBearish;

	private Brush brushBackgroundLevel1Broken;

	private Brush brushBackgroundLevel1ActiveBullish;

	private Brush brushBackgroundLevel1ActiveBearish;

	private Brush brushBackgroundLevel1InactiveBullish;

	private Brush brushBackgroundLevel1InactiveBearish;

	private Window alertWindow;

	private const string prefix = "gbSuperJumpBoost";

	private const string indicatorName = "Super JumpBoost";

	private const string indicatorNameFull = "Super JumpBoost by GreyBeard";

	private bool isCharting;

	private Dictionary<int, MarkerInfo> dictMarkers;

	private int signalTrade;

	private int signalZone;

	private int signalState;

	private ZoneInfo lastZoneInfo;

	private List<ExtremumLevel> listNakedMaxima;

	private List<ExtremumLevel> listNakedMinima;

	private List<ExtremumLevel> listTestedMaxima;

	private List<ExtremumLevel> listTestedMinima;

	private int barCount;

	private int eCurrentBar;

	private int barShift;

	private bool isNewZoneInfo;

	private int fromIndex;

	private int toIndex;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	private FrameworkElement instructionPanel;

	[Display(Name = "Condition: Bullish/Bearish", Order = 0, GroupName = "Alerts")]
	public bool ConditionBullishBearish { get; set; }

	[Display(Name = "Condition: Zone Start", Order = 1, GroupName = "Alerts")]
	public bool ConditionZoneStart { get; set; }

	[Display(Name = "Popup: Enabled", Order = 2, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[Display(Name = "Popup: Background Color", Order = 4, GroupName = "Alerts")]
	[XmlIgnore]
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

	[Display(Name = "Popup: Background Opacity", Order = 6, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 8, GroupName = "Alerts")]
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

	[Display(Name = "Popup: Text Size", Order = 10, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 12, GroupName = "Alerts")]
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

	[Display(Name = "Sound: Enabled", Order = 14, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[TypeConverter(typeof(gbSuperJumpBoost_SoundConverter))]
	[Display(Name = "Sound: Bullish", Order = 16, GroupName = "Alerts")]
	public string SoundBullish { get; set; }

	[TypeConverter(typeof(gbSuperJumpBoost_SoundConverter))]
	[Display(Name = "Sound: Bearish", Order = 18, GroupName = "Alerts")]
	public string SoundBearish { get; set; }

	[Display(Name = "Sound: Zone Bullish Start", Order = 17, GroupName = "Alerts")]
	[TypeConverter(typeof(gbSuperJumpBoost_SoundConverter))]
	public string SoundZoneBullishStart { get; set; }

	[Display(Name = "Sound: Zone Bearish Start", Order = 19, GroupName = "Alerts")]
	[TypeConverter(typeof(gbSuperJumpBoost_SoundConverter))]
	public string SoundZoneBearishStart { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 20, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 22, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 24, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 28, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 32, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbSuperJumpBoost_RenderingMethod MarkerRenderingMethod { get; set; }

	[Display(Name = "Marker: Color Bullish", Order = 34, GroupName = "Alerts")]
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

	[Display(Name = "Marker: Color Bearish", Order = 36, GroupName = "Alerts")]
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

	[Display(Name = "Marker: String Bullish", Order = 38, GroupName = "Alerts")]
	public string MarkerStringBullish { get; set; }

	[Display(Name = "Marker: String Bearish", Order = 40, GroupName = "Alerts")]
	public string MarkerStringBearish { get; set; }

	[Display(Name = "Marker: String Zone Bullish Start", Order = 42, GroupName = "Alerts")]
	public string MarkerStringZoneBullishStart { get; set; }

	[Display(Name = "Marker: String Zone Bearish Start", Order = 44, GroupName = "Alerts")]
	public string MarkerStringZoneBearishStart { get; set; }

	[Display(Name = "Marker: Font", Order = 46, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 48, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
	public bool SwitchedOn { get; set; }

	[Display(Name = "Author",  Order = 0,  GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Website", Order = 5,  GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Version", Order = 10, GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Bar: Enabled", Order = 10, GroupName = "Graphics")]
	public bool BarEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Bar: Bullish", Order = 12, GroupName = "Graphics")]
	public Brush BarBullish { get; set; }

	[Browsable(false)]
	public string BarBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarBullish);
		}
		set
		{
			BarBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Bearish", Order = 14, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush BarBearish { get; set; }

	[Browsable(false)]
	public string BarBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarBearish);
		}
		set
		{
			BarBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Outline Enabled", Order = 16, GroupName = "Graphics")]
	public bool BarOutlineEnabled { get; set; }

	[Display(Name = "Bar: Bias Based", Order = 18, GroupName = "Graphics")]
	public bool BarBiasBased { get; set; }

	[Display(Name = "Background: Enabled", Order = 30, GroupName = "Graphics")]
	public bool BackgroundEnabled { get; set; }

	[Display(Name = "Background: Bullish", Order = 32, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush BackgroundBullish { get; set; }

	[Browsable(false)]
	public string BackgroundBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(BackgroundBullish);
		}
		set
		{
			BackgroundBullish = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Background: Bearish", Order = 34, GroupName = "Graphics")]
	public Brush BackgroundBearish { get; set; }

	[Browsable(false)]
	public string BackgroundBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(BackgroundBearish);
		}
		set
		{
			BackgroundBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Background: Opacity", Order = 36, GroupName = "Graphics")]
	public int BackgroundOpacity { get; set; }

	[Display(Name = "Zone Active: Line #1 Bullish", Order = 50, GroupName = "Graphics")]
	public Stroke ZoneActiveLine1Bullish { get; set; }

	[Display(Name = "Zone Active: Line #2 Bullish", Order = 52, GroupName = "Graphics")]
	public Stroke ZoneActiveLine2Bullish { get; set; }

	[Display(Name = "Zone Active: Line #3 Bullish", Order = 54, GroupName = "Graphics")]
	public Stroke ZoneActiveLine3Bullish { get; set; }

	[Display(Name = "Zone Active: Line #4 Bullish", Order = 56, GroupName = "Graphics")]
	public Stroke ZoneActiveLine4Bullish { get; set; }

	[Display(Name = "Zone Active: Line #1 Bearish", Order = 60, GroupName = "Graphics")]
	public Stroke ZoneActiveLine1Bearish { get; set; }

	[Display(Name = "Zone Active: Line #2 Bearish", Order = 62, GroupName = "Graphics")]
	public Stroke ZoneActiveLine2Bearish { get; set; }

	[Display(Name = "Zone Active: Line #3 Bearish", Order = 64, GroupName = "Graphics")]
	public Stroke ZoneActiveLine3Bearish { get; set; }

	[Display(Name = "Zone Active: Line #4 Bearish", Order = 66, GroupName = "Graphics")]
	public Stroke ZoneActiveLine4Bearish { get; set; }

	[Display(Name = "Zone Inactive: Line Enabled", Order = 68, GroupName = "Graphics")]
	public bool ZoneInactiveLineEnabled { get; set; }

	[Display(Name = "Zone Inactive: Line #1 Bullish", Order = 70, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine1Bullish { get; set; }

	[Display(Name = "Zone Inactive: Line #2 Bullish", Order = 72, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine2Bullish { get; set; }

	[Display(Name = "Zone Inactive: Line #3 Bullish", Order = 74, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine3Bullish { get; set; }

	[Display(Name = "Zone Inactive: Line #4 Bullish", Order = 76, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine4Bullish { get; set; }

	[Display(Name = "Zone Inactive: Line #1 Bearish", Order = 80, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine1Bearish { get; set; }

	[Display(Name = "Zone Inactive: Line #2 Bearish", Order = 82, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine2Bearish { get; set; }

	[Display(Name = "Zone Inactive: Line #3 Bearish", Order = 84, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine3Bearish { get; set; }

	[Display(Name = "Zone Inactive: Line #4 Bearish", Order = 86, GroupName = "Graphics")]
	public Stroke ZoneInactiveLine4Bearish { get; set; }

	[Display(Name = "Zone Empty: Line Enabled", Order = 100, GroupName = "Graphics")]
	public bool ZoneEmptyLineEnabled { get; set; }

	[Display(Name = "Zone Empty: Line #1", Order = 102, GroupName = "Graphics")]
	public Stroke ZoneEmptyLine1 { get; set; }

	[Display(Name = "Zone Empty: Line #2", Order = 104, GroupName = "Graphics")]
	public Stroke ZoneEmptyLine2 { get; set; }

	[Display(Name = "Zone Empty: Line #3", Order = 106, GroupName = "Graphics")]
	public Stroke ZoneEmptyLine3 { get; set; }

	[Display(Name = "Zone Empty: Line #4", Order = 108, GroupName = "Graphics")]
	public Stroke ZoneEmptyLine4 { get; set; }

	[Display(Name = "Extreme: Intact Level Top", Order = 120, GroupName = "Graphics")]
	public Stroke ExtremeIntactLevelTop { get; set; }

	[Display(Name = "Extreme: Intact Level Bottom", Order = 122, GroupName = "Graphics")]
	public Stroke ExtremeIntactLevelBottom { get; set; }

	[Display(Name = "Extreme: Broken Level Enabled", Order = 124, GroupName = "Graphics")]
	public bool ExtremeBrokenLevelEnabled { get; set; }

	[Display(Name = "Extreme: Broken Level Top", Order = 126, GroupName = "Graphics")]
	public Stroke ExtremeBrokenLevelTop { get; set; }

	[Display(Name = "Extreme: Broken Level Bottom", Order = 128, GroupName = "Graphics")]
	public Stroke ExtremeBrokenLevelBottom { get; set; }

	[Display(Name = "Price: Enabled", Order = 140, GroupName = "Graphics")]
	public bool PriceEnabled { get; set; }

	[Display(Name = "Price: Font", Order = 142, GroupName = "Graphics")]
	public SimpleFont PriceFont { get; set; }

	[Display(Name = "Price: Margin", Order = 144, GroupName = "Graphics")]
	public int PriceMargin { get; set; }

	[Display(Name = "Highlight: Enabled", Order = 150, GroupName = "Graphics")]
	public bool HighlightEnabled { get; set; }

	[Display(Name = "Highlight: Bullish", Order = 152, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush HighlightBullish { get; set; }

	[Browsable(false)]
	public string HighlightBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(HighlightBullish);
		}
		set
		{
			HighlightBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Highlight: Bearish", Order = 154, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush HighlightBearish { get; set; }

	[Browsable(false)]
	public string HighlightBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(HighlightBearish);
		}
		set
		{
			HighlightBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Highlight: Background Width (%)", Order = 156, GroupName = "Graphics")]
	public int HighlightBackgroundWidthPercent { get; set; }

	[Display(Name = "Highlight: Background Opacity", Order = 158, GroupName = "Graphics")]
	public int HighlightBackgroundOpacity { get; set; }

	[Display(Name = "Highlight: Width (%)", Order = 162, GroupName = "Graphics")]
	public int HighlightWidthPercent { get; set; }

	[Display(Name = "Highlight: Opacity", Order = 164, GroupName = "Graphics")]
	public int HighlightOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Sensitive Mode: Enabled", Order = 2, GroupName = "Parameters")]
	public bool SensitiveModeEnabled { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Offset: Level #1", Order = 10, GroupName = "Parameters")]
	public double OffsetLevel1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Offset: Level #2", Order = 12, GroupName = "Parameters")]
	public double OffsetLevel2 { get; set; }

	[Display(Name = "Offset: Level #3", Order = 14, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public double OffsetLevel3 { get; set; }

	[Display(Name = "Offset: Level #4", Order = 16, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public double OffsetLevel4 { get; set; }

	[Display(Name = "Offset: Base", Order = 18, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public double OffsetBase { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Reference Price: Period", Order = 20, GroupName = "Parameters")]
	public int ReferencePricePeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Line Levels Offset (Ticks)", Order = 22, GroupName = "Parameters")]
	public int LineLevelsOffset { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Extreme: Neighborhood (Bars)", Order = 50, GroupName = "Parameters")]
	public int ExtremeNeighborhood { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Signal: Close Threshold (%)", Order = 102, GroupName = "Parameters")]
	public int SignalCloseThreshold { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Signal: Quantity Per Zone", Order = 104, GroupName = "Parameters")]
	public int SignalQuantityPerZone { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Signal: Split (Bars)", Order = 106, GroupName = "Parameters")]
	public int SignalSplit { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_State => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Zone => Values[2];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "Super JumpBoost by GreyBeard" + GetUserNote();
			}
			return base.DisplayName;
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
		if (State != State.SetDefaults)
		{
			if (State != State.Configure)
			{
				if (State != State.DataLoaded)
				{
					if (State != State.Historical)
					{
						if (State != State.Terminated)
						{
							return;
						}
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
						DisposeDxBrushCache();
					}
					else
					{
						if (ScreenDPI < 100)
						{
							ScreenDPI = 96;
						}
						if (IndicatorZOrder != 0)
						{
							SetZOrder(IndicatorZOrder);
						}
						isCharting = ChartControl != null;
						if (!isCharting)
						{
							return;
						}

					}
				}
				else
				{
					// State.DataLoaded — Instrument is available here
					lineLevelsOffset = (double)LineLevelsOffset * TickSize;
				}
				return;
			}
			OffsetLevel1 = Math.Min(OffsetLevel1, OffsetBase);
			OffsetLevel2 = Math.Min(OffsetLevel2, OffsetBase);
			OffsetLevel3 = Math.Min(OffsetLevel3, OffsetBase);
			OffsetLevel4 = Math.Min(OffsetLevel4, OffsetBase);
			OffsetBase = Math.Max(OffsetLevel4, OffsetBase);
			seriesSignalTrend = new Series<int>(this, MaximumBarsLookBack.Infinite);
			seriesSignalTrend1 = new Series<int>(this, MaximumBarsLookBack.Infinite);
			seriesSignalTrend2 = new Series<int>(this, MaximumBarsLookBack.Infinite);
			seriesSignalTrend3 = new Series<int>(this, MaximumBarsLookBack.Infinite);
			seriesSignalTrend4 = new Series<int>(this, MaximumBarsLookBack.Infinite);
			seriesTrendVector = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesTrendVector1 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesTrendVector2 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesTrendVector3 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesTrendVector4 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			listZoneInfoActive = new SortedList<int, ZoneInfo>();
			listZoneInfoInactive = new SortedList<int, ZoneInfo>();
			listZoneInfoBroken = new SortedList<int, ZoneInfo>();
			SlowdownScan = ((!SensitiveModeEnabled) ? 1 : 5);
			WeakWeakSplit = ((!SensitiveModeEnabled) ? 1 : 10);
			OffsetATRPeriod = 100;
			ReferencePriceCloseWeight = 1;
			TrendMultiplierStop = 4;
			listJumpBoostInfo = new List<JumpBoostInfo>();
			listJumpBoostInfo.Add(new JumpBoostInfo(OffsetBase, OffsetLevel1, 0, SlowdownScan, WeakWeakSplit, seriesTrendVector1, seriesSignalTrend1));
			listJumpBoostInfo.Add(new JumpBoostInfo(OffsetBase, OffsetLevel2, 0, SlowdownScan, WeakWeakSplit, seriesTrendVector2, seriesSignalTrend2));
			listJumpBoostInfo.Add(new JumpBoostInfo(OffsetBase, OffsetLevel3, 0, SlowdownScan, WeakWeakSplit, seriesTrendVector3, seriesSignalTrend3));
			listJumpBoostInfo.Add(new JumpBoostInfo(OffsetBase, OffsetLevel4, 0, SlowdownScan, WeakWeakSplit, seriesTrendVector4, seriesSignalTrend4));
			jumpBoostInfo = new JumpBoostInfo(TrendMultiplierStop, 2.0, 0, 5, 10, seriesTrendVector, seriesSignalTrend);
			listNakedMaxima = new List<ExtremumLevel>();
			listNakedMinima = new List<ExtremumLevel>();
			listTestedMaxima = new List<ExtremumLevel>();
			listTestedMinima = new List<ExtremumLevel>();
			signalCloseThreshold = (float)SignalCloseThreshold / 100f;
			isMarkerCustomRenderingMethod = MarkerRenderingMethod == gbSuperJumpBoost_RenderingMethod.Custom;
			if (isMarkerCustomRenderingMethod)
			{
				dictMarkers = new Dictionary<int, MarkerInfo>();
			}
			isOnBarClose = Calculate == Calculate.OnBarClose;
			zoneBullishActive = CreateOpacityBrush(Brushes.DodgerBlue, 60);
			zoneBullishInactive = CreateOpacityBrush(Brushes.DodgerBlue, 30);
			zoneBearishActive = CreateOpacityBrush(Brushes.DeepPink, 60);
			zoneBearishInactive = CreateOpacityBrush(Brushes.DeepPink, 30);
			brushNakedLevelMaximum = CreateOpacityBrush(ExtremeIntactLevelTop.Brush, ExtremeIntactLevelTop.Opacity);
			brushNakedLevelMinimum = CreateOpacityBrush(ExtremeIntactLevelBottom.Brush, ExtremeIntactLevelBottom.Opacity);
			brushTestedLevelMaximum = CreateOpacityBrush(ExtremeBrokenLevelTop.Brush, ExtremeBrokenLevelTop.Opacity);
			brushTestedLevelMinimum = CreateOpacityBrush(ExtremeBrokenLevelBottom.Brush, ExtremeBrokenLevelBottom.Opacity);
			if (HighlightEnabled)
			{
				brushBarHighlightBackgroundBullish = CreateOpacityBrush(HighlightBullish, HighlightBackgroundOpacity);
				brushBarHighlightBackgroundBearish = CreateOpacityBrush(HighlightBearish, HighlightBackgroundOpacity);
				brushBarHighlightCoreLineBullish = CreateOpacityBrush(HighlightBullish, HighlightOpacity);
				brushBarHighlightCoreLineBearish = CreateOpacityBrush(HighlightBearish, HighlightOpacity);
			}
			if (BackgroundEnabled)
			{
				backgroundBullish = CreateOpacityBrush(BackgroundBullish, BackgroundOpacity);
				backgroundBearish = CreateOpacityBrush(BackgroundBearish, BackgroundOpacity);
			}
			brushBackgroundLevel1ActiveBullish = CreateOpacityBrush(ZoneActiveLine1Bullish.Brush, ZoneActiveLine1Bullish.Opacity / 4);
			brushBackgroundLevel1ActiveBearish = CreateOpacityBrush(ZoneActiveLine1Bearish.Brush, ZoneActiveLine1Bearish.Opacity / 4);
			brushBackgroundLevel1InactiveBullish = CreateOpacityBrush(ZoneInactiveLine1Bullish.Brush, ZoneInactiveLine1Bullish.Opacity / 4);
			brushBackgroundLevel1InactiveBearish = CreateOpacityBrush(ZoneInactiveLine1Bearish.Brush, ZoneInactiveLine1Bearish.Opacity / 4);
			brushBackgroundLevel1Broken = CreateOpacityBrush(ZoneEmptyLine1.Brush, ZoneEmptyLine1.Opacity / 4);
			rearmTimer = new DispatcherTimer();
			rearmTimer.Interval = TimeSpan.FromMilliseconds(100.0);
			rearmTimer.Tick += OnRearmTimerTick;
			Calculate = Calculate.OnBarClose;
		}
		else
		{
			Description = "Supply/demand zone detector with multi-level price zones.";
			Name = "gbSuperJumpBoost";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = true;
			ScaleJustification = ScaleJustification.Right;
			IsSuspendedWhileInactive = false;
			ShowTransparentPlotsInDataBox = true;
			BarsRequiredToPlot = 0;
			ConditionBullishBearish = true;
			ConditionZoneStart = false;
			PopupEnabled = false;
			PopupBackgroundBrush = Brushes.Gold;
			PopupBackgroundOpacity = 60;
			PopupTextBrush = Brushes.DarkSlateGray;
			PopupTextSize = 16;
			PopupButtonBrush = Brushes.Transparent;
			SoundEnabled = false;
			SoundBullish = "Alert4.wav";
			SoundBearish = "Alert3.wav";
			SoundZoneBullishStart = "Alert1.wav";
			SoundZoneBearishStart = "Alert2.wav";
			SoundRearmEnabled = true;
			SoundRearmSeconds = 5;
			EmailEnabled = false;
			EmailReceiver = "receiver@example.com";
			MarkerEnabled = true;
			MarkerRenderingMethod = gbSuperJumpBoost_RenderingMethod.Custom;
			MarkerBrushBullish = Brushes.DodgerBlue;
			MarkerBrushBearish = Brushes.HotPink;
			MarkerStringBullish = "▲ + JB";
			MarkerStringBearish = "JB + ▼";
			MarkerStringZoneBullishStart = "⬆ + TS";
			MarkerStringZoneBearishStart = "TS + ⬇";
			MarkerFont = new SimpleFont("Arial", 20);
			MarkerOffset = 10;
			AlertBlockingSeconds = 60;
			SwitchedOn = true;
			ScreenDPI = 99;
			BarEnabled = true;
			BarBullish = Brushes.DodgerBlue;
			BarBearish = Brushes.DeepPink;
			BarOutlineEnabled = true;
			BarBiasBased = true;
			BackgroundEnabled = true;
			BackgroundBullish = Brushes.DodgerBlue;
			BackgroundBearish = Brushes.DeepPink;
			BackgroundOpacity = 30;
			ZoneActiveLine1Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 4f, 100);
			ZoneActiveLine2Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 3f, 70);
			ZoneActiveLine3Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 2f, 60);
			ZoneActiveLine4Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 1f, 50);
			ZoneActiveLine1Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 4f, 100);
			ZoneActiveLine2Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 3f, 70);
			ZoneActiveLine3Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 2f, 60);
			ZoneActiveLine4Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 1f, 50);
			ZoneInactiveLineEnabled = true;
			ZoneInactiveLine1Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 4f, 90);
			ZoneInactiveLine2Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 3f, 50);
			ZoneInactiveLine3Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 2f, 40);
			ZoneInactiveLine4Bullish = new Stroke(Brushes.Lime, DashStyleHelper.Solid, 1f, 30);
			ZoneInactiveLine1Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 4f, 90);
			ZoneInactiveLine2Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 3f, 50);
			ZoneInactiveLine3Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 2f, 40);
			ZoneInactiveLine4Bearish = new Stroke(Brushes.OrangeRed, DashStyleHelper.Solid, 1f, 30);
			ZoneEmptyLineEnabled = true;
			ZoneEmptyLine1 = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 4f, 90);
			ZoneEmptyLine2 = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 3f, 50);
			ZoneEmptyLine3 = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 2f, 40);
			ZoneEmptyLine4 = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 1f, 30);
			ExtremeIntactLevelTop = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 1f, 60);
			ExtremeIntactLevelBottom = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 1f, 60);
			ExtremeBrokenLevelEnabled = true;
			ExtremeBrokenLevelTop = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 1f, 30);
			ExtremeBrokenLevelBottom = new Stroke(Brushes.SlateGray, DashStyleHelper.Solid, 1f, 30);
			PriceEnabled = true;
			PriceFont = new SimpleFont("Arial", 12);
			PriceMargin = 5;
			HighlightEnabled = true;
			HighlightBullish = Brushes.Lime;
			HighlightBearish = Brushes.OrangeRed;
			HighlightBackgroundWidthPercent = 100;
			HighlightBackgroundOpacity = 20;
			HighlightWidthPercent = 20;
			HighlightOpacity = 100;
			OffsetLevel1 = 1.0;
			OffsetLevel2 = 2.0;
			OffsetLevel3 = 3.0;
			OffsetLevel4 = 4.0;
			OffsetBase = 4.0;
			ReferencePricePeriod = 2;
			LineLevelsOffset = 100;
			SensitiveModeEnabled = true;
			ExtremeNeighborhood = 30;
			SignalSplit = 20;
			SignalCloseThreshold = 70;
			SignalQuantityPerZone = 2;
			IndicatorZOrder = -10;
			UserNote = "instrument (period)";
			AddPlot(Brushes.Transparent, "Signal: State");
			AddPlot(Brushes.Transparent, "Signal: Trade");
			AddPlot(Brushes.Transparent, "Signal: Zone");
		}
	}

	protected override void OnBarUpdate()
	{
		ComputeMarketExtremes();
		signalZone = 0;
		signalTrade = 0;
		ComputeListJumpBoostInfo();
		FindZoneInfo();
		CheckBrokenAndFindSignal();
		CheckStopPaintBarByTrend();
		PaintBar(isToggleClickEvent: false, signalState, CurrentBar);
		ComputeSignalZone();
		Signal_State[0] = signalState;
		Signal_Trade[0] = signalTrade;
		Signal_Zone[0] = signalZone;
		PaintBackground();
	}

	private void PaintBackground()
	{
		if (Math.Abs(signalState) != 1 && (lastZoneInfo == null || lastZoneInfo.CountSignalReturn <= 0))
		{
			return;
		}
		bool flag = signalState == 1 || lastZoneInfo.IsBullish;
		if (isCharting && BackgroundEnabled && BackgroundOpacity > 0)
		{
			Brush brush = ((!flag) ? backgroundBearish : backgroundBullish);
			if (!BrushExtensions.IsTransparent(brush))
			{
				BackBrushAll = brush;
			}
		}
	}

	private bool IsAlreadyExisting(double price, List<ExtremumLevel> list)
	{
		if (list.Count != 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (MathExtentions.ApproxCompare(price, list[i].Price) == 0)
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	private void Add2List(List<ExtremumLevel> list, double price, int barStart, int barEnd, bool isMaximum)
	{
		if (list.Count != 0)
		{
			if (!isMaximum)
			{
				if (MathExtentions.ApproxCompare(price, list[list.Count - 1].Price) <= 0)
				{
					list.Add(new ExtremumLevel(price, barStart, barEnd));
					return;
				}
				if (MathExtentions.ApproxCompare(price, list[0].Price) >= 0)
				{
					list.Insert(0, new ExtremumLevel(price, barStart, barEnd));
					return;
				}
			}
			else
			{
				if (MathExtentions.ApproxCompare(price, list[list.Count - 1].Price) >= 0)
				{
					list.Add(new ExtremumLevel(price, barStart, barEnd));
					return;
				}
				if (MathExtentions.ApproxCompare(price, list[0].Price) <= 0)
				{
					list.Insert(0, new ExtremumLevel(price, barStart, barEnd));
					return;
				}
			}
			int num = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (!isMaximum || MathExtentions.ApproxCompare(list[i].Price, price) <= 0)
				{
					if (!isMaximum && MathExtentions.ApproxCompare(list[i].Price, price) < 0)
					{
						num = i;
						break;
					}
					continue;
				}
				num = i;
				break;
			}
			if (num >= 0)
			{
				list.Insert(num, new ExtremumLevel(price, barStart, barEnd));
			}
		}
		else
		{
			list.Add(new ExtremumLevel(price, barStart, barEnd));
		}
	}

	private void MoveExtremum(int srcIndex, List<ExtremumLevel> srcList, List<ExtremumLevel> destList, bool isMaximum)
	{
		if (srcIndex <= srcList.Count - 1)
		{
			Add2List(destList, srcList[srcIndex].Price, srcList[srcIndex].BarStart, srcList[srcIndex].BarEnd, isMaximum);
			srcList.RemoveAt(srcIndex);
		}
	}

	private void ComputeMarketExtremes()
	{
		if (CurrentBar == 0)
		{
			barCount = Bars.Count;
		}
		bool flag = !isOnBarClose && IsFirstTickOfBar;
		if (isOnBarClose || flag)
		{
			if (!isOnBarClose && (State == State.Realtime || CurrentBar == barCount - 1))
			{
				barShift = 1;
			}
			eCurrentBar = CurrentBar - barShift;
			int num = ExtremeNeighborhood + barShift;
			int num2 = CurrentBar - num;
			if (CurrentBar < ExtremeNeighborhood + num)
			{
				return;
			}
			if (!IsAlreadyExisting(High[num], listNakedMaxima) && IsMaximum(num2, ExtremeNeighborhood, ExtremeNeighborhood))
			{
				Add2List(listNakedMaxima, High[num], num2, eCurrentBar, isMaximum: true);
			}
			if (!IsAlreadyExisting(Low[num], listNakedMinima) && IsMinimum(num2, ExtremeNeighborhood, ExtremeNeighborhood))
			{
				Add2List(listNakedMinima, Low[num], num2, eCurrentBar, isMaximum: false);
			}
		}
		if (!(isOnBarClose || flag))
		{
			return;
		}
		if (listNakedMaxima != null && listNakedMaxima.Count > 0)
		{
			for (int num3 = listNakedMaxima.Count - 1; num3 >= 0; num3--)
			{
				if (MathExtentions.ApproxCompare(Close[barShift], listNakedMaxima[num3].Price) <= 0)
				{
					listNakedMaxima[num3].BarEnd = eCurrentBar;
				}
				else if (!ExtremeBrokenLevelEnabled)
				{
					listNakedMaxima.RemoveAt(num3);
				}
				else
				{
					MoveExtremum(num3, listNakedMaxima, listTestedMaxima, isMaximum: true);
				}
			}
		}
		if (listNakedMinima == null || listNakedMinima.Count <= 0)
		{
			return;
		}
		for (int num4 = listNakedMinima.Count - 1; num4 >= 0; num4--)
		{
			if (MathExtentions.ApproxCompare(Close[barShift], listNakedMinima[num4].Price) >= 0)
			{
				listNakedMinima[num4].BarEnd = eCurrentBar;
			}
			else if (!ExtremeBrokenLevelEnabled)
			{
				listNakedMinima.RemoveAt(num4);
			}
			else
			{
				MoveExtremum(num4, listNakedMinima, listTestedMinima, isMaximum: false);
			}
		}
	}

	private bool IsMaximum(int barIndex, int leftBars, int rightBars)
	{
		double high = Bars.GetHigh(barIndex);
		bool flag = true;
		bool flag2 = true;
		for (int i = barIndex - leftBars; i < barIndex; i++)
		{
			if (MathExtentions.ApproxCompare(high, Bars.GetHigh(i)) < 0)
			{
				return false;
			}
		}
		if (rightBars > 0)
		{
			for (int j = barIndex + 1; j <= barIndex + rightBars; j++)
			{
				if (MathExtentions.ApproxCompare(high, Bars.GetHigh(j)) < 0)
				{
					flag2 = false;
					break;
				}
			}
		}
		return flag && flag2;
	}

	private bool IsMinimum(int barIndex, int leftBars, int rightBars)
	{
		double low = Bars.GetLow(barIndex);
		bool flag = true;
		bool flag2 = true;
		for (int i = barIndex - leftBars; i < barIndex; i++)
		{
			if (MathExtentions.ApproxCompare(low, Bars.GetLow(i)) > 0)
			{
				return false;
			}
		}
		if (rightBars > 0)
		{
			for (int j = barIndex + 1; j <= barIndex + rightBars; j++)
			{
				if (MathExtentions.ApproxCompare(low, Bars.GetLow(j)) > 0)
				{
					flag2 = false;
					break;
				}
			}
		}
		return flag && flag2;
	}

	private void ComputeSignalZone()
	{
		if (lastZoneInfo != null)
		{
			signalZone = (lastZoneInfo.IsBullish ? 1 : (-1));
		}
	}

	private void CheckStopPaintBarByTrend()
	{
		if (CurrentBar == 0)
		{
			return;
		}
		int num = jumpBoostInfo.SeriesSignalTrend[0];
		int num2 = jumpBoostInfo.SeriesSignalTrend[1];
		if (signalState == 0)
		{
			return;
		}
		if (signalState != 1 || num != -2 || (num2 != 1 && num2 != 2))
		{
			if (signalState == -1 && num == 2 && (num2 == -1 || num2 == -2))
			{
				signalState = 0;
			}
		}
		else
		{
			signalState = 0;
		}
	}

	public void CheckBrokenAndFindSignal()
	{
		if (listZoneInfoActive.Count == 0)
		{
			return;
		}
		bool flag = MathExtentions.ApproxCompare(Close[0], Open[0]) > 0;
		bool flag2 = MathExtentions.ApproxCompare(Close[0], Open[0]) < 0;
		if (!flag)
		{
			if (flag2)
			{
				openPriceDownBar = Open[0];
			}
		}
		else
		{
			openPriceUpBar = Open[0];
		}
		for (int i = 0; i < listZoneInfoActive.Count; i++)
		{
			int key = listZoneInfoActive.Keys[i];
			ZoneInfo zoneInfo = listZoneInfoActive.Values[i];
			if (!isNewZoneInfo)
			{
				zoneInfo.BarEnd = CurrentBar;
				bool num = MathExtentions.ApproxCompare((((!zoneInfo.IsBullish) ? zoneInfo.TopPrice : zoneInfo.BottomPrice) - Close[0]) * (double)zoneInfo.Sign, 0.0) > 0;
				bool flag3 = zoneInfo.CountSignalReturn < SignalQuantityPerZone;
				if (!num)
				{
					double num2 = ((!zoneInfo.IsBullish) ? zoneInfo.BottomPrice : zoneInfo.TopPrice);
					bool flag4 = IsCloseOk();
					int num3 = 1;
					if (!zoneInfo.IsBullish)
					{
						bool flag5 = MathExtentions.ApproxCompare(Close[0], num2) < 0 && MathExtentions.ApproxCompare(High[0], num2) > 0;
						bool flag6 = MathExtentions.ApproxCompare(Close[0], num2) >= 0;
						bool flag7 = MathExtentions.ApproxCompare(Close[0], openPriceUpBar) < 0;
						if (!(flag4 && flag3 && CurrentBar - lastBearishReturnSignalIndex > SignalSplit && flag2) || (!flag5 && !(flag6 && flag7)))
						{
							continue;
						}
						lastBearishReturnSignalIndex = CurrentBar;
						zoneInfo.CountSignalReturn++;
						signalTrade = -num3;
						signalState = -1;
						if (ConditionBullishBearish)
						{
							if (!isMarkerCustomRenderingMethod)
							{
								PrintMarker(isBullish: false, SignalType.BullishBearish);
							}
							else
							{
								AddMarker(CurrentBar, isBullish: false, SignalType.BullishBearish);
							}
							TriggerAlerts(isBullish: false, SignalType.BullishBearish);
						}
						continue;
					}
					bool flag8 = MathExtentions.ApproxCompare(Close[0], num2) > 0 && MathExtentions.ApproxCompare(Low[0], num2) < 0;
					bool flag9 = MathExtentions.ApproxCompare(Close[0], num2) <= 0;
					bool flag10 = MathExtentions.ApproxCompare(Close[0], openPriceDownBar) > 0;
					if (!(flag4 && flag3 && CurrentBar - lastBullishReturnSignalIndex > SignalSplit && flag) || (!flag8 && !(flag9 && flag10)))
					{
						continue;
					}
					lastBullishReturnSignalIndex = CurrentBar;
					zoneInfo.CountSignalReturn++;
					signalTrade = num3;
					signalState = 1;
					if (ConditionBullishBearish)
					{
						if (!isMarkerCustomRenderingMethod)
						{
							PrintMarker(isBullish: true, SignalType.BullishBearish);
						}
						else
						{
							AddMarker(CurrentBar, isBullish: true, SignalType.BullishBearish);
						}
						TriggerAlerts(isBullish: true, SignalType.BullishBearish);
					}
				}
				else
				{
					if (zoneInfo.CountSignalReturn != 0)
					{
						zoneInfo.BarEnd = CurrentBar - 1;
						MoveDataFromListToList(key, zoneInfo, listZoneInfoActive, listZoneInfoInactive);
					}
					else
					{
						MoveDataFromListToList(key, zoneInfo, listZoneInfoActive, listZoneInfoBroken);
					}
					lastZoneInfo = null;
					signalState = 0;
				}
			}
			else if (i != listZoneInfoActive.Count - 1)
			{
				if (zoneInfo.CountSignalReturn != 0)
				{
					zoneInfo.BarEnd = CurrentBar - 1;
					MoveDataFromListToList(key, zoneInfo, listZoneInfoActive, listZoneInfoInactive);
				}
				else
				{
					MoveDataFromListToList(key, zoneInfo, listZoneInfoActive, listZoneInfoBroken);
				}
			}
		}
		isNewZoneInfo = false;
	}

	private bool IsCloseOk()
	{
		double num = Close[0];
		double num2 = Open[0];
		double num3 = High[0];
		double num4 = Low[0];
		if (MathExtentions.ApproxCompare(num, num2) != 0)
		{
			if (MathExtentions.ApproxCompare(num, num2) <= 0)
			{
				double num5 = num3 - (double)signalCloseThreshold * (num3 - num4);
				return MathExtentions.ApproxCompare(num, num5) < 0;
			}
			double num6 = num4 + (double)signalCloseThreshold * (num3 - num4);
			return MathExtentions.ApproxCompare(num, num6) > 0;
		}
		return false;
	}

	public void FindZoneInfo()
	{
		List<double> list = new List<double>();
		bool flag = false;
		bool flag2 = true;
		for (int i = 0; i < listJumpBoostInfo.Count - 1; i++)
		{
			JumpBoostInfo jumpBoostInfo = listJumpBoostInfo[i];
			JumpBoostInfo jumpBoostInfo2 = listJumpBoostInfo[i + 1];
			if (jumpBoostInfo.SeriesSignalTrend[0] == jumpBoostInfo2.SeriesSignalTrend[0] && Math.Abs(jumpBoostInfo.SeriesSignalTrend[0]) == 1)
			{
				if (!flag2)
				{
					list.Add(jumpBoostInfo2.SeriesTrendVector[0]);
				}
				else
				{
					flag = jumpBoostInfo.SeriesSignalTrend[0] == 1;
					list.Add(jumpBoostInfo.SeriesTrendVector[0]);
					list.Add(jumpBoostInfo2.SeriesTrendVector[0]);
				}
				flag2 = false;
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		ZoneInfo zoneInfo = new ZoneInfo(CurrentBar, CurrentBar, 0.0, 0.0, 0.0, 0.0, flag);
		ComputeZonePriceLevel(list, zoneInfo);
		if (lastZoneInfo != null)
		{
			if (MathExtentions.ApproxCompare(lastZoneInfo.TopPrice, zoneInfo.TopPrice) == 0 && MathExtentions.ApproxCompare(lastZoneInfo.BottomPrice, zoneInfo.BottomPrice) == 0)
			{
				return;
			}
			if (zoneInfo.IsBullish != lastZoneInfo.IsBullish)
			{
				signalState = 0;
			}
			else
			{
				double num = ((!lastZoneInfo.IsBullish) ? lastZoneInfo.BottomPrice : lastZoneInfo.TopPrice);
				double num2 = ((!zoneInfo.IsBullish) ? zoneInfo.BottomPrice : zoneInfo.TopPrice);
				if (MathExtentions.ApproxCompare(Math.Abs(num - num2), lineLevelsOffset) < 0)
				{
					return;
				}
			}
		}
		double num3 = ((!flag) ? zoneInfo.TopPrice : zoneInfo.BottomPrice);
		if (MathExtentions.ApproxCompare((Close[0] - num3) * (double)zoneInfo.Sign, 0.0) < 0)
		{
			return;
		}
		lastZoneInfo = zoneInfo;
		AddDataToList(CurrentBar, zoneInfo, listZoneInfoActive);
		int num4 = (flag ? 1 : (-1));
		signalTrade = num4 * 2;
		if (ConditionZoneStart)
		{
			if (!isMarkerCustomRenderingMethod)
			{
				PrintMarker(flag, SignalType.ZoneStart);
			}
			else
			{
				AddMarker(CurrentBar, flag, SignalType.ZoneStart);
			}
			TriggerAlerts(flag, SignalType.ZoneStart);
		}
		isNewZoneInfo = true;
	}

	private void ComputeZonePriceLevel(List<double> listPrice, ZoneInfo zoneInfo)
	{
		listPrice.Sort();
		if (listPrice.Count != 2)
		{
			if (listPrice.Count != 3)
			{
				zoneInfo.TopPrice = listPrice[3];
				zoneInfo.PriceLevel1 = listPrice[2];
				zoneInfo.PriceLevel2 = listPrice[1];
				zoneInfo.BottomPrice = listPrice[0];
			}
			else
			{
				zoneInfo.TopPrice = listPrice[2];
				zoneInfo.PriceLevel1 = listPrice[1];
				zoneInfo.BottomPrice = listPrice[0];
			}
		}
		else
		{
			zoneInfo.TopPrice = listPrice[1];
			zoneInfo.BottomPrice = listPrice[0];
		}
		zoneInfo.CountLevel = listPrice.Count;
		listPrice.Clear();
	}

	private void ComputeListJumpBoostInfo()
	{
		List<JumpBoostInfo>.Enumerator enumerator = listJumpBoostInfo.GetEnumerator();
		while (enumerator.MoveNext())
		{
			JumpBoostInfo current = enumerator.Current;
			ComputeJumpBoostInfo(current);
		}
		ComputeJumpBoostInfo(jumpBoostInfo);
	}

	private void ComputeJumpBoostInfo(JumpBoostInfo jumpBoostInfo)
	{
		bool flag = false;
		double num = Open[0];
		double num2 = Close[0];
		double num3 = MAX(High, ReferencePricePeriod)[0];
		double num4 = MIN(Low, ReferencePricePeriod)[0];
		double num5 = (MAX(Close, ReferencePricePeriod)[0] + MIN(Close, ReferencePricePeriod)[0]) / 2.0;
		double num6 = (num3 + num4 + (double)ReferencePriceCloseWeight * num5) / (double)(2 + ReferencePriceCloseWeight);
		double num7 = num6;
		double num8 = ATR(OffsetATRPeriod)[0];
		double num9 = Instrument.MasterInstrument.RoundToTickSize(jumpBoostInfo.OffsetBase * num8);
		double num10 = Instrument.MasterInstrument.RoundToTickSize(num6 - num9);
		double num11 = Instrument.MasterInstrument.RoundToTickSize(num7 + num9);
		double num12 = Instrument.MasterInstrument.RoundToTickSize(jumpBoostInfo.OffsetLevel * num8);
		double num13 = num6 - num12;
		double num14 = num7 + num12;
		if (CurrentBar != 0)
		{
			if (!jumpBoostInfo.IsUptrend)
			{
				if (MathExtentions.ApproxCompare(num2, jumpBoostInfo.StopCurrentValue) <= 0)
				{
					if (MathExtentions.ApproxCompare(num14, jumpBoostInfo.SeriesTrendVector[1]) >= 0)
					{
						jumpBoostInfo.CountSlowdown++;
						if (jumpBoostInfo.CountSlowdown < jumpBoostInfo.SlowdownScan || CurrentBar < jumpBoostInfo.NextWeakTrendBar)
						{
							flag = true;
						}
						jumpBoostInfo.SeriesTrendVector[0] = jumpBoostInfo.SeriesTrendVector[1];
					}
					else
					{
						jumpBoostInfo.SeriesTrendVector[0] = num14;
						if (jumpBoostInfo.CountSlowdown != 0)
						{
							jumpBoostInfo.CountSlowdown = 0;
						}
					}
					jumpBoostInfo.StopCurrentValue = Math.Min(num11, jumpBoostInfo.StopCurrentValue);
				}
				else
				{
					jumpBoostInfo.IsUptrend = true;
					if (MathExtentions.ApproxCompare(num13, jumpBoostInfo.SeriesTrendVector[1]) <= 0)
					{
						jumpBoostInfo.SeriesTrendVector[0] = jumpBoostInfo.SeriesTrendVector[1];
						flag = true;
					}
					else
					{
						jumpBoostInfo.SeriesTrendVector[0] = num13;
					}
					jumpBoostInfo.NextWeakTrendBar = CurrentBar + jumpBoostInfo.WeakWeakSplit;
					if (jumpBoostInfo.CountSlowdown != 0)
					{
						jumpBoostInfo.CountSlowdown = 0;
					}
					jumpBoostInfo.StopCurrentValue = num10;
				}
			}
			else if (MathExtentions.ApproxCompare(num2, jumpBoostInfo.StopCurrentValue) >= 0)
			{
				if (MathExtentions.ApproxCompare(num13, jumpBoostInfo.SeriesTrendVector[1]) <= 0)
				{
					jumpBoostInfo.CountSlowdown++;
					if (jumpBoostInfo.CountSlowdown < jumpBoostInfo.SlowdownScan || CurrentBar < jumpBoostInfo.NextWeakTrendBar)
					{
						flag = true;
					}
					jumpBoostInfo.SeriesTrendVector[0] = jumpBoostInfo.SeriesTrendVector[1];
				}
				else
				{
					jumpBoostInfo.SeriesTrendVector[0] = num13;
					if (jumpBoostInfo.CountSlowdown != 0)
					{
						jumpBoostInfo.CountSlowdown = 0;
					}
				}
				jumpBoostInfo.StopCurrentValue = Math.Max(num10, jumpBoostInfo.StopCurrentValue);
			}
			else
			{
				jumpBoostInfo.IsUptrend = false;
				if (MathExtentions.ApproxCompare(num14, jumpBoostInfo.SeriesTrendVector[1]) >= 0)
				{
					jumpBoostInfo.SeriesTrendVector[0] = jumpBoostInfo.SeriesTrendVector[1];
					flag = true;
				}
				else
				{
					jumpBoostInfo.SeriesTrendVector[0] = num14;
				}
				jumpBoostInfo.NextWeakTrendBar = CurrentBar + jumpBoostInfo.WeakWeakSplit;
				if (jumpBoostInfo.CountSlowdown != 0)
				{
					jumpBoostInfo.CountSlowdown = 0;
				}
				jumpBoostInfo.StopCurrentValue = num11;
			}
			int num15 = (jumpBoostInfo.IsUptrend ? 1 : (-1));
			int num16 = ((MathExtentions.ApproxCompare(jumpBoostInfo.SeriesTrendVector[0], jumpBoostInfo.SeriesTrendVector[1]) == 0) ? ((!flag) ? num15 : (num15 * 2)) : (num15 * 2));
			jumpBoostInfo.SeriesSignalTrend[0] = num16;
		}
		else
		{
			jumpBoostInfo.IsUptrend = MathExtentions.ApproxCompare(num2, num) > 0;
			if (!jumpBoostInfo.IsUptrend)
			{
				jumpBoostInfo.SeriesTrendVector[0] = num14;
				jumpBoostInfo.StopCurrentValue = num11;
			}
			else
			{
				jumpBoostInfo.SeriesTrendVector[0] = num13;
				jumpBoostInfo.StopCurrentValue = num10;
			}
		}
	}

	private void MoveDataFromListToList<T>(int key, T TData, SortedList<int, T> listActive, SortedList<int, T> listInactive)
	{
		// Body unrecoverable from DDMP/runtime IL (Agile.NET encrypted, generic method never JIT'd during capture).
		// Reconstruction from method name + signature; verify against NT8Dumper capture if behavior anomalies appear.
		listActive.Remove(key);
		listInactive[key] = TData;
	}

	private void AddDataToList<T>(int key, T TData, SortedList<int, T> listActive)
	{
		// Body unrecoverable from DDMP/runtime IL (Agile.NET encrypted, generic method never JIT'd during capture).
		// Reconstructed from method name + signature + call sites; verify with NT8Dumper if behavior anomalies appear.
		listActive[key] = TData;
	}

	// Device-brush cache. All brushes drawn here are reference-stable fields or
	// Stroke/marker property references, so the cache stays bounded. Invalidated
	// when the RenderTarget changes, disposed in State.Terminated.
	private readonly Dictionary<Brush, SharpDX.Direct2D1.Brush> dxBrushCache = new Dictionary<Brush, SharpDX.Direct2D1.Brush>();

	private SharpDX.Direct2D1.RenderTarget dxCacheRenderTarget;

	private SharpDX.Direct2D1.Brush GetDxBrushCached(Brush brush)
	{
		SharpDX.Direct2D1.Brush dxBrush;
		if (!dxBrushCache.TryGetValue(brush, out dxBrush))
		{
			dxBrush = DxExtensions.ToDxBrush(brush, RenderTarget);
			dxBrushCache[brush] = dxBrush;
		}
		return dxBrush;
	}

	private void DisposeDxBrushCache()
	{
		foreach (SharpDX.Direct2D1.Brush cachedBrush in dxBrushCache.Values)
		{
			try { cachedBrush.Dispose(); } catch { }
		}
		dxBrushCache.Clear();
		dxCacheRenderTarget = null;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (isCharting && SwitchedOn && !IsInHitTest)
		{
			// Device brushes are tied to the RenderTarget — rebuild the cache
			// lazily when NT8 hands us a new target (resize / device loss).
			if (!object.ReferenceEquals(RenderTarget, dxCacheRenderTarget))
			{
				DisposeDxBrushCache();
				dxCacheRenderTarget = RenderTarget;
			}
			base.OnRender(chartControl, chartScale);
				fromIndex = ChartBars.FromIndex;
				toIndex = ChartBars.ToIndex;
				DrawZones(chartScale, listZoneInfoActive, isActive: true, isBroken: false);
				if (ZoneInactiveLineEnabled)
				{
					DrawZones(chartScale, listZoneInfoInactive, isActive: false, isBroken: false);
				}
				if (ZoneInactiveLineEnabled && ZoneEmptyLineEnabled)
				{
					DrawZones(chartScale, listZoneInfoBroken, isActive: false, isBroken: true);
				}
				if (isMarkerCustomRenderingMethod)
				{
					DrawMarkers(chartScale);
				}
				DrawExtremumLevels(chartScale);
				DrawHighlights(chartScale);
		}
	}

	private void DrawHighlights(ChartScale chartScale)
	{
		if (HighlightEnabled)
		{
			for (int i = ChartBars.FromIndex; i <= toIndex; i++)
			{
				DrawOneHighlight(chartScale, i);
			}
		}
	}

	private void DrawOneHighlight(ChartScale chartScale, int barIndex)
	{
		int num = Convert.ToInt32(Signal_Trade.GetValueAt(barIndex));
		if (Math.Abs(num) != 1)
		{
			return;
		}
		bool flag;
		Brush brush = ((!(flag = num == 1)) ? brushBarHighlightBackgroundBearish : brushBarHighlightBackgroundBullish);
		if (!BrushExtensions.IsTransparent(brush))
		{
			int barPaintWidth = ChartControl.GetBarPaintWidth(ChartControl.BarsArray[0]);
			float num2 = ((HighlightBackgroundWidthPercent != 100) ? Math.Max(1f, (float)(barPaintWidth * HighlightBackgroundWidthPercent) / 100f) : ((float)barPaintWidth));
			float num3 = (float)ChartControl.GetXByBarIndex(ChartBars, barIndex) - num2 / 2f;
			double valueAt = ((!flag) ? Low : High).GetValueAt(barIndex);
			int num4 = chartScale.GetYByValue(valueAt) - num * 10;
			RectangleF val = new RectangleF(num3, (float)num4, num2, (float)(-num) * 2.1474836E+09f);
			RenderTarget.FillRectangle(val, GetDxBrushCached(brush));
			Brush brush2 = ((!flag) ? brushBarHighlightCoreLineBearish : brushBarHighlightCoreLineBullish);
			if (!BrushExtensions.IsTransparent(brush2))
			{
				float num5 = ((HighlightWidthPercent != 100) ? Math.Max(1f, (float)(barPaintWidth * HighlightWidthPercent) / 100f) : ((float)barPaintWidth));
				float num6 = (float)ChartControl.GetXByBarIndex(ChartBars, barIndex) - num5 / 2f;
				RectangleF val2 = new RectangleF(num6, (float)num4, num5, (float)(-num) * 2.1474836E+09f);
				RenderTarget.FillRectangle(val2, GetDxBrushCached(brush2));
			}
		}
	}

	private void DrawExtremumLevels(ChartScale chartScale)
	{
		DrawExtremumLevel(chartScale, listNakedMaxima, isNaked: true, isMaximum: true);
		DrawExtremumLevel(chartScale, listNakedMinima, isNaked: true, isMaximum: false);
		if (ExtremeBrokenLevelEnabled)
		{
			DrawExtremumLevel(chartScale, listTestedMaxima, isNaked: false, isMaximum: true);
		}
		if (ExtremeBrokenLevelEnabled)
		{
			DrawExtremumLevel(chartScale, listTestedMinima, isNaked: false, isMaximum: false);
		}
		PrintExtremumPrices(chartScale, listNakedMaxima, isMaximum: true);
		PrintExtremumPrices(chartScale, listNakedMinima, isMaximum: false);
	}

	private void DrawZones(ChartScale chartScale, SortedList<int, ZoneInfo> listZoneInfo, bool isActive, bool isBroken)
	{
		if (listZoneInfo.Count <= 0)
		{
			return;
		}
		IEnumerator<KeyValuePair<int, ZoneInfo>> enumerator = listZoneInfo.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ZoneInfo value = enumerator.Current.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawLineLevels(chartScale, value, isActive, isBroken);
			}
		}
	}

	private void DrawLineLevels(ChartScale chartScale, ZoneInfo zoneInfo, bool isActive, bool isBroken)
	{
		if (zoneInfo == null)
		{
			return;
		}
		int barStart = zoneInfo.BarStart;
		int barEnd = zoneInfo.BarEnd;
		bool isBullish = zoneInfo.IsBullish;
		if (barStart >= barEnd)
		{
			return;
		}
		int num = ((!isBullish) ? 1 : zoneInfo.CountLevel);
		int num2 = ((!isBullish) ? 1 : (-1));
		if (!(zoneInfo.TopPrice <= 0.0))
		{
			Stroke strock = GetStrock(num, isActive, isBullish, isBroken);
			if (isActive && PriceEnabled)
			{
				DrawPriceInfo(chartScale, ChartBars.ToIndex, barStart, barEnd, zoneInfo.TopPrice, strock.Brush);
			}
			if (!isBullish)
			{
				Brush brush = (isBroken ? brushBackgroundLevel1Broken : ((!isActive) ? brushBackgroundLevel1InactiveBearish : brushBackgroundLevel1ActiveBearish));
				if (!BrushExtensions.IsTransparent(brush))
				{
					DrawLineLevel(chartScale, barStart, barEnd, strock, brush, zoneInfo.TopPrice, strock.Width * 4f);
				}
			}
			DrawLineLevel(chartScale, barStart, barEnd, strock, strock.Brush, zoneInfo.TopPrice, strock.Width);
		}
		if (!(zoneInfo.PriceLevel1 <= 0.0))
		{
			num += num2;
			Stroke strock2 = GetStrock(num, isActive, isBullish, isBroken);
			if (isActive && PriceEnabled)
			{
				DrawPriceInfo(chartScale, ChartBars.ToIndex, barStart, barEnd, zoneInfo.PriceLevel1, strock2.Brush);
			}
			DrawLineLevel(chartScale, barStart, barEnd, strock2, strock2.Brush, zoneInfo.PriceLevel1, strock2.Width);
		}
		if (!(zoneInfo.PriceLevel2 <= 0.0))
		{
			num += num2;
			Stroke strock3 = GetStrock(num, isActive, isBullish, isBroken);
			if (isActive && PriceEnabled)
			{
				DrawPriceInfo(chartScale, ChartBars.ToIndex, barStart, barEnd, zoneInfo.PriceLevel2, strock3.Brush);
			}
			DrawLineLevel(chartScale, barStart, barEnd, strock3, strock3.Brush, zoneInfo.PriceLevel2, strock3.Width);
		}
		if (zoneInfo.BottomPrice <= 0.0)
		{
			return;
		}
		num += num2;
		Stroke strock4 = GetStrock(num, isActive, isBullish, isBroken);
		if (isActive && PriceEnabled)
		{
			DrawPriceInfo(chartScale, ChartBars.ToIndex, barStart, barEnd, zoneInfo.BottomPrice, strock4.Brush);
		}
		if (isBullish)
		{
			Brush brush2 = (isBroken ? brushBackgroundLevel1Broken : ((!isActive) ? brushBackgroundLevel1InactiveBullish : brushBackgroundLevel1ActiveBullish));
			if (!BrushExtensions.IsTransparent(brush2))
			{
				DrawLineLevel(chartScale, barStart, barEnd, strock4, brush2, zoneInfo.BottomPrice, strock4.Width * 4f);
			}
		}
		DrawLineLevel(chartScale, barStart, barEnd, strock4, strock4.Brush, zoneInfo.BottomPrice, strock4.Width);
	}

	private void DrawLineLevel(ChartScale chartScale, int startIndex, int endIndex, Stroke Zonetroke, Brush brush, double linePrice, float lineWidth)
	{
		if (Zonetroke != null && !BrushExtensions.IsTransparent(Zonetroke.Brush))
		{
			StrokeStyle strokeStyle = Zonetroke.StrokeStyle;
			float num = ChartControl.GetXByBarIndex(ChartBars, Math.Max(startIndex, ChartBars.FromIndex));
			float num2 = ChartControl.GetXByBarIndex(ChartBars, Math.Min(endIndex, ChartBars.ToIndex));
			float num3 = chartScale.GetYByValue(linePrice);
			Vector2 val = new Vector2(num, num3);
			Vector2 val2 = new Vector2(num2, num3);
			AntialiasMode antialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = (AntialiasMode)0;
			RenderTarget.DrawLine(val, val2, GetDxBrushCached(brush), lineWidth, strokeStyle);
			RenderTarget.AntialiasMode = antialiasMode;
		}
	}

	private Stroke GetStrock(int level, bool isActive, bool isBullish, bool isBroken)
	{
		Stroke val = null;
		if (!isBroken)
		{
			if (!isBullish)
			{
				return (Stroke)(level switch
				{
					4 => (!isActive) ? ZoneInactiveLine4Bearish : ZoneActiveLine4Bearish, 
					1 => (!isActive) ? ZoneInactiveLine1Bearish : ZoneActiveLine1Bearish, 
					2 => (!isActive) ? ZoneInactiveLine2Bearish : ZoneActiveLine2Bearish, 
					3 => (!isActive) ? ZoneInactiveLine3Bearish : ZoneActiveLine3Bearish, 
					_ => null, 
				});
			}
			return (Stroke)(level switch
			{
				4 => (!isActive) ? ZoneInactiveLine4Bullish : ZoneActiveLine4Bullish, 
				1 => (!isActive) ? ZoneInactiveLine1Bullish : ZoneActiveLine1Bullish, 
				2 => (!isActive) ? ZoneInactiveLine2Bullish : ZoneActiveLine2Bullish, 
				3 => (!isActive) ? ZoneInactiveLine3Bullish : ZoneActiveLine3Bullish, 
				_ => null, 
			});
		}
		return (Stroke)(level switch
		{
			4 => ZoneEmptyLine4, 
			1 => ZoneEmptyLine1, 
			2 => ZoneEmptyLine2, 
			3 => ZoneEmptyLine3, 
			_ => null, 
		});
	}

	private void AddMarker(int barIndex, bool isBullish, SignalType signalType)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo(barIndex, isBullish, signalType);
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
		if (!MarkerEnabled || dictMarkers.Count == 0)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= ChartBars.ToIndex; i++)
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
		string text = ((markerInfo.SignalType == SignalType.BullishBearish) ? ((!markerInfo.IsBullish) ? MarkerStringBearish : MarkerStringBullish) : ((!markerInfo.IsBullish) ? MarkerStringZoneBearishStart : MarkerStringZoneBullishStart));
		string text2 = FormatMarkerString(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		int barIndex = markerInfo.BarIndex;
		if ((isBullish || MathExtentions.ApproxCompare(Highs[0].GetValueAt(barIndex), chartScale.MaxValue) < 0) && (!isBullish || MathExtentions.ApproxCompare(Lows[0].GetValueAt(barIndex), chartScale.MinValue) > 0))
		{
			Brush brush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			if (!BrushExtensions.IsTransparent(brush))
			{
				int num = (isBullish ? 1 : (-1));
				float num2 = ChartControl.GetXByBarIndex(ChartBars, barIndex);
				float num3 = chartScale.GetYByValue(((!isBullish) ? Highs[0] : Lows[0]).GetValueAt(barIndex)) + num * MarkerOffset;
				DrawTextOnChart(text2, MarkerFont, num2, num3, 0, num, brush, ScreenDPI, RenderTarget);
			}
		}
	}

	private void DrawExtremumLevel(ChartScale chartScale, List<ExtremumLevel> list, bool isNaked, bool isMaximum)
	{
		if (!SwitchedOn || list == null || list.Count <= 0)
		{
			return;
		}
		if (!isNaked)
		{
			if ((isMaximum && BrushExtensions.IsTransparent(brushTestedLevelMaximum)) || (!isMaximum && BrushExtensions.IsTransparent(brushTestedLevelMinimum)))
			{
				return;
			}
		}
		else if ((isMaximum && BrushExtensions.IsTransparent(brushNakedLevelMaximum)) || (!isMaximum && BrushExtensions.IsTransparent(brushNakedLevelMinimum)))
		{
			return;
		}
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		for (int i = 0; i < list.Count; i++)
		{
			double price = list[i].Price;
			if (MathExtentions.ApproxCompare(price, chartScale.MinValue) >= 0 && MathExtentions.ApproxCompare(price, chartScale.MaxValue) <= 0 && list[i].BarStart <= ChartBars.ToIndex && list[i].BarEnd >= ChartBars.FromIndex)
			{
				float num = chartScale.GetYByValue(price);
				float num2 = ChartControl.GetXByBarIndex(ChartBars, Math.Max(list[i].BarStart, ChartBars.FromIndex));
				float num3 = ChartControl.GetXByBarIndex(ChartBars, Math.Min(list[i].BarEnd, ChartBars.ToIndex));
				if (num3 > num2)
				{
					double num4 = (isNaked ? ((!isMaximum) ? ExtremeIntactLevelBottom.Width : ExtremeIntactLevelTop.Width) : ((!isMaximum) ? ExtremeBrokenLevelBottom.Width : ExtremeBrokenLevelTop.Width));
					val = new Vector2(num2, num);
					val2 = new Vector2(num3, num);
					Brush brush = (isNaked ? ((!isMaximum) ? brushNakedLevelMinimum : brushNakedLevelMaximum) : ((!isMaximum) ? brushTestedLevelMinimum : brushTestedLevelMaximum));
					Stroke val3 = (isNaked ? ((!isMaximum) ? ExtremeIntactLevelBottom : ExtremeIntactLevelTop) : ((!isMaximum) ? ExtremeBrokenLevelBottom : ExtremeBrokenLevelTop));
					RenderTarget.DrawLine(val, val2, GetDxBrushCached(brush), (float)num4, val3.StrokeStyle);
				}
			}
		}
	}

	private void PrintExtremumPrices(ChartScale chartScale, List<ExtremumLevel> list, bool isMaximum)
	{
		if (SwitchedOn && list != null && list.Count > 0 && PriceEnabled && (!isMaximum || !BrushExtensions.IsTransparent(brushNakedLevelMaximum)) && (isMaximum || !BrushExtensions.IsTransparent(brushNakedLevelMinimum)))
		{
			Brush priceBrush = ((!isMaximum) ? ExtremeIntactLevelBottom.Brush : ExtremeIntactLevelTop.Brush);
			for (int i = 0; i < list.Count; i++)
			{
				DrawPriceInfo(chartScale, ChartBars.ToIndex, list[i].BarStart, list[i].BarEnd, list[i].Price, priceBrush);
			}
		}
	}

	private void DrawPriceInfo(ChartScale chartScale, int index, int barStart, int barEnd, double price, Brush priceBrush)
	{
		if (MathExtentions.ApproxCompare(price, chartScale.MinValue) >= 0 && MathExtentions.ApproxCompare(price, chartScale.MaxValue) <= 0 && barStart <= ChartBars.ToIndex && barEnd >= ChartBars.FromIndex)
		{
			float num = ChartControl.GetXByBarIndex(ChartBars, Math.Max(barStart, ChartBars.FromIndex));
			if ((float)ChartControl.GetXByBarIndex(ChartBars, Math.Min(barEnd, ChartBars.ToIndex)) > num)
			{
				float num2 = ChartControl.GetXByBarIndex(ChartBars, index) + PriceMargin;
				float num3 = chartScale.GetYByValue(price);
				DrawTextOnChart(FormatPriceMarker(price), PriceFont, num2, num3, 1, 0, priceBrush, ScreenDPI, RenderTarget);
			}
		}
	}

	private void PrintMarker(bool isBullish, SignalType signalType)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string tag = "gbSuperJumpBoost.marker." + GetTagSuffix(isBullish, signalType) + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((!isBullish) ? High[0] : Low[0]);
			string text = ((signalType == SignalType.BullishBearish) ? ((!isBullish) ? MarkerStringBearish : MarkerStringBullish) : ((!isBullish) ? MarkerStringZoneBearishStart : MarkerStringZoneBullishStart));
			text = FormatMarkerString(text);
			int num = ComputeTextHeight(text, MarkerFont);
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text, 0, y, yPixelOffset, textBrush, MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private string GetTagSuffix(bool isBullish, SignalType signalType)
	{
		if (signalType != SignalType.BullishBearish)
		{
			if (isBullish)
			{
				return "zonestart.bullish.";
			}
			return "zonestart.bearish.";
		}
		if (isBullish)
		{
			return "bullish.";
		}
		return "bearish.";
	}

	private void PaintBar(bool isToggleClickEvent, int signalState, int barIndex)
	{
		if ((!isToggleClickEvent && signalState == 0) || !isCharting || !BarEnabled)
		{
			return;
		}
		bool num = signalState == 1;
		Brush brush = ((!num) ? BarBearish : BarBullish);
		int num2 = ((!isToggleClickEvent) ? MathExtentions.ApproxCompare(Close[0], Open[0]) : MathExtentions.ApproxCompare(Close.GetValueAt(barIndex), Open.GetValueAt(barIndex)));
		int num3 = (num ? 1 : (-1));
		int num4 = CurrentBar - barIndex;
		if (BarOutlineEnabled && !BrushExtensions.IsTransparent(brush))
		{
			if (!isToggleClickEvent)
			{
				CandleOutlineBrush = brush;
			}
			else
			{
				CandleOutlineBrushes[num4] = brush;
			}
		}
		if (!BarBiasBased)
		{
			if (num2 != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = brush;
				}
				else
				{
					BarBrushes[num4] = brush;
				}
			}
		}
		else if (!BrushExtensions.IsTransparent(brush))
		{
			if (num2 != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = ((num3 * num2 <= 0) ? Brushes.Transparent : brush);
				}
				else
				{
					BarBrushes[num4] = ((num3 * num2 <= 0) ? Brushes.Transparent : brush);
				}
			}
		}
		else if (num3 * num2 < 0)
		{
			if (!isToggleClickEvent)
			{
				BarBrush = Brushes.Transparent;
			}
			else
			{
				BarBrushes[num4] = Brushes.Transparent;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void TriggerAlerts(bool isBullish, SignalType signalType)
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
		string arg = now.ToString("HH:mm");
		string text = now.ToString("HH:mm:ss, dd MMM yyyy");
		nextAlert = now + TimeSpan.FromSeconds(AlertBlockingSeconds);
		string arg2 = $"{Instrument.FullName} ({BarsPeriod})";
		string arg3 = ((signalType == SignalType.BullishBearish) ? ((!isBullish) ? "BEARISH" : "BULLISH") : ((!isBullish) ? "ZONE BEARISH START" : "ZONE BULLISH START"));
		string text2 = "gb Super JumpBoost" + $": {arg3} alert on {arg2} at {arg}";
		string popupMessage = $"There has been a {arg3} signal.\n\nAlert chart: {arg2}.\nAlert time: {text}";
		string text3 = "\n_______________________\n\n";
		string text4 = popupMessage + text3 + "gb Super JumpBoost";
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
					Title = "gb Super JumpBoost",
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
			string text5 = "alert @ " + text;
			string text6 = ((!isBullish) ? SoundBearish : SoundBullish);
			soundPath = Globals.InstallDir + "sounds\\" + text6;
			Alert(text5, Priority.Low, text2, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text2, text4);
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
					if (alertWindow != null && alertWindow.IsVisible)
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

	private void OnInstructionClose(object sender, RoutedEventArgs e)
	{
	}

	private static Brush CreateOpacityBrush(Brush brush, int opacity)
	{
		if (brush is SolidColorBrush solid)
		{
			byte alpha = (byte)(255 * opacity / 100);
			Color c = solid.Color;
			SolidColorBrush b = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
			b.Freeze();
			return b;
		}
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
		using (var format = font.ToDirectWriteTextFormat())
		{
			format.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, format, 200f, 200f))
			{
				float textH = layout.Metrics.Height;
				float drawX = x - 100f + xOffset;
				float drawY = (direction < 0) ? y - textH : y;
				rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), layout, GetDxBrushCached(wpfBrush));
			}
		}
	}

}

}

public class gbSuperJumpBoost_SoundConverter : System.ComponentModel.TypeConverter
{
	public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
	{
		if (context != null)
		{
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			System.IO.FileInfo[] files = new System.IO.DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (System.IO.FileInfo fileInfo in files)
			{
				list.Add(fileInfo.Name);
			}
			return new System.ComponentModel.TypeConverter.StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context)
	{
		return true;
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbSuperJumpBoost[] cachegbSuperJumpBoost;
		public GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			return gbSuperJumpBoost(Input, sensitiveModeEnabled, offsetLevel1, offsetLevel2, offsetLevel3, offsetLevel4, offsetBase, referencePricePeriod, lineLevelsOffset, extremeNeighborhood, signalCloseThreshold, signalQuantityPerZone, signalSplit);
		}

		public GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(ISeries<double> input, bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			if (cachegbSuperJumpBoost != null)
				for (int idx = 0; idx < cachegbSuperJumpBoost.Length; idx++)
					if (cachegbSuperJumpBoost[idx] != null && cachegbSuperJumpBoost[idx].SensitiveModeEnabled == sensitiveModeEnabled && cachegbSuperJumpBoost[idx].OffsetLevel1 == offsetLevel1 && cachegbSuperJumpBoost[idx].OffsetLevel2 == offsetLevel2 && cachegbSuperJumpBoost[idx].OffsetLevel3 == offsetLevel3 && cachegbSuperJumpBoost[idx].OffsetLevel4 == offsetLevel4 && cachegbSuperJumpBoost[idx].OffsetBase == offsetBase && cachegbSuperJumpBoost[idx].ReferencePricePeriod == referencePricePeriod && cachegbSuperJumpBoost[idx].LineLevelsOffset == lineLevelsOffset && cachegbSuperJumpBoost[idx].ExtremeNeighborhood == extremeNeighborhood && cachegbSuperJumpBoost[idx].SignalCloseThreshold == signalCloseThreshold && cachegbSuperJumpBoost[idx].SignalQuantityPerZone == signalQuantityPerZone && cachegbSuperJumpBoost[idx].SignalSplit == signalSplit && cachegbSuperJumpBoost[idx].EqualsInput(input))
						return cachegbSuperJumpBoost[idx];
			return CacheIndicator<GreyBeard.gbSuperJumpBoost>(new GreyBeard.gbSuperJumpBoost(){ SensitiveModeEnabled = sensitiveModeEnabled, OffsetLevel1 = offsetLevel1, OffsetLevel2 = offsetLevel2, OffsetLevel3 = offsetLevel3, OffsetLevel4 = offsetLevel4, OffsetBase = offsetBase, ReferencePricePeriod = referencePricePeriod, LineLevelsOffset = lineLevelsOffset, ExtremeNeighborhood = extremeNeighborhood, SignalCloseThreshold = signalCloseThreshold, SignalQuantityPerZone = signalQuantityPerZone, SignalSplit = signalSplit }, input, ref cachegbSuperJumpBoost);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			return indicator.gbSuperJumpBoost(Input, sensitiveModeEnabled, offsetLevel1, offsetLevel2, offsetLevel3, offsetLevel4, offsetBase, referencePricePeriod, lineLevelsOffset, extremeNeighborhood, signalCloseThreshold, signalQuantityPerZone, signalSplit);
		}

		public Indicators.GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(ISeries<double> input , bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			return indicator.gbSuperJumpBoost(input, sensitiveModeEnabled, offsetLevel1, offsetLevel2, offsetLevel3, offsetLevel4, offsetBase, referencePricePeriod, lineLevelsOffset, extremeNeighborhood, signalCloseThreshold, signalQuantityPerZone, signalSplit);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			return indicator.gbSuperJumpBoost(Input, sensitiveModeEnabled, offsetLevel1, offsetLevel2, offsetLevel3, offsetLevel4, offsetBase, referencePricePeriod, lineLevelsOffset, extremeNeighborhood, signalCloseThreshold, signalQuantityPerZone, signalSplit);
		}

		public Indicators.GreyBeard.gbSuperJumpBoost gbSuperJumpBoost(ISeries<double> input , bool sensitiveModeEnabled, double offsetLevel1, double offsetLevel2, double offsetLevel3, double offsetLevel4, double offsetBase, int referencePricePeriod, int lineLevelsOffset, int extremeNeighborhood, int signalCloseThreshold, int signalQuantityPerZone, int signalSplit)
		{
			return indicator.gbSuperJumpBoost(input, sensitiveModeEnabled, offsetLevel1, offsetLevel2, offsetLevel3, offsetLevel4, offsetBase, referencePricePeriod, lineLevelsOffset, extremeNeighborhood, signalCloseThreshold, signalQuantityPerZone, signalSplit);
		}
	}
}

#endregion
