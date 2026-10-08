#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{

public enum gbKingOrderBlockTextPosition
{
	BottomLeft = 0,
	BottomRight = 1,
	Center = 2,
	TopLeft = 3,
	TopRight = 4
}

public enum gbKingOrderBlock_MarkerRenderingMethod
{
	Custom = 0,
	Builtin = 1
}

public enum gbKingOrderBlock_SwingPointRenderingMethod
{
	Custom = 0,
	Builtin = 1
}

public enum gbKingOrderBlock_SwingPointDisplayMode
{
	Smart = 0,
	All = 1,
	Disabled = 2
}

public enum gbKingOrderBlock_BosChochDisplayMode
{
	Smart = 0,
	All = 1,
	Disabled = 2
}

public class gbKingOrderBlock_SoundConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (context != null)
		{
			List<string> list = new List<string>();
			FileInfo[] files = new DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (FileInfo fileInfo in files)
				list.Add(fileInfo.Name);
			return new StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

[CategoryOrder("Developer", 0)]
[CategoryOrder("General",   1000010)]
[CategoryOrder("Graphics",  1000020)]
[CategoryOrder("Gradient",  1000030)]
[CategoryOrder("Alerts",    1000040)]
[CategoryOrder("Special",   1000060)]
[CategoryOrder("Critical",  1000070)]
public class gbKingOrderBlock : Indicator
{
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

	public class SwingPoint : BackupExtension
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		[BackupProperties]
		public bool BackupIsBroken { get; set; }

		public bool IsBroken { get; set; }

		[BackupProperties]
		public bool PrevAllowDraw { get; set; }

		public bool AllowDraw { get; set; }

		[BackupProperties]
		public bool BackupIsHasOrderBlock { get; set; }

		public bool IsHasOrderBlock { get; set; }

		public SwingPoint(bool isTop, double price, int barStart, int barEnd, bool isBroken = false, bool isHasOrderBlock = false)
		{
			IsTop = isTop;
			Price = price;
			BarStart = barStart;
			BarEnd = barEnd;
			IsBroken = isBroken;
			IsHasOrderBlock = isHasOrderBlock;
		}
	}

	public enum BosChochType
	{
		Bos,
		Choch
	}

	public class BosChochInfo : BackupExtension
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		[BackupProperties]
		public bool BackupIsHasOrderBlock { get; set; }

		public bool IsHasOrderBlock { get; set; }

		public BosChochType BosChochType { get; set; }

		public BosChochInfo(bool isTop, double price, int barStart, BosChochType bosChochType, int barEnd = -1, bool isBroken = false, bool isHasOrderBlock = false)
		{
			IsTop = isTop;
			Price = price;
			BarStart = barStart;
			BosChochType = bosChochType;
			BarEnd = barEnd;
			IsHasOrderBlock = isHasOrderBlock;
		}
	}

	public class ZoneInfo : BackupExtension
	{
		public bool IsTop { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		public bool IsBroken { get; set; }

		public double PriceTop { get; set; }

		public double PriceBottom { get; set; }

		public int Sign
		{
			get
			{
				if (!IsTop)
				{
					return 1;
				}
				return -1;
			}
		}

		public ZoneInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0)
		{
			IsTop = isTop;
			IsBroken = isBroken;
			BarStart = barStart;
			BarEnd = barEnd;
			PriceTop = priceTop;
			PriceBottom = priceBottom;
		}

		public ZoneInfo()
		{
		}
	}

	public class OrderBlockInfo : ZoneInfo
	{
		public double PriceBroken
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceBottom;
				}
				return base.PriceTop;
			}
		}

		public double PriceSignal
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceTop;
				}
				return base.PriceBottom;
			}
		}

		[BackupProperties]
		public int BackupCountReturnSignal { get; set; }

		public int CountReturnSignal { get; set; }

		public OrderBlockInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0, int countReturnSignal = 0)
			: base(isTop, isBroken, barStart, barEnd, priceTop, priceBottom)
		{
			CountReturnSignal = countReturnSignal;
		}

		public OrderBlockInfo()
		{
		}
	}

	public class ImbalanceInfo : ZoneInfo
	{
		[BackupProperties]
		public bool BackupIsFixed { get; set; }

		public bool IsFixed { get; set; }

		[BackupProperties]
		public bool BackupIsBroken { get; set; }

		[BackupProperties]
		public int BackupBarEnd { get; set; }

		[BackupProperties]
		public double BackupPriceTop { get; set; }

		[BackupProperties]
		public double BackupPriceBottom { get; set; }

		public double PriceBroken
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceTop;
				}
				return base.PriceBottom;
			}
		}

		public ImbalanceInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0, bool isFixed = false, bool isActive = true)
			: base(isTop, isBroken, barStart, barEnd, priceTop, priceBottom)
		{
			IsFixed = isFixed;
		}

		public ImbalanceInfo()
		{
		}
	}

	public class MarubozuInfo
	{
		public double PriceTop { get; set; }

		public double PriceBottom { get; set; }

		public MarubozuInfo(double priceTop, double priceBottom)
		{
			PriceTop = priceTop;
			PriceBottom = priceBottom;
		}
	}

	private class ValueData
	{
		public double MinValue { get; set; }

		public double MaxValue { get; set; }

		public ValueData()
		{
			MinValue = (MaxValue = -1.0);
		}

		public void Add(double price)
		{
			if (price.ApproxCompare(0.0) > 0)
			{
				if (MinValue.ApproxCompare(0.0) >= 0 && MaxValue.ApproxCompare(0.0) >= 0)
				{
					MinValue = Math.Min(MinValue, price);
					MaxValue = Math.Max(MaxValue, price);
				}
				else
				{
					double minValue = (MaxValue = price);
					MinValue = minValue;
				}
			}
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class BackupPropertiesAttribute : Attribute
	{
	}

	public class BackupExtension
	{
		public void BackupProperties(Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			BackupOrRevertProperties(isBackup: true, cachePropertyInfo);
		}

		public void RevertProperties(Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			BackupOrRevertProperties(isBackup: false, cachePropertyInfo);
		}

		public void BackupOrRevertProperties(bool isBackup, Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			// Cached as interleaved [backup, source] pairs so the per-call work is
			// only the Get/SetValue copies. The original resolved the attribute and
			// the "Backup"-stripped source property by reflection on every call for
			// every property of every zone/swing element.
			if (!cachePropertyInfo.TryGetValue(GetType(), out PropertyInfo[] pairs))
			{
				List<PropertyInfo> pairList = new List<PropertyInfo>();
				foreach (PropertyInfo propertyInfo in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!Attribute.IsDefined(propertyInfo, typeof(BackupPropertiesAttribute)))
					{
						continue;
					}
					PropertyInfo property = GetType().GetProperty(propertyInfo.Name.Replace("Backup", ""));
					if (property != null)
					{
						pairList.Add(propertyInfo);
						pairList.Add(property);
					}
				}
				pairs = pairList.ToArray();
				cachePropertyInfo[GetType()] = pairs;
			}
			for (int i = 0; i < pairs.Length; i += 2)
			{
				PropertyInfo backupProperty = pairs[i];
				PropertyInfo sourceProperty = pairs[i + 1];
				if (isBackup)
				{
					backupProperty.SetValue(this, sourceProperty.GetValue(this));
				}
				else
				{
					sourceProperty.SetValue(this, backupProperty.GetValue(this));
				}
			}
		}
	}

	private enum SignalType
	{
		Return = 1,
		Breakout
	}

	private const int defaultMargin = 5;

	private const string toolTipSpace = "  ";

	private float barOutlineWidth;

	private double orderBlockOffsetDifferenceDirection;

	private double orderBlockOffsetSameDirection;

	private bool isMarkerCustomRenderingMethod;

	private bool isSwingPointCustomRenderingMethod;

	private bool isSwingPointSmartDisplayMode;

	private bool isOnBarCloseMode;

	private bool isBosChochSmartDisplayMode;

	private bool flagNewImbalance;

	private bool flagNewOrderBlock;

	private bool flagNewMaker;

	private bool flagNewSwingPoint;

	private bool flagNewBosChochTop;

	private bool flagNewBosChochBottom;

	private SortedList<int, SwingPoint> listSwingsTop;

	private SortedList<int, SwingPoint> listSwingsBottom;

	private SortedList<int, BosChochInfo> listBosChochInfoTop;

	private SortedList<int, BosChochInfo> listBosChochInfoBottom;

	private SortedList<int, OrderBlockInfo> listOrderBlockActive;

	private SortedList<int, OrderBlockInfo> listOrderBlockInactive;

	private SortedList<int, ImbalanceInfo> listImbalanceActive;

	private SortedList<int, ImbalanceInfo> listImbalanceInactive;

	private SortedList<int, MarkerInfo> listMarkers;

	private SharpDX.Direct2D1.GradientStop[] imbalanceActiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceActiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceInactiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceInactiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockActiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockActiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockInactiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockInactiveBottomGradientStop;

	private System.Windows.Media.Brush imbalanceActiveTop;

	private System.Windows.Media.Brush imbalanceActiveBottom;

	private System.Windows.Media.Brush imbalanceInactiveTop;

	private System.Windows.Media.Brush imbalanceInactiveBottom;

	private System.Windows.Media.Brush orderBlockActiveTop;

	private System.Windows.Media.Brush orderBlockActiveBottom;

	private System.Windows.Media.Brush orderBlockInactiveTop;

	private System.Windows.Media.Brush orderBlockInactiveBottom;

	private System.Windows.Media.Brush chochTopTextColor;

	private System.Windows.Media.Brush chochBottomTextColor;

	private System.Windows.Media.Brush bosTopTextColor;

	private System.Windows.Media.Brush bosBottomTextColor;

	private SwingPoint lastSwingPoint;

	private SwingPoint lastSwingPointTop;

	private SwingPoint lastSwingPointBottom;

	private BosChochInfo lastBosChochInfo;

	private ImbalanceInfo lastImbalanceInfo;

	private Dictionary<int, OrderBlockInfo> dictOrderBlockInfosBroken;

	private Dictionary<int, ImbalanceInfo> dictImbalanceInfosBroken;

	private Dictionary<Type, PropertyInfo[]> cachePropertyInfo = new Dictionary<Type, PropertyInfo[]>();

	// SharpDX resource caches (UI thread only). DrawOneBox / DrawOneBosChochLine /
	// DrawOneSwingPoint / DrawTextDX previously created and disposed a device
	// brush per element per frame. Cached per source WPF brush / gradient-stop
	// array, invalidated when the RenderTarget changes (see OnRender gate and
	// OnRenderTargetChanged), disposed in State.Terminated.
	private Dictionary<System.Windows.Media.Brush, SharpDX.Direct2D1.Brush> dxBrushCache = new Dictionary<System.Windows.Media.Brush, SharpDX.Direct2D1.Brush>();

	private Dictionary<SharpDX.Direct2D1.GradientStop[], SharpDX.Direct2D1.GradientStopCollection> dxGradientStopCache = new Dictionary<SharpDX.Direct2D1.GradientStop[], SharpDX.Direct2D1.GradientStopCollection>();

	private Dictionary<SharpDX.Direct2D1.GradientStop[], SharpDX.Direct2D1.LinearGradientBrush> dxGradientBrushCache = new Dictionary<SharpDX.Direct2D1.GradientStop[], SharpDX.Direct2D1.LinearGradientBrush>();

	private Dictionary<SimpleFont, SharpDX.DirectWrite.TextFormat> dxTextFormatCache = new Dictionary<SimpleFont, SharpDX.DirectWrite.TextFormat>();

	private SharpDX.Direct2D1.RenderTarget dxCacheRenderTarget;

	private Window alertWindow;
	private bool alertWindowClosed;

	private const string prefix = "gbKingOrderBlock";

	private const string indicatorName = "King Order Block";

	private const string indicatorNameFull = "King Order Block by GreyBeard";

	private const string receiverEmail = "receiver@example.com";

	private bool isCharting;

	private bool isLastBarOnEachTick;

	private State prevState = State.Historical;

	private int signalState;

	private int signalZoneBullish;

	private int signalZoneBearish;

	private int prevReturnBullishBarIndex = -1;

	private int prevReturnBearishBarIndex = -1;

	private int returnBullishBarIndex = -1;

	private int returnBearishBarIndex = -1;

	private int fromIndex;

	private int toIndex;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	[Display(Name = "Condition: Breakout", Order = 0, GroupName = "Alerts")]
	public bool ConditionBreakout { get; set; }

	[Display(Name = "Condition: Return", Order = 1, GroupName = "Alerts")]
	public bool ConditionReturn { get; set; }

	[Display(Name = "Popup: Enabled", Order = 2, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[Display(Name = "Popup: Background Color", Order = 3, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush PopupBackgroundBrush { get; set; }

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

	[Display(Name = "Popup: Background Opacity", Order = 4, GroupName = "Alerts")]
	[Range(0, 100)]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 5, GroupName = "Alerts")]
	public System.Windows.Media.Brush PopupTextBrush { get; set; }

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

	[Range(8, int.MaxValue)]
	[Display(Name = "Popup: Text Size", Order = 6, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 7, GroupName = "Alerts")]
	public System.Windows.Media.Brush PopupButtonBrush { get; set; }

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

	[Display(Name = "Sound: Break Up", Order = 11, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundBreakUp { get; set; }

	[Display(Name = "Sound: Break Down", Order = 12, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundBreakDown { get; set; }

	[Display(Name = "Sound: Return Bullish", Order = 14, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundReturnBullish { get; set; }

	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	[Display(Name = "Sound: Return Bearish", Order = 15, GroupName = "Alerts")]
	public string SoundReturnBearish { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 16, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 17, GroupName = "Alerts")]
	[Range(0, int.MaxValue)]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 20, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 21, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 31, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbKingOrderBlock_MarkerRenderingMethod MarkerRenderingMethod { get; set; }

	[XmlIgnore]
	[Display(Name = "Marker: Color Break Up", Order = 32, GroupName = "Alerts")]
	public System.Windows.Media.Brush MarkerBrushBreakUp { get; set; }

	[Browsable(false)]
	public string MarkerBrushBreakUp_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBreakUp);
		}
		set
		{
			MarkerBrushBreakUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Marker: Color Break Down", Order = 33, GroupName = "Alerts")]
	public System.Windows.Media.Brush MarkerBrushBreakDown { get; set; }

	[Browsable(false)]
	public string MarkerBrushBreakDown_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBreakDown);
		}
		set
		{
			MarkerBrushBreakDown = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Return Bullish", Order = 34, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush MarkerBrushReturnBullish { get; set; }

	[Browsable(false)]
	public string MarkerBrushReturnBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushReturnBullish);
		}
		set
		{
			MarkerBrushReturnBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Return Bearish", Order = 35, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush MarkerBrushReturnBearish { get; set; }

	[Browsable(false)]
	public string MarkerBrushReturnBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushReturnBearish);
		}
		set
		{
			MarkerBrushReturnBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: String Break Up", Order = 40, GroupName = "Alerts")]
	public string MarkerStringBreakUp { get; set; }

	[Display(Name = "Marker: String Break Down", Order = 41, GroupName = "Alerts")]
	public string MarkerStringBreakDown { get; set; }

	[Display(Name = "Marker: String Return Bullish", Order = 42, GroupName = "Alerts")]
	public string MarkerStringReturnBullish { get; set; }

	[Display(Name = "Marker: String Return Bearish", Order = 43, GroupName = "Alerts")]
	public string MarkerStringReturnBearish { get; set; }

	[Display(Name = "Marker: Font", Order = 45, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 47, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Range(0, int.MaxValue)]
	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Author",    Order = 0,   GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Website",   Order = 5,   GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Version",   Order = 10,  GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	[Range(99, 500)]
	public int ScreenDPI { get; set; }

	[Display(Name = "Gradient: Enabled", Order = 0, GroupName = "Graphics")]
	public bool GradientEnabled { get; set; }

	[Display(Name = "Imbalance Active: Top Start", Order = 2, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveTopStart { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveTopStart);
		}
		set
		{
			ImbalanceActiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Top End", Order = 4, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveTopEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveTopEnd);
		}
		set
		{
			ImbalanceActiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Bottom Start", Order = 6, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveBottomStart { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveBottomStart);
		}
		set
		{
			ImbalanceActiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Bottom End", Order = 8, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveBottomEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveBottomEnd);
		}
		set
		{
			ImbalanceActiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Opacity", Order = 10, GroupName = "Graphics")]
	[Range(0, 100)]
	public int ImbalanceActiveOpacity { get; set; }

	[Display(Name = "Imbalance Active: Border Top", Order = 12, GroupName = "Graphics")]
	public Stroke ImbalanceActiveBorderTop { get; set; }

	[Display(Name = "Imbalance Active: Border Bottom", Order = 14, GroupName = "Graphics")]
	public Stroke ImbalanceActiveBorderBottom { get; set; }

	[Display(Name = "Imbalance Inactive: Enabled", Order = 16, GroupName = "Graphics")]
	public bool ImbalanceInactiveEnabled { get; set; }

	[Display(Name = "Imbalance Inactive: Top Start", Order = 18, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceInactiveTopStart { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveTopStart);
		}
		set
		{
			ImbalanceInactiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Inactive: Top End", Order = 20, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceInactiveTopEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveTopEnd);
		}
		set
		{
			ImbalanceInactiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Imbalance Inactive: Bottom Start", Order = 22, GroupName = "Graphics")]
	public System.Windows.Media.Brush ImbalanceInactiveBottomStart { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveBottomStart);
		}
		set
		{
			ImbalanceInactiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Imbalance Inactive: Bottom End", Order = 24, GroupName = "Graphics")]
	public System.Windows.Media.Brush ImbalanceInactiveBottomEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveBottomEnd);
		}
		set
		{
			ImbalanceInactiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(Name = "Imbalance Inactive: Opacity", Order = 26, GroupName = "Graphics")]
	public int ImbalanceInactiveOpacity { get; set; }

	[Display(Name = "Imbalance Inactive: Border Top", Order = 28, GroupName = "Graphics")]
	public Stroke ImbalanceInactiveBorderTop { get; set; }

	[Display(Name = "Imbalance Inactive: Border Bottom", Order = 30, GroupName = "Graphics")]
	public Stroke ImbalanceInactiveBorderBottom { get; set; }

	[XmlIgnore]
	[Display(Name = "Order Block Active: Top Start", Order = 32, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveTopStart { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveTopStart);
		}
		set
		{
			OrderBlockActiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Active: Top End", Order = 34, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveTopEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveTopEnd);
		}
		set
		{
			OrderBlockActiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Active: Bottom Start", Order = 36, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockActiveBottomStart { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveBottomStart);
		}
		set
		{
			OrderBlockActiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Active: Bottom End", Order = 38, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveBottomEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveBottomEnd);
		}
		set
		{
			OrderBlockActiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Active: Opacity", Order = 40, GroupName = "Graphics")]
	[Range(0, 100)]
	public int OrderBlockActiveOpacity { get; set; }

	[Display(Name = "Order Block Active: Border Top", Order = 42, GroupName = "Graphics")]
	public Stroke OrderBlockActiveBorderTop { get; set; }

	[Display(Name = "Order Block Active: Border Bottom", Order = 44, GroupName = "Graphics")]
	public Stroke OrderBlockActiveBorderBottom { get; set; }

	[Display(Name = "Order Block Inactive: Enabled", Order = 46, GroupName = "Graphics")]
	public bool OrderBlockInactiveEnabled { get; set; }

	[Display(Name = "Order Block Inactive: Top Start", Order = 48, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockInactiveTopStart { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveTopStart);
		}
		set
		{
			OrderBlockInactiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Inactive: Top End", Order = 50, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockInactiveTopEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveTopEnd);
		}
		set
		{
			OrderBlockInactiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Inactive: Bottom Start", Order = 52, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockInactiveBottomStart { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveBottomStart);
		}
		set
		{
			OrderBlockInactiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Inactive: Bottom End", Order = 54, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockInactiveBottomEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveBottomEnd);
		}
		set
		{
			OrderBlockInactiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(Name = "Order Block Inactive: Opacity", Order = 56, GroupName = "Graphics")]
	public int OrderBlockInactiveOpacity { get; set; }

	[Display(Name = "Order Block Inactive: Border Top", Order = 58, GroupName = "Graphics")]
	public Stroke OrderBlockInactiveBorderTop { get; set; }

	[Display(Name = "Order Block Inactive: Border Bottom", Order = 60, GroupName = "Graphics")]
	public Stroke OrderBlockInactiveBorderBottom { get; set; }

	[Display(Name = "Swing Point: Display Mode", Order = 62, GroupName = "Graphics")]
	public gbKingOrderBlock_SwingPointDisplayMode SwingPointDisplayMode { get; set; }

	[Display(Name = "Swing Point: Top", Order = 64, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush SwingPointTop { get; set; }

	[Browsable(false)]
	public string SwingPointTop_Serialize
	{
		get
		{
			return Serialize.BrushToString(SwingPointTop);
		}
		set
		{
			SwingPointTop = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Swing Point: Bottom", Order = 66, GroupName = "Graphics")]
	public System.Windows.Media.Brush SwingPointBottom { get; set; }

	[Browsable(false)]
	public string SwingPointBottom_Serialize
	{
		get
		{
			return Serialize.BrushToString(SwingPointBottom);
		}
		set
		{
			SwingPointBottom = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Swing Point: Rendering Method", Order = 68, GroupName = "Graphics", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbKingOrderBlock_SwingPointRenderingMethod SwingPointRenderingMethod { get; set; }

	[Display(Name = "Bos/Choch: Display Mode", Order = 69, GroupName = "Graphics")]
	public gbKingOrderBlock_BosChochDisplayMode BosChochDisplayMode { get; set; }

	[Display(Name = "Bos Top: Line Enabled", Order = 70, GroupName = "Graphics")]
	public bool BosTopLineEnabled { get; set; }

	[Display(Name = "Bos Top: Line Stroke", Order = 72, GroupName = "Graphics")]
	public Stroke BosTopLineStroke { get; set; }

	[Display(Name = "Bos Top: Text Enabled", Order = 74, GroupName = "Graphics")]
	public bool BosTopTextEnabled { get; set; }

	[Display(Name = "Bos Bottom: Line Enabled", Order = 76, GroupName = "Graphics")]
	public bool BosBottomLineEnabled { get; set; }

	[Display(Name = "Bos Bottom: Line Stroke", Order = 78, GroupName = "Graphics")]
	public Stroke BosBottomLineStroke { get; set; }

	[Display(Name = "Bos Bottom: Text Enabled", Order = 80, GroupName = "Graphics")]
	public bool BosBottomTextEnabled { get; set; }

	[Display(Name = "Choch Top: Line Enabled", Order = 82, GroupName = "Graphics")]
	public bool ChochTopLineEnabled { get; set; }

	[Display(Name = "Choch Top: Line Stroke", Order = 84, GroupName = "Graphics")]
	public Stroke ChochTopLineStroke { get; set; }

	[Display(Name = "Choch Top: Text Enabled", Order = 86, GroupName = "Graphics")]
	public bool ChochTopTextEnabled { get; set; }

	[Display(Name = "Choch Bottom: Line Enabled", Order = 88, GroupName = "Graphics")]
	public bool ChochBottomLineEnabled { get; set; }

	[Display(Name = "Choch Bottom: Line Stroke", Order = 90, GroupName = "Graphics")]
	public Stroke ChochBottomLineStroke { get; set; }

	[Display(Name = "Choch Bottom: Text Enabled", Order = 92, GroupName = "Graphics")]
	public bool ChochBottomTextEnabled { get; set; }

	[Display(Name = "Bos & Choch: Text Opacity", Order = 94, GroupName = "Graphics")]
	[Range(0, 100)]
	public int BosChochTextOpacity { get; set; }

	[Display(Name = "Bos & Choch: Text Font", Order = 96, GroupName = "Graphics")]
	public SimpleFont BosChochTextFont { get; set; }

	[Display(Name = "Bos & Choch: Text Offset", Order = 98, GroupName = "Graphics")]
	public int BosChochTextOffset { get; set; }

	[Range(1, int.MaxValue)]
	[Display(Name = "Swing Point: Neighborhood", Order = 0, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SwingPointNeighborhood { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Imbalance: Qualifying (Bars)", Order = 10, GroupName = "Parameters")]
	public int ImbalanceQualifying { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Order Block: Finding Bos/Choch Period", Order = 22, GroupName = "Parameters")]
	public int OrderBlockFindingBosChochPeriod { get; set; }

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(Name = "Order Block: Age (Bars)", Order = 24, GroupName = "Parameters")]
	public int OrderBlockAge { get; set; }

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Order Blocks: Same Direction Offset (Ticks)", Order = 26, GroupName = "Parameters")]
	public int OrderBlocksSameDirectionOffset { get; set; }

	[Display(Name = "Order Blocks: Difference Direction Offset (Ticks)", Order = 28, GroupName = "Parameters")]
	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	public int OrderBlocksDifferenceDirectionOffset { get; set; }

	[Display(Name = "Signal Trade: Quantity Per Order Block", Order = 30, GroupName = "Parameters")]
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	public int SignalTradeQuantityPerOrderBlock { get; set; }

	[Range(1, int.MaxValue)]
	[Display(Name = "Signal Trade: Split (Bars)", Order = 40, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalTradeSplitBars { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
	public bool SwitchedOn { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => base.Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_State => base.Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Zone_Bullish => base.Values[2];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Zone_Bearish => base.Values[3];

	public override string DisplayName
	{
		get
		{
			if (base.Parent is MarketAnalyzerColumnBase)
			{
				return base.DisplayName;
			}
			return "King Order Block by GreyBeard" + GetUserNote();
		}
	}

	private string GetUserNote()
	{
		string text = UserNote.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		text = text.ToLower();
		string text2 = "instrument";
		string text3 = "period";
		if (text.Contains(text2) && base.Instruments[0] != null)
		{
			text = text.Replace(text2, base.Instruments[0].FullName);
		}
		if (text.Contains(text3) && base.BarsPeriods[0] != null)
		{
			text = text.Replace(text3, base.BarsPeriods[0].ToString());
		}
		return " (" + text + ")";
	}

	protected override void OnStateChange()
	{
		try
		{
			if (base.State == State.SetDefaults)
			{
				base.Description = "Identifies order blocks with BOS/ChoCh market structure.";
				base.Name = "gbKingOrderBlock";
				base.Calculate = Calculate.OnBarClose;
				base.IsOverlay = true;
				base.DisplayInDataBox = true;
				base.DrawOnPricePanel = true;
				base.DrawHorizontalGridLines = true;
				base.DrawVerticalGridLines = true;
				base.PaintPriceMarkers = true;
				base.ScaleJustification = ScaleJustification.Right;
				base.IsSuspendedWhileInactive = false;
				base.BarsRequiredToPlot = 0;
				ShowTransparentPlotsInDataBox = true;
				ConditionReturn = true;
				ConditionBreakout = false;
				PopupEnabled = false;
				PopupBackgroundBrush = Brushes.Gold;
				PopupBackgroundOpacity = 60;
				PopupTextBrush = Brushes.DarkSlateGray;
				PopupTextSize = 16;
				PopupButtonBrush = Brushes.Transparent;
				SoundEnabled = false;
				SoundBreakUp = "Alert1.wav";
				SoundBreakDown = "Alert2.wav";
				SoundReturnBullish = "Alert4.wav";
				SoundReturnBearish = "Alert3.wav";
				SoundRearmEnabled = true;
				SoundRearmSeconds = 5;
				EmailEnabled = false;
				EmailReceiver = "receiver@example.com";
				MarkerEnabled = true;
				MarkerRenderingMethod = gbKingOrderBlock_MarkerRenderingMethod.Custom;
				MarkerBrushBreakUp = Brushes.SpringGreen;
				MarkerBrushBreakDown = Brushes.HotPink;
				MarkerBrushReturnBullish = Brushes.Chartreuse;
				MarkerBrushReturnBearish = Brushes.DeepPink;
				MarkerStringBreakUp = "⇑ + Break";
				MarkerStringBreakDown = "Break + ⇓";
				MarkerStringReturnBullish = "▲ + OB";
				MarkerStringReturnBearish = "OB + ▼";
				MarkerFont = new SimpleFont("Arial", 20);
				MarkerOffset = 10;
				AlertBlockingSeconds = 60;
				SwitchedOn = true;
				ScreenDPI = 99;
				GradientEnabled = true;
				ImbalanceActiveTopStart = Brushes.DeepPink;
				ImbalanceActiveTopEnd = Brushes.Black;
				ImbalanceActiveBottomStart = Brushes.LightSeaGreen;
				ImbalanceActiveBottomEnd = Brushes.Black;
				ImbalanceActiveOpacity = 90;
				ImbalanceActiveBorderTop = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 90);
				ImbalanceActiveBorderBottom = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 90);
				ImbalanceInactiveEnabled = true;
				ImbalanceInactiveTopStart = Brushes.DeepPink;
				ImbalanceInactiveTopEnd = Brushes.Black;
				ImbalanceInactiveBottomStart = Brushes.LightSeaGreen;
				ImbalanceInactiveBottomEnd = Brushes.Black;
				ImbalanceInactiveOpacity = 50;
				ImbalanceInactiveBorderTop = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 50);
				ImbalanceInactiveBorderBottom = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 50);
				OrderBlockActiveTopStart = Brushes.PaleVioletRed;
				OrderBlockActiveTopEnd = Brushes.Black;
				OrderBlockActiveBottomStart = Brushes.LimeGreen;
				OrderBlockActiveBottomEnd = Brushes.Black;
				OrderBlockActiveOpacity = 90;
				OrderBlockActiveBorderTop = new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1f, 90);
				OrderBlockActiveBorderBottom = new Stroke(Brushes.DeepSkyBlue, DashStyleHelper.Dash, 1f, 90);
				OrderBlockInactiveEnabled = true;
				OrderBlockInactiveTopStart = Brushes.HotPink;
				OrderBlockInactiveTopEnd = Brushes.Black;
				OrderBlockInactiveBottomStart = Brushes.MediumSeaGreen;
				OrderBlockInactiveBottomEnd = Brushes.Black;
				OrderBlockInactiveOpacity = 50;
				OrderBlockInactiveBorderTop = new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1f, 50);
				OrderBlockInactiveBorderBottom = new Stroke(Brushes.DeepSkyBlue, DashStyleHelper.Dash, 1f, 50);
				SwingPointRenderingMethod = gbKingOrderBlock_SwingPointRenderingMethod.Custom;
				SwingPointDisplayMode = gbKingOrderBlock_SwingPointDisplayMode.Smart;
				SwingPointTop = Brushes.DodgerBlue;
				SwingPointBottom = Brushes.HotPink;
				BosChochDisplayMode = gbKingOrderBlock_BosChochDisplayMode.Smart;
				BosTopLineEnabled = true;
				BosTopLineStroke = new Stroke(Brushes.Green, DashStyleHelper.Dot, 2f);
				BosTopTextEnabled = false;
				BosBottomLineEnabled = true;
				BosBottomLineStroke = new Stroke(Brushes.HotPink, DashStyleHelper.Dot, 2f);
				BosBottomTextEnabled = false;
				ChochTopLineEnabled = true;
				ChochTopLineStroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Dot, 2f);
				ChochTopTextEnabled = false;
				ChochBottomLineEnabled = true;
				ChochBottomLineStroke = new Stroke(Brushes.DeepPink, DashStyleHelper.Dot, 2f);
				ChochBottomTextEnabled = false;
				BosChochTextFont = new SimpleFont("Arial", 15);
				BosChochTextOffset = 0;
				BosChochTextOpacity = 30;
				SwingPointNeighborhood = 3;
				ImbalanceQualifying = 2;
				OrderBlockFindingBosChochPeriod = 20;
				OrderBlocksSameDirectionOffset = 10;
				OrderBlocksDifferenceDirectionOffset = 10;
				OrderBlockAge = 500;
				SignalTradeQuantityPerOrderBlock = 3;
				SignalTradeSplitBars = 6;
				IndicatorZOrder = -10;
				UserNote = "instrument (period)";
				AddPlot(Brushes.Transparent, "Signal Trade");
				AddPlot(Brushes.Transparent, "Signal State");
				AddPlot(Brushes.Transparent, "Signal Zone: Bullish");
				AddPlot(Brushes.Transparent, "Signal Zone: Bearish");
			}
			else if (base.State == State.Configure)
			{
				orderBlockOffsetSameDirection = (double)OrderBlocksSameDirectionOffset * base.TickSize;
				orderBlockOffsetDifferenceDirection = (double)OrderBlocksDifferenceDirectionOffset * base.TickSize;
				listSwingsTop = new SortedList<int, SwingPoint>();
				listSwingsBottom = new SortedList<int, SwingPoint>();
				listBosChochInfoTop = new SortedList<int, BosChochInfo>();
				listBosChochInfoBottom = new SortedList<int, BosChochInfo>();
				listOrderBlockActive = new SortedList<int, OrderBlockInfo>();
				listOrderBlockInactive = new SortedList<int, OrderBlockInfo>();
				listImbalanceActive = new SortedList<int, ImbalanceInfo>();
				listImbalanceInactive = new SortedList<int, ImbalanceInfo>();
				dictImbalanceInfosBroken = new Dictionary<int, ImbalanceInfo>();
				dictOrderBlockInfosBroken = new Dictionary<int, OrderBlockInfo>();
				isMarkerCustomRenderingMethod = MarkerRenderingMethod == gbKingOrderBlock_MarkerRenderingMethod.Custom;
				if (isMarkerCustomRenderingMethod)
				{
					listMarkers = new SortedList<int, MarkerInfo>();
				}
				isSwingPointCustomRenderingMethod = SwingPointRenderingMethod == gbKingOrderBlock_SwingPointRenderingMethod.Custom;
				isSwingPointSmartDisplayMode = SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Smart;
				isBosChochSmartDisplayMode = BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Smart;
				bosTopTextColor = BosTopLineStroke.Brush;
				bosBottomTextColor = BosBottomLineStroke.Brush;
				chochTopTextColor = ChochTopLineStroke.Brush;
				chochBottomTextColor = ChochBottomLineStroke.Brush;
				if (GradientEnabled)
				{
					imbalanceActiveBottomGradientStop = CreateGradientStopArr(ImbalanceActiveBottomStart, ImbalanceActiveBottomEnd, ImbalanceActiveOpacity);
					imbalanceActiveTopGradientStop = CreateGradientStopArr(ImbalanceActiveTopStart, ImbalanceActiveTopEnd, ImbalanceActiveOpacity);
					imbalanceInactiveBottomGradientStop = CreateGradientStopArr(ImbalanceInactiveBottomStart, ImbalanceInactiveBottomEnd, ImbalanceInactiveOpacity);
					imbalanceInactiveTopGradientStop = CreateGradientStopArr(ImbalanceInactiveTopStart, ImbalanceInactiveTopEnd, ImbalanceInactiveOpacity);
					orderBlockActiveBottomGradientStop = CreateGradientStopArr(OrderBlockActiveBottomStart, OrderBlockActiveBottomEnd, OrderBlockActiveOpacity);
					orderBlockActiveTopGradientStop = CreateGradientStopArr(OrderBlockActiveTopStart, OrderBlockActiveTopEnd, OrderBlockActiveOpacity);
					orderBlockInactiveBottomGradientStop = CreateGradientStopArr(OrderBlockInactiveBottomStart, OrderBlockInactiveBottomEnd, OrderBlockInactiveOpacity);
					orderBlockInactiveTopGradientStop = CreateGradientStopArr(OrderBlockInactiveTopStart, OrderBlockInactiveTopEnd, OrderBlockInactiveOpacity);
				}
				else
				{
					imbalanceActiveTop = CreateOpacityBrush(ImbalanceActiveTopStart, ImbalanceActiveOpacity);
					imbalanceActiveBottom = CreateOpacityBrush(ImbalanceActiveBottomStart, ImbalanceActiveOpacity);
					imbalanceInactiveTop = CreateOpacityBrush(ImbalanceInactiveTopStart, ImbalanceInactiveOpacity);
					imbalanceInactiveBottom = CreateOpacityBrush(ImbalanceInactiveBottomStart, ImbalanceInactiveOpacity);
					orderBlockActiveTop = CreateOpacityBrush(OrderBlockActiveTopStart, OrderBlockActiveOpacity);
					orderBlockActiveBottom = CreateOpacityBrush(OrderBlockActiveBottomStart, OrderBlockActiveOpacity);
					orderBlockInactiveTop = CreateOpacityBrush(OrderBlockInactiveTopStart, OrderBlockInactiveOpacity);
					orderBlockInactiveBottom = CreateOpacityBrush(OrderBlockInactiveBottomStart, OrderBlockInactiveOpacity);
				}
				isOnBarCloseMode = base.Calculate == Calculate.OnBarClose;
			}
			else if (base.State == State.Historical)
			{
				if (ScreenDPI < 100 && isCharting)
				{
					base.ChartControl.Dispatcher.Invoke(() =>
					{
						try
						{
							ScreenDPI = (int)System.Windows.Media.VisualTreeHelper.GetDpi(base.ChartControl).PixelsPerInchX;
						}
						catch { ScreenDPI = 99; }
					});
				}
				if (IndicatorZOrder != 0)
				{
					SetZOrder(IndicatorZOrder);
				}
				isCharting = base.ChartControl != null;
				if (isCharting)
				{
					barOutlineWidth = base.ChartBars.Properties.ChartStyle.Stroke2.Width;
				}
				if (!isCharting)
				{
					return;
				}
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (rearmTimer == null)
					{
						rearmTimer = new DispatcherTimer();
						rearmTimer.Interval = TimeSpan.FromMilliseconds(100.0);
						rearmTimer.Tick += OnRearmTimerTick;
					}
				});
			}
			else
			{
				if (base.State != State.Terminated)
				{
					return;
				}
				if (isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
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
				DisposeDxResourceCaches();
				DisposeTextFormatCache();
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private SharpDX.Direct2D1.GradientStop[] CreateGradientStopArr(System.Windows.Media.Brush brushStart, System.Windows.Media.Brush brushEnd, int opacity)
	{
		if (brushStart.IsTransparent() && brushEnd.IsTransparent())
		{
			return null;
		}
		System.Windows.Media.SolidColorBrush solidColorBrush = (System.Windows.Media.SolidColorBrush)CreateOpacityBrush(brushStart, opacity);
		Color4 color = new Color4((float)(int)solidColorBrush.Color.R / 255f, (float)(int)solidColorBrush.Color.G / 255f, (float)(int)solidColorBrush.Color.B / 255f, (float)(int)solidColorBrush.Color.A / 255f);
		System.Windows.Media.SolidColorBrush solidColorBrush2 = (System.Windows.Media.SolidColorBrush)CreateOpacityBrush(brushEnd, opacity);
		Color4 color2 = new Color4((float)(int)solidColorBrush2.Color.R / 255f, (float)(int)solidColorBrush2.Color.G / 255f, (float)(int)solidColorBrush2.Color.B / 255f, (float)(int)solidColorBrush2.Color.A / 255f);
		return new SharpDX.Direct2D1.GradientStop[3]
		{
			new SharpDX.Direct2D1.GradientStop
			{
				Position = -0.2f,
				Color = color2
			},
			new SharpDX.Direct2D1.GradientStop
			{
				Position = 0.5f,
				Color = color
			},
			new SharpDX.Direct2D1.GradientStop
			{
				Position = 1.2f,
				Color = color2
			}
		};
	}

	protected override void OnBarUpdate()
	{
		try
		{
			int num = (signalZoneBearish = 0);
			signalZoneBullish = 0;
			signalState = 0;
			if (!isOnBarCloseMode)
			{
				isLastBarOnEachTick = IsLastBarOnEachTick();
				CalculateOnEachTickMode();
			}
			FindSwingsByNeighborhood();
			FindBrokenSwingPointAndAddBosChochInfoToList(listSwingsTop, isTop: true);
			FindBrokenSwingPointAndAddBosChochInfoToList(listSwingsBottom, isTop: false);
			if (base.CurrentBar >= 3)
			{
				FindImbalanceAndAddToList();
				FindBrokenImbalance();
				num = FindBrokenOrderBlockAndSignal();
				FindOrderBlockAndAddToList();
				if (base.IsFirstTickOfBar)
				{
					PruneInactiveZoneLists();
				}
				ComputeSignalState();
				Signal_Trade[0] = num;
				Signal_State[0] = signalState;
				Signal_Zone_Bullish[0] = signalZoneBullish;
				Signal_Zone_Bearish[0] = signalZoneBearish;
				if (base.IsFirstTickOfBar)
				{
					prevState = base.State;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void ComputeSignalState()
	{
		if (listOrderBlockActive == null || listOrderBlockActive.Count == 0)
		{
			return;
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		int num = listOrderBlockActive.Count - 1;
		while (num >= 0 && (flag || flag3 || flag2))
		{
			ZoneInfo zoneInfo = listOrderBlockActive.Values[num];
			if (zoneInfo != null)
			{
				if (flag)
				{
					if (zoneInfo.IsTop)
					{
						if (base.Close[0].ApproxCompare(zoneInfo.PriceBottom) >= 0)
						{
							signalState = -2;
							flag = false;
						}
						else if (base.High[0].ApproxCompare(zoneInfo.PriceBottom) >= 0)
						{
							signalState = -1;
							flag = false;
						}
					}
					else if (base.Close[0].ApproxCompare(zoneInfo.PriceTop) <= 0)
					{
						signalState = 2;
						flag = false;
					}
					else if (base.Low[0].ApproxCompare(zoneInfo.PriceTop) <= 0)
					{
						signalState = 1;
						flag = false;
					}
				}
				if (flag2 && !zoneInfo.IsTop)
				{
					signalZoneBullish = 1;
					flag2 = false;
				}
				if (flag3 && zoneInfo.IsTop)
				{
					signalZoneBearish = 1;
					flag3 = false;
				}
			}
			num--;
		}
	}

	private void CalculateOnEachTickMode()
	{
		if (base.State != State.Historical)
		{
			if (base.IsFirstTickOfBar)
			{
				FindLastValues();
				BackupPropertiesAndLastValues();
				ChangePropertiesListActive(listOrderBlockActive, isBackup: true);
				ResetFlagAndDictBroken();
			}
			RevertAllChanged();
		}
	}

	public void FindLastValues()
	{
		// Indexed access — SortedList exposes no IList fast path for Enumerable.Last(),
		// so .Last() walked the entire list on every first tick of bar.
		lastSwingPointTop = ((listSwingsTop.Count > 0) ? listSwingsTop.Values[listSwingsTop.Count - 1] : null);
		lastSwingPointBottom = ((listSwingsBottom.Count > 0) ? listSwingsBottom.Values[listSwingsBottom.Count - 1] : null);
		if (flagNewSwingPoint)
		{
			KeyValuePair<int, SwingPoint>? lastElement = GetLastElement(listSwingsTop, listSwingsBottom);
			lastSwingPoint = ((!lastElement.HasValue) ? null : lastElement.Value.Value);
		}
		if (flagNewBosChochTop || flagNewBosChochBottom)
		{
			KeyValuePair<int, BosChochInfo>? lastElement2 = GetLastElement(listBosChochInfoTop, listBosChochInfoBottom);
			lastBosChochInfo = ((!lastElement2.HasValue) ? null : lastElement2.Value.Value);
		}
		if (flagNewImbalance)
		{
			KeyValuePair<int, ImbalanceInfo>? lastElement3 = GetLastElement(listImbalanceActive, listImbalanceInactive);
			lastImbalanceInfo = ((!lastElement3.HasValue) ? null : lastElement3.Value.Value);
		}
	}

	private bool IsLastBarOnEachTick()
	{
		if (!isOnBarCloseMode)
		{
			return base.CurrentBar == base.Bars.Count - 1;
		}
		return false;
	}

	public void ChangePropertiesListActive<TValue>(SortedList<int, TValue> listActive, bool isBackup) where TValue : BackupExtension
	{
		if (listActive.Count == 0)
		{
			return;
		}
		// Direct calls via the BackupExtension base — the previous implementation
		// resolved BackupProperties/RevertProperties through reflection and boxed
		// the cache dictionary into a new object[] for every element on every
		// first tick of bar.
		for (int i = 0; i < listActive.Count; i++)
		{
			BackupExtension element = listActive.Values[i];
			if (isBackup)
			{
				element.BackupProperties(cachePropertyInfo);
			}
			else
			{
				element.RevertProperties(cachePropertyInfo);
			}
		}
	}

	public void BackupPropertiesAndLastValues()
	{
		if (lastSwingPointTop != null)
		{
			lastSwingPointTop.BackupProperties(cachePropertyInfo);
		}
		if (lastSwingPointBottom != null)
		{
			lastSwingPointBottom.BackupProperties(cachePropertyInfo);
		}
		if (lastSwingPoint != null)
		{
			lastSwingPoint.BackupProperties(cachePropertyInfo);
		}
		if (lastBosChochInfo != null)
		{
			lastBosChochInfo.BackupProperties(cachePropertyInfo);
		}
		if (lastImbalanceInfo != null)
		{
			lastImbalanceInfo.BackupProperties(cachePropertyInfo);
		}
		prevReturnBullishBarIndex = returnBullishBarIndex;
		prevReturnBearishBarIndex = returnBearishBarIndex;
	}

	public void RevertPropertiesAndLastValues()
	{
		if (lastSwingPointTop != null)
		{
			lastSwingPointTop.RevertProperties(cachePropertyInfo);
		}
		if (lastSwingPointBottom != null)
		{
			lastSwingPointBottom.RevertProperties(cachePropertyInfo);
		}
		if (lastSwingPoint != null)
		{
			lastSwingPoint.RevertProperties(cachePropertyInfo);
		}
		if (lastBosChochInfo != null)
		{
			lastBosChochInfo.RevertProperties(cachePropertyInfo);
		}
		if (lastImbalanceInfo != null)
		{
			lastImbalanceInfo.RevertProperties(cachePropertyInfo);
		}
		returnBullishBarIndex = prevReturnBullishBarIndex;
		returnBearishBarIndex = prevReturnBearishBarIndex;
	}

	public void ResetFlagAndDictBroken()
	{
		dictImbalanceInfosBroken.Clear();
		dictOrderBlockInfosBroken.Clear();
		flagNewBosChochBottom = false;
		flagNewBosChochTop = false;
		flagNewSwingPoint = false;
		flagNewMaker = false;
		flagNewOrderBlock = false;
		flagNewImbalance = false;
	}

	public void RevertAllChanged()
	{
		if (flagNewOrderBlock)
		{
			RemoveLastElementInList(listOrderBlockActive);
		}
		if (dictOrderBlockInfosBroken.Count > 0)
		{
			RevertListBroken(dictOrderBlockInfosBroken, listOrderBlockInactive, listOrderBlockActive);
		}
		if (flagNewImbalance)
		{
			RemoveLastElementInList(listImbalanceActive);
		}
		if (dictImbalanceInfosBroken.Count > 0)
		{
			RevertListBroken(dictImbalanceInfosBroken, listImbalanceInactive, listImbalanceActive);
		}
		if (flagNewBosChochTop)
		{
			RemoveLastElementInList(listBosChochInfoTop);
		}
		if (flagNewBosChochBottom)
		{
			RemoveLastElementInList(listBosChochInfoBottom);
		}
		if (flagNewSwingPoint)
		{
			SortedList<int, SwingPoint> listData = (lastSwingPoint.IsTop ? listSwingsTop : listSwingsBottom);
			RemoveLastElementInList(listData);
			RemoveSwingPointBuiltinMode(lastSwingPoint.IsTop, base.CurrentBar - SwingPointNeighborhood);
		}
		if (flagNewMaker)
		{
			RemoveMarker();
		}
		if (!base.IsFirstTickOfBar)
		{
			FindLastValues();
		}
		if (prevState == State.Realtime)
		{
			RevertPropertiesAndLastValues();
		}
		ResetFlagAndDictBroken();
		ChangePropertiesListActive(listOrderBlockActive, isBackup: false);
	}

	private void RemoveMarker()
	{
		if (isMarkerCustomRenderingMethod)
		{
			RemoveLastElementInList(listMarkers);
			return;
		}
		string tag = "gbKingOrderBlock.marker." + base.CurrentBar;
		if (base.DrawObjects[tag] != null)
		{
			RemoveDrawObject(tag);
		}
	}

	private KeyValuePair<int, TValue>? GetLastElement<TValue>(SortedList<int, TValue> list1, SortedList<int, TValue> list2)
	{
		// Indexed access instead of Enumerable.Last() — see FindLastValues.
		KeyValuePair<int, TValue>? result = ((list1.Count > 0) ? new KeyValuePair<int, TValue>?(new KeyValuePair<int, TValue>(list1.Keys[list1.Count - 1], list1.Values[list1.Count - 1])) : ((KeyValuePair<int, TValue>?)null));
		KeyValuePair<int, TValue>? result2 = ((list2.Count > 0) ? new KeyValuePair<int, TValue>?(new KeyValuePair<int, TValue>(list2.Keys[list2.Count - 1], list2.Values[list2.Count - 1])) : ((KeyValuePair<int, TValue>?)null));
		if (!result.HasValue && !result2.HasValue)
		{
			return null;
		}
		if (result2.HasValue)
		{
			if (result.HasValue)
			{
				if (result.Value.Key <= result2.Value.Key)
				{
					return result2;
				}
				return result;
			}
			return result2;
		}
		return result;
	}

	private void RevertListBroken<TValue>(Dictionary<int, TValue> dictBroken, SortedList<int, TValue> sortedListInactive, SortedList<int, TValue> sortedListActive)
	{
		foreach (KeyValuePair<int, TValue> item in dictBroken)
		{
			int key = item.Key;
			TValue value = item.Value;
			MoveElementFromListToList(key, value, sortedListInactive, sortedListActive);
		}
	}

	private void FindBrokenImbalance()
	{
		if (listImbalanceActive.Count == 0)
		{
			return;
		}
		for (int i = 0; i < listImbalanceActive.Count; i++)
		{
			int key = listImbalanceActive.Keys[i];
			ImbalanceInfo imbalanceInfo = listImbalanceActive.Values[i];
			if (IsImbalanceBroken(imbalanceInfo))
			{
				imbalanceInfo.IsBroken = true;
				MoveElementFromListToList(key, imbalanceInfo, listImbalanceActive, listImbalanceInactive);
				if (isLastBarOnEachTick)
				{
					dictImbalanceInfosBroken.Add(key, imbalanceInfo);
				}
			}
			else
			{
				imbalanceInfo.BarEnd = base.CurrentBar;
			}
		}
	}

	private bool IsImbalanceBroken(ImbalanceInfo imbalanceInfo)
	{
		return ((((imbalanceInfo.IsTop ? base.High[0] : base.Low[0]) - imbalanceInfo.PriceBroken) * (double)imbalanceInfo.Sign).ApproxCompare(0.0) < 0 && imbalanceInfo.BarEnd != base.CurrentBar) | (base.CurrentBar - imbalanceInfo.BarStart > OrderBlockAge);
	}

	private void FindImbalanceAndAddToList()
	{
		ImbalanceInfo imbalanceInfo = GetImbalanceInfo();
		if (imbalanceInfo != null)
		{
			AddElementToList(imbalanceInfo.BarStart, imbalanceInfo, listImbalanceActive);
			lastImbalanceInfo = imbalanceInfo;
			if (isLastBarOnEachTick)
			{
				flagNewImbalance = true;
			}
		}
	}

	private ImbalanceInfo GetImbalanceInfo()
	{
		if (base.Close[0].ApproxCompare(base.Open[0]) == 0)
		{
			return null;
		}
		bool flag = base.Close[0].ApproxCompare(base.Open[0]) < 0;
		MarubozuInfo marubozuInfo = CheckMarubozuBar(0, flag);
		if (lastImbalanceInfo != null)
		{
			if (marubozuInfo == null)
			{
				lastImbalanceInfo.IsFixed = true;
				return null;
			}
			if (!lastImbalanceInfo.IsFixed && !lastImbalanceInfo.IsBroken && lastImbalanceInfo.IsTop == flag && lastImbalanceInfo.BarEnd == base.CurrentBar - 1)
			{
				if (flag)
				{
					lastImbalanceInfo.PriceBottom = base.Close[0];
				}
				else
				{
					lastImbalanceInfo.PriceTop = base.Close[0];
				}
				lastImbalanceInfo.BarEnd = base.CurrentBar;
				return null;
			}
		}
		double num = -2147483648.0;
		double num2 = 2147483647.0;
		int num3 = ImbalanceQualifying - 1;
		while (true)
		{
			if (num3 >= 0)
			{
				MarubozuInfo marubozuInfo2 = CheckMarubozuBar(num3, flag);
				if (marubozuInfo2 == null)
				{
					break;
				}
				num = Math.Max(num, marubozuInfo2.PriceTop);
				num2 = Math.Min(num2, marubozuInfo2.PriceBottom);
				num3--;
				continue;
			}
			int barStart = base.CurrentBar - ImbalanceQualifying + 1;
			return new ImbalanceInfo(flag, isBroken: false, barStart, base.CurrentBar, num, num2);
		}
		return null;
	}

	private MarubozuInfo CheckMarubozuBar(int barAgo, bool isTop)
	{
		double num = (isTop ? base.Open[barAgo] : base.Close[barAgo]);
		double num2 = (isTop ? base.Close[barAgo] : base.Open[barAgo]);
		if (num.ApproxCompare(num2) == 0)
		{
			return null;
		}
		if (num.ApproxCompare(base.High[barAgo]) == 0 && num2.ApproxCompare(base.Low[barAgo]) == 0)
		{
			return new MarubozuInfo(num, num2);
		}
		return null;
	}

	private int FindBrokenOrderBlockAndSignal()
	{
		if (listOrderBlockActive.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < listOrderBlockActive.Count; i++)
		{
			int key = listOrderBlockActive.Keys[i];
			OrderBlockInfo orderBlockInfo = listOrderBlockActive.Values[i];
			orderBlockInfo.BarEnd = base.CurrentBar - 1;
			bool flag = base.CurrentBar - orderBlockInfo.BarStart > OrderBlockAge;
			bool flag2 = !orderBlockInfo.IsTop;
			int num2 = 1;
			int num3 = 2;
			if (base.Closes[0][0].ApproxCompare(orderBlockInfo.PriceBroken) * orderBlockInfo.Sign < 0 || flag)
			{
				if (orderBlockInfo.CountReturnSignal == 0 && Math.Abs(num) != num2 && !flag)
				{
					num = -num3 * orderBlockInfo.Sign;
					if (ConditionBreakout)
					{
						if (isLastBarOnEachTick)
						{
							flagNewMaker = true;
						}
						if (isMarkerCustomRenderingMethod)
						{
							AddMarker(base.CurrentBar, !flag2, SignalType.Breakout);
						}
						else
						{
							PrintMarker(!flag2, SignalType.Breakout);
						}
						TriggerAlerts(!flag2, SignalType.Breakout);
					}
				}
				MoveElementFromListToList(key, orderBlockInfo, listOrderBlockActive, listOrderBlockInactive);
				if (isLastBarOnEachTick)
				{
					dictOrderBlockInfosBroken.Add(key, orderBlockInfo);
				}
				continue;
			}
			bool flag3 = ((!orderBlockInfo.IsTop) ? (returnBullishBarIndex == -1 || base.CurrentBar - returnBullishBarIndex > SignalTradeSplitBars) : (returnBearishBarIndex == -1 || base.CurrentBar - returnBearishBarIndex > SignalTradeSplitBars));
			bool flag4 = orderBlockInfo.CountReturnSignal >= SignalTradeQuantityPerOrderBlock;
			if (!((((flag2 ? base.Low[0] : base.High[0]) - orderBlockInfo.PriceSignal) * (double)orderBlockInfo.Sign).ApproxCompare(0.0) < 0 && IsReversalBar(-orderBlockInfo.Sign) && flag3) || flag4 || Math.Abs(num) == num2)
			{
				continue;
			}
			num = orderBlockInfo.Sign;
			if (num == num2)
			{
				returnBullishBarIndex = base.CurrentBar;
			}
			else
			{
				returnBearishBarIndex = base.CurrentBar;
			}
			orderBlockInfo.CountReturnSignal++;
			if (ConditionReturn)
			{
				if (isLastBarOnEachTick)
				{
					flagNewMaker = true;
				}
				if (isMarkerCustomRenderingMethod)
				{
					AddMarker(base.CurrentBar, flag2, SignalType.Return);
				}
				else
				{
					PrintMarker(flag2, SignalType.Return);
				}
				TriggerAlerts(flag2, SignalType.Return);
			}
		}
		return num;
	}

	private void FindOrderBlockAndAddToList()
	{
		if (lastImbalanceInfo == null || lastSwingPoint == null || lastBosChochInfo == null || lastSwingPoint.IsHasOrderBlock || base.CurrentBar - lastSwingPoint.BarEnd > OrderBlockFindingBosChochPeriod || lastSwingPoint.IsTop != lastImbalanceInfo.IsTop || lastSwingPoint.IsTop == lastBosChochInfo.IsTop || lastBosChochInfo.BarEnd < lastSwingPoint.BarEnd || lastImbalanceInfo.BarStart < lastSwingPoint.BarStart)
		{
			return;
		}
		int num = base.CurrentBar - lastSwingPoint.BarStart;
		double num2 = base.High[num];
		double num3 = base.Low[num];
		if (!IsOffsetOf2OrderBlockOk(num))
		{
			return;
		}
		double num4 = (lastSwingPoint.IsTop ? lastImbalanceInfo.PriceTop : lastImbalanceInfo.PriceBottom);
		double num5 = (lastSwingPoint.IsTop ? num2 : num3);
		if ((num4 - num5) * (double)lastImbalanceInfo.Sign < 0.0)
		{
			return;
		}
		OrderBlockInfo element = new OrderBlockInfo(lastSwingPoint.IsTop, isBroken: false, lastSwingPoint.BarStart, base.CurrentBar, num2, num3);
		AddElementToList(lastSwingPoint.BarEnd, element, listOrderBlockActive);
		lastSwingPoint.IsHasOrderBlock = true;
		lastBosChochInfo.IsHasOrderBlock = true;
		if (isLastBarOnEachTick)
		{
			flagNewOrderBlock = true;
		}
		SwingPoint swingPoint = (lastBosChochInfo.IsTop ? listSwingsTop : listSwingsBottom)[lastBosChochInfo.BarStart];
		if (isBosChochSmartDisplayMode)
		{
			swingPoint.AllowDraw = true;
			if (isSwingPointSmartDisplayMode)
			{
				DrawSwingPointBuiltinMode(swingPoint.IsTop, swingPoint.BarEnd, base.CurrentBar - swingPoint.BarEnd);
			}
		}
	}

	private bool IsOffsetOf2OrderBlockOk(int startBarAgo)
	{
		if (listOrderBlockActive.Count == 0)
		{
			return true;
		}
		OrderBlockInfo orderBlockInfo = null;
		OrderBlockInfo orderBlockInfo2 = null;
		for (int num = listOrderBlockActive.Count - 1; num >= 0; num--)
		{
			OrderBlockInfo orderBlockInfo3 = listOrderBlockActive.Values[num];
			if (orderBlockInfo != null && orderBlockInfo2 != null)
			{
				break;
			}
			if (orderBlockInfo3.IsTop && orderBlockInfo == null)
			{
				orderBlockInfo = orderBlockInfo3;
			}
			else if (!orderBlockInfo3.IsTop && orderBlockInfo2 == null)
			{
				orderBlockInfo2 = orderBlockInfo3;
			}
		}
		if (orderBlockInfo != null && (orderBlockInfo.PriceBottom - base.High[startBarAgo]).ApproxCompare(lastSwingPoint.IsTop ? orderBlockOffsetSameDirection : orderBlockOffsetDifferenceDirection) <= 0)
		{
			return false;
		}
		if (orderBlockInfo2 != null && (base.Low[startBarAgo] - orderBlockInfo2.PriceTop).ApproxCompare(lastSwingPoint.IsTop ? orderBlockOffsetDifferenceDirection : orderBlockOffsetSameDirection) <= 0)
		{
			return false;
		}
		return true;
	}

	private double GetPrice(bool isCheckingTop, int barIndex)
	{
		if (!isCheckingTop)
		{
			return base.Low.GetValueAt(barIndex);
		}
		return base.High.GetValueAt(barIndex);
	}

	private void FindSwingsByNeighborhood()
	{
		CheckSwingPoint(isCheckingTop: true, listSwingsTop);
		CheckSwingPoint(isCheckingTop: false, listSwingsBottom);
	}

	private void CheckSwingPoint(bool isCheckingTop, SortedList<int, SwingPoint> listSwings)
	{
		try
		{
			int key = base.CurrentBar - SwingPointNeighborhood;
			SwingPoint swingPoint = CheckSwingPoint(isCheckingTop);
			if (swingPoint != null)
			{
				if (!listSwings.ContainsKey(key))
				{
					listSwings.Add(key, swingPoint);
				}
				if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.All)
				{
					DrawSwingPointBuiltinMode(isCheckingTop, key, SwingPointNeighborhood);
				}
				lastSwingPoint = swingPoint;
				if (isLastBarOnEachTick)
				{
					flagNewSwingPoint = true;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void DrawSwingPointBuiltinMode(bool isTop, int key, int swingPointIndex)
	{
		try
		{
			if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Disabled || isSwingPointCustomRenderingMethod)
			{
				return;
			}
			System.Windows.Media.Brush brush = (isTop ? SwingPointTop : SwingPointBottom);
			if (!brush.IsTransparent())
			{
				string tag = "gbKingOrderBlock.dot.price." + (isTop ? "top." : "bottom.") + key;
				Dot dot = Draw.Dot(this, tag, isAutoScale: true, swingPointIndex, (isTop ? base.High : base.Low)[swingPointIndex], brush);
				if (dot != null)
				{
					dot.ZOrder = -100;
				}
				if (!SwitchedOn)
				{
					dot.IsVisible = false;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void RemoveSwingPointBuiltinMode(bool isTop, int key)
	{
		try
		{
			if (SwingPointDisplayMode != gbKingOrderBlock_SwingPointDisplayMode.Disabled && !isSwingPointCustomRenderingMethod)
			{
				string tag = "gbKingOrderBlock.dot.price." + (isTop ? "top." : "bottom.") + key;
				if (base.DrawObjects[tag] != null)
				{
					RemoveDrawObject(tag);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private SwingPoint CheckSwingPoint(bool isCheckingTop)
	{
		SwingPoint swingPoint = CheckSwingPointType1(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		swingPoint = CheckSwingPointType2(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		swingPoint = CheckSwingPointType3(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		return null;
	}

	private SwingPoint CheckSwingPointType1(bool isCheckingTop)
	{
		if (base.CurrentBar < 2 * SwingPointNeighborhood)
		{
			return null;
		}
		int barStart = base.CurrentBar - SwingPointNeighborhood;
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = (isCheckingTop ? 1 : (-1));
		double price = GetPrice(isCheckingTop, num);
		int num3 = base.CurrentBar - 2 * SwingPointNeighborhood;
		while (true)
		{
			if (num3 <= base.CurrentBar)
			{
				if (num3 != num)
				{
					int num4 = GetPrice(isCheckingTop, num3).ApproxCompare(price);
					if (num2 * num4 >= 0)
					{
						break;
					}
				}
				num3++;
				continue;
			}
			return new SwingPoint(isCheckingTop, price, barStart, num);
		}
		return null;
	}

	private SwingPoint CheckSwingPointType2(bool isCheckingTop)
	{
		if (base.CurrentBar < 3 * SwingPointNeighborhood)
		{
			return null;
		}
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = num - SwingPointNeighborhood;
		int num3 = (isCheckingTop ? 1 : (-1));
		int num4 = 1;
		double price = GetPrice(isCheckingTop, num);
		int num5 = num - 1;
		while (num5 >= num2 && GetPrice(isCheckingTop, num5).ApproxCompare(price) == 0)
		{
			num4++;
			num5--;
		}
		if (num4 < 2)
		{
			return null;
		}
		num2 = num - num4 + 1;
		int num6 = num2 - SwingPointNeighborhood;
		while (true)
		{
			if (num6 <= base.CurrentBar)
			{
				if (num6 < num2 || num6 > num)
				{
					int num7 = GetPrice(isCheckingTop, num6).ApproxCompare(price);
					if (num3 * num7 >= 0)
					{
						break;
					}
				}
				num6++;
				continue;
			}
			return new SwingPoint(isCheckingTop, price, num2, num);
		}
		return null;
	}

	private SwingPoint CheckSwingPointType3(bool isCheckingTop)
	{
		if (base.CurrentBar < 3 * SwingPointNeighborhood)
		{
			return null;
		}
		ValueData valueData = new ValueData();
		ValueData valueData2 = new ValueData();
		ValueData valueData3 = new ValueData();
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = num - SwingPointNeighborhood;
		int num3 = (isCheckingTop ? 1 : (-1));
		int num4 = 1;
		double price = GetPrice(isCheckingTop, num);
		int num5 = 0;
		int num6 = num - 1;
		while (true)
		{
			if (num6 >= num2)
			{
				int num7 = GetPrice(isCheckingTop, num6).ApproxCompare(price);
				if (num3 * num7 > 0)
				{
					break;
				}
				if (num7 == 0)
				{
					num5 = num6;
					num4++;
				}
				num6--;
				continue;
			}
			if (num4 < 2)
			{
				return null;
			}
			num2 = num5;
			int num8 = num2 - SwingPointNeighborhood;
			while (true)
			{
				if (num8 <= base.CurrentBar)
				{
					double price2 = GetPrice(isCheckingTop, num8);
					if (num8 >= num2 && num8 <= num)
					{
						valueData.Add(price2);
					}
					else
					{
						int num9 = price2.ApproxCompare(price);
						if (num3 * num9 >= 0)
						{
							break;
						}
						if (num8 < num2)
						{
							valueData2.Add(price2);
						}
						else
						{
							valueData3.Add(price2);
						}
					}
					num8++;
					continue;
				}
				if (valueData.MaxValue.ApproxCompare(valueData.MinValue) == 0)
				{
					return null;
				}
				double @double = (isCheckingTop ? Math.Max(valueData2.MinValue, valueData3.MinValue) : Math.Min(valueData2.MaxValue, valueData3.MaxValue));
				int num10 = (isCheckingTop ? valueData.MinValue : valueData.MaxValue).ApproxCompare(@double);
				if (num3 * num10 <= 0)
				{
					return null;
				}
				return new SwingPoint(isCheckingTop, price, num2, num);
			}
			return null;
		}
		return null;
	}

	private void FindBrokenSwingPointAndAddBosChochInfoToList(SortedList<int, SwingPoint> listSwingPoint, bool isTop)
	{
		try
		{
			if (listSwingPoint.Count <= 0)
			{
				return;
			}
			SwingPoint swingPoint = listSwingPoint.Values[listSwingPoint.Count - 1];
			if (swingPoint.IsBroken)
			{
				return;
			}
			int barEnd = swingPoint.BarEnd;
			double price = swingPoint.Price;
			double num = base.Open[0];
			double num2 = base.Close[0];
			double num3 = Math.Abs(num - num2);
			int barEnd2 = base.CurrentBar - 1;
			if (isTop)
			{
				if (price.ApproxCompare(num) > 0 && (!(num3 > 0.0) || ((num.ApproxCompare(price) < 0 || num2.ApproxCompare(price) > 0) && (num2.ApproxCompare(price) < 0 || num.ApproxCompare(price) > 0))))
				{
					return;
				}
				swingPoint.IsBroken = true;
				if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.All)
				{
					swingPoint.AllowDraw = true;
					if (isSwingPointSmartDisplayMode)
					{
						DrawSwingPointBuiltinMode(isTop, barEnd, base.CurrentBar - barEnd);
					}
				}
				BosChochInfo value;
				if (lastBosChochInfo == null)
				{
					value = new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2);
				}
				else
				{
					bool flag = lastBosChochInfo.BosChochType == BosChochType.Choch;
					bool isTop2 = lastBosChochInfo.IsTop;
					value = ((!flag && isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : ((flag && isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : ((flag || isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2))));
				}
				if (isLastBarOnEachTick)
				{
					flagNewBosChochTop = true;
				}
				listBosChochInfoTop.Add(barEnd, value);
				lastBosChochInfo = value;
			}
			else
			{
				if (price.ApproxCompare(num) < 0 && (!(num3 > 0.0) || ((num.ApproxCompare(price) < 0 || num2.ApproxCompare(price) > 0) && (num2.ApproxCompare(price) < 0 || num.ApproxCompare(price) > 0))))
				{
					return;
				}
				swingPoint.IsBroken = true;
				if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.All)
				{
					swingPoint.AllowDraw = true;
					if (isSwingPointSmartDisplayMode)
					{
						DrawSwingPointBuiltinMode(isTop, barEnd, base.CurrentBar - barEnd);
					}
				}
				BosChochInfo value2;
				if (lastBosChochInfo == null)
				{
					value2 = new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2);
				}
				else
				{
					bool flag2 = lastBosChochInfo.BosChochType == BosChochType.Choch;
					bool isTop3 = lastBosChochInfo.IsTop;
					value2 = ((!flag2 && isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : ((flag2 && isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : ((flag2 || isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2))));
				}
				if (isLastBarOnEachTick)
				{
					flagNewBosChochBottom = true;
				}
				listBosChochInfoBottom.Add(barEnd, value2);
				lastBosChochInfo = value2;
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		try
		{
			if (isCharting && SwitchedOn && !base.IsInHitTest)
			{
				// Device brushes are tied to the RenderTarget — rebuild the caches
				// lazily when NT8 hands us a new target (resize / device loss).
				if (!object.ReferenceEquals(base.RenderTarget, dxCacheRenderTarget))
				{
					DisposeDxResourceCaches();
					dxCacheRenderTarget = base.RenderTarget;
				}
				base.OnRender(chartControl, chartScale);
				fromIndex = base.ChartBars.FromIndex;
				toIndex = base.ChartBars.ToIndex;
				DrawSwingPoints(chartScale, listSwingsTop, isTop: true);
				DrawSwingPoints(chartScale, listSwingsBottom, isTop: false);
				DrawBosChochLines(chartScale, listBosChochInfoTop);
				DrawBosChochLines(chartScale, listBosChochInfoBottom);
				DrawImbalances(chartScale, listImbalanceActive, isActive: true);
				DrawImbalances(chartScale, listImbalanceInactive, isActive: false);
				DrawOrderBlocks(chartScale, listOrderBlockActive, isActive: true);
				DrawOrderBlocks(chartScale, listOrderBlockInactive, isActive: false);
				DrawMarkers(chartScale);
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	public override void OnRenderTargetChanged()
	{
		// Called on device loss and chart resize — release device resources here;
		// they are lazily recreated on the next OnRender (see the cache gate there).
		try
		{
			DisposeDxResourceCaches();
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
		base.OnRenderTargetChanged();
	}

	private void DrawImbalances(ChartScale chartScale, SortedList<int, ImbalanceInfo> listImbalance, bool isActive)
	{
		if (listImbalance.Count <= 0 || (!isActive && !ImbalanceInactiveEnabled))
		{
			return;
		}
		foreach (KeyValuePair<int, ImbalanceInfo> item in listImbalance)
		{
			ImbalanceInfo value = item.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBox(chartScale, value, isActive, isImbalance: true);
			}
		}
	}

	private void DrawOrderBlocks(ChartScale chartScale, SortedList<int, OrderBlockInfo> listOrderBlock, bool isActive)
	{
		if (listOrderBlock.Count <= 0 || (!isActive && !ImbalanceInactiveEnabled))
		{
			return;
		}
		foreach (KeyValuePair<int, OrderBlockInfo> item in listOrderBlock)
		{
			OrderBlockInfo value = item.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBox(chartScale, value, isActive, isImbalance: false);
			}
		}
	}

	private void DrawOneBox(ChartScale chartScale, ZoneInfo zoneInfo, bool isActive, bool isImbalance)
	{
		if (zoneInfo == null)
		{
			return;
		}
		int barStart = zoneInfo.BarStart;
		int barEnd = zoneInfo.BarEnd;
		if (barStart >= barEnd || zoneInfo.PriceBottom.ApproxCompare(base.ChartPanel.MaxValue) > 0 || zoneInfo.PriceTop.ApproxCompare(base.ChartPanel.MinValue) < 0)
		{
			return;
		}
		bool isTop = zoneInfo.IsTop;
		SharpDX.Direct2D1.GradientStop[] array = null;
		System.Windows.Media.Brush brush = null;
		if (GradientEnabled)
		{
			array = (isActive ? ((!isImbalance) ? (isTop ? orderBlockActiveTopGradientStop : orderBlockActiveBottomGradientStop) : (isTop ? imbalanceActiveTopGradientStop : imbalanceActiveBottomGradientStop)) : ((!isImbalance) ? (isTop ? orderBlockInactiveTopGradientStop : orderBlockInactiveBottomGradientStop) : (isTop ? imbalanceInactiveTopGradientStop : imbalanceInactiveBottomGradientStop)));
			if (array == null)
			{
				return;
			}
		}
		else
		{
			brush = ((!isActive) ? ((!isImbalance) ? (isTop ? orderBlockInactiveTop : orderBlockInactiveBottom) : (isTop ? imbalanceInactiveTop : imbalanceInactiveBottom)) : ((!isImbalance) ? (isTop ? orderBlockActiveTop : orderBlockActiveBottom) : (isTop ? imbalanceActiveTop : imbalanceActiveBottom)));
			if (brush.IsTransparent())
			{
				return;
			}
		}
		int num = ((barStart < fromIndex) ? (base.ChartPanel.X - 10) : base.ChartControl.GetXByBarIndex(base.ChartBars, barStart));
		int num2 = ((barEnd > toIndex) ? (base.ChartPanel.X + base.ChartPanel.W + 10) : base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd));
		int yByValue = chartScale.GetYByValue(zoneInfo.PriceTop);
		int yByValue2 = chartScale.GetYByValue(zoneInfo.PriceBottom);
		int num3 = Math.Abs(num2 - num);
		int num4 = Math.Abs(yByValue - yByValue2);
		RectangleF rect = new RectangleF(num, yByValue, num3, num4);
		if (GradientEnabled)
		{
			// Cached gradient brush — only the start/end anchors change per zone.
			SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush = GetDxGradientBrushCached(array);
			linearGradientBrush.StartPoint = rect.TopLeft;
			linearGradientBrush.EndPoint = rect.BottomLeft;
			base.RenderTarget.FillRectangle(rect, linearGradientBrush);
		}
		else
		{
			base.RenderTarget.FillRectangle(rect, GetDxBrushCached(brush));
		}
		Stroke stroke = (isActive ? ((!isImbalance) ? (isTop ? OrderBlockActiveBorderTop : OrderBlockActiveBorderBottom) : (isTop ? ImbalanceActiveBorderTop : ImbalanceActiveBorderBottom)) : ((!isImbalance) ? (isTop ? OrderBlockInactiveBorderTop : OrderBlockInactiveBorderBottom) : (isTop ? ImbalanceInactiveBorderTop : ImbalanceInactiveBorderBottom)));
		if (!stroke.Brush.IsTransparent())
		{
			StrokeStyle strokeStyle = stroke.StrokeStyle;
			float width = stroke.Width;
			base.RenderTarget.DrawRectangle(rect, GetDxBrushCached(stroke.Brush), width, strokeStyle);
		}
	}

	private void DrawBosChochLines(ChartScale chartScale, SortedList<int, BosChochInfo> listBosChochInfos)
	{
		if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Disabled || listBosChochInfos.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<int, BosChochInfo> listBosChochInfo in listBosChochInfos)
		{
			BosChochInfo value = listBosChochInfo.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBosChochLine(chartScale, value);
			}
		}
	}

	private void DrawOneBosChochLine(ChartScale chartScale, BosChochInfo bosChochInfo)
	{
		if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Smart && !bosChochInfo.IsHasOrderBlock)
		{
			return;
		}
		int barStart = bosChochInfo.BarStart;
		bool isTop = bosChochInfo.IsTop;
		if (bosChochInfo == null)
		{
			return;
		}
		int barEnd = bosChochInfo.BarEnd;
		if (barStart >= barEnd)
		{
			return;
		}
		bool flag = bosChochInfo.BosChochType == BosChochType.Choch;
		double price = bosChochInfo.Price;
		if (isTop)
		{
			if (price.ApproxCompare(chartScale.MaxValue) >= 0 || (flag && !ChochTopLineEnabled) || (!flag && !BosTopLineEnabled))
			{
				return;
			}
		}
		else if (price.ApproxCompare(chartScale.MinValue) <= 0 || (flag && !ChochBottomLineEnabled) || (!flag && !BosBottomLineEnabled))
		{
			return;
		}
		Stroke stroke = ((!isTop) ? (flag ? ChochBottomLineStroke : BosBottomLineStroke) : (flag ? ChochTopLineStroke : BosTopLineStroke));
		System.Windows.Media.Brush brush = stroke.Brush;
		if (brush.IsTransparent())
		{
			return;
		}
		float width = stroke.Width;
		StrokeStyle strokeStyle = stroke.StrokeStyle;
		float num = base.ChartControl.GetXByBarIndex(base.ChartBars, barStart);
		float num2 = base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
		float y = chartScale.GetYByValue(price);
		Vector2 point = new Vector2(num, y);
		Vector2 point2 = new Vector2(num2, y);
		AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
		base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
		base.RenderTarget.DrawLine(point, point2, GetDxBrushCached(brush), width, strokeStyle);
		base.RenderTarget.AntialiasMode = antialiasMode;
		if (isTop)
		{
			if ((flag && !ChochTopTextEnabled) || (!flag && !BosTopTextEnabled))
			{
				return;
			}
		}
		else if ((flag && !ChochBottomTextEnabled) || (!flag && !BosBottomTextEnabled))
		{
			return;
		}
		string text = (flag ? "Choch" : "Bos");
		float x = (num + num2) / 2f;
		float y2 = chartScale.GetYByValue(price) + ((!isTop) ? 1 : (-1)) * BosChochTextOffset;
		if (!isTop)
		{
			if (flag)
			{
			}
		}
		else if (!flag)
		{
		}
		DrawTextDX(text, BosChochTextFont, x, y2, 0, (!isTop) ? 1 : (-1), brush, ScreenDPI, base.RenderTarget);
	}

	private void DrawSwingPoints(ChartScale chartScale, SortedList<int, SwingPoint> listSwingsPoint, bool isTop)
	{
		if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Disabled || !isSwingPointCustomRenderingMethod || listSwingsPoint.Count <= 0)
		{
			return;
		}
		System.Windows.Media.Brush brush = (isTop ? SwingPointTop : SwingPointBottom);
		if (brush.IsTransparent())
		{
			return;
		}
		float barWidth = Math.Max(3f, (float)base.ChartControl.BarWidth - barOutlineWidth);
		for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
		{
			if (listSwingsPoint.ContainsKey(i))
			{
				DrawOneSwingPoint(chartScale, listSwingsPoint[i], barWidth, brush);
			}
		}
	}

	private void DrawOneSwingPoint(ChartScale chartScale, SwingPoint swingPoint, float barWidth, System.Windows.Media.Brush brushSwingPoint)
	{
		try
		{
			if (!isSwingPointSmartDisplayMode || swingPoint.AllowDraw)
			{
				double price = swingPoint.Price;
				if (price.ApproxCompare(base.ChartPanel.MaxValue) <= 0 && price.ApproxCompare(base.ChartPanel.MinValue) >= 0)
				{
					int barEnd = swingPoint.BarEnd;
					float x = base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
					float y = chartScale.GetYByValue(price);
					Vector2 center = new Vector2(x, y);
					float num = Math.Max(4.7f, barWidth);
					SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(center, num + 0.2f, num + 0.2f);
					SharpDX.Direct2D1.Ellipse ellipse2 = new SharpDX.Direct2D1.Ellipse(center, num, num);
					AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
					base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
					base.RenderTarget.DrawEllipse(ellipse, GetDxBrushCached(Brushes.Silver));
					base.RenderTarget.FillEllipse(ellipse2, GetDxBrushCached(brushSwingPoint));
					base.RenderTarget.AntialiasMode = antialiasMode;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void AddMarker(int barIndex, bool isBullish, SignalType signalType)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo(barIndex, isBullish, signalType);
			if (!listMarkers.ContainsKey(barIndex))
			{
				listMarkers.Add(barIndex, value);
			}
			else
			{
				listMarkers[barIndex] = value;
			}
		}
	}

	private void DrawMarkers(ChartScale chartScale)
	{
		try
		{
			if (!MarkerEnabled || listMarkers == null || listMarkers.Count <= 0)
			{
				return;
			}
			for (int i = base.ChartBars.FromIndex; i <= Math.Min(base.CurrentBars[0], base.ChartBars.ToIndex); i++)
			{
				if (listMarkers.ContainsKey(i))
				{
					DrawOneMarker(chartScale, listMarkers[i]);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void DrawOneMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		try
		{
			string input = ((markerInfo.SignalType != SignalType.Breakout) ? (markerInfo.IsBullish ? MarkerStringReturnBullish : MarkerStringReturnBearish) : (markerInfo.IsBullish ? MarkerStringBreakUp : MarkerStringBreakDown));
			string text = input;
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			bool isBullish = markerInfo.IsBullish;
			int barIndex = markerInfo.BarIndex;
			if ((isBullish || base.Highs[0].GetValueAt(barIndex).ApproxCompare(chartScale.MaxValue) < 0) && (!isBullish || base.Lows[0].GetValueAt(barIndex).ApproxCompare(chartScale.MinValue) > 0))
			{
				System.Windows.Media.Brush brush = ((markerInfo.SignalType != SignalType.Breakout) ? (isBullish ? MarkerBrushReturnBullish : MarkerBrushReturnBearish) : (isBullish ? MarkerBrushBreakUp : MarkerBrushBreakDown));
				if (!brush.IsTransparent())
				{
					int num = (isBullish ? 1 : (-1));
					float x = base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
					float y = chartScale.GetYByValue((isBullish ? base.Lows[0] : base.Highs[0]).GetValueAt(barIndex)) + num * MarkerOffset;
					DrawTextDX(text, MarkerFont, x, y, 0, num, brush, ScreenDPI, base.RenderTarget);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void PrintMarker(bool isBullish, SignalType signalType)
	{
		if (isCharting && MarkerEnabled && base.CurrentBars[0] >= base.BarsRequiredToPlot)
		{
			string tag = "gbKingOrderBlock.marker." + GetTagSuffix(isBullish, signalType) + base.CurrentBars[0];
			System.Windows.Media.Brush textBrush = ((signalType != SignalType.Breakout) ? (isBullish ? MarkerBrushReturnBullish : MarkerBrushReturnBearish) : (isBullish ? MarkerBrushBreakUp : MarkerBrushBreakDown));
			double y = (isBullish ? base.Lows[0][0] : base.Highs[0][0]);
			string input = ((signalType != SignalType.Breakout) ? (isBullish ? MarkerStringReturnBullish : MarkerStringReturnBearish) : (isBullish ? MarkerStringBreakUp : MarkerStringBreakDown));
			int num;
			using (var tf = MarkerFont.ToDirectWriteTextFormat())
			using (var tl = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, input, tf, 400, 400))
				num = (int)tl.Metrics.Height;
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, base.IsAutoScale, input, 0, y, yPixelOffset, textBrush, MarkerFont, System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private string GetTagSuffix(bool isBullish, SignalType signalType)
	{
		if (signalType == SignalType.Breakout)
		{
			if (!isBullish)
			{
				return "breakdown.";
			}
			return "breakup.";
		}
		if (!isBullish)
		{
			return "returnbearish.";
		}
		return "returnbullish.";
	}

	public override string FormatPriceMarker(double price)
	{
		return base.Instrument.MasterInstrument.FormatPrice(base.Instrument.MasterInstrument.RoundToTickSize(price));
	}

	// Inactive zones are display-only history: the signal logic never reads them
	// again, but both lists previously grew unbounded over a session while
	// OnRender enumerated them fully every frame. Zones leave the active lists
	// no later than OrderBlockAge bars after their key bar, so entries keyed more
	// than 2×OrderBlockAge bars back have been off the right edge for at least a
	// full OrderBlockAge window — evict them. Scrolling back further than that
	// no longer shows the evicted zones (accepted trade-off; raise OrderBlockAge
	// to keep more history).
	private void PruneInactiveZoneLists()
	{
		int cutoffKey = base.CurrentBar - 2 * OrderBlockAge;
		if (cutoffKey <= 0)
		{
			return;
		}
		while (listOrderBlockInactive.Count > 0 && listOrderBlockInactive.Keys[0] < cutoffKey)
		{
			listOrderBlockInactive.RemoveAt(0);
		}
		while (listImbalanceInactive.Count > 0 && listImbalanceInactive.Keys[0] < cutoffKey)
		{
			listImbalanceInactive.RemoveAt(0);
		}
	}

	private void MoveElementFromListToList<TValue>(int key, TValue element, SortedList<int, TValue> listActive, SortedList<int, TValue> listInactive)
	{
		if (!listInactive.ContainsKey(key))
		{
			listInactive.Add(key, element);
		}
		listActive.Remove(key);
	}

	private void AddElementToList<TValue>(int key, TValue element, SortedList<int, TValue> listActive)
	{
		if (!listActive.ContainsKey(key))
		{
			listActive.Add(key, element);
		}
		else
		{
			listActive[key] = element;
		}
	}

	private void RemoveLastElementInList<TValue>(SortedList<int, TValue> listData)
	{
		int count = listData.Count;
		if (count != 0)
		{
			listData.RemoveAt(count - 1);
		}
	}

	private bool IsReversalBar(int sign)
	{
		if (base.CurrentBar < 2)
		{
			return false;
		}
		if (sign * base.Closes[0][1].ApproxCompare(base.Closes[0][0]) > 0)
		{
			return sign * base.Closes[0][2].ApproxCompare(base.Closes[0][1]) < 0;
		}
		return false;
	}

	private void TriggerAlerts(bool isBullish, SignalType signalType)
	{
		if (base.State == State.Historical || (!PopupEnabled && !SoundEnabled && !EmailEnabled))
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
		string arg2 = $"{base.Instrument.FullName} ({base.BarsPeriod})";
		string arg3;
		string text2;
		if (signalType == SignalType.Breakout)
		{
			arg3 = (isBullish ? "BREAK UP" : "BREAK DOWN");
			text2 = "a";
		}
		else
		{
			arg3 = (isBullish ? "RETURN BULLISH" : "RETURN BEARISH");
			text2 = "a";
		}
		string text3 = "King Order Block" + $": {arg3} alert on {arg2} at {arg}";
		string popupMessage = string.Format("There has been " + text2 + " {0} signal.\n\nAlert chart: {1}.\nAlert time: {2}", arg3, arg2, text);
		string text4 = "\n_______________________\n\n";
		string text5 = popupMessage + text4 + "King Order Block by GreyBeard\nWebsite: https://greybeardconsulting.net/";
		if (PopupEnabled && isCharting)
		{
			base.ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
				{
					alertWindow.Close();
				}
				alertWindow = new Window
				{
					Title = "King Order Block",
					Width = 400,
					Height = 250,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Background = CreateOpacityBrush(PopupBackgroundBrush, PopupBackgroundOpacity),
					Content = new TextBlock
					{
						Text = popupMessage,
						Foreground = PopupTextBrush,
						FontSize = PopupTextSize,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(15),
						VerticalAlignment = VerticalAlignment.Center
					}
				};
				alertWindowClosed = false;
				alertWindow.Closed += (s, args) => alertWindowClosed = true;
				alertWindow.Show();
			});
		}
		if (SoundEnabled)
		{
			string id = "alert @ " + text;
			soundPath = string.Concat(str2: (signalType != SignalType.Breakout) ? (isBullish ? SoundReturnBullish : SoundReturnBearish) : (isBullish ? SoundBreakUp : SoundBreakDown), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(id, Priority.High, text3, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text3, text5);
		}
	}

	private void OnRearmTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent(delegate
		{
			try
			{
				if (isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (alertWindow != null && alertWindowClosed)
						{
							rearmTimer.Stop();
						}
						else if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					});
				}
			}
			catch
			{
			}
		}, e);
	}

	private static System.Windows.Media.Brush CreateOpacityBrush(System.Windows.Media.Brush brush, int opacityPercent)
	{
		if (brush == null || brush.IsTransparent())
			return brush;
		var scb = (System.Windows.Media.SolidColorBrush)brush;
		byte alpha = (byte)(255 * opacityPercent / 100);
		var color = System.Windows.Media.Color.FromArgb(alpha, scb.Color.R, scb.Color.G, scb.Color.B);
		var result = new System.Windows.Media.SolidColorBrush(color);
		result.Freeze();
		return result;
	}

	private SharpDX.Direct2D1.Brush GetDxBrushCached(System.Windows.Media.Brush brush)
	{
		if (!dxBrushCache.TryGetValue(brush, out SharpDX.Direct2D1.Brush dxBrush))
		{
			dxBrush = brush.ToDxBrush(base.RenderTarget);
			dxBrushCache[brush] = dxBrush;
		}
		return dxBrush;
	}

	private SharpDX.Direct2D1.LinearGradientBrush GetDxGradientBrushCached(SharpDX.Direct2D1.GradientStop[] gradientStops)
	{
		if (!dxGradientBrushCache.TryGetValue(gradientStops, out SharpDX.Direct2D1.LinearGradientBrush gradientBrush))
		{
			SharpDX.Direct2D1.GradientStopCollection stopCollection = new SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, gradientStops);
			gradientBrush = new SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, new LinearGradientBrushProperties
			{
				StartPoint = new Vector2(0f, 0f),
				EndPoint = new Vector2(0f, 1f)
			}, stopCollection);
			dxGradientStopCache[gradientStops] = stopCollection;
			dxGradientBrushCache[gradientStops] = gradientBrush;
		}
		return gradientBrush;
	}

	private SharpDX.DirectWrite.TextFormat GetTextFormatCached(SimpleFont font)
	{
		if (!dxTextFormatCache.TryGetValue(font, out SharpDX.DirectWrite.TextFormat textFormat))
		{
			textFormat = font.ToDirectWriteTextFormat();
			textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;
			dxTextFormatCache[font] = textFormat;
		}
		return textFormat;
	}

	private void DisposeDxResourceCaches()
	{
		foreach (SharpDX.Direct2D1.Brush cachedBrush in dxBrushCache.Values)
		{
			try { cachedBrush.Dispose(); } catch { }
		}
		dxBrushCache.Clear();
		foreach (SharpDX.Direct2D1.LinearGradientBrush cachedGradientBrush in dxGradientBrushCache.Values)
		{
			try { cachedGradientBrush.Dispose(); } catch { }
		}
		dxGradientBrushCache.Clear();
		foreach (SharpDX.Direct2D1.GradientStopCollection cachedStops in dxGradientStopCache.Values)
		{
			try { cachedStops.Dispose(); } catch { }
		}
		dxGradientStopCache.Clear();
		dxCacheRenderTarget = null;
	}

	private void DisposeTextFormatCache()
	{
		foreach (SharpDX.DirectWrite.TextFormat cachedFormat in dxTextFormatCache.Values)
		{
			try { cachedFormat.Dispose(); } catch { }
		}
		dxTextFormatCache.Clear();
	}

	private void DrawTextDX(string text, SimpleFont font, float x, float y, int xAlign, int ySign, System.Windows.Media.Brush brush, int screenDPI, SharpDX.Direct2D1.RenderTarget rt)
	{
		if (string.IsNullOrEmpty(text) || brush.IsTransparent())
			return;
		SharpDX.DirectWrite.TextFormat textFormat = GetTextFormatCached(font);
		using (var textLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, textFormat, 400, 400))
		{
			float textHeight = textLayout.Metrics.Height;
			float drawX = x - 200;
			float drawY = (ySign < 0) ? y - textHeight : y;
			rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), textLayout, GetDxBrushCached(brush));
		}
	}

} // class gbKingOrderBlock

} // namespace

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbKingOrderBlock[] cachegbKingOrderBlock;
		public GreyBeard.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}

		public GreyBeard.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input, int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			if (cachegbKingOrderBlock != null)
				for (int idx = 0; idx < cachegbKingOrderBlock.Length; idx++)
					if (cachegbKingOrderBlock[idx] != null && cachegbKingOrderBlock[idx].SwingPointNeighborhood == swingPointNeighborhood && cachegbKingOrderBlock[idx].ImbalanceQualifying == imbalanceQualifying && cachegbKingOrderBlock[idx].OrderBlockFindingBosChochPeriod == orderBlockFindingBosChochPeriod && cachegbKingOrderBlock[idx].OrderBlockAge == orderBlockAge && cachegbKingOrderBlock[idx].OrderBlocksSameDirectionOffset == orderBlocksSameDirectionOffset && cachegbKingOrderBlock[idx].OrderBlocksDifferenceDirectionOffset == orderBlocksDifferenceDirectionOffset && cachegbKingOrderBlock[idx].SignalTradeQuantityPerOrderBlock == signalTradeQuantityPerOrderBlock && cachegbKingOrderBlock[idx].SignalTradeSplitBars == signalTradeSplitBars && cachegbKingOrderBlock[idx].EqualsInput(input))
						return cachegbKingOrderBlock[idx];
			return CacheIndicator<GreyBeard.gbKingOrderBlock>(new GreyBeard.gbKingOrderBlock(){ SwingPointNeighborhood = swingPointNeighborhood, ImbalanceQualifying = imbalanceQualifying, OrderBlockFindingBosChochPeriod = orderBlockFindingBosChochPeriod, OrderBlockAge = orderBlockAge, OrderBlocksSameDirectionOffset = orderBlocksSameDirectionOffset, OrderBlocksDifferenceDirectionOffset = orderBlocksDifferenceDirectionOffset, SignalTradeQuantityPerOrderBlock = signalTradeQuantityPerOrderBlock, SignalTradeSplitBars = signalTradeSplitBars }, input, ref cachegbKingOrderBlock);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}

		public Indicators.GreyBeard.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input , int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}

		public Indicators.GreyBeard.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input , int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}
	}
}

#endregion
