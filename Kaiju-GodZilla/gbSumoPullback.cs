#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

public enum gbSumoPullback_MAType
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
[CategoryOrder("Developer", 0)]
[CategoryOrder("General",   1000010)]
[CategoryOrder("Graphics",  1000020)]
[CategoryOrder("Gradient",  1000030)]
[CategoryOrder("Alerts",    1000040)]
[CategoryOrder("Special",   1000060)]
[CategoryOrder("Critical",  1000070)]
public class gbSumoPullback : Indicator
{
	private class InstructionPanel : StackPanel
	{
		internal TextBlock closeButton;

		internal InstructionPanel(string caption)
		{
			MouseEventHandler mouseEventHandler = null;
			MouseEventHandler mouseEventHandler2 = null;
			MouseButtonEventHandler mouseButtonEventHandler = null;
			Brush background = new SolidColorBrush(Color.FromRgb(211, 0, 0));
			base.HorizontalAlignment = HorizontalAlignment.Left;
			base.VerticalAlignment = VerticalAlignment.Top;
			base.Margin = new Thickness(5.0, 5.0, 0.0, 0.0);
			base.Background = background;
			closeButton = new TextBlock
			{
				Text = "âœ–"
			};
			DockPanel.SetDock(closeButton, Dock.Right);
			closeButton.FontSize += 2.0;
			closeButton.Foreground = Brushes.Lime;
			closeButton.FontWeight = FontWeights.Bold;
			closeButton.ToolTip = "  Click to close (you can turn on this instruction again from the indicator-setting window).";
			closeButton.Margin = new Thickness(2.0);
			closeButton.Padding = new Thickness(4.0, 3.0, 4.0, 3.0);
			TextBlock textBlock = closeButton;
			mouseEventHandler = delegate
			{
				closeButton.Background = Brushes.Black;
			};
			textBlock.MouseEnter += mouseEventHandler;
			TextBlock textBlock2 = closeButton;
			mouseEventHandler2 = delegate
			{
				closeButton.Background = Brushes.Transparent;
			};
			textBlock2.MouseLeave += mouseEventHandler2;
			TextBlock textBlock3 = closeButton;
			mouseButtonEventHandler = delegate
			{
				base.Visibility = Visibility.Collapsed;
			};
			textBlock3.MouseLeftButtonUp += mouseButtonEventHandler;
			TextBlock textBlock4 = new TextBlock
			{
				Text = caption
			};
			DockPanel.SetDock(textBlock4, Dock.Top);
			textBlock4.Foreground = Brushes.White;
			textBlock4.FontWeight = FontWeights.Bold;
			textBlock4.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock4.VerticalAlignment = VerticalAlignment.Center;
			textBlock4.Margin = new Thickness(6.0);
			DockPanel element = new DockPanel
			{
				Children = 
				{
					(UIElement)closeButton,
					(UIElement)textBlock4
				}
			};
			TextBlock element2 = new TextBlock
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				Padding = new Thickness(15.0),
				Margin = new Thickness(2.0, 0.0, 2.0, 2.0),
				Background = Brushes.White,
				Foreground = Brushes.Black,
				Inlines =
				{
					"Please learn how to configure email settings at\n",
					(Inline)new Run("https://greybeardconsulting.net/"),
					"."
				}
			};
			base.Children.Add(element);
			base.Children.Add(element2);
		}

		}

	private const int defaultMargin = 5;

	private const string toolTipSpace = "  ";

	private const string nickname = "sumopullback:exc";

	private const string prefix = "gbSumoPullback";

	private const string indicatorName = "Sumo Pullback";

	private Series<double> seriesSlowMA;

	private Series<double> seriesMax;

	private Series<double> seriesMin;

	private Brush backgroundBullish;

	private Brush backgroundBearish;

	private Window alertWindow;

	private string indicatorNameFull;

	private bool isCharting;

	private int nextBar = -1;

	private int countSignalBars;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	private InstructionPanel instructionPanel;

	[Display(Name = "Popup: Enabled", Order = 2, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[Display(Name = "Popup: Background Color", Order = 3, GroupName = "Alerts")]
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

	[Display(Name = "Popup: Background Opacity", Order = 4, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 5, GroupName = "Alerts")]
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

	[Display(Name = "Popup: Text Size", Order = 6, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 7, GroupName = "Alerts")]
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

	[Display(Name = "Sound: Bullish", Order = 11, GroupName = "Alerts")]
	[TypeConverter(typeof(gbSumoPullback_SoundConverter))]
	public string SoundBullish { get; set; }

	[TypeConverter(typeof(gbSumoPullback_SoundConverter))]
	[Display(Name = "Sound: Bearish", Order = 12, GroupName = "Alerts")]
	public string SoundBearish { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 15, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 16, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 20, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 21, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Marker: Color Bullish", Order = 31, GroupName = "Alerts")]
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

	[Display(Name = "Marker: Color Bearish", Order = 32, GroupName = "Alerts")]
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

	[Display(Name = "Marker: String Bullish", Order = 33, GroupName = "Alerts")]
	public string MarkerStringBullish { get; set; }

	[Display(Name = "Marker: String Bearish", Order = 34, GroupName = "Alerts")]
	public string MarkerStringBearish { get; set; }

	[Display(Name = "Marker: Font", Order = 35, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 36, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Author",  Order = 0,  GroupName = "Developer")]
	public string Author  => "GreyBeard";

	[Display(Name = "Website", Order = 5,  GroupName = "Developer")]
	public string Website => "https://greybeardconsulting.net/";

	[Display(Name = "Version", Order = 10, GroupName = "Developer")]
	public string Version => "1.1.1";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Background: Enabled", Order = 0, GroupName = "Graphics")]
	public bool BackgroundEnabled { get; set; }

	[Display(Name = "Background: Bullish", Order = 1, GroupName = "Graphics")]
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

	[Display(Name = "Background: Bearish", Order = 2, GroupName = "Graphics")]
	[XmlIgnore]
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

	[Display(Name = "Background: Opacity", Order = 3, GroupName = "Graphics")]
	public int BackgroundOpacity { get; set; }

	[Display(Name = "Cloud: Bullish", Order = 10, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush CloudBullish { get; set; }

	[Browsable(false)]
	public string CloudBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudBullish);
		}
		set
		{
			CloudBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Cloud: Bearish", Order = 11, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush CloudBearish { get; set; }

	[Browsable(false)]
	public string CloudBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudBearish);
		}
		set
		{
			CloudBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Cloud: Opacity", Order = 12, GroupName = "Graphics")]
	public int CloudOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Slow MA: Type", Order = 0, GroupName = "Parameters")]
	public gbSumoPullback_MAType SlowMAType { get; set; }

	[Display(Name = "Slow MA: Period", Order = 1, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SlowMAPeriod { get; set; }

	[Display(Name = "Slow MA: Smoothing Enabled", Order = 2, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public bool SlowMASmoothingEnabled { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Slow MA: Smoothing Method", Order = 3, GroupName = "Parameters")]
	public gbSumoPullback_MAType SlowMASmoothingMethod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Slow MA: Smoothing Period", Order = 4, GroupName = "Parameters")]
	public int SlowMASmoothingPeriod { get; set; }

	[Display(Name = "Fast MA #1: Type", Order = 10, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbSumoPullback_MAType FastMA1Type { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #1: Period", Order = 11, GroupName = "Parameters")]
	public int FastMA1Period { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #1: Smoothing Enabled", Order = 12, GroupName = "Parameters")]
	public bool FastMA1SmoothingEnabled { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #1: Smoothing Method", Order = 13, GroupName = "Parameters")]
	public gbSumoPullback_MAType FastMA1SmoothingMethod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #1: Smoothing Period", Order = 14, GroupName = "Parameters")]
	public int FastMA1SmoothingPeriod { get; set; }

	[Display(Name = "Fast MA #2: Type", Order = 20, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbSumoPullback_MAType FastMA2Type { get; set; }

	[Display(Name = "Fast MA #2: Period", Order = 21, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int FastMA2Period { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #2: Smoothing Enabled", Order = 22, GroupName = "Parameters")]
	public bool FastMA2SmoothingEnabled { get; set; }

	[Display(Name = "Fast MA #2: Smoothing Method", Order = 23, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbSumoPullback_MAType FastMA2SmoothingMethod { get; set; }

	[Display(Name = "Fast MA #2: Smoothing Period", Order = 24, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int FastMA2SmoothingPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #3: Type", Order = 30, GroupName = "Parameters")]
	public gbSumoPullback_MAType FastMA3Type { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #3: Period", Order = 31, GroupName = "Parameters")]
	public int FastMA3Period { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #3: Smoothing Enabled", Order = 32, GroupName = "Parameters")]
	public bool FastMA3SmoothingEnabled { get; set; }

	[Display(Name = "Fast MA #3: Smoothing Method", Order = 33, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbSumoPullback_MAType FastMA3SmoothingMethod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Fast MA #3: Smoothing Period", Order = 34, GroupName = "Parameters")]
	public int FastMA3SmoothingPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Signal Split: First", Order = 40, GroupName = "Parameters")]
	public int SignalSplitFirst { get; set; }

	[Display(Name = "Signal Split: Second", Order = 41, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalSplitSecond { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> FairValue => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => Values[1];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "Sumo Pullback by GreyBeard" + GetUserNote();
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
			if (text.Contains("instrument") && Instrument != null)
			{
				text = text.Replace("instrument", Instrument.FullName);
			}
			if (text.Contains("period") && BarsPeriod != null)
			{
				text = text.Replace("period", ((object)BarsPeriod).ToString());
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
								if (instructionPanel != null)
								{
									instructionPanel.closeButton.MouseLeftButtonUp -= OnInstructionClose;
									instructionPanel = null;
								}
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
			}
			else
			{
				indicatorNameFull = "Sumo Pullback by GreyBeard";
				seriesSlowMA = new Series<double>(this, MaximumBarsLookBack.Infinite);
				seriesMax = new Series<double>(this, MaximumBarsLookBack.Infinite);
				seriesMin = new Series<double>(this, MaximumBarsLookBack.Infinite);
				backgroundBullish = CreateOpacityBrush(BackgroundBullish, BackgroundOpacity);
				backgroundBearish = CreateOpacityBrush(BackgroundBearish, BackgroundOpacity);
				rearmTimer = new DispatcherTimer();
				rearmTimer.Interval = TimeSpan.FromMilliseconds(100.0);
				rearmTimer.Tick += OnRearmTimerTick;
			}
		}
		else
		{
			Description = "Multi-MA pullback signal generator with fair value zones.";
			Name = "gbSumoPullback";
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
			AddPlot(new Stroke(Brushes.Gold, DashStyleHelper.Solid, 3f), PlotStyle.Square, "Fair Value");
			AddPlot(Brushes.Transparent, "Signal: Trade");
			PopupEnabled = false;
			PopupBackgroundBrush = Brushes.Gold;
			PopupBackgroundOpacity = 60;
			PopupTextBrush = Brushes.DarkSlateGray;
			PopupTextSize = 16;
			PopupButtonBrush = Brushes.Transparent;
			SoundEnabled = false;
			SoundBullish = "Alert4.wav";
			SoundBearish = "Alert3.wav";
			SoundRearmEnabled = true;
			SoundRearmSeconds = 5;
			EmailEnabled = false;
			EmailReceiver = "receiver@example.com";
			MarkerEnabled = true;
			MarkerBrushBullish = Brushes.DodgerBlue;
			MarkerBrushBearish = Brushes.HotPink;
			MarkerStringBullish = "▲ + Sumo";
			MarkerStringBearish = "Sumo + ▼";
			MarkerFont = new SimpleFont("Arial", 20);
			MarkerOffset = 10;
			AlertBlockingSeconds = 60;
			ScreenDPI = 99;
			BackgroundEnabled = true;
			BackgroundBullish = Brushes.LimeGreen;
			BackgroundBearish = Brushes.HotPink;
			BackgroundOpacity = 20;
			CloudBullish = Brushes.DodgerBlue;
			CloudBearish = Brushes.Crimson;
			CloudOpacity = 60;
			SlowMAType = gbSumoPullback_MAType.SMA;
			SlowMAPeriod = 60;
			SlowMASmoothingEnabled = false;
			SlowMASmoothingMethod = gbSumoPullback_MAType.EMA;
			SlowMASmoothingPeriod = 10;
			FastMA1Type = gbSumoPullback_MAType.EMA;
			FastMA1Period = 14;
			FastMA1SmoothingEnabled = false;
			FastMA1SmoothingMethod = gbSumoPullback_MAType.SMA;
			FastMA1SmoothingPeriod = 5;
			FastMA2Type = gbSumoPullback_MAType.EMA;
			FastMA2Period = 30;
			FastMA2SmoothingEnabled = false;
			FastMA2SmoothingMethod = gbSumoPullback_MAType.SMA;
			FastMA2SmoothingPeriod = 10;
			FastMA3Type = gbSumoPullback_MAType.EMA;
			FastMA3Period = 45;
			FastMA3SmoothingEnabled = false;
			FastMA3SmoothingMethod = gbSumoPullback_MAType.SMA;
			FastMA3SmoothingPeriod = 15;
			SignalSplitFirst = 15;
			SignalSplitSecond = 30;
			IndicatorZOrder = -10;
			UserNote = "instrument (period)";
		}
	}

	protected override void OnBarUpdate()
	{
		if (CurrentBar == 0)
		{
			return;
		}
		int num = 0;
		double num2 = Open[0];
		double num3 = High[0];
		double num4 = Low[0];
		double num5 = Close[0];
		double num6 = GetMASmoothed(Input, SlowMAType, SlowMAPeriod, SlowMASmoothingEnabled, SlowMASmoothingMethod, SlowMASmoothingPeriod);
		double num7 = GetMASmoothed(Input, FastMA1Type, FastMA1Period, FastMA1SmoothingEnabled, FastMA1SmoothingMethod, FastMA1SmoothingPeriod);
		double num8 = GetMASmoothed(Input, FastMA2Type, FastMA2Period, FastMA2SmoothingEnabled, FastMA2SmoothingMethod, FastMA2SmoothingPeriod);
		double num9 = GetMASmoothed(Input, FastMA3Type, FastMA3Period, FastMA3SmoothingEnabled, FastMA3SmoothingMethod, FastMA3SmoothingPeriod);
		double[] source = new double[4] { num6, num7, num8, num9 };
		double num10 = source.Max();
		double num11 = source.Min();
		if (MathExtentions.ApproxCompare(num5, num2) <= 0 || MathExtentions.ApproxCompare(Close[1], Open[1]) >= 0)
		{
			if (MathExtentions.ApproxCompare(num5, num2) < 0 && MathExtentions.ApproxCompare(Close[1], Open[1]) > 0 && MathExtentions.ApproxCompare(num11, num4) > 0 && MathExtentions.ApproxCompare(num10, num3) < 0 && MathExtentions.ApproxCompare(num6, num10) == 0)
			{
				if (countSignalBars != 0 || nextBar >= 0)
				{
					if (countSignalBars != 1)
					{
						if (countSignalBars == 2 && CurrentBar > nextBar)
						{
							num = -1;
						}
					}
					else if (CurrentBar >= nextBar)
					{
						num = -1;
						countSignalBars++;
						nextBar = CurrentBar + SignalSplitSecond;
					}
				}
				else
				{
					num = -1;
					countSignalBars++;
					nextBar = CurrentBar + SignalSplitFirst;
				}
			}
		}
		else if (MathExtentions.ApproxCompare(num11, num4) > 0 && MathExtentions.ApproxCompare(num10, num3) < 0 && MathExtentions.ApproxCompare(num6, num11) == 0)
		{
			if (countSignalBars != 0 || nextBar >= 0)
			{
				if (countSignalBars != 1)
				{
					if (countSignalBars == 2 && CurrentBar > nextBar)
					{
						num = 1;
					}
				}
				else if (CurrentBar >= nextBar)
				{
					num = 1;
					countSignalBars++;
					nextBar = CurrentBar + SignalSplitSecond;
				}
			}
			else
			{
				num = 1;
				countSignalBars++;
				nextBar = CurrentBar + SignalSplitFirst;
			}
		}
		if (num != 0)
		{
			PrintMarker(num > 0);
			TriggerAlerts(num > 0);
			if (isCharting && BackgroundEnabled && BackgroundOpacity > 0)
			{
				Brush brush = ((num <= 0) ? backgroundBearish : backgroundBullish);
				if (!BrushExtensions.IsTransparent(brush))
				{
					BackBrushAll = brush;
				}
			}
		}
		if (CurrentBar > nextBar && countSignalBars != 0)
		{
			countSignalBars = 0;
			nextBar = -1;
		}
		seriesSlowMA[0] = num6;
		seriesMax[0] = num10;
		seriesMin[0] = num11;
		if (CloudOpacity > 0)
		{
			Draw.Region(this, "gbSumoPullback.cloud.bullish", CurrentBar, 0, seriesMax, seriesSlowMA, Brushes.Transparent, CloudBullish, CloudOpacity);
			Draw.Region(this, "gbSumoPullback.cloud.bearish", CurrentBar, 0, seriesMin, seriesSlowMA, Brushes.Transparent, CloudBearish, CloudOpacity);
		}
		FairValue[0] = source.Average();
		Signal_Trade[0] = num;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (ChartControl != null)
		{
			base.OnRender(chartControl, chartScale);
		}
	}

	private void PrintMarker(bool isBullish)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string tag = "gbSumoPullback.marker." + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((!isBullish) ? High[0] : Low[0]);
			string text = ((!isBullish) ? MarkerStringBearish : MarkerStringBullish);
			text = FormatMarkerString(text);
			int num = ComputeTextHeight(text, MarkerFont);
			int num2 = ((!isBullish) ? 1 : (-1));
			int yPixelOffset = num2 * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text, 0, y, yPixelOffset, textBrush, MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private void PrintException(Exception exception)
	{
		string text = "gbSumoPullback: " + exception.ToString() + " (" + exception.StackTrace + ")";
		Print((object)text);
		Log(text, LogLevel.Error);
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void TriggerAlerts(bool isBullish)
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
		string text3 = string.Concat(Instrument.FullName, " (", BarsPeriod, ")");
		string text4 = ((!isBullish) ? "BEARISH" : "BULLISH");
		string text5 = "gb Sumo Pullback: " + text4 + " alert on " + text3 + " at " + text;
		string popupMessage = "There has been a " + text4 + " signal.";
		popupMessage = popupMessage + "\n\nAlert chart: " + text3 + "\nAlert time: " + text2;
		string text6 = "\n_______________________\n\n";
		string text7 = popupMessage + text6 + indicatorNameFull;
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
					Title = indicatorNameFull,
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
			string text9 = ((!isBullish) ? SoundBearish : SoundBullish);
			soundPath = Globals.InstallDir + "sounds\\" + text9;
			Alert(text8, Priority.Low, text5, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text5, text7);
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

	private void OnInstructionClose(object sender, RoutedEventArgs e)
	{
	}

	private double GetMA(ISeries<double> input, gbSumoPullback_MAType maType, int period)
	{
		switch (maType)
		{
			case gbSumoPullback_MAType.EMA: return EMA(input, period)[0];
			case gbSumoPullback_MAType.SMA: return SMA(input, period)[0];
			case gbSumoPullback_MAType.WMA: return WMA(input, period)[0];
			case gbSumoPullback_MAType.HMA: return HMA(input, period)[0];
			case gbSumoPullback_MAType.DEMA: return DEMA(input, period)[0];
			case gbSumoPullback_MAType.TEMA: return TEMA(input, period)[0];
			case gbSumoPullback_MAType.TMA: return TMA(input, period)[0];
			case gbSumoPullback_MAType.LinReg: return LinReg(input, period)[0];
			case gbSumoPullback_MAType.VWMA: return VWMA(input, period)[0];
			case gbSumoPullback_MAType.WilderMA: return EMA(input, 2 * period - 1)[0];
			case gbSumoPullback_MAType.ZLEMA: return ZLEMA(input, period)[0];
			default: return SMA(input, period)[0];
		}
	}

	private double GetMASmoothed(ISeries<double> input, gbSumoPullback_MAType maType, int period, bool smoothEnabled, gbSumoPullback_MAType smoothMethod, int smoothPeriod)
	{
		return GetMA(input, maType, period);
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

}

}

public class gbSumoPullback_SoundConverter : System.ComponentModel.TypeConverter
{
	public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
	{
		if (context != null)
		{
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds");
			System.IO.FileInfo[] files = directoryInfo.GetFiles("*.wav");
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
		private GreyBeard.gbSumoPullback[] cachegbSumoPullback;
		public GreyBeard.gbSumoPullback gbSumoPullback(gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			return gbSumoPullback(Input, slowMAType, slowMAPeriod, slowMASmoothingEnabled, slowMASmoothingMethod, slowMASmoothingPeriod, fastMA1Type, fastMA1Period, fastMA1SmoothingEnabled, fastMA1SmoothingMethod, fastMA1SmoothingPeriod, fastMA2Type, fastMA2Period, fastMA2SmoothingEnabled, fastMA2SmoothingMethod, fastMA2SmoothingPeriod, fastMA3Type, fastMA3Period, fastMA3SmoothingEnabled, fastMA3SmoothingMethod, fastMA3SmoothingPeriod, signalSplitFirst, signalSplitSecond);
		}

		public GreyBeard.gbSumoPullback gbSumoPullback(ISeries<double> input, gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			if (cachegbSumoPullback != null)
				for (int idx = 0; idx < cachegbSumoPullback.Length; idx++)
					if (cachegbSumoPullback[idx] != null && cachegbSumoPullback[idx].SlowMAType == slowMAType && cachegbSumoPullback[idx].SlowMAPeriod == slowMAPeriod && cachegbSumoPullback[idx].SlowMASmoothingEnabled == slowMASmoothingEnabled && cachegbSumoPullback[idx].SlowMASmoothingMethod == slowMASmoothingMethod && cachegbSumoPullback[idx].SlowMASmoothingPeriod == slowMASmoothingPeriod && cachegbSumoPullback[idx].FastMA1Type == fastMA1Type && cachegbSumoPullback[idx].FastMA1Period == fastMA1Period && cachegbSumoPullback[idx].FastMA1SmoothingEnabled == fastMA1SmoothingEnabled && cachegbSumoPullback[idx].FastMA1SmoothingMethod == fastMA1SmoothingMethod && cachegbSumoPullback[idx].FastMA1SmoothingPeriod == fastMA1SmoothingPeriod && cachegbSumoPullback[idx].FastMA2Type == fastMA2Type && cachegbSumoPullback[idx].FastMA2Period == fastMA2Period && cachegbSumoPullback[idx].FastMA2SmoothingEnabled == fastMA2SmoothingEnabled && cachegbSumoPullback[idx].FastMA2SmoothingMethod == fastMA2SmoothingMethod && cachegbSumoPullback[idx].FastMA2SmoothingPeriod == fastMA2SmoothingPeriod && cachegbSumoPullback[idx].FastMA3Type == fastMA3Type && cachegbSumoPullback[idx].FastMA3Period == fastMA3Period && cachegbSumoPullback[idx].FastMA3SmoothingEnabled == fastMA3SmoothingEnabled && cachegbSumoPullback[idx].FastMA3SmoothingMethod == fastMA3SmoothingMethod && cachegbSumoPullback[idx].FastMA3SmoothingPeriod == fastMA3SmoothingPeriod && cachegbSumoPullback[idx].SignalSplitFirst == signalSplitFirst && cachegbSumoPullback[idx].SignalSplitSecond == signalSplitSecond && cachegbSumoPullback[idx].EqualsInput(input))
						return cachegbSumoPullback[idx];
			return CacheIndicator<GreyBeard.gbSumoPullback>(new GreyBeard.gbSumoPullback(){ SlowMAType = slowMAType, SlowMAPeriod = slowMAPeriod, SlowMASmoothingEnabled = slowMASmoothingEnabled, SlowMASmoothingMethod = slowMASmoothingMethod, SlowMASmoothingPeriod = slowMASmoothingPeriod, FastMA1Type = fastMA1Type, FastMA1Period = fastMA1Period, FastMA1SmoothingEnabled = fastMA1SmoothingEnabled, FastMA1SmoothingMethod = fastMA1SmoothingMethod, FastMA1SmoothingPeriod = fastMA1SmoothingPeriod, FastMA2Type = fastMA2Type, FastMA2Period = fastMA2Period, FastMA2SmoothingEnabled = fastMA2SmoothingEnabled, FastMA2SmoothingMethod = fastMA2SmoothingMethod, FastMA2SmoothingPeriod = fastMA2SmoothingPeriod, FastMA3Type = fastMA3Type, FastMA3Period = fastMA3Period, FastMA3SmoothingEnabled = fastMA3SmoothingEnabled, FastMA3SmoothingMethod = fastMA3SmoothingMethod, FastMA3SmoothingPeriod = fastMA3SmoothingPeriod, SignalSplitFirst = signalSplitFirst, SignalSplitSecond = signalSplitSecond }, input, ref cachegbSumoPullback);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbSumoPullback gbSumoPullback(Indicators.gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, Indicators.gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			return indicator.gbSumoPullback(Input, slowMAType, slowMAPeriod, slowMASmoothingEnabled, slowMASmoothingMethod, slowMASmoothingPeriod, fastMA1Type, fastMA1Period, fastMA1SmoothingEnabled, fastMA1SmoothingMethod, fastMA1SmoothingPeriod, fastMA2Type, fastMA2Period, fastMA2SmoothingEnabled, fastMA2SmoothingMethod, fastMA2SmoothingPeriod, fastMA3Type, fastMA3Period, fastMA3SmoothingEnabled, fastMA3SmoothingMethod, fastMA3SmoothingPeriod, signalSplitFirst, signalSplitSecond);
		}

		public Indicators.GreyBeard.gbSumoPullback gbSumoPullback(ISeries<double> input , Indicators.gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, Indicators.gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			return indicator.gbSumoPullback(input, slowMAType, slowMAPeriod, slowMASmoothingEnabled, slowMASmoothingMethod, slowMASmoothingPeriod, fastMA1Type, fastMA1Period, fastMA1SmoothingEnabled, fastMA1SmoothingMethod, fastMA1SmoothingPeriod, fastMA2Type, fastMA2Period, fastMA2SmoothingEnabled, fastMA2SmoothingMethod, fastMA2SmoothingPeriod, fastMA3Type, fastMA3Period, fastMA3SmoothingEnabled, fastMA3SmoothingMethod, fastMA3SmoothingPeriod, signalSplitFirst, signalSplitSecond);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbSumoPullback gbSumoPullback(Indicators.gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, Indicators.gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			return indicator.gbSumoPullback(Input, slowMAType, slowMAPeriod, slowMASmoothingEnabled, slowMASmoothingMethod, slowMASmoothingPeriod, fastMA1Type, fastMA1Period, fastMA1SmoothingEnabled, fastMA1SmoothingMethod, fastMA1SmoothingPeriod, fastMA2Type, fastMA2Period, fastMA2SmoothingEnabled, fastMA2SmoothingMethod, fastMA2SmoothingPeriod, fastMA3Type, fastMA3Period, fastMA3SmoothingEnabled, fastMA3SmoothingMethod, fastMA3SmoothingPeriod, signalSplitFirst, signalSplitSecond);
		}

		public Indicators.GreyBeard.gbSumoPullback gbSumoPullback(ISeries<double> input , Indicators.gbSumoPullback_MAType slowMAType, int slowMAPeriod, bool slowMASmoothingEnabled, Indicators.gbSumoPullback_MAType slowMASmoothingMethod, int slowMASmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA1Type, int fastMA1Period, bool fastMA1SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA1SmoothingMethod, int fastMA1SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA2Type, int fastMA2Period, bool fastMA2SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA2SmoothingMethod, int fastMA2SmoothingPeriod, Indicators.gbSumoPullback_MAType fastMA3Type, int fastMA3Period, bool fastMA3SmoothingEnabled, Indicators.gbSumoPullback_MAType fastMA3SmoothingMethod, int fastMA3SmoothingPeriod, int signalSplitFirst, int signalSplitSecond)
		{
			return indicator.gbSumoPullback(input, slowMAType, slowMAPeriod, slowMASmoothingEnabled, slowMASmoothingMethod, slowMASmoothingPeriod, fastMA1Type, fastMA1Period, fastMA1SmoothingEnabled, fastMA1SmoothingMethod, fastMA1SmoothingPeriod, fastMA2Type, fastMA2Period, fastMA2SmoothingEnabled, fastMA2SmoothingMethod, fastMA2SmoothingPeriod, fastMA3Type, fastMA3Period, fastMA3SmoothingEnabled, fastMA3SmoothingMethod, fastMA3SmoothingPeriod, signalSplitFirst, signalSplitSecond);
		}
	}
}

#endregion
