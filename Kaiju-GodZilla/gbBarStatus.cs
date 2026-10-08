#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.DirectWrite;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;
using FlowDirection = System.Windows.FlowDirection;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
public enum gbBarStatus_CountMode
{
	CountUp,
	CountDown
}

public enum gbBarStatus_PositionAlignment
{
	Top,
	Bottom
}

[CategoryOrder("Developer", 0)]
[CategoryOrder("General",   1000010)]
[CategoryOrder("Graphics",  1000020)]
[CategoryOrder("Gradient",  1000030)]
[CategoryOrder("Special",   1000040)]
public class gbBarStatus : Indicator
{
	private class BrushManager
	{
		internal static bool IsDarkBrush(Brush input)
		{
			if (input is SolidColorBrush solidColorBrush)
			{
				if (!BrushExtensions.IsTransparent(solidColorBrush))
				{
					int num = 50;
					if (solidColorBrush.Color.R >= 50 || solidColorBrush.Color.G >= num || solidColorBrush.Color.B >= num)
					{
						return false;
					}
					return true;
				}
				return true;
			}
			return false;
		}

		internal static Brush CreateOpacityBrush(Brush input, int opacity)
		{
			if (input is SolidColorBrush solidColorBrush)
			{
				if (!BrushExtensions.IsTransparent(solidColorBrush))
				{
					byte a = (byte)Math.Min(255, Math.Max(0, Convert.ToInt32((double)opacity * 2.55)));
					SolidColorBrush solidColorBrush2 = new SolidColorBrush(Color.FromArgb(a, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B));
					solidColorBrush2.Freeze();
					return solidColorBrush2;
				}
				return solidColorBrush;
			}
			return input;
		}

		internal static SolidColorBrush CreateAverageBrush(Brush input1, Brush input2)
		{
			SolidColorBrush solidColorBrush = ((!(input1 is SolidColorBrush)) ? Brushes.Black : (input1 as SolidColorBrush));
			SolidColorBrush solidColorBrush2 = ((!(input2 is SolidColorBrush)) ? Brushes.Black : (input2 as SolidColorBrush));
			byte r = Convert.ToByte((solidColorBrush.Color.R + solidColorBrush2.Color.R) / 2);
			byte g = Convert.ToByte((solidColorBrush.Color.G + solidColorBrush2.Color.G) / 2);
			byte b = Convert.ToByte((solidColorBrush.Color.B + solidColorBrush2.Color.B) / 2);
			SolidColorBrush solidColorBrush3 = new SolidColorBrush(Color.FromRgb(r, g, b));
			solidColorBrush3.Freeze();
			return solidColorBrush3;
		}

		internal static Brush CreateGradientBrush(Brush brushStart, Brush brushEnd, double ratio)
		{
			SolidColorBrush solidColorBrush = brushStart as SolidColorBrush;
			SolidColorBrush solidColorBrush2 = brushEnd as SolidColorBrush;
			if (solidColorBrush == null || solidColorBrush2 == null)
			{
				return CreateAverageBrush(brushStart, brushEnd);
			}
			double num = Math.Max(0.0, Math.Min(1.0, ratio));
			byte r = Convert.ToByte((1.0 - num) * (double)(int)solidColorBrush.Color.R + num * (double)(int)solidColorBrush2.Color.R);
			byte g = Convert.ToByte((1.0 - num) * (double)(int)solidColorBrush.Color.G + num * (double)(int)solidColorBrush2.Color.G);
			byte b = Convert.ToByte((1.0 - num) * (double)(int)solidColorBrush.Color.B + num * (double)(int)solidColorBrush2.Color.B);
			SolidColorBrush solidColorBrush3 = new SolidColorBrush(Color.FromRgb(r, g, b));
			solidColorBrush3.Freeze();
			return solidColorBrush3;
		}

		}

	private const string boundOffsetDescription = "This parameter applies to price-based charts like Renko, ninZaRenko, Range.\nOffset = 0 indicates the farthest price limits at which the current bar is still developing (i.e. a new bar still does NOT open).\nA positive offset should be used if you would like to utilize the plots as trailing stops or entries.";

	private bool isPriceBased;

	private bool isSecond;

	private bool isMinute;

	private bool isTick;

	private bool isRenko;

	private bool isRange;

	private bool isVolume;

	private bool isNinZaRenko;

	private bool isKingRenko;

	private double upperBound;

	private double lowerBound;

	private double lastDrawnUpperBound = double.NaN;

	private double lastDrawnLowerBound = double.NaN;

	private int displayState;

	private TimeSpan remainingTime;

	private TimeSpan elapsedTime;

	private TimeSpan fullTime;

	private TimeSpan displayTime;

	private long remainingAmount;

	private long elapsedAmount;

	private long fullAmount;

	private long displayAmount;

	private double elapsedPercentage;

	private double displayPercentage;

	private string prefix = "gbBarStatus";

	private string indicatorName = "Bar Status";

	private const string indicatorNameFull = "Bar Status by GreyBeard";

	private bool isCharting;

	private bool centerMenuAdded;

	private bool dataUpdated;

	private int padding = 1;

	private bool initialized;

	private bool allowPainting;

	private DateTime initializeMoment;

	private ChartScale chartScale;

	private Brush eBrush;

	// Device-brush cache for the reference-stable WPF brushes (bound strokes,
	// label text, MasterBrush). eBrush is excluded: in gradient mode it is a NEW
	// WPF brush every frame, so it gets one per-frame DX conversion (eDxBrush)
	// instead of the previous four. Cache invalidates on RenderTarget change and
	// disposes in State.Terminated.
	private readonly Dictionary<Brush, SharpDX.Direct2D1.Brush> dxBrushCache = new Dictionary<Brush, SharpDX.Direct2D1.Brush>();

	private SharpDX.Direct2D1.RenderTarget dxCacheRenderTarget;

	private SharpDX.Direct2D1.Brush eDxBrush;

	private int symbolPadding;

	private RectangleF progressDisplayRect = default(RectangleF);

	private RectangleF progressFullRect = default(RectangleF);

	private RectangleF symbolRect = default(RectangleF);

	private RectangleF refreshRect = default(RectangleF);

	private Point symbolCenter;

	private float eProgressBarWidth;

	private float eProgressBarBorder;

	private float sampleStringHeight;

	private float greatestHeight;

	private DispatcherTimer timer;

	[Display(Name = "Author",  Order = 0,  GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Website", Order = 5,  GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Version", Order = 10, GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Count Mode", Order = 0, GroupName = "General")]
	public gbBarStatus_CountMode CountMode { get; set; }

	[Display(Name = "Interactive Click: Enabled", Order = 5, GroupName = "General")]
	public bool InteractiveClickEnabled { get; set; }
	[Display(Name = "Screen DPI", Order = 10, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Master Color", Order = 0, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush MasterBrush { get; set; }

	[Browsable(false)]
	public string MasterBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(MasterBrush);
		}
		set
		{
			MasterBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Position: Alignment", Order = 10, GroupName = "Graphics")]
	public gbBarStatus_PositionAlignment PositionAlignment { get; set; }
	[Display(Name = "Position: Margin X", Order = 11, GroupName = "Graphics")]
	public int PositionMarginX { get; set; }

	[Display(Name = "Position: Margin Y", Order = 12, GroupName = "Graphics")]
	public int PositionMarginY { get; set; }

	[Display(Name = "Info Text: Enabled", Order = 20, GroupName = "Graphics")]
	public bool InfoTextEnabled { get; set; }

	[Display(Name = "Info Text: Font", Order = 21, GroupName = "Graphics")]
	public SimpleFont InfoTextFont { get; set; }

	[Display(Name = "Info Text: Padding", Order = 22, GroupName = "Graphics")]
	public int InfoTextPadding { get; set; }

	[Display(Name = "Progress Bar: Enabled", Order = 30, GroupName = "Graphics")]
	public bool ProgressBarEnabled { get; set; }

	[Display(Name = "Progress Bar: Width", Order = 31, GroupName = "Graphics")]
	public int ProgressBarWidth { get; set; }
	[Display(Name = "Progress Bar: Height", Order = 32, GroupName = "Graphics")]
	public int ProgressBarHeight { get; set; }

	[Display(Name = "Progress Bar: Border", Order = 33, GroupName = "Graphics")]
	public int ProgressBarBorder { get; set; }

	[Display(Name = "Count-Mode Symbol: Size", Order = 35, GroupName = "Graphics")]
	public int SymbolSize { get; set; }

	[Display(Name = "Bound: Upper", Order = 40, GroupName = "Graphics")]
	public Stroke BoundUpper { get; set; }

	[Display(Name = "Bound: Lower", Order = 41, GroupName = "Graphics")]
	public Stroke BoundLower { get; set; }
	[Display(Name = "Bound: Width", Order = 42, GroupName = "Graphics")]
	public int BoundWidth { get; set; }

	[Display(Name = "Label: Enabled", Order = 43, GroupName = "Graphics")]
	public bool LabelEnabled { get; set; }

	[Display(Name = "Label: Text Color", Order = 44, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush LabelTextBrush { get; set; }

	[Browsable(false)]
	public string LabelTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(LabelTextBrush);
		}
		set
		{
			LabelTextBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Label: Text Font", Order = 45, GroupName = "Graphics")]
	public SimpleFont LabelTextFont { get; set; }

	[Display(Name = "Enabled", Order = 0, GroupName = "Gradient")]
	public bool GradientEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Color: 100% Completed", Order = 10, GroupName = "Gradient")]
	public Brush GradientXHigh { get; set; }

	[Browsable(false)]
	public string GradientXHighSerialize
	{
		get
		{
			return Serialize.BrushToString(GradientXHigh);
		}
		set
		{
			GradientXHigh = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Color: 75% Completed", Order = 11, GroupName = "Gradient")]
	[XmlIgnore]
	public Brush GradientHigh { get; set; }

	[Browsable(false)]
	public string GradientHighSerialize
	{
		get
		{
			return Serialize.BrushToString(GradientHigh);
		}
		set
		{
			GradientHigh = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color: 50% Completed", Order = 12, GroupName = "Gradient")]
	public Brush GradientMiddle { get; set; }

	[Browsable(false)]
	public string GradientMiddleSerialize
	{
		get
		{
			return Serialize.BrushToString(GradientMiddle);
		}
		set
		{
			GradientMiddle = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Color: 25% Completed", Order = 13, GroupName = "Gradient")]
	[XmlIgnore]
	public Brush GradientLow { get; set; }

	[Browsable(false)]
	public string GradientLowSerialize
	{
		get
		{
			return Serialize.BrushToString(GradientLow);
		}
		set
		{
			GradientLow = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Color: 0% Completed", Order = 14, GroupName = "Gradient")]
	[XmlIgnore]
	public Brush GradientXLow { get; set; }

	[Browsable(false)]
	public string GradientXLowSerialize
	{
		get
		{
			return Serialize.BrushToString(GradientXLow);
		}
		set
		{
			GradientXLow = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Bound Offset (Ticks)", Description = "This parameter applies to price-based charts like Renko, ninZaRenko, Range.\nOffset = 0 indicates the farthest price limits at which the current bar is still developing (i.e. a new bar still does NOT open).\nA positive offset should be used if you would like to utilize the plots as trailing stops or entries.", Order = 10, GroupName = "Parameters")]
	public int BoundOffset { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> UpperBound => Values[0];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> LowerBound => Values[1];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return indicatorName + GetUserNote();
			}
			return base.DisplayName;
		}
	}

	private DateTime Now
	{
		get
		{
			DateTime result = ((Connection.PlaybackConnection == null) ? Globals.Now : Connection.PlaybackConnection.Now);
			if (result.Millisecond > 0)
			{
				DateTime minDate = Globals.MinDate;
				result = minDate.AddSeconds((long)Math.Floor(result.Subtract(Globals.MinDate).TotalSeconds));
			}
			return result;
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
			if (text.Contains(text2) && Instrument != null)
			{
				text = text.Replace(text2, Instrument.FullName);
			}
			if (text.Contains(text3) && BarsPeriod != null)
			{
				text = text.Replace(text3, ((object)BarsPeriod).ToString());
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
				Description = "Displays bar count and session statistics on chart.";
				Name = prefix;
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				ShowTransparentPlotsInDataBox = true;
				AddPlot(Brushes.Transparent, "Upper Bound");
				AddPlot(Brushes.Transparent, "Lower Bound");
				CountMode = gbBarStatus_CountMode.CountUp;
				InteractiveClickEnabled = true;
				ScreenDPI = 99;
				MasterBrush = Brushes.DodgerBlue;
				PositionAlignment = gbBarStatus_PositionAlignment.Bottom;
				PositionMarginX = 20;
				PositionMarginY = 20;
				InfoTextEnabled = true;
				InfoTextFont = new SimpleFont("Arial", 14);
				InfoTextPadding = 15;
				ProgressBarEnabled = true;
				ProgressBarWidth = 120;
				ProgressBarHeight = 8;
				ProgressBarBorder = 2;
				SymbolSize = 6;
				BoundUpper = new Stroke(Brushes.Green, DashStyleHelper.DashDotDot, 2f);
				BoundLower = new Stroke(Brushes.Crimson, DashStyleHelper.DashDotDot, 2f);
				BoundWidth = 200;
				LabelEnabled = true;
				LabelTextFont = new SimpleFont("Arial", 13);
				LabelTextBrush = Brushes.Yellow;
				GradientEnabled = true;
				GradientXHigh = Brushes.Crimson;
				GradientHigh = Brushes.OrangeRed;
				GradientMiddle = Brushes.Orange;
				GradientLow = Brushes.Green;
				GradientXLow = Brushes.LimeGreen;
				IndicatorZOrder = 0;
				UserNote = "instrument (period)";
				BoundOffset = 0;
				break;

			case State.Configure:
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				timer = new DispatcherTimer();
				timer.Interval = TimeSpan.FromMilliseconds(500.0);
				timer.Tick += OnTick;
				timer.Start();
				eProgressBarWidth = (ProgressBarEnabled ? ProgressBarWidth : 0);
				eProgressBarBorder = (ProgressBarEnabled ? ProgressBarBorder : 0);
				progressFullRect.Width = eProgressBarWidth;
				float height = ProgressBarHeight;
				progressFullRect.Height = height;
				progressDisplayRect.Height = height;
				float width = 2 * SymbolSize + 2 * ProgressBarBorder;
				symbolRect.Height = width;
				symbolRect.Width = width;
				sampleStringHeight = ComputeTextSize("SAMPLE", InfoTextFont).Height;
				greatestHeight = Math.Max(2 * SymbolSize, Math.Max(0.85f * sampleStringHeight, ProgressBarHeight + ProgressBarBorder));
				refreshRect.Height = (float)(2 * padding) + greatestHeight;
				if (!InfoTextEnabled || !ProgressBarEnabled)
				{
					if (!ProgressBarEnabled)
					{
						if (InfoTextEnabled)
						{
							displayState = 3;
						}
					}
					else
					{
						displayState = 2;
					}
				}
				else
				{
					displayState = 1;
				}
				initializeMoment = DateTime.Now + TimeSpan.FromMilliseconds(500.0);
				break;

			case State.DataLoaded:
				isCharting = ChartControl != null;
				if (IndicatorZOrder != 0)
				{
					SetZOrder(IndicatorZOrder);
				}
				if (ScreenDPI < 100)
				{
					ScreenDPI = GetDPI();
				}
				if (!isCharting)
				{
					return;
				}
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					((UIElement)(object)ChartPanel).MouseLeftButtonUp += OnMouseClick;
				});
				break;

			case State.Terminated:
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						((UIElement)(object)ChartPanel).MouseLeftButtonUp -= OnMouseClick;
					});
				}
				if (timer != null)
				{
					timer.Stop();
					timer.Tick -= OnTick;
					timer = null;
				}
				DisposeDxBrushCache();
				break;
		}
	}

	protected override void OnBarUpdate()
	{
		BarsPeriodType barsPeriodType = BarsPeriod.BarsPeriodType;
		int value = BarsPeriod.Value;
		if (CurrentBar != 0)
		{
			if (!isSecond && !isMinute && !isTick && !isVolume && !isRenko && !isRange && !isNinZaRenko && !isKingRenko)
			{
				return;
			}
			if (!isPriceBased)
			{
				if (!isTick)
				{
					if (isVolume)
					{
						elapsedAmount = (long)Volume[0];
						remainingAmount = Math.Max(0L, fullAmount - elapsedAmount);
						dataUpdated = true;
					}
				}
				else
				{
					elapsedAmount = Bars.TickCount;
					remainingAmount = Math.Max(0L, fullAmount - elapsedAmount);
					dataUpdated = true;
				}
			}
			else if (!isRenko)
			{
				if (!isRange)
				{
					if (!isNinZaRenko)
					{
						if (isKingRenko)
						{
							int value2 = BarsPeriod.Value;
							int value3 = BarsPeriod.Value2;
							int num = value2 + (value2 - value3);
							if (Bars.IsFirstBarOfSession || CurrentBar == 0)
							{
								upperBound = Open[0] + (double)(value3 + BoundOffset) * TickSize;
								lowerBound = Open[0] - (double)(value3 + BoundOffset) * TickSize;
							}
							else if (!((CurrentBar != 1) ? (MathExtentions.ApproxCompare(Close[1], Close[2]) > 0) : (MathExtentions.ApproxCompare(Close[0], Close[1]) > 0)))
							{
								upperBound = Close[1] + (double)(num + BoundOffset) * TickSize;
								lowerBound = Close[1] - (double)(value3 + BoundOffset) * TickSize;
							}
							else
							{
								upperBound = Close[1] + (double)(value3 + BoundOffset) * TickSize;
								lowerBound = Close[1] - (double)(num + BoundOffset) * TickSize;
							}
							UpperBound[0] = upperBound;
							LowerBound[0] = lowerBound;
						}
					}
					else
					{
						if (Bars.IsFirstBarOfSession || CurrentBar == 0)
						{
							upperBound = Open[0] + (double)(BarsPeriod.Value2 + BoundOffset) * TickSize;
							lowerBound = Open[0] - (double)(BarsPeriod.Value2 + BoundOffset) * TickSize;
						}
						else
						{
							upperBound = Open[0] + (double)(value + BoundOffset) * TickSize;
							lowerBound = Open[0] - (double)(value + BoundOffset) * TickSize;
						}
						UpperBound[0] = upperBound;
						LowerBound[0] = lowerBound;
					}
				}
				else
				{
					upperBound = Low[0] + (double)(value + BoundOffset) * TickSize;
					lowerBound = High[0] - (double)(value + BoundOffset) * TickSize;
					UpperBound[0] = upperBound;
					LowerBound[0] = lowerBound;
				}
			}
			else
			{
				if (!Bars.IsFirstBarOfSession)
				{
					upperBound = High[1] + (double)(value + BoundOffset - 1) * TickSize;
					lowerBound = Low[1] - (double)(value + BoundOffset - 1) * TickSize;
				}
				else
				{
					upperBound = Open[0] + (double)(value + BoundOffset - 1) * TickSize;
					lowerBound = Open[0] - (double)(value + BoundOffset - 1) * TickSize;
				}
				UpperBound[0] = upperBound;
				LowerBound[0] = lowerBound;
			}
			if (isCharting && CurrentBar >= 1)
			{
				// The bounds are fixed within a bar (derived from prior-bar prices),
				// but this indicator calculates OnEachTick — re-anchoring the
				// transparent auto-scale rays on every tick was pure churn. Redraw
				// only when a bound actually moves; the horizontal rays extend
				// infinitely right, so an older anchor bar renders identically.
				if (BrushExtensions.IsTransparent(Plots[0].Brush) && upperBound != lastDrawnUpperBound)
				{
					lastDrawnUpperBound = upperBound;
					string tag = prefix + ".bound.upper";
					((DrawingTool)Draw.Ray(this, tag, IsAutoScale, 1, upperBound, 0, upperBound, Brushes.Transparent, DashStyleHelper.Solid, 1)).IgnoresUserInput = true;
				}
				if (BrushExtensions.IsTransparent(Plots[1].Brush) && lowerBound != lastDrawnLowerBound)
				{
					lastDrawnLowerBound = lowerBound;
					string tag2 = prefix + ".bound.lower";
					((DrawingTool)Draw.Ray(this, tag2, IsAutoScale, 1, lowerBound, 0, lowerBound, Brushes.Transparent, DashStyleHelper.Solid, 1)).IgnoresUserInput = true;
				}
			}
			return;
		}
		int baseBarsPeriodValue = BarsPeriod.BaseBarsPeriodValue;
		BarsPeriodType baseBarsPeriodType = BarsPeriod.BaseBarsPeriodType;
		bool flag2;
		bool flag = (flag2 = barsPeriodType == BarsPeriodType.HeikenAshi) && baseBarsPeriodType == BarsPeriodType.Second;
		bool flag3 = flag2 && baseBarsPeriodType == BarsPeriodType.Minute;
		bool flag4 = flag2 && baseBarsPeriodType == BarsPeriodType.Tick;
		bool flag5 = flag2 && baseBarsPeriodType == BarsPeriodType.Volume;
		if (barsPeriodType == BarsPeriodType.Second || flag)
		{
			isSecond = true;
			int num2 = ((!flag2) ? value : baseBarsPeriodValue);
			fullAmount = num2 * 1000;
			fullTime = TimeSpan.FromSeconds(num2);
		}
		else if (barsPeriodType == BarsPeriodType.Minute || flag3)
		{
			isMinute = true;
			int num3 = ((!flag2) ? value : baseBarsPeriodValue);
			fullAmount = num3 * 60 * 1000;
			fullTime = TimeSpan.FromMinutes(num3);
		}
		else if (barsPeriodType == BarsPeriodType.Tick || flag4)
		{
			isTick = true;
			fullAmount = ((!flag2) ? value : baseBarsPeriodValue);
		}
		else if (barsPeriodType == BarsPeriodType.Volume || flag5)
		{
			isVolume = true;
			fullAmount = ((!flag2) ? value : baseBarsPeriodValue);
		}
		else if (barsPeriodType != BarsPeriodType.Renko)
		{
			if (barsPeriodType != BarsPeriodType.Range)
			{
				if ((int)barsPeriodType != 12345)
				{
					if ((int)barsPeriodType == 678910)
					{
						isPriceBased = true;
						isKingRenko = true;
					}
				}
				else
				{
					isPriceBased = true;
					isNinZaRenko = true;
				}
			}
			else
			{
				isPriceBased = true;
				isRange = true;
				fullAmount = value + 1;
			}
		}
		else
		{
			isPriceBased = true;
			isRenko = true;
			fullAmount = value;
		}
		initialized = true;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (ChartControl == null)
		{
			return;
		}
		base.OnRender(chartControl, chartScale);
		if (IsInHitTest)
		{
			return;
		}
		this.chartScale = chartScale;
		if ((!isSecond && !isMinute && !isTick && !isVolume && !isRenko && !isRange && !isNinZaRenko && !isKingRenko) || (!isPriceBased && !InfoTextEnabled && !ProgressBarEnabled) || !initialized)
		{
			return;
		}
		// Device brushes are tied to the RenderTarget — rebuild the cache lazily
		// when NT8 hands us a new target (resize / device loss).
		if (!object.ReferenceEquals(RenderTarget, dxCacheRenderTarget))
		{
			DisposeDxBrushCache();
			dxCacheRenderTarget = RenderTarget;
		}
		if (!isPriceBased)
		{
			if (allowPainting)
			{
				if (fullAmount != 0L)
				{
					elapsedPercentage = Math.Max(0.0, Math.Min(100.0, 100.0 * (double)elapsedAmount / (1.0 * (double)fullAmount)));
				}
				else
				{
					elapsedPercentage = 0.0;
				}
				symbolPadding = SymbolSize;
				progressFullRect.X = (float)(ChartPanel.X + ChartPanel.W) - (eProgressBarWidth + eProgressBarBorder + (float)symbolPadding + (float)(2 * SymbolSize) + (float)PositionMarginX);
				if (PositionAlignment != gbBarStatus_PositionAlignment.Bottom)
				{
					progressFullRect.Y = ChartPanel.Y + PositionMarginY;
				}
				else
				{
					progressFullRect.Y = ChartPanel.Y + ChartPanel.H - (ProgressBarHeight + ProgressBarBorder + PositionMarginY);
				}
				progressDisplayRect.Location = progressFullRect.Location;
				symbolCenter.X = (int)(progressFullRect.Right + (float)ProgressBarBorder + (float)symbolPadding + (float)SymbolSize);
				symbolCenter.Y = (int)(progressFullRect.Y + progressFullRect.Height / 2f);
				symbolRect.X = (float)symbolCenter.X - SymbolSize - ProgressBarBorder;
				symbolRect.Y = (float)symbolCenter.Y - SymbolSize - ProgressBarBorder;
				eBrush = ((!GradientEnabled) ? MasterBrush : CreateGradientBrushByInput(elapsedPercentage));
				// One DX conversion per frame shared by the progress bar, info text,
				// and symbol — previously each draw call converted eBrush separately.
				// Not dictionary-cached because gradient mode creates a new WPF brush
				// every frame.
				using (eDxBrush = DxExtensions.ToDxBrush(eBrush, RenderTarget))
				{
					DrawProgressBar();
					DrawInfoText();
					DrawSymbol();
				}
				eDxBrush = null;
				if (!InfoTextEnabled)
				{
					refreshRect.X = progressFullRect.X - (float)padding;
				}
				refreshRect.Y = (float)symbolCenter.Y - greatestHeight / 2f - (float)padding;
				refreshRect.Width = symbolRect.Right + (float)padding - refreshRect.X;
			}
		}
		else
		{
			DrawBounds();
		}
	}

	private void DrawBounds()
	{
		if (CurrentBar < 2)
			return;
		int xByBarIndex = ChartControl.GetXByBarIndex(ChartBars, CurrentBar);
		Vector2 val = new Vector2((float)xByBarIndex, (float)chartScale.GetYByValue(upperBound));
		Vector2 val2 = new Vector2((float)(xByBarIndex + BoundWidth), val.Y);
		Vector2 val3 = new Vector2((float)xByBarIndex, (float)chartScale.GetYByValue(lowerBound));
		Vector2 val4 = new Vector2((float)(xByBarIndex + BoundWidth), val3.Y);
		RenderTarget.DrawLine(val, val2, GetDxBrushCached(BoundUpper.Brush), BoundUpper.Width, BoundUpper.StrokeStyle);
		if (LabelEnabled)
		{
			PaintPriceMarker((int)val2.X, (int)val2.Y, isLeft: true, FormatPriceMarker(upperBound), LabelTextFont, GetDxBrushCached(LabelTextBrush), GetDxBrushCached(BoundUpper.Brush));
		}
		RenderTarget.DrawLine(val3, val4, GetDxBrushCached(BoundLower.Brush), BoundLower.Width, BoundLower.StrokeStyle);
		if (LabelEnabled)
		{
			PaintPriceMarker((int)val4.X, (int)val4.Y, isLeft: true, FormatPriceMarker(lowerBound), LabelTextFont, GetDxBrushCached(LabelTextBrush), GetDxBrushCached(BoundLower.Brush));
		}
	}

	private void DrawProgressBar()
	{
		if (ProgressBarEnabled)
		{
			displayPercentage = ((CountMode != gbBarStatus_CountMode.CountUp) ? (100.0 - elapsedPercentage) : elapsedPercentage);
			progressDisplayRect.Width = Math.Max(0f, Math.Min(eProgressBarWidth, Convert.ToInt32(displayPercentage * (double)eProgressBarWidth / 100.0)));
			RenderTarget.FillRectangle(progressDisplayRect, eDxBrush);
			RenderTarget.DrawRectangle(progressFullRect, eDxBrush, (float)ProgressBarBorder);
		}
	}

	private void DrawSymbol()
	{
		Vector2 val = new Vector2();
		val.X = (float)symbolCenter.X - SymbolSize;
		val.Y = (float)symbolCenter.Y;
		Vector2 val2 = new Vector2();
		val2.X = (float)symbolCenter.X + SymbolSize;
		val2.Y = (float)symbolCenter.Y;
		RenderTarget.DrawLine(val, val2, eDxBrush, (float)ProgressBarBorder);
		if (CountMode == gbBarStatus_CountMode.CountUp)
		{
			val.X = (float)symbolCenter.X;
			val.Y = (float)symbolCenter.Y - SymbolSize;
			val2.X = (float)symbolCenter.X;
			val2.Y = (float)symbolCenter.Y + SymbolSize;
			RenderTarget.DrawLine(val, val2, eDxBrush, (float)ProgressBarBorder);
		}
	}

	private void DrawInfoText()
	{
		if (InfoTextEnabled)
		{
			if (!isSecond && !isMinute)
			{
				displayAmount = ((CountMode != gbBarStatus_CountMode.CountDown) ? elapsedAmount : remainingAmount);
			}
			string text = ((!isSecond && !isMinute) ? displayAmount.ToString() : FormatDisplayTime());
			int num = ((!ProgressBarEnabled) ? (InfoTextPadding - 2 * ProgressBarBorder - symbolPadding) : InfoTextPadding);
			refreshRect.X = progressFullRect.X - (float)num - ComputeTextSize(text, InfoTextFont).Width - (float)padding;
			DrawText(text, InfoTextFont, progressFullRect.X - (float)num, progressFullRect.Y + progressFullRect.Height / 2f, -1, 0, eDxBrush);
		}
	}

	private string FormatDisplayTime()
	{
		string text = string.Empty;
		bool flag = false;
		string text2 = " ";
		if (displayTime.Hours != 0)
		{
			if (flag)
			{
				text += text2;
			}
			text = text + displayTime.Hours + "h";
			flag = true;
		}
		if (displayTime.Minutes != 0)
		{
			if (flag)
			{
				text += text2;
			}
			text = text + displayTime.Minutes + "m";
			flag = true;
		}
		if (displayTime.Seconds != 0)
		{
			if (flag)
			{
				text += text2;
			}
			text = text + displayTime.Seconds + "s";
			flag = true;
		}
		if (!flag)
		{
			text += "0s";
		}
		return text;
	}

	private void OnMouseClick(object sender, MouseEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (InteractiveClickEnabled && !isPriceBased && (isSecond || isMinute || isTick || isVolume) && (InfoTextEnabled || ProgressBarEnabled))
			{
				Point position = e.GetPosition((IInputElement)ChartControl);
				Vector2 val = new Vector2((float)position.X * (float)ScreenDPI / 100f, (float)position.Y * (float)ScreenDPI / 100f);
				if (refreshRect.Contains(val))
				{
					if (SymbolSize == 0 || !symbolRect.Contains(val))
					{
						if (displayState == 0)
						{
							return;
						}
						if (displayState != 1)
						{
							if (displayState != 2)
							{
								displayState = 1;
								InfoTextEnabled = true;
								ProgressBarEnabled = true;
							}
							else
							{
								displayState = 3;
								InfoTextEnabled = true;
								ProgressBarEnabled = false;
							}
						}
						else
						{
							displayState = 2;
							InfoTextEnabled = false;
							ProgressBarEnabled = true;
						}
						eProgressBarWidth = (ProgressBarEnabled ? ProgressBarWidth : 0);
						eProgressBarBorder = (ProgressBarEnabled ? ProgressBarBorder : 0);
						progressFullRect.Width = eProgressBarWidth;
					}
					else if (CountMode != gbBarStatus_CountMode.CountDown)
					{
						CountMode = gbBarStatus_CountMode.CountDown;
					}
					else
					{
						CountMode = gbBarStatus_CountMode.CountUp;
					}
					ChartControl.InvalidateVisual();
				}
			}
		}, (object)e);
	}

	private void OnTick(object sender, EventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (isPriceBased)
					{
						timer.Stop();
					}
					if (isSecond || isMinute)
					{
						TimeSpan timeSpan = TimeSpan.FromMilliseconds(500.0);
						remainingTime = Bars.GetTime(CurrentBar) - Now + timeSpan;
						if (remainingTime < TimeSpan.Zero)
						{
							remainingTime = TimeSpan.Zero;
						}
						if (remainingTime > fullTime)
						{
							remainingTime = fullTime;
						}
						elapsedTime = fullTime - remainingTime;
						displayTime = ((CountMode != gbBarStatus_CountMode.CountDown) ? elapsedTime : remainingTime);
						elapsedAmount = fullAmount - (long)remainingTime.TotalMilliseconds;
						if (elapsedAmount < 0L)
						{
							elapsedAmount = 0L;
						}
					}
					if (!allowPainting && DateTime.Now >= initializeMoment)
					{
						allowPainting = true;
						ChartControl.InvalidateVisual();
					}
					if (isSecond || isMinute)
					{
						ChartControl.InvalidateVisual();
					}
					else if (dataUpdated)
					{
						ChartControl.InvalidateVisual();
						dataUpdated = false;
					}
				});
			}
		}, (object)e);
	}

	private Brush CreateGradientBrushByInput(double input)
	{
		Brush transparent = Brushes.Transparent;
		Brush transparent2 = Brushes.Transparent;
		double ratio = 0.0;
		double num = 0.0;
		double num2 = 100.0;
		double num3 = 75.0;
		double num4 = 50.0;
		double num5 = 25.0;
		double num6 = 0.0;
		num = input;
		if (num < num3)
		{
			if (num < num4)
			{
				if (num < num5)
				{
					transparent = GradientLow;
					transparent2 = GradientXLow;
					if (num5 != num6)
					{
						ratio = (num5 - num) / (num5 - num6);
					}
				}
				else
				{
					transparent = GradientMiddle;
					transparent2 = GradientLow;
					if (num4 != num5)
					{
						ratio = (num4 - num) / (num4 - num5);
					}
				}
			}
			else
			{
				transparent = GradientMiddle;
				transparent2 = GradientHigh;
				if (num4 != num3)
				{
					ratio = (num - num4) / (num3 - num4);
				}
			}
		}
		else
		{
			transparent = GradientHigh;
			transparent2 = GradientXHigh;
			if (num3 != num2)
			{
				ratio = (num - num3) / (num2 - num3);
			}
		}
		return BrushManager.CreateGradientBrush(transparent, transparent2, ratio);
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void PaintPriceMarker(int x, int y, bool isLeft, string text, SimpleFont font, SharpDX.Direct2D1.Brush foreground, SharpDX.Direct2D1.Brush background)
	{
		float num = (float)font.Size / 1.5f;
		float num2 = (float)font.Size / 4f;
		float num3 = (float)font.Size / 2f;
		Size2F val = ComputeTextSize(text, font);
		float num4 = val.Width + 2f * num3;
		RectangleF val2;
		RectangleF val3;
		if (!isLeft)
		{
			val2 = new RectangleF((float)x, (float)y - val.Height / 2f, num4, val.Height);
			val3 = new RectangleF((float)x, (float)y - val.Height / 2f - num2, num4, val.Height + 2f * num2);
		}
		else
		{
			val2 = new RectangleF((float)x - num4, (float)y - val.Height / 2f, num4, val.Height);
			val3 = new RectangleF((float)x - num4, (float)y - val.Height / 2f - num2, num4, val.Height + 2f * num2);
		}
		Vector2 val4;
		Vector2 val5;
		Vector2 val6;
		Vector2 val7;
		Vector2 val8;
		if (!isLeft)
		{
			val4 = new Vector2(val3.Left, val3.Top);
			val5 = new Vector2(val3.Right, val3.Top);
			val6 = new Vector2(val3.Right + num, (val3.Top + val3.Bottom) / 2f);
			val7 = new Vector2(val3.Right, val3.Bottom);
			val8 = new Vector2(val3.Left, val3.Bottom);
		}
		else
		{
			val4 = new Vector2(val3.Left, val3.Top);
			val5 = new Vector2(val3.Right, val3.Top);
			val6 = new Vector2(val3.Right, val3.Bottom);
			val7 = new Vector2(val3.Left, val3.Bottom);
			val8 = new Vector2(val3.Left - num, (val3.Top + val3.Bottom) / 2f);
		}
		SharpDX.Direct2D1.PathGeometry val9 = new SharpDX.Direct2D1.PathGeometry(RenderTarget.Factory);
		SharpDX.Direct2D1.GeometrySink val10 = val9.Open();
		val10.BeginFigure(val4, SharpDX.Direct2D1.FigureBegin.Filled);
		val10.AddLines(new Vector2[] { val5, val6, val7, val8, val4 });
		val10.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
		val10.Close();
		SharpDX.Direct2D1.AntialiasMode antialiasMode = RenderTarget.AntialiasMode;
		RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
		RenderTarget.FillGeometry(val9, background);
		RenderTarget.AntialiasMode = antialiasMode;
		TextFormat val11 = font.ToDirectWriteTextFormat();
		val11.TextAlignment = TextAlignment.Center;
		RenderTarget.DrawText(text, val11, val2, foreground);
		val10.Dispose();
		val9.Dispose();
		val11.Dispose();
	}

	private void DrawText(string text, SimpleFont font, float x, float y, int horizontalAlignment, int verticalAlignment, SharpDX.Direct2D1.Brush dxBrush)
	{
		TextFormat val = font.ToDirectWriteTextFormat();
		Size2F val2 = ComputeTextSize(text, font);
		float num = val2.Width + 10f;
		float height = val2.Height;
		float num2;
		if (horizontalAlignment <= 0)
		{
			if (horizontalAlignment >= 0)
			{
				num2 = x - num / 2f;
				val.TextAlignment = TextAlignment.Center;
			}
			else
			{
				num2 = x - num;
				val.TextAlignment = TextAlignment.Trailing;
			}
		}
		else
		{
			num2 = x;
			val.TextAlignment = TextAlignment.Leading;
		}
		float num3 = ((verticalAlignment > 0) ? y : ((verticalAlignment < 0) ? (y - height) : (y - height / 2f)));
			RectangleF val3 = new RectangleF(num2, num3, num, height);
		RenderTarget.DrawText(text, val, val3, dxBrush);
		val.Dispose();
	}

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

	private Size2F ComputeTextSize(string text, SimpleFont font)
	{
		FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font.Typeface, font.Size, Brushes.Black);
		return new Size2F((float)formattedText.Width * (float)ScreenDPI / 100f, (float)formattedText.Height * (float)ScreenDPI / 100f);
	}

	private int GetDPI()
	{
		PropertyInfo property = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.Static | BindingFlags.NonPublic);
		if (property != null)
		{
			return Math.Max(100, Convert.ToInt32(property.GetValue(null, null)) * 100 / 96);
		}
		return 100;
	}


}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbBarStatus[] cachegbBarStatus;
		public GreyBeard.gbBarStatus gbBarStatus(int boundOffset)
		{
			return gbBarStatus(Input, boundOffset);
		}

		public GreyBeard.gbBarStatus gbBarStatus(ISeries<double> input, int boundOffset)
		{
			if (cachegbBarStatus != null)
				for (int idx = 0; idx < cachegbBarStatus.Length; idx++)
					if (cachegbBarStatus[idx] != null && cachegbBarStatus[idx].BoundOffset == boundOffset && cachegbBarStatus[idx].EqualsInput(input))
						return cachegbBarStatus[idx];
			return CacheIndicator<GreyBeard.gbBarStatus>(new GreyBeard.gbBarStatus(){ BoundOffset = boundOffset }, input, ref cachegbBarStatus);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbBarStatus gbBarStatus(int boundOffset)
		{
			return indicator.gbBarStatus(Input, boundOffset);
		}

		public Indicators.GreyBeard.gbBarStatus gbBarStatus(ISeries<double> input , int boundOffset)
		{
			return indicator.gbBarStatus(input, boundOffset);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbBarStatus gbBarStatus(int boundOffset)
		{
			return indicator.gbBarStatus(Input, boundOffset);
		}

		public Indicators.GreyBeard.gbBarStatus gbBarStatus(ISeries<double> input , int boundOffset)
		{
			return indicator.gbBarStatus(input, boundOffset);
		}
	}
}

#endregion
