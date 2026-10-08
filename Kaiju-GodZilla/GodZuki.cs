#region Using declarations
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System.Windows;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using Brush      = System.Windows.Media.Brush;
using Color      = System.Windows.Media.Color;
using FontStyle  = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;
#endregion

public enum GodZukiSignalOperator
{
	Equal, GreaterOrEqual, GreaterThan, LessOrEqual, LessThan, NotEqual
}

public enum GodZukiHudCorner
{
	TopLeft, TopRight, BottomLeft, BottomRight, Center, Hidden
}

public enum GodZukiHudSize
{
	Tiny, Small, Normal, Large, Huge
}

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{

	#region GUI Categories
	[CategoryOrder ("Indicator Information", 0)]
	[CategoryOrder ("Signals",               1)]
	[CategoryOrder ("Filters",               2)]
	[CategoryOrder ("Indicator Settings",    3)]
	[CategoryOrder ("Indicator: KingOrderBlock", 4)]
	[CategoryOrder ("Indicator: PANAKanal",      5)]
	[CategoryOrder ("Indicator: ThunderZilla",   6)]
	[CategoryOrder ("Indicator: SuperJumpBoost", 7)]
	[CategoryOrder ("Indicator: SumoPullback",   8)]
	[CategoryOrder ("Indicator: NobleCloud",     9)]
	[CategoryOrder ("Display",               10)]
	[CategoryOrder ("Audio Alerts",          11)]
	[CategoryOrder ("Logging",               12)]
	#endregion
	public class GodZuki : Indicator, ICustomTypeDescriptor
	{
		public override string DisplayName => Name;


		private class GroupTriggerResult
		{
			public bool   Long, Short;
			public int    GroupSize;
			public string TriggerName;
			public bool   UsesKO, UsesPA, UsesTH, UsesSJ, UsesSU, UsesNC;
		}

		#region Variables
		private string _version = "1.4.1";

		private gbKingOrderBlock _king;
		private gbPANAKanal      _pana;
		private gbThunderZilla   _thunder;
		private gbSumoPullback   _sumo;
		private gbNobleCloud     _nc;
		private gbSuperJumpBoost _sjb;

		private Series<double> _koSignalSeries, _paSignalSeries, _thSignalSeries;
		private Series<double> _sjSignalSeries, _suSignalSeries, _ncSignalSeries;

		private EMA _emaShortFilter, _emaLongFilter;

		private const int  DRAW_TAG_KEEP    = 250;
		private SimpleFont _signalArrowFont;

		private bool _indicatorNullWarned = false;

		private Dictionary<string, string> _lastAudioAlertStampByKey = new Dictionary<string, string> ();
		private StreamWriter _logWriter;
		private int          _lastLoggedBar = -1;
		private bool         _logPendingOpen = false;
		private string       _logSafeAccount = "NoAccount";

		private int    _s1PendingDir   = 0;
		private int    _s1PendingBars  = 0;
		private double _s1SignalClose  = 0;
		private int    _s2PendingDir   = 0;
		private int    _s2PendingBars  = 0;
		private double _s2SignalClose  = 0;

		// HUD snapshot (data thread writes, UI thread reads)
		private string   _hudTitle      = "GodZuki";
		private string   _hudVersion    = "";
		private string   _hudEmaLine    = "";
		private bool     _hudEmaEnabled = false;
		private bool     _hudEmaBullish = false;
		private string   _hudSet1ConfigLine = "";
		private bool     _hudSet1On         = false;
		private int      _hudS1State        = 0;   // -1=short  0=flat  1=long
		private bool     _hudS1Warning      = false;
		private string   _hudSet2ConfigLine = "";
		private bool     _hudSet2On         = false;
		private int      _hudS2State        = 0;
		private bool     _hudS2Warning      = false;

		// SharpDX
		private SharpDX.DirectWrite.TextFormat         _dashFormat, _dashTitleFormat;
		private SharpDX.Direct2D1.SolidColorBrush      _bTextWhite, _bTextDim, _bTextGreen, _bTextRed, _bTextYellow, _bTextCyan, _bBackground, _bBorder;
		private SharpDX.Direct2D1.RenderTarget          _lastSeenRenderTarget;
		private bool                                    _dxInitialized;
		private int                                     _hudErrors;
		private GodZukiHudSize                          _lastSizeApplied      = (GodZukiHudSize)(-1);
		private DateTime                                _lastHudInvalidateUtc = DateTime.MinValue;
		private const int                               HUD_MIN_INVALIDATE_MS = 100;
		#endregion

		protected override void OnStateChange ()
		{
			if (State == State.SetDefaults)
			{
				Description = "GodZuki — pure signal indicator. KO/PA/TH/SJ/SU/NC signals with EMA filter, audio alerts, and CSV logging. No trading.";
				Name        = "GodZuki";
				IsOverlay                = true;
				IsAutoScale              = false;
				Calculate                = Calculate.OnBarClose;
				MaximumBarsLookBack      = MaximumBarsLookBack.TwoHundredFiftySix;
				BarsRequiredToPlot       = 2;
				ShowTransparentPlotsInDataBox = true;
				IsSuspendedWhileInactive      = false;
				_signalArrowFont         = new SimpleFont ("Arial", 10) { Bold = true };

				// EMA filter overlay plots (Values[0], Values[1]) — NaN when filter is off
				AddPlot (new Stroke (Brushes.DodgerBlue, 2), PlotStyle.Line, "EMA Short");
				AddPlot (new Stroke (Brushes.HotPink,    2), PlotStyle.Line, "EMA Long");

				// Output signal plots — transparent; data box only, no chart line
				// ShowTransparentPlotsInDataBox = true ensures values appear even with transparent brush
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "EMA Dir");      // Values[2]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "Set1 Signal");  // Values[3]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "Set2 Signal");  // Values[4]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "Both Signal");  // Values[5] — 1 when S1=1 AND S2=1, -1 when S1=-1 AND S2=-1
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_KO Signal"); // Values[6]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_PA Signal"); // Values[7]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_TH Signal"); // Values[8]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_SJ Signal"); // Values[9]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_SU Signal"); // Values[10]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S1_NC Signal"); // Values[11]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_KO Signal"); // Values[12]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_PA Signal"); // Values[13]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_TH Signal"); // Values[14]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_SJ Signal"); // Values[15]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_SU Signal"); // Values[16]
				AddPlot (new Stroke (Brushes.Transparent, 1), PlotStyle.Hash, "S2_NC Signal"); // Values[17]

				// Signals — Set 1
				GroupTriggerSet1RequiredCount = 3;
				UseKOSignals = true;   RequireKOSignal = false;  KO_LongOperator = GodZukiSignalOperator.GreaterOrEqual; KO_LongValue = 1;  KO_ShortOperator = GodZukiSignalOperator.LessOrEqual; KO_ShortValue = -1;
				UsePASignals = true;   RequirePASignal = false;  PA_LongOperator = GodZukiSignalOperator.GreaterOrEqual; PA_LongValue = 2;  PA_ShortOperator = GodZukiSignalOperator.LessOrEqual; PA_ShortValue = -2;
				UseTHSignals = true;   RequireTHSignal = false;  TH_LongOperator = GodZukiSignalOperator.GreaterOrEqual; TH_LongValue = 2;  TH_ShortOperator = GodZukiSignalOperator.LessOrEqual; TH_ShortValue = -2;
				UseSJSignals = true;   RequireSJSignal = false;  SJ_LongOperator = GodZukiSignalOperator.GreaterOrEqual; SJ_LongValue = 1;  SJ_ShortOperator = GodZukiSignalOperator.LessOrEqual; SJ_ShortValue = -1;
				UseSUSignals = true;   RequireSUSignal = false;  SU_LongOperator = GodZukiSignalOperator.GreaterOrEqual; SU_LongValue = 1;  SU_ShortOperator = GodZukiSignalOperator.LessOrEqual; SU_ShortValue = -1;
				UseNCSignals = true;   RequireNCSignal = false;  NC_LongOperator = GodZukiSignalOperator.GreaterOrEqual; NC_LongValue = 1;  NC_ShortOperator = GodZukiSignalOperator.LessOrEqual; NC_ShortValue = -1;

				// Signals — Set 2
				EnableGroupTriggerSet2 = false; GroupTriggerSet2RequiredCount = 3;
				G2_UseKOSignals = true;  G2_RequireKOSignal = false; G2_KO_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_KO_LongValue = 1;  G2_KO_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_KO_ShortValue = -1;
				G2_UsePASignals = true;  G2_RequirePASignal = false; G2_PA_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_PA_LongValue = 3;  G2_PA_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_PA_ShortValue = -3;
				G2_UseTHSignals = true;  G2_RequireTHSignal = false; G2_TH_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_TH_LongValue = 3;  G2_TH_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_TH_ShortValue = -3;
				G2_UseSJSignals = true;  G2_RequireSJSignal = false; G2_SJ_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_SJ_LongValue = 1;  G2_SJ_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_SJ_ShortValue = -1;
				G2_UseSUSignals = true;  G2_RequireSUSignal = false; G2_SU_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_SU_LongValue = 1;  G2_SU_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_SU_ShortValue = -1;
				G2_UseNCSignals = true;  G2_RequireNCSignal = false; G2_NC_LongOperator = GodZukiSignalOperator.GreaterOrEqual; G2_NC_LongValue = 1;  G2_NC_ShortOperator = GodZukiSignalOperator.LessOrEqual; G2_NC_ShortValue = -1;

				// Confirmation
				ConfirmationBars = 0;

				// Filters
				EnableEmaFilter = false; EmaShortPeriod = 21; EmaLongPeriod = 50;

				// Indicator Settings
				ShowIndicatorSettings = false;

				// KingOrderBlock
				King_SwingPointNeighborhood = 5; King_ImbalanceQualifying = 3; King_OrderBlockFindingBosChochPeriod = 50; King_OrderBlockAge = 500;
				King_OrderBlocksSameDirectionOffset = 10; King_OrderBlocksDifferenceDirectionOffset = 10; King_SignalTradeQuantityPerOrderBlock = 3; King_SignalTradeSplitBars = 6;

				// PANAKanal
				Pana_Period = 20; Pana_Factor = 4.0; Pana_MiddlePeriod = 14; Pana_SignalBreakSplit = 20; Pana_SignalPullbackFindingPeriod = 10;

				// ThunderZilla
				Thunder_TrendMAType = gbThunderZilla_MAType.SMA; Thunder_TrendPeriod = 200; Thunder_TrendSmoothingEnabled = false;
				Thunder_TrendSmoothingMethod = gbThunderZilla_MAType.EMA; Thunder_TrendSmoothingPeriod = 10;
				Thunder_StopOffsetMultiplierStop = 60.0; Thunder_SignalQuantityPerFlat = 2; Thunder_SignalQuantityPerTrend = 999;

				// SuperJumpBoost
				SJ_SensitiveModeEnabled = true; SJ_OffsetLevel1 = 1.0; SJ_OffsetLevel2 = 2.0; SJ_OffsetLevel3 = 3.0; SJ_OffsetLevel4 = 4.0; SJ_OffsetBase = 4.0;
				SJ_ReferencePricePeriod = 2; SJ_LineLevelsOffset = 100; SJ_ExtremeNeighborhood = 30; SJ_SignalCloseThreshold = 70; SJ_SignalQuantityPerZone = 2; SJ_SignalSplit = 20;

				// SumoPullback
				SU_SlowMAType = gbSumoPullback_MAType.SMA; SU_SlowMAPeriod = 60; SU_SlowMASmoothingEnabled = false; SU_SlowMASmoothingMethod = gbSumoPullback_MAType.EMA; SU_SlowMASmoothingPeriod = 10;
				SU_FastMA1Type = gbSumoPullback_MAType.EMA; SU_FastMA1Period = 14; SU_FastMA1SmoothingEnabled = false; SU_FastMA1SmoothingMethod = gbSumoPullback_MAType.SMA; SU_FastMA1SmoothingPeriod = 5;
				SU_FastMA2Type = gbSumoPullback_MAType.EMA; SU_FastMA2Period = 30; SU_FastMA2SmoothingEnabled = false; SU_FastMA2SmoothingMethod = gbSumoPullback_MAType.SMA; SU_FastMA2SmoothingPeriod = 10;
				SU_FastMA3Type = gbSumoPullback_MAType.EMA; SU_FastMA3Period = 45; SU_FastMA3SmoothingEnabled = false; SU_FastMA3SmoothingMethod = gbSumoPullback_MAType.SMA; SU_FastMA3SmoothingPeriod = 15;
				SU_SignalSplitFirst = 15; SU_SignalSplitSecond = 30;

				// NobleCloud
				NC_Sensitivity = 60.0; NC_Smoothness = 1;
				NC_BaselineMAType = gbNobleCloud_MAType.SMA; NC_BaselinePeriod = 60; NC_BaselineSmoothingEnabled = true; NC_BaselineSmoothingMethod = gbNobleCloud_MAType.EMA; NC_BaselineSmoothingPeriod = 60;
				NC_KernelMAType = gbNobleCloud_MAType.SMA; NC_KernelPeriod = 20; NC_KernelSmoothingEnabled = true; NC_KernelSmoothingMethod = gbNobleCloud_MAType.EMA; NC_KernelSmoothingPeriod = 5;
				NC_SignalSplit = 5; NC_FilterEnabled = true; NC_FilterBarMin = 10; NC_FilterBarMax = 300;

				// Display
				ShowDashboard = true; DashboardPosition = GodZukiHudCorner.BottomLeft; DashboardSize = GodZukiHudSize.Normal;
				ShowKOSignalArrows = false; ShowPASignalArrows = false; ShowTHSignalArrows = false; ShowSJSignalArrows = false; ShowSUSignalArrows = false; ShowNCSignalArrows = false;
				ShowGroupTriggerArrows = true;
				ShowKOSignalArrowLabels = false; ShowPASignalArrowLabels = false; ShowTHSignalArrowLabels = false; ShowSJSignalArrowLabels = false; ShowSUSignalArrowLabels = false; ShowNCSignalArrowLabels = false;
				ShowGroupTriggerArrowLabel = false;
				KOSignalArrowText = "KO"; PASignalArrowText = "PA"; THSignalArrowText = "TH"; SJSignalArrowText = "SJ"; SUSignalArrowText = "SU"; NCSignalArrowText = "NC";
				GroupTriggerArrowText = "GODZUKI"; SignalArrowTextOffsetTicks = 20;
				KO_Brush = Brushes.DodgerBlue; PA_Brush = Brushes.Cyan; TH_Brush = Brushes.LimeGreen; SJ_Brush = Brushes.Orange; SU_Brush = Brushes.Magenta; NC_Brush = Brushes.Cyan;
				KOSignalArrowBrush = Brushes.DodgerBlue; PASignalArrowBrush = Brushes.Cyan; THSignalArrowBrush = Brushes.LimeGreen;
				SJSignalArrowBrush = Brushes.Orange; SUSignalArrowBrush = Brushes.Magenta; NCSignalArrowBrush = Brushes.Cyan;
				EnableGroupTriggerBackBrush = true; GroupTriggerBackBrush = MakeFrozenBrush (55, 255, 215, 0); GroupTriggerBrush = Brushes.Gold; ArrowOffset = 5;

				// Audio Alerts
				EnableSignalAudioAlerts = false; EnableIndividualSignalAudioAlerts = true; IndividualSignalAlertSound = "Alert1.wav";
				EnableGroupSignalAudioAlerts = true; GroupSignalAlertSound = "Alert2.wav";

				// Logging
				LogEnabled = false; EnableDebug = false;
			}

			else if (State == State.DataLoaded)
			{
				// Only instantiate child indicators whose signal is enabled (in Set 1 or Set 2).
				// Hosted indicators are calculated by NT8 every bar whether or not we read them,
				// so skipping the unused ones avoids running heavy sub-indicators (ThunderZilla,
				// SumoPullback, NobleCloud, etc.) for signals the user has turned off.
				if (NeedKO) _king=gbKingOrderBlock(King_SwingPointNeighborhood,King_ImbalanceQualifying,King_OrderBlockFindingBosChochPeriod,King_OrderBlockAge,King_OrderBlocksSameDirectionOffset,King_OrderBlocksDifferenceDirectionOffset,King_SignalTradeQuantityPerOrderBlock,King_SignalTradeSplitBars);
				if (NeedPA) _pana=gbPANAKanal(Pana_Period,Pana_Factor,Pana_MiddlePeriod,Pana_SignalBreakSplit,Pana_SignalPullbackFindingPeriod);
				if (NeedTH) _thunder=gbThunderZilla(Thunder_TrendMAType,Thunder_TrendPeriod,Thunder_TrendSmoothingEnabled,Thunder_TrendSmoothingMethod,Thunder_TrendSmoothingPeriod,Thunder_StopOffsetMultiplierStop,Thunder_SignalQuantityPerFlat,Thunder_SignalQuantityPerTrend);
				if (NeedSJ) _sjb=gbSuperJumpBoost(SJ_SensitiveModeEnabled,SJ_OffsetLevel1,SJ_OffsetLevel2,SJ_OffsetLevel3,SJ_OffsetLevel4,SJ_OffsetBase,SJ_ReferencePricePeriod,SJ_LineLevelsOffset,SJ_ExtremeNeighborhood,SJ_SignalCloseThreshold,SJ_SignalQuantityPerZone,SJ_SignalSplit);
				if (NeedSU) _sumo=gbSumoPullback(SU_SlowMAType,SU_SlowMAPeriod,SU_SlowMASmoothingEnabled,SU_SlowMASmoothingMethod,SU_SlowMASmoothingPeriod,SU_FastMA1Type,SU_FastMA1Period,SU_FastMA1SmoothingEnabled,SU_FastMA1SmoothingMethod,SU_FastMA1SmoothingPeriod,SU_FastMA2Type,SU_FastMA2Period,SU_FastMA2SmoothingEnabled,SU_FastMA2SmoothingMethod,SU_FastMA2SmoothingPeriod,SU_FastMA3Type,SU_FastMA3Period,SU_FastMA3SmoothingEnabled,SU_FastMA3SmoothingMethod,SU_FastMA3SmoothingPeriod,SU_SignalSplitFirst,SU_SignalSplitSecond);
				if (NeedNC) _nc=gbNobleCloud(NC_Sensitivity,NC_Smoothness,NC_BaselineMAType,NC_BaselinePeriod,NC_BaselineSmoothingEnabled,NC_BaselineSmoothingMethod,NC_BaselineSmoothingPeriod,NC_KernelMAType,NC_KernelPeriod,NC_KernelSmoothingEnabled,NC_KernelSmoothingMethod,NC_KernelSmoothingPeriod,NC_SignalSplit,NC_FilterEnabled,NC_FilterBarMin,NC_FilterBarMax);
				_koSignalSeries=new Series<double>(this); _paSignalSeries=new Series<double>(this);
				_thSignalSeries=new Series<double>(this); _sjSignalSeries=new Series<double>(this);
				_suSignalSeries=new Series<double>(this); _ncSignalSeries=new Series<double>(this);
				if (EnableEmaFilter)
				{
					_emaShortFilter = EMA (EmaShortPeriod);
					_emaLongFilter  = EMA (EmaLongPeriod);
				}
				if (LogEnabled)
				{
					try { if (_logWriter!=null) { _logWriter.Flush(); _logWriter.Dispose(); } } catch {} _logWriter=null;
					string acctName = "NoAccount";
					try
					{
						var ct = ChartControl?.OwnerChart?.ChartTrader;
						if (ct?.Account != null && !string.IsNullOrEmpty(ct.Account.Name))
							acctName = ct.Account.Name;
					}
					catch {}
					_logSafeAccount = string.Concat(acctName.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ","_");
					// Open lazily on the first logged bar so the filename uses Time[0] (the bar's
					// session date) rather than wall-clock — correct in Market Replay/Playback.
					_logPendingOpen = true;
				}
				if (EnableDebug)
				{
					var en1=new List<string>();
					if(UseKOSignals)en1.Add((RequireKOSignal?"+":"")+"KO"); if(UsePASignals)en1.Add((RequirePASignal?"+":"")+"PA"); if(UseTHSignals)en1.Add((RequireTHSignal?"+":"")+"TH");
					if(UseSJSignals)en1.Add((RequireSJSignal?"+":"")+"SJ"); if(UseSUSignals)en1.Add((RequireSUSignal?"+":"")+"SU"); if(UseNCSignals)en1.Add((RequireNCSignal?"+":"")+"NC");
					var en2=new List<string>();
					if(EnableGroupTriggerSet2){
						if(G2_UseKOSignals)en2.Add((G2_RequireKOSignal?"+":"")+"KO"); if(G2_UsePASignals)en2.Add((G2_RequirePASignal?"+":"")+"PA"); if(G2_UseTHSignals)en2.Add((G2_RequireTHSignal?"+":"")+"TH");
						if(G2_UseSJSignals)en2.Add((G2_RequireSJSignal?"+":"")+"SJ"); if(G2_UseSUSignals)en2.Add((G2_RequireSUSignal?"+":"")+"SU"); if(G2_UseNCSignals)en2.Add((G2_RequireNCSignal?"+":"")+"NC");
					}
					Print(string.Format("[{0}] DataLoaded | Instr={1} | Set1=[{2}] | Set2=[{3}] | Set1Req={4} | EMA={5} | Log={6}",
						Name, Instrument.FullName, string.Join(",", en1),
						EnableGroupTriggerSet2?string.Join(",", en2):"OFF",
						GroupTriggerSet1RequiredCount,
						EnableEmaFilter?"ON ("+EmaShortPeriod+"/"+EmaLongPeriod+")":"OFF",
						LogEnabled?"ON":"OFF"));
				}
			}
			else if (State==State.Terminated)
			{
				try { if (_logWriter!=null) { _logWriter.Flush(); _logWriter.Dispose(); _logWriter=null; } } catch {}
				_logPendingOpen = false;
				try { DisposeSharpDxResources(); } catch {}
			}
		}

		protected override void OnBarUpdate ()
		{
			if (CurrentBar < BarsRequiredToPlot) return;

			// Only a *needed* (enabled-anywhere) indicator being null is a failure — disabled
			// signals are intentionally not instantiated and read as 0.
			bool missingNeeded = (NeedKO && _king == null) || (NeedPA && _pana == null) || (NeedTH && _thunder == null)
							  || (NeedSJ && _sjb == null) || (NeedSU && _sumo == null) || (NeedNC && _nc == null);
			if (missingNeeded)
			{
				if (!_indicatorNullWarned && State == State.Realtime)
				{
					_indicatorNullWarned = true;
					Print ($"[{Name}] INDICATOR NULL | One or more enabled child indicators failed to load. KO={_king != null} PA={_pana != null} TH={_thunder != null} SJ={_sjb != null} SU={_sumo != null} NC={_nc != null}");
				}
				return;
			}

			try
			{
				double koRaw = SafeSignalRead (_king?.Signal_Trade, "KO");
				double paRaw = SafeSignalRead (_pana?.Signal_Trade, "PA");
				double thRaw = SafeSignalRead (_thunder?.Signal_Trade, "TH");
				double sjRaw = SafeSignalRead (_sjb?.Signal_Trade, "SJ");
				double suRaw = SafeSignalRead (_sumo?.Signal_Trade, "SU");
				double ncRaw = SafeSignalRead (_nc?.Signal_Trade, "NC");
				int ko=ComputeSignal(UseKOSignals,koRaw,KO_LongOperator,KO_LongValue,KO_ShortOperator,KO_ShortValue);
				int pa=ComputeSignal(UsePASignals,paRaw,PA_LongOperator,PA_LongValue,PA_ShortOperator,PA_ShortValue);
				int th=ComputeSignal(UseTHSignals,thRaw,TH_LongOperator,TH_LongValue,TH_ShortOperator,TH_ShortValue);
				int sj=ComputeSignal(UseSJSignals,sjRaw,SJ_LongOperator,SJ_LongValue,SJ_ShortOperator,SJ_ShortValue);
				int su=ComputeSignal(UseSUSignals,suRaw,SU_LongOperator,SU_LongValue,SU_ShortOperator,SU_ShortValue);
				int nc=ComputeSignal(UseNCSignals,ncRaw,NC_LongOperator,NC_LongValue,NC_ShortOperator,NC_ShortValue);
				if (_koSignalSeries!=null) _koSignalSeries[0]=ko; if (_paSignalSeries!=null) _paSignalSeries[0]=pa;
				if (_thSignalSeries!=null) _thSignalSeries[0]=th; if (_sjSignalSeries!=null) _sjSignalSeries[0]=sj;
				if (_suSignalSeries!=null) _suSignalSeries[0]=su; if (_ncSignalSeries!=null) _ncSignalSeries[0]=nc;

				if (EnableDebug&&(ko!=0||pa!=0||th!=0||sj!=0||su!=0||nc!=0))
					Print(string.Format("[{0}] Bar={1} {2} | KO={3} PA={4} TH={5} SJ={6} SU={7} NC={8}",
						Name, CurrentBar, Time[0].ToString("HH:mm:ss"), ko, pa, th, sj, su, nc));

				// EMA filter plots — Values[0] = short EMA, Values[1] = long EMA
				if (EnableEmaFilter && _emaShortFilter != null && _emaLongFilter != null
					&& CurrentBar >= Math.Max (EmaShortPeriod, EmaLongPeriod))
				{
					Values[0][0] = _emaShortFilter[0];
					Values[1][0] = _emaLongFilter[0];
				}
				else
				{
					Values[0][0] = double.NaN;
					Values[1][0] = double.NaN;
				}
				GroupTriggerResult set1=EvaluatePrimaryGroupTriggerSet(
					ko==1&&UseKOSignals,pa==1&&UsePASignals,th==1&&UseTHSignals,sj==1&&UseSJSignals,su==1&&UseSUSignals,nc==1&&UseNCSignals,
					ko==-1&&UseKOSignals,pa==-1&&UsePASignals,th==-1&&UseTHSignals,sj==-1&&UseSJSignals,su==-1&&UseSUSignals,nc==-1&&UseNCSignals);
				GroupTriggerResult set2=EvaluateSecondaryGroupTriggerSet(koRaw,paRaw,thRaw,sjRaw,suRaw,ncRaw);
				int koVis=SignalVisualFilterPassed(ko)?ko:0; int paVis=SignalVisualFilterPassed(pa)?pa:0;
				int thVis=SignalVisualFilterPassed(th)?th:0; int sjVis=SignalVisualFilterPassed(sj)?sj:0;
				int suVis=SignalVisualFilterPassed(su)?su:0; int ncVis=SignalVisualFilterPassed(nc)?nc:0;

				if (EnableDebug&&EnableEmaFilter&&_emaShortFilter!=null&&_emaLongFilter!=null)
				{
					bool anyBlocked=(ko!=0&&koVis==0)||(pa!=0&&paVis==0)||(th!=0&&thVis==0)
								   ||(sj!=0&&sjVis==0)||(su!=0&&suVis==0)||(nc!=0&&ncVis==0);
					if (anyBlocked)
					{
						string emaDir=_emaShortFilter[0]>_emaLongFilter[0]?"BULLISH":"BEARISH";
						Print(string.Format("[{0}] Bar={1} | EMA filter BLOCKED signal(s) | EMA={2} ({3:F2}/{4:F2})",
							Name, CurrentBar, emaDir, _emaShortFilter[0], _emaLongFilter[0]));
					}
				}

				// Set 1 and Set 2 evaluated independently — both can fire on the same bar
				int s1Vis=(set1!=null&&(set1.Long||set1.Short)&&SignalVisualFilterPassed(set1.Long?1:-1))?(set1.Long?1:-1):0;
				int s2Vis=(set2!=null&&(set2.Long||set2.Short)&&SignalVisualFilterPassed(set2.Long?1:-1))?(set2.Long?1:-1):0;
				int confirmBars = Math.Max(0, Math.Min(25, ConfirmationBars));
				if (confirmBars > 0)
				{
					// Set 1: new signal starts wait window; after N bars check price direction
					if (s1Vis != 0)
					{
						_s1PendingDir  = s1Vis;
						_s1PendingBars = confirmBars;
						_s1SignalClose = Close[0];
						s1Vis = 0;
					}
					else if (_s1PendingDir != 0)
					{
						_s1PendingBars--;
						if (_s1PendingBars <= 0)
						{
							bool ok = (_s1PendingDir > 0 && Close[0] > _s1SignalClose)
									|| (_s1PendingDir < 0 && Close[0] < _s1SignalClose);
							s1Vis = ok ? _s1PendingDir : 0;
							_s1PendingDir  = 0;
							_s1SignalClose = 0;
						}
					}

					// Set 2
					if (s2Vis != 0)
					{
						_s2PendingDir  = s2Vis;
						_s2PendingBars = confirmBars;
						_s2SignalClose = Close[0];
						s2Vis = 0;
					}
					else if (_s2PendingDir != 0)
					{
						_s2PendingBars--;
						if (_s2PendingBars <= 0)
						{
							bool ok = (_s2PendingDir > 0 && Close[0] > _s2SignalClose)
									|| (_s2PendingDir < 0 && Close[0] < _s2SignalClose);
							s2Vis = ok ? _s2PendingDir : 0;
							_s2PendingDir  = 0;
							_s2SignalClose = 0;
						}
					}
				}

				if (EnableDebug&&(s1Vis!=0||s2Vis!=0||(set1!=null&&(set1.Long||set1.Short))||(set2!=null&&(set2.Long||set2.Short))))
				{
					bool b1=set1!=null&&(set1.Long||set1.Short), b2=set2!=null&&(set2.Long||set2.Short);
					string ds1=b1?string.Format("Set1={0}[{1}]{2}",set1.Long?"LONG":"SHORT",BuildSignalFiredList(set1),s1Vis!=0?" OK":" EMA-BLOCKED"):"Set1=FLAT";
					string ds2=b2?string.Format("Set2={0}[{1}]{2}",set2.Long?"LONG":"SHORT",BuildSignalFiredList(set2),s2Vis!=0?" OK":" EMA-BLOCKED"):"Set2=FLAT";
					Print(string.Format("[{0}] Bar={1} | {2} | {3}", Name, CurrentBar, ds1, ds2));
				}

				DrawSignalArrow("GZK_KO_",koVis,UseKOSignals&&ShowKOSignalArrows,KOSignalArrowBrush, 0,ShowKOSignalArrowLabels,KOSignalArrowText);
				DrawSignalArrow("GZK_PA_",paVis,UsePASignals&&ShowPASignalArrows,PASignalArrowBrush, 2,ShowPASignalArrowLabels,PASignalArrowText);
				DrawSignalArrow("GZK_TH_",thVis,UseTHSignals&&ShowTHSignalArrows,THSignalArrowBrush, 4,ShowTHSignalArrowLabels,THSignalArrowText);
				DrawSignalArrow("GZK_SJ_",sjVis,UseSJSignals&&ShowSJSignalArrows,SJSignalArrowBrush, 6,ShowSJSignalArrowLabels,SJSignalArrowText);
				DrawSignalArrow("GZK_SU_",suVis,UseSUSignals&&ShowSUSignalArrows,SUSignalArrowBrush, 8,ShowSUSignalArrowLabels,SUSignalArrowText);
				DrawSignalArrow("GZK_NC_",ncVis,UseNCSignals&&ShowNCSignalArrows,NCSignalArrowBrush,10,ShowNCSignalArrowLabels,NCSignalArrowText);
				// Draw Set 1 and Set 2 arrows independently
				if (ShowGroupTriggerArrows&&s1Vis!=0)
					DrawSignalArrow("GZK_GRP_S1_",s1Vis,true,GroupTriggerBrush,12,ShowGroupTriggerArrowLabel,BuildGroupTriggerArrowLabel("S1"));
				if (ShowGroupTriggerArrows&&s2Vis!=0)
					DrawSignalArrow("GZK_GRP_S2_",s2Vis,true,GroupTriggerBrush,22,ShowGroupTriggerArrowLabel,BuildGroupTriggerArrowLabel("S2"));
				// BackBrush: Set 1 takes priority, fall back to Set 2
				SetSignalBackBrush(s1Vis!=0?s1Vis:s2Vis);
				if (EnableSignalAudioAlerts)
				{
					if (EnableIndividualSignalAudioAlerts)
					{
						if(UseKOSignals)TriggerSignalAudioAlert("KO",koVis,"KingOrderBlock",IndividualSignalAlertSound);
						if(UsePASignals)TriggerSignalAudioAlert("PA",paVis,"PANAKanal",IndividualSignalAlertSound);
						if(UseTHSignals)TriggerSignalAudioAlert("TH",thVis,"ThunderZilla",IndividualSignalAlertSound);
						if(UseSJSignals)TriggerSignalAudioAlert("SJ",sjVis,"SuperJumpBoost",IndividualSignalAlertSound);
						if(UseSUSignals)TriggerSignalAudioAlert("SU",suVis,"SumoPullback",IndividualSignalAlertSound);
						if(UseNCSignals)TriggerSignalAudioAlert("NC",ncVis,"NobleCloud",IndividualSignalAlertSound);
					}
					if (EnableGroupSignalAudioAlerts)
					{
						if (s1Vis!=0) TriggerSignalAudioAlert("GROUP_Set1",s1Vis,"Group Trigger Set1",GroupSignalAlertSound);
						if (s2Vis!=0) TriggerSignalAudioAlert("GROUP_Set2",s2Vis,"Group Trigger Set2",GroupSignalAlertSound);
					}
				}

				// Output signals → Values[2-10] (data box + programmatic access)
				// Set1/Set2 are EMA-filtered; individual signals are pre-filter (raw computed)
				// -1 = short  |  0 = flat / inactive or filtered  |  1 = long
				int s1Out = s1Vis;
				int s2Out = s2Vis;
				int emaOut = 0;
				if (EnableEmaFilter&&_emaShortFilter!=null&&_emaLongFilter!=null
					&&CurrentBar>=Math.Max(EmaShortPeriod,EmaLongPeriod))
					emaOut = _emaShortFilter[0] > _emaLongFilter[0] ? 1 : -1;
				Values[2][0]  = emaOut;
				Values[3][0]  = s1Out;
				Values[4][0]  = s2Out;
				Values[5][0]  = (s1Out == 1 && s2Out == 1) ? 1 : (s1Out == -1 && s2Out == -1) ? -1 : 0;
				Values[6][0]  = UseKOSignals ? ko : 0;
				Values[7][0]  = UsePASignals ? pa : 0;
				Values[8][0]  = UseTHSignals ? th : 0;
				Values[9][0]  = UseSJSignals ? sj : 0;
				Values[10][0] = UseSUSignals ? su : 0;
				Values[11][0] = UseNCSignals ? nc : 0;
				Values[12][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UseKOSignals,koRaw,G2_KO_LongOperator,G2_KO_LongValue,G2_KO_ShortOperator,G2_KO_ShortValue) : 0;
				Values[13][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UsePASignals,paRaw,G2_PA_LongOperator,G2_PA_LongValue,G2_PA_ShortOperator,G2_PA_ShortValue) : 0;
				Values[14][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UseTHSignals,thRaw,G2_TH_LongOperator,G2_TH_LongValue,G2_TH_ShortOperator,G2_TH_ShortValue) : 0;
				Values[15][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UseSJSignals,sjRaw,G2_SJ_LongOperator,G2_SJ_LongValue,G2_SJ_ShortOperator,G2_SJ_ShortValue) : 0;
				Values[16][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UseSUSignals,suRaw,G2_SU_LongOperator,G2_SU_LongValue,G2_SU_ShortOperator,G2_SU_ShortValue) : 0;
				Values[17][0] = EnableGroupTriggerSet2 ? ComputeSignal(G2_UseNCSignals,ncRaw,G2_NC_LongOperator,G2_NC_LongValue,G2_NC_ShortOperator,G2_NC_ShortValue) : 0;

				// CSV — one row per bar when any signal is non-zero
				if (LogEnabled&&CurrentBar!=_lastLoggedBar
					&&(ko!=0||pa!=0||th!=0||sj!=0||su!=0||nc!=0||s1Out!=0||s2Out!=0))
					WriteSignalLogRow(s1Out,s2Out,emaOut,ko,pa,th,sj,su,nc);

				BuildHudSnapshot(ko,pa,th,sj,su,nc,set1,set2,s1Vis,s2Vis);
				RequestHudRepaint();
			}
			catch (Exception ex) { if (EnableDebug) Print(string.Format("[{0}] OnBarUpdate ERROR | {1}",Name,ex.Message)); }
		}


		// ── Signal computation ────────────────────────────────────────────────────
		private int ComputeSignal(bool enabled, double rawValue, GodZukiSignalOperator longOp, int longVal, GodZukiSignalOperator shortOp, int shortVal)
		{
			if (!enabled) return 0;
			int code=(int)Math.Round(rawValue,MidpointRounding.AwayFromZero);
			bool lm=longVal!=0&&IsSignalComparisonMatch(code,longOp,longVal);
			bool sm=shortVal!=0&&IsSignalComparisonMatch(code,shortOp,shortVal);
			if (lm&&sm) return 0;
			if (lm) return 1;
			if (sm) return -1;
			return 0;
		}

		private bool IsSignalComparisonMatch(int code, GodZukiSignalOperator op, int target)
		{
			switch (op)
			{
				case GodZukiSignalOperator.Equal:          return code==target;
				case GodZukiSignalOperator.GreaterOrEqual: return code>=target;
				case GodZukiSignalOperator.GreaterThan:    return code>target;
				case GodZukiSignalOperator.LessOrEqual:    return code<=target;
				case GodZukiSignalOperator.LessThan:       return code<target;
				case GodZukiSignalOperator.NotEqual:       return code!=target;
				default: return code==target;
			}
		}

		private double SafeSignalRead(Func<double> getter, string src)
		{
			try { return getter!=null?getter():0.0; }
			catch (Exception ex) { if (EnableDebug) Print(string.Format("[{0}] SafeSignalRead ERROR src={1} | {2}",Name,src,ex.Message)); return 0.0; }
		}

		// Series overload — avoids allocating a Func<double> closure per read, and returns
		// 0.0 for a disabled (null) child indicator. Reads index [0] inside the guard.
		private double SafeSignalRead(ISeries<double> series, string src)
		{
			try { return series!=null?series[0]:0.0; }
			catch (Exception ex) { if (EnableDebug) Print(string.Format("[{0}] SafeSignalRead ERROR src={1} | {2}",Name,src,ex.Message)); return 0.0; }
		}

		private bool SignalVisualFilterPassed(int signal)
		{
			if (signal==0) return false;
			if (EnableEmaFilter&&_emaShortFilter!=null&&_emaLongFilter!=null&&CurrentBar>=Math.Max(EmaShortPeriod,EmaLongPeriod))
			{
				if (signal>0&&_emaShortFilter[0]<=_emaLongFilter[0]) return false;
				if (signal<0&&_emaShortFilter[0]>=_emaLongFilter[0]) return false;
			}
			return true;
		}

		// ── Group trigger evaluation ──────────────────────────────────────────────
		private GroupTriggerResult EvaluatePrimaryGroupTriggerSet(bool koL,bool paL,bool thL,bool sjL,bool suL,bool ncL,bool koS,bool paS,bool thS,bool sjS,bool suS,bool ncS)
		{
			var r=new GroupTriggerResult{TriggerName="Set1"};
			if (!IsPrimaryGroupModeActive()) return r;
			int needed=Math.Min(Math.Max(1,GroupTriggerSet1RequiredCount),CountEnabledSignals());
			int la=0,sa=0;
			if (UseKOSignals){if(koL)la++;else if(koS)sa++;}
			if (UsePASignals){if(paL)la++;else if(paS)sa++;}
			if (UseTHSignals){if(thL)la++;else if(thS)sa++;}
			if (UseSJSignals){if(sjL)la++;else if(sjS)sa++;}
			if (UseSUSignals){if(suL)la++;else if(suS)sa++;}
			if (UseNCSignals){if(ncL)la++;else if(ncS)sa++;}
			if (la>=needed&&sa>=needed) return r;
			bool lrm=(!UseKOSignals||!RequireKOSignal||koL)&&(!UsePASignals||!RequirePASignal||paL)&&(!UseTHSignals||!RequireTHSignal||thL)&&(!UseSJSignals||!RequireSJSignal||sjL)&&(!UseSUSignals||!RequireSUSignal||suL)&&(!UseNCSignals||!RequireNCSignal||ncL);
			bool srm=(!UseKOSignals||!RequireKOSignal||koS)&&(!UsePASignals||!RequirePASignal||paS)&&(!UseTHSignals||!RequireTHSignal||thS)&&(!UseSJSignals||!RequireSJSignal||sjS)&&(!UseSUSignals||!RequireSUSignal||suS)&&(!UseNCSignals||!RequireNCSignal||ncS);
			if (la>=needed&&lrm){r.Long=true;r.GroupSize=needed;r.UsesKO=UseKOSignals&&koL;r.UsesPA=UsePASignals&&paL;r.UsesTH=UseTHSignals&&thL;r.UsesSJ=UseSJSignals&&sjL;r.UsesSU=UseSUSignals&&suL;r.UsesNC=UseNCSignals&&ncL;}
			else if(sa>=needed&&srm){r.Short=true;r.GroupSize=needed;r.UsesKO=UseKOSignals&&koS;r.UsesPA=UsePASignals&&paS;r.UsesTH=UseTHSignals&&thS;r.UsesSJ=UseSJSignals&&sjS;r.UsesSU=UseSUSignals&&suS;r.UsesNC=UseNCSignals&&ncS;}
			return r;
		}

		private GroupTriggerResult EvaluateSecondaryGroupTriggerSet(double koRaw,double paRaw,double thRaw,double sjRaw,double suRaw,double ncRaw)
		{
			var r=new GroupTriggerResult{TriggerName="Set2"};
			if (!IsSecondaryGroupModeActive()) return r;
			int needed=Math.Min(Math.Max(1,GroupTriggerSet2RequiredCount),CountEnabledGroupTriggerSet2Signals());
			int la=0,sa=0;
			int kos=0,pas=0,ths=0,sjs=0,sus=0,ncs=0;
			if(G2_UseKOSignals){kos=ComputeSignal(true,koRaw,G2_KO_LongOperator,G2_KO_LongValue,G2_KO_ShortOperator,G2_KO_ShortValue);if(kos>0)la++;else if(kos<0)sa++;}
			if(G2_UsePASignals){pas=ComputeSignal(true,paRaw,G2_PA_LongOperator,G2_PA_LongValue,G2_PA_ShortOperator,G2_PA_ShortValue);if(pas>0)la++;else if(pas<0)sa++;}
			if(G2_UseTHSignals){ths=ComputeSignal(true,thRaw,G2_TH_LongOperator,G2_TH_LongValue,G2_TH_ShortOperator,G2_TH_ShortValue);if(ths>0)la++;else if(ths<0)sa++;}
			if(G2_UseSJSignals){sjs=ComputeSignal(true,sjRaw,G2_SJ_LongOperator,G2_SJ_LongValue,G2_SJ_ShortOperator,G2_SJ_ShortValue);if(sjs>0)la++;else if(sjs<0)sa++;}
			if(G2_UseSUSignals){sus=ComputeSignal(true,suRaw,G2_SU_LongOperator,G2_SU_LongValue,G2_SU_ShortOperator,G2_SU_ShortValue);if(sus>0)la++;else if(sus<0)sa++;}
			if(G2_UseNCSignals){ncs=ComputeSignal(true,ncRaw,G2_NC_LongOperator,G2_NC_LongValue,G2_NC_ShortOperator,G2_NC_ShortValue);if(ncs>0)la++;else if(ncs<0)sa++;}
			if (la>=needed&&sa>=needed) return r;
			bool lrm=(!G2_UseKOSignals||!G2_RequireKOSignal||kos>0)&&(!G2_UsePASignals||!G2_RequirePASignal||pas>0)&&(!G2_UseTHSignals||!G2_RequireTHSignal||ths>0)&&(!G2_UseSJSignals||!G2_RequireSJSignal||sjs>0)&&(!G2_UseSUSignals||!G2_RequireSUSignal||sus>0)&&(!G2_UseNCSignals||!G2_RequireNCSignal||ncs>0);
			bool srm=(!G2_UseKOSignals||!G2_RequireKOSignal||kos<0)&&(!G2_UsePASignals||!G2_RequirePASignal||pas<0)&&(!G2_UseTHSignals||!G2_RequireTHSignal||ths<0)&&(!G2_UseSJSignals||!G2_RequireSJSignal||sjs<0)&&(!G2_UseSUSignals||!G2_RequireSUSignal||sus<0)&&(!G2_UseNCSignals||!G2_RequireNCSignal||ncs<0);
			if(la>=needed&&lrm){r.Long=true;r.GroupSize=needed;r.UsesKO=G2_UseKOSignals&&kos>0;r.UsesPA=G2_UsePASignals&&pas>0;r.UsesTH=G2_UseTHSignals&&ths>0;r.UsesSJ=G2_UseSJSignals&&sjs>0;r.UsesSU=G2_UseSUSignals&&sus>0;r.UsesNC=G2_UseNCSignals&&ncs>0;}
			else if(sa>=needed&&srm){r.Short=true;r.GroupSize=needed;r.UsesKO=G2_UseKOSignals&&kos<0;r.UsesPA=G2_UsePASignals&&pas<0;r.UsesTH=G2_UseTHSignals&&ths<0;r.UsesSJ=G2_UseSJSignals&&sjs<0;r.UsesSU=G2_UseSUSignals&&sus<0;r.UsesNC=G2_UseNCSignals&&ncs<0;}
			return r;
		}

		private bool IsPrimaryGroupModeActive()  { int n=CountEnabledSignals();       return n>=1&&GroupTriggerSet1RequiredCount>=1&&GroupTriggerSet1RequiredCount<=n; }
		private bool IsSecondaryGroupModeActive() { if(!EnableGroupTriggerSet2)return false; int n=CountEnabledGroupTriggerSet2Signals(); return n>=1&&GroupTriggerSet2RequiredCount>=1&&GroupTriggerSet2RequiredCount<=n; }
		private int  CountEnabledSignals()        { int c=0; if(UseKOSignals)c++; if(UsePASignals)c++; if(UseTHSignals)c++; if(UseSJSignals)c++; if(UseSUSignals)c++; if(UseNCSignals)c++; return c; }
		private int  CountEnabledGroupTriggerSet2Signals() { int c=0; if(G2_UseKOSignals)c++; if(G2_UsePASignals)c++; if(G2_UseTHSignals)c++; if(G2_UseSJSignals)c++; if(G2_UseSUSignals)c++; if(G2_UseNCSignals)c++; return c; }

		// "Enabled anywhere" — a child indicator is needed if its signal is used in Set 1 or
		// (when Set 2 is on) in Set 2. Drives both instantiation and the OnBarUpdate null-guard.
		private bool NeedKO => UseKOSignals || (EnableGroupTriggerSet2 && G2_UseKOSignals);
		private bool NeedPA => UsePASignals || (EnableGroupTriggerSet2 && G2_UsePASignals);
		private bool NeedTH => UseTHSignals || (EnableGroupTriggerSet2 && G2_UseTHSignals);
		private bool NeedSJ => UseSJSignals || (EnableGroupTriggerSet2 && G2_UseSJSignals);
		private bool NeedSU => UseSUSignals || (EnableGroupTriggerSet2 && G2_UseSUSignals);
		private bool NeedNC => UseNCSignals || (EnableGroupTriggerSet2 && G2_UseNCSignals);

		private string BuildSignalFiredList(GroupTriggerResult r)
		{
			if (r==null) return string.Empty;
			var p=new List<string>();
			if(r.UsesKO)p.Add("KO"); if(r.UsesPA)p.Add("PA"); if(r.UsesTH)p.Add("TH");
			if(r.UsesSJ)p.Add("SJ"); if(r.UsesSU)p.Add("SU"); if(r.UsesNC)p.Add("NC");
			return string.Join("+",p);
		}

		private string BuildGroupTriggerArrowLabel(string setTag)
		{
			string lbl = string.IsNullOrWhiteSpace(GroupTriggerArrowText) ? "GODZUKI" : GroupTriggerArrowText;
			return lbl + "-" + setTag;
		}

		// ── Drawing ───────────────────────────────────────────────────────────────
		private void DrawSignalArrow(string prefix, int signal, bool draw, Brush brush, int extraTicks, bool showLabel, string labelText)
		{
			if (!draw||signal==0||brush==null) return;
			if (CurrentBar<0||TickSize<=0) return;
			if (double.IsNaN(High[0])||double.IsNaN(Low[0])) return;
			int ao=Math.Max(0,ArrowOffset)+Math.Max(0,extraTicks);
			int to=Math.Max(1,SignalArrowTextOffsetTicks);
			double aOff=ao*TickSize, lOff=(ao+to)*TickSize;
			string tag=prefix+CurrentBar, ttag=prefix+"T_"+CurrentBar;
			int old=CurrentBar-DRAW_TAG_KEEP;
			if (old>=0){try{RemoveDrawObject(prefix+old);}catch{} try{RemoveDrawObject(prefix+"T_"+old);}catch{}}
			try
			{
				if (signal>0){Draw.ArrowUp(this,tag,false,0,Low[0]-aOff,brush); DrawSignalArrowLabel(ttag,showLabel,labelText,Low[0]-lOff,brush);}
				else        {Draw.ArrowDown(this,tag,false,0,High[0]+aOff,brush); DrawSignalArrowLabel(ttag,showLabel,labelText,High[0]+lOff,brush);}
			}
			catch(Exception ex){if(EnableDebug)Print(string.Format("[{0}] DrawSignalArrow ERROR | {1}",Name,ex.Message));}
		}

		private void DrawSignalArrowLabel(string tag, bool show, string text, double price, Brush brush)
		{
			if (!show||string.IsNullOrWhiteSpace(text)||brush==null) return;
			if (double.IsNaN(price)||double.IsInfinity(price)) return;
			SimpleFont font=_signalArrowFont??new SimpleFont("Arial",10){Bold=true};
			try { Draw.Text(this,tag,false,text,0,price,0,brush,font,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0); }
			catch {}
		}

		private void SetSignalBackBrush(int groupSignal)
		{
			if (CurrentBar<0) return;
			try { if (EnableGroupTriggerBackBrush&&groupSignal!=0&&GroupTriggerBackBrush!=null) BackBrushes[0]=GroupTriggerBackBrush; }
			catch {}
		}

		// ── Audio alerts ──────────────────────────────────────────────────────────
		private void TriggerSignalAudioAlert(string key, int dir, string label, string sound)
		{
			if (dir==0) return;
			string d=dir>0?"LONG":"SHORT";
			string stamp=CurrentBar+":"+d;
			string last; if (_lastAudioAlertStampByKey.TryGetValue(key,out last)&&last==stamp) return;
			_lastAudioAlertStampByKey[key]=stamp;
			string sp=ResolveAudioAlertSoundPath(sound);
			string aid=Name+"_"+key+"_"+CurrentBar+"_"+d;
			if (EnableDebug) Print(string.Format("[{0}] Bar={1} | AUDIO | {2} {3} | {4}", Name, CurrentBar, label, d, sound));
			try { Alert(aid,Priority.Medium,label+" "+d+" signal",sp,0,Brushes.Black,Brushes.White); }
			catch(Exception ex){if(EnableDebug)Print(string.Format("[{0}] ALERT ERROR | key={1} | {2}",Name,key,ex.Message));}
		}

		private string ResolveAudioAlertSoundPath(string soundFile)
		{
			if (string.IsNullOrWhiteSpace(soundFile)) soundFile="Alert1.wav";
			try
			{
				if (System.IO.Path.IsPathRooted(soundFile)) return soundFile;
				string p=System.IO.Path.Combine(NinjaTrader.Core.Globals.InstallDir,"sounds",soundFile);
				if (System.IO.File.Exists(p)) return p;
			}
			catch {}
			return soundFile;
		}

		// ── CSV logging ───────────────────────────────────────────────────────────
		// One row per bar, written when any signal is non-zero.
		// Columns: DateTime, Instrument, Set1, Set2, EMA, KO, PA, TH, SJ, SU, NC
		private void EnsureLogOpen()
		{
			if (!_logPendingOpen || !LogEnabled || CurrentBar < 0) return;
			try
			{
				string lp = System.IO.Path.Combine(NinjaTrader.Core.Globals.UserDataDir,
					"GodZuki_"+_logSafeAccount+"_"+Time[0].ToString("yyyyMMdd_HHmmss")+".csv");
				_logWriter = new StreamWriter(lp, append:false, encoding:Encoding.UTF8);
				_logWriter.WriteLine("DateTime,Instrument,Set1,Set2,EMA,KO,PA,TH,SJ,SU,NC");
				_logWriter.Flush();
				_logPendingOpen = false;
				if (EnableDebug) Print(string.Format("[{0}] CSV log opened | Acct={1} | {2}", Name, _logSafeAccount, lp));
			}
			catch (Exception ex)
			{
				_logPendingOpen = false;
				if (EnableDebug) Print(string.Format("[{0}] CSV log open failed | {1}", Name, ex.Message));
			}
		}

		private void WriteSignalLogRow(int s1,int s2,int ema,int ko,int pa,int th,int sj,int su,int nc)
		{
			EnsureLogOpen();
			if (_logWriter==null) return;
			try
			{
				_lastLoggedBar=CurrentBar;
				_logWriter.WriteLine(string.Join(",",
					CsvSafe(Time[0].ToString("yyyy-MM-dd HH:mm:ss")),
					CsvSafe(Instrument.FullName),
					s1,s2,ema,ko,pa,th,sj,su,nc));
				_logWriter.Flush();
				if (EnableDebug) Print(string.Format(
					"[{0}] Bar={1} | CSV | Set1={2} Set2={3} EMA={4} KO={5} PA={6} TH={7} SJ={8} SU={9} NC={10}",
					Name,CurrentBar,s1,s2,ema,ko,pa,th,sj,su,nc));
			}
			catch(Exception ex){if(EnableDebug)Print(string.Format("[{0}] CSV ERROR | {1}",Name,ex.Message));}
		}

		private string CsvSafe(string v) { if(v==null)v=string.Empty; return "\""+v.Replace("\"","\"\"")+"\"";}

		// ── HUD snapshot ──────────────────────────────────────────────────────────
		private void BuildHudSnapshot(int ko,int pa,int th,int sj,int su,int nc,GroupTriggerResult set1,GroupTriggerResult set2,int s1Vis,int s2Vis)
		{
			try
			{
				_hudTitle=Name??"GodZuki"; _hudVersion=_version;
				_hudEmaEnabled=EnableEmaFilter;
				if (EnableEmaFilter&&_emaShortFilter!=null&&_emaLongFilter!=null&&CurrentBar>=Math.Max(EmaShortPeriod,EmaLongPeriod))
				{
					bool bull=_emaShortFilter[0]>_emaLongFilter[0]; _hudEmaBullish=bull;
					_hudEmaLine=string.Format("EMA Cross: ON   {0}={1:F2} / {2}={3:F2}",
						EmaShortPeriod, _emaShortFilter[0], EmaLongPeriod, _emaLongFilter[0]);
				}
				else { _hudEmaLine=EnableEmaFilter?"EMA Cross: ON   (warming up)":"EMA Cross: OFF"; _hudEmaBullish=true; }

				// Set1 config row — "Set1 Enabled R:X/Y: KO, PA, TH, SJ, SU, NC" format
				var s1Sigs=new List<string>();
				if(UseKOSignals)s1Sigs.Add((RequireKOSignal?"+":"")+"KO");
				if(UsePASignals)s1Sigs.Add((RequirePASignal?"+":"")+"PA");
				if(UseTHSignals)s1Sigs.Add((RequireTHSignal?"+":"")+"TH");
				if(UseSJSignals)s1Sigs.Add((RequireSJSignal?"+":"")+"SJ");
				if(UseSUSignals)s1Sigs.Add((RequireSUSignal?"+":"")+"SU");
				if(UseNCSignals)s1Sigs.Add((RequireNCSignal?"+":"")+"NC");
				_hudS1Warning = s1Sigs.Count>0 && GroupTriggerSet1RequiredCount>s1Sigs.Count;
				_hudSet1On    = s1Sigs.Count>0 && IsPrimaryGroupModeActive();
				_hudS1State   = s1Vis;
				string s1Body = s1Sigs.Count>0
					? "Set1 Enabled R:"+GroupTriggerSet1RequiredCount+"/"+s1Sigs.Count+": "+string.Join(", ",s1Sigs)
					: "Set1 Enabled: None";
				_hudSet1ConfigLine = _hudS1Warning ? "[!] "+s1Body : s1Body;

				// Set2 config row
				var s2Sigs=new List<string>();
				if(EnableGroupTriggerSet2)
				{
					if(G2_UseKOSignals)s2Sigs.Add((G2_RequireKOSignal?"+":"")+"KO");
					if(G2_UsePASignals)s2Sigs.Add((G2_RequirePASignal?"+":"")+"PA");
					if(G2_UseTHSignals)s2Sigs.Add((G2_RequireTHSignal?"+":"")+"TH");
					if(G2_UseSJSignals)s2Sigs.Add((G2_RequireSJSignal?"+":"")+"SJ");
					if(G2_UseSUSignals)s2Sigs.Add((G2_RequireSUSignal?"+":"")+"SU");
					if(G2_UseNCSignals)s2Sigs.Add((G2_RequireNCSignal?"+":"")+"NC");
				}
				_hudS2Warning = EnableGroupTriggerSet2 && s2Sigs.Count>0 && GroupTriggerSet2RequiredCount>s2Sigs.Count;
				_hudSet2On    = EnableGroupTriggerSet2 && s2Sigs.Count>0 && IsSecondaryGroupModeActive();
				_hudS2State   = s2Vis;
				string s2Body = EnableGroupTriggerSet2
					? (s2Sigs.Count>0
						? "Set2 Enabled R:"+GroupTriggerSet2RequiredCount+"/"+s2Sigs.Count+": "+string.Join(", ",s2Sigs)
						: "Set2 Enabled: None")
					: "Set2: OFF";
				_hudSet2ConfigLine = _hudS2Warning ? "[!] "+s2Body : s2Body;
			}
			catch(Exception ex){if(EnableDebug)Print(string.Format("[{0}] HUD snap error: {1}",Name,ex.Message));}
		}

		private void RequestHudRepaint()
		{
			if (!ShowDashboard||DashboardPosition==GodZukiHudCorner.Hidden) return;
			ChartControl cc=ChartControl; if(cc==null) return;
			DateTime now=DateTime.UtcNow;
			if ((now-_lastHudInvalidateUtc).TotalMilliseconds<HUD_MIN_INVALIDATE_MS) return;
			_lastHudInvalidateUtc=now;
			try{cc.InvalidateVisual();}catch{}
		}

		public override void OnRenderTargetChanged()
		{
			try{base.OnRenderTargetChanged();}catch{}
			try
			{
				if (RenderTarget!=null&&_dxInitialized&&object.ReferenceEquals(RenderTarget,_lastSeenRenderTarget)) return;
				DisposeSharpDxResources();
				if (RenderTarget==null) return;
				CreateSharpDxResources(); _lastSeenRenderTarget=RenderTarget;
			}
			catch(Exception ex){if(_hudErrors<3){_hudErrors++;if(EnableDebug)Print(string.Format("[{0}] HUD RTC err: {1}",Name,ex.Message));}}
		}

		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			try{base.OnRender(cc,cs);}catch{}
			if (RenderTarget==null) return;
			if (!ShowDashboard||DashboardPosition==GodZukiHudCorner.Hidden) return;
			if (_dashFormat==null||_bTextWhite==null)
			{
				try{CreateSharpDxResources();}catch(Exception ex){if(_hudErrors<3){_hudErrors++;if(EnableDebug)Print(string.Format("[{0}] HUD init err: {1}",Name,ex.Message));}}
				if (_dashFormat==null) return;
			}
			try
			{
				string title=_hudTitle, ver=_hudVersion, emaLine=_hudEmaLine;
				bool emaEn=_hudEmaEnabled, emaBull=_hudEmaBullish;
				string s1cfg=_hudSet1ConfigLine; bool s1on=_hudSet1On; int s1st=_hudS1State; bool s1warn=_hudS1Warning;
				string s2cfg=_hudSet2ConfigLine; bool s2on=_hudSet2On; int s2st=_hudS2State; bool s2warn=_hudS2Warning;
				EnsureDashboardFonts();
				const float PAD=8f,SEP_H=4f,MARGIN_X=18f,MARGIN_Y=35f,RIGHT_PAD=80f;
				float RH=HudRowHeight(),TH=HudTitleHeight(),BW=HudBoxWidth();
				const int rows=4; // fixed: title + EMA + Set1 + Set2
				float boxH=PAD*2f+TH+SEP_H+RH*(rows-1)+4f;
				Size2F rt=RenderTarget.Size;
				float bx,by;
				switch(DashboardPosition)
				{
					case GodZukiHudCorner.TopLeft:    bx=MARGIN_X; by=MARGIN_Y; break;
					case GodZukiHudCorner.TopRight:   bx=rt.Width-BW-MARGIN_X-RIGHT_PAD; by=MARGIN_Y; break;
					case GodZukiHudCorner.BottomLeft: bx=MARGIN_X; by=rt.Height-boxH-MARGIN_Y; break;
					case GodZukiHudCorner.Center:     bx=(rt.Width-BW)*0.5f; by=(rt.Height-boxH)*0.5f; break;
					default:                   bx=rt.Width-BW-MARGIN_X-RIGHT_PAD; by=rt.Height-boxH-MARGIN_Y; break;
				}
				RectangleF box=new RectangleF(bx,by,BW,boxH);
				RenderTarget.FillRectangle(box,_bBackground);
				RenderTarget.DrawRectangle(box,_bBorder,1f);
				float x=bx+PAD,y=by+PAD,w=BW-PAD*2f;
				DrawHudLine((string.IsNullOrEmpty(title)?"GodZuki":title)+"  v"+ver,x,y,w,TH,_bTextCyan,_dashTitleFormat); y+=TH;
				RenderTarget.DrawLine(new Vector2(x,y+1f),new Vector2(x+w,y+1f),_bBorder,1f); y+=SEP_H;
				// EMA row — green=bullish, red=bearish, dim=off
				SharpDX.Direct2D1.SolidColorBrush emaBrush = emaEn ? (emaBull ? _bTextGreen : _bTextRed) : _bTextDim;
				DrawHudLine(emaLine, x, y, w, RH, emaBrush, _dashFormat); y += RH;
				// Set1 row — yellow=[!]warning  green=long  red=short  white=watching  dim=off
				var s1Brush = s1warn?_bTextYellow:s1st>0?_bTextGreen:s1st<0?_bTextRed:s1on?_bTextWhite:_bTextDim;
				DrawHudLine(s1cfg, x, y, w, RH, s1Brush, _dashFormat); y += RH;
				// Set2 row — same colour logic
				var s2Brush = s2warn?_bTextYellow:s2st>0?_bTextGreen:s2st<0?_bTextRed:s2on?_bTextWhite:_bTextDim;
				DrawHudLine(s2cfg, x, y, w, RH, s2Brush, _dashFormat);
			}
			catch(Exception ex){if(_hudErrors<3){_hudErrors++;if(EnableDebug)Print(string.Format("[{0}] HUD render err: {1}",Name,ex.Message));}}
		}

		private void DrawHudLine(string text,float x,float y,float w,float h,SharpDX.Direct2D1.SolidColorBrush brush,SharpDX.DirectWrite.TextFormat fmt)
		{
			RenderTarget.DrawText(text??string.Empty,fmt,new RectangleF(x,y,w,h+4f),brush,SharpDX.Direct2D1.DrawTextOptions.Clip,SharpDX.Direct2D1.MeasuringMode.Natural);
		}

		private void CreateSharpDxResources()
		{
			if (RenderTarget==null) return;
			EnsureDashboardFonts();
			_bTextWhite  =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.95f,0.95f,0.95f,1f));
			_bTextDim    =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.65f,0.65f,0.70f,1f));
			_bTextGreen  =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.20f,1.00f,0.30f,1f));
			_bTextRed    =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(1.00f,0.30f,0.30f,1f));
			_bTextYellow =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(1.00f,0.90f,0.10f,1f));
			_bTextCyan   =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.30f,0.85f,1.00f,1f));
			_bBackground =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.05f,0.06f,0.10f,0.86f));
			_bBorder     =new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,new Color4(0.35f,0.40f,0.55f,1.00f));
			_dxInitialized=true;
		}

		private void DisposeSharpDxResources()
		{
			_dxInitialized=false;
			void D<T>(ref T r) where T:class,IDisposable{if(r!=null){try{r.Dispose();}catch{}}r=null;}
			D(ref _dashFormat); D(ref _dashTitleFormat);
			D(ref _bTextWhite); D(ref _bTextDim); D(ref _bTextGreen); D(ref _bTextRed); D(ref _bTextYellow); D(ref _bTextCyan); D(ref _bBackground); D(ref _bBorder);
		}

		private void EnsureDashboardFonts()
		{
			if (_lastSizeApplied==DashboardSize&&_dashFormat!=null&&_dashTitleFormat!=null) return;
			try{if(_dashFormat!=null)_dashFormat.Dispose();}catch{} _dashFormat=null;
			try{if(_dashTitleFormat!=null)_dashTitleFormat.Dispose();}catch{} _dashTitleFormat=null;
			var dwf=NinjaTrader.Core.Globals.DirectWriteFactory;
			_dashFormat=new SharpDX.DirectWrite.TextFormat(dwf,"Consolas",FontWeight.Normal,FontStyle.Normal,HudBodyFontSize());
			_dashFormat.WordWrapping=SharpDX.DirectWrite.WordWrapping.NoWrap;
			_dashFormat.TextAlignment=SharpDX.DirectWrite.TextAlignment.Leading;
			_dashFormat.ParagraphAlignment=SharpDX.DirectWrite.ParagraphAlignment.Near;
			_dashTitleFormat=new SharpDX.DirectWrite.TextFormat(dwf,"Segoe UI",FontWeight.Bold,FontStyle.Normal,HudTitleFontSize());
			_dashTitleFormat.WordWrapping=SharpDX.DirectWrite.WordWrapping.NoWrap;
			_dashTitleFormat.TextAlignment=SharpDX.DirectWrite.TextAlignment.Leading;
			_dashTitleFormat.ParagraphAlignment=SharpDX.DirectWrite.ParagraphAlignment.Near;
			_lastSizeApplied=DashboardSize;
		}

		private float HudBodyFontSize()  {switch(DashboardSize){case GodZukiHudSize.Tiny:return 10f;case GodZukiHudSize.Small:return 11f;case GodZukiHudSize.Large:return 14f;case GodZukiHudSize.Huge:return 17f;default:return 12f;}}
		private float HudTitleFontSize() {switch(DashboardSize){case GodZukiHudSize.Tiny:return 12f;case GodZukiHudSize.Small:return 13f;case GodZukiHudSize.Large:return 17f;case GodZukiHudSize.Huge:return 21f;default:return 14f;}}
		private float HudBoxWidth()      {switch(DashboardSize){case GodZukiHudSize.Tiny:return 260f;case GodZukiHudSize.Small:return 295f;case GodZukiHudSize.Large:return 380f;case GodZukiHudSize.Huge:return 460f;default:return 325f;}}
		private float HudRowHeight()     {switch(DashboardSize){case GodZukiHudSize.Tiny:return 13f;case GodZukiHudSize.Small:return 14f;case GodZukiHudSize.Large:return 19f;case GodZukiHudSize.Huge:return 22f;default:return 16f;}}
		private float HudTitleHeight()   {switch(DashboardSize){case GodZukiHudSize.Tiny:return 18f;case GodZukiHudSize.Small:return 20f;case GodZukiHudSize.Large:return 25f;case GodZukiHudSize.Huge:return 28f;default:return 22f;}}

		private static Brush MakeFrozenBrush(byte a,byte r,byte g,byte b){var br=new SolidColorBrush(Color.FromArgb(a,r,g,b));br.Freeze();return br;}


		// ── ICustomTypeDescriptor ─────────────────────────────────────────────────
		public System.ComponentModel.AttributeCollection GetAttributes()                              => TypeDescriptor.GetAttributes(GetType());
		public string GetClassName()                                                                   => TypeDescriptor.GetClassName(GetType());
		public string GetComponentName()                                                               => TypeDescriptor.GetComponentName(GetType());
		public TypeConverter GetConverter()                                                            => TypeDescriptor.GetConverter(GetType());
		public EventDescriptor GetDefaultEvent()                                                       => TypeDescriptor.GetDefaultEvent(GetType());
		public PropertyDescriptor GetDefaultProperty()                                                 => TypeDescriptor.GetDefaultProperty(GetType());
		public object GetEditor(Type editorBaseType)                                                   => TypeDescriptor.GetEditor(GetType(),editorBaseType);
		public EventDescriptorCollection GetEvents(Attribute[] attributes)                             => TypeDescriptor.GetEvents(GetType(),attributes);
		public EventDescriptorCollection GetEvents()                                                   => TypeDescriptor.GetEvents(GetType());
		public PropertyDescriptorCollection GetProperties() => GetProperties(new Attribute[0]);
		public object GetPropertyOwner(PropertyDescriptor pd) => this;

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
			PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
			orig.CopyTo(arr, 0);
			PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);
			ModifySignalProperties(col);
			ModifyGroupTriggerProperties(col);
			ModifyEmaFilterProperties(col);
			ModifyIndicatorSettingsProperties(col);
			ModifyAudioAlertProperties(col);
			ModifyArrowOffsetProperties(col);
			ModifySignalTextOffsetProperties(col);
			ModifySignalBackBrushProperties(col);
			return col;
		}

		private void RemoveProperties(PropertyDescriptorCollection col, params string[] names)
		{
			foreach (string n in names) { if (col[n]!=null) col.Remove(col[n]); }
		}

		private void ModifySignalProperties(PropertyDescriptorCollection col)
		{
			if (!UseKOSignals) RemoveProperties(col,"RequireKOSignal","KO_LongOperator","KO_LongValue","KO_ShortOperator","KO_ShortValue","ShowKOSignalArrows","ShowKOSignalArrowLabels","KOSignalArrowText","KOSignalArrowBrush");
			else { if (!ShowKOSignalArrows) RemoveProperties(col,"ShowKOSignalArrowLabels","KOSignalArrowText","KOSignalArrowBrush"); else if (!ShowKOSignalArrowLabels) RemoveProperties(col,"KOSignalArrowText"); }
			if (!UsePASignals) RemoveProperties(col,"RequirePASignal","PA_LongOperator","PA_LongValue","PA_ShortOperator","PA_ShortValue","ShowPASignalArrows","ShowPASignalArrowLabels","PASignalArrowText","PASignalArrowBrush");
			else { if (!ShowPASignalArrows) RemoveProperties(col,"ShowPASignalArrowLabels","PASignalArrowText","PASignalArrowBrush"); else if (!ShowPASignalArrowLabels) RemoveProperties(col,"PASignalArrowText"); }
			if (!UseTHSignals) RemoveProperties(col,"RequireTHSignal","TH_LongOperator","TH_LongValue","TH_ShortOperator","TH_ShortValue","ShowTHSignalArrows","ShowTHSignalArrowLabels","THSignalArrowText","THSignalArrowBrush");
			else { if (!ShowTHSignalArrows) RemoveProperties(col,"ShowTHSignalArrowLabels","THSignalArrowText","THSignalArrowBrush"); else if (!ShowTHSignalArrowLabels) RemoveProperties(col,"THSignalArrowText"); }
			if (!UseSJSignals) RemoveProperties(col,"RequireSJSignal","SJ_LongOperator","SJ_LongValue","SJ_ShortOperator","SJ_ShortValue","ShowSJSignalArrows","ShowSJSignalArrowLabels","SJSignalArrowText","SJSignalArrowBrush");
			else { if (!ShowSJSignalArrows) RemoveProperties(col,"ShowSJSignalArrowLabels","SJSignalArrowText","SJSignalArrowBrush"); else if (!ShowSJSignalArrowLabels) RemoveProperties(col,"SJSignalArrowText"); }
			if (!UseSUSignals) RemoveProperties(col,"RequireSUSignal","SU_LongOperator","SU_LongValue","SU_ShortOperator","SU_ShortValue","ShowSUSignalArrows","ShowSUSignalArrowLabels","SUSignalArrowText","SUSignalArrowBrush");
			else { if (!ShowSUSignalArrows) RemoveProperties(col,"ShowSUSignalArrowLabels","SUSignalArrowText","SUSignalArrowBrush"); else if (!ShowSUSignalArrowLabels) RemoveProperties(col,"SUSignalArrowText"); }
			if (!UseNCSignals) RemoveProperties(col,"RequireNCSignal","NC_LongOperator","NC_LongValue","NC_ShortOperator","NC_ShortValue","ShowNCSignalArrows","ShowNCSignalArrowLabels","NCSignalArrowText","NCSignalArrowBrush","NC_Brush");
			else { if (!ShowNCSignalArrows) RemoveProperties(col,"ShowNCSignalArrowLabels","NCSignalArrowText","NCSignalArrowBrush"); else if (!ShowNCSignalArrowLabels) RemoveProperties(col,"NCSignalArrowText"); }
		}

		private void ModifyGroupTriggerProperties(PropertyDescriptorCollection col)
		{
			if (CountEnabledSignals()<1) RemoveProperties(col,"GroupTriggerSet1RequiredCount");
			if (!EnableGroupTriggerSet2) RemoveProperties(col,"GroupTriggerSet2RequiredCount","G2_UseKOSignals","G2_RequireKOSignal","G2_KO_LongOperator","G2_KO_LongValue","G2_KO_ShortOperator","G2_KO_ShortValue","G2_UsePASignals","G2_RequirePASignal","G2_PA_LongOperator","G2_PA_LongValue","G2_PA_ShortOperator","G2_PA_ShortValue","G2_UseTHSignals","G2_RequireTHSignal","G2_TH_LongOperator","G2_TH_LongValue","G2_TH_ShortOperator","G2_TH_ShortValue","G2_UseSJSignals","G2_RequireSJSignal","G2_SJ_LongOperator","G2_SJ_LongValue","G2_SJ_ShortOperator","G2_SJ_ShortValue","G2_UseSUSignals","G2_RequireSUSignal","G2_SU_LongOperator","G2_SU_LongValue","G2_SU_ShortOperator","G2_SU_ShortValue","G2_UseNCSignals","G2_RequireNCSignal","G2_NC_LongOperator","G2_NC_LongValue","G2_NC_ShortOperator","G2_NC_ShortValue");
			else
			{
				if (!G2_UseKOSignals) RemoveProperties(col,"G2_RequireKOSignal","G2_KO_LongOperator","G2_KO_LongValue","G2_KO_ShortOperator","G2_KO_ShortValue");
				if (!G2_UsePASignals) RemoveProperties(col,"G2_RequirePASignal","G2_PA_LongOperator","G2_PA_LongValue","G2_PA_ShortOperator","G2_PA_ShortValue");
				if (!G2_UseTHSignals) RemoveProperties(col,"G2_RequireTHSignal","G2_TH_LongOperator","G2_TH_LongValue","G2_TH_ShortOperator","G2_TH_ShortValue");
				if (!G2_UseSJSignals) RemoveProperties(col,"G2_RequireSJSignal","G2_SJ_LongOperator","G2_SJ_LongValue","G2_SJ_ShortOperator","G2_SJ_ShortValue");
				if (!G2_UseSUSignals) RemoveProperties(col,"G2_RequireSUSignal","G2_SU_LongOperator","G2_SU_LongValue","G2_SU_ShortOperator","G2_SU_ShortValue");
				if (!G2_UseNCSignals) RemoveProperties(col,"G2_RequireNCSignal","G2_NC_LongOperator","G2_NC_LongValue","G2_NC_ShortOperator","G2_NC_ShortValue");
			}
			bool anyGroup=IsPrimaryGroupModeActive()||IsSecondaryGroupModeActive();
			if (!anyGroup) { RemoveProperties(col,"ShowGroupTriggerArrows","ShowGroupTriggerArrowLabel","GroupTriggerArrowText","GroupTriggerBrush"); return; }
			if (!ShowGroupTriggerArrows) RemoveProperties(col,"ShowGroupTriggerArrowLabel","GroupTriggerArrowText","GroupTriggerBrush");
			else if (!ShowGroupTriggerArrowLabel) RemoveProperties(col,"GroupTriggerArrowText");
		}

		private void ModifyEmaFilterProperties(PropertyDescriptorCollection col)
		{ if (!EnableEmaFilter) { RemoveProperties(col,"EmaShortPeriod","EmaLongPeriod"); } }

		private void ModifyIndicatorSettingsProperties(PropertyDescriptorCollection col)
		{
			if (!ShowIndicatorSettings||!UseKOSignals)  RemoveProperties(col,"King_SwingPointNeighborhood","King_ImbalanceQualifying","King_OrderBlockFindingBosChochPeriod","King_OrderBlockAge","King_OrderBlocksSameDirectionOffset","King_OrderBlocksDifferenceDirectionOffset","King_SignalTradeQuantityPerOrderBlock","King_SignalTradeSplitBars","KO_Brush");
			if (!ShowIndicatorSettings||!UsePASignals)  RemoveProperties(col,"Pana_Period","Pana_Factor","Pana_MiddlePeriod","Pana_SignalBreakSplit","Pana_SignalPullbackFindingPeriod","PA_Brush");
			if (!ShowIndicatorSettings||!UseTHSignals)  RemoveProperties(col,"Thunder_TrendMAType","Thunder_TrendPeriod","Thunder_TrendSmoothingEnabled","Thunder_TrendSmoothingMethod","Thunder_TrendSmoothingPeriod","Thunder_StopOffsetMultiplierStop","Thunder_SignalQuantityPerFlat","Thunder_SignalQuantityPerTrend","TH_Brush");
			if (!ShowIndicatorSettings||!UseSJSignals)  RemoveProperties(col,"SJ_SensitiveModeEnabled","SJ_OffsetLevel1","SJ_OffsetLevel2","SJ_OffsetLevel3","SJ_OffsetLevel4","SJ_OffsetBase","SJ_ReferencePricePeriod","SJ_LineLevelsOffset","SJ_ExtremeNeighborhood","SJ_SignalCloseThreshold","SJ_SignalQuantityPerZone","SJ_SignalSplit","SJ_Brush");
			if (!ShowIndicatorSettings||!UseSUSignals)  RemoveProperties(col,"SU_SlowMAType","SU_SlowMAPeriod","SU_SlowMASmoothingEnabled","SU_SlowMASmoothingMethod","SU_SlowMASmoothingPeriod","SU_FastMA1Type","SU_FastMA1Period","SU_FastMA1SmoothingEnabled","SU_FastMA1SmoothingMethod","SU_FastMA1SmoothingPeriod","SU_FastMA2Type","SU_FastMA2Period","SU_FastMA2SmoothingEnabled","SU_FastMA2SmoothingMethod","SU_FastMA2SmoothingPeriod","SU_FastMA3Type","SU_FastMA3Period","SU_FastMA3SmoothingEnabled","SU_FastMA3SmoothingMethod","SU_FastMA3SmoothingPeriod","SU_SignalSplitFirst","SU_SignalSplitSecond","SU_Brush");
			if (!ShowIndicatorSettings||!UseNCSignals)  RemoveProperties(col,"NC_Sensitivity","NC_Smoothness","NC_BaselineMAType","NC_BaselinePeriod","NC_BaselineSmoothingEnabled","NC_BaselineSmoothingMethod","NC_BaselineSmoothingPeriod","NC_KernelMAType","NC_KernelPeriod","NC_KernelSmoothingEnabled","NC_KernelSmoothingMethod","NC_KernelSmoothingPeriod","NC_SignalSplit","NC_FilterEnabled","NC_FilterBarMin","NC_FilterBarMax");
		}

		private void ModifyAudioAlertProperties(PropertyDescriptorCollection col)
		{
			if (!EnableSignalAudioAlerts) { RemoveProperties(col,"EnableIndividualSignalAudioAlerts","IndividualSignalAlertSound","EnableGroupSignalAudioAlerts","GroupSignalAlertSound"); return; }
			bool anyInd=CountEnabledSignals()>0, anyGroup=IsPrimaryGroupModeActive()||IsSecondaryGroupModeActive();
			if (!anyInd) RemoveProperties(col,"EnableIndividualSignalAudioAlerts","IndividualSignalAlertSound");
			else if (!EnableIndividualSignalAudioAlerts) RemoveProperties(col,"IndividualSignalAlertSound");
			if (!anyGroup) RemoveProperties(col,"EnableGroupSignalAudioAlerts","GroupSignalAlertSound");
			else if (!EnableGroupSignalAudioAlerts) RemoveProperties(col,"GroupSignalAlertSound");
		}

		private void ModifyArrowOffsetProperties(PropertyDescriptorCollection col)
		{
			bool anyArrow=(UseKOSignals&&ShowKOSignalArrows)||(UsePASignals&&ShowPASignalArrows)||(UseTHSignals&&ShowTHSignalArrows)||(UseSJSignals&&ShowSJSignalArrows)||(UseSUSignals&&ShowSUSignalArrows)||(UseNCSignals&&ShowNCSignalArrows)||((IsPrimaryGroupModeActive()||IsSecondaryGroupModeActive())&&ShowGroupTriggerArrows);
			if (!anyArrow) RemoveProperties(col,"ArrowOffset");
		}

		private void ModifySignalTextOffsetProperties(PropertyDescriptorCollection col)
		{
			bool anyText=(UseKOSignals&&ShowKOSignalArrows&&ShowKOSignalArrowLabels)||(UsePASignals&&ShowPASignalArrows&&ShowPASignalArrowLabels)||(UseTHSignals&&ShowTHSignalArrows&&ShowTHSignalArrowLabels)||(UseSJSignals&&ShowSJSignalArrows&&ShowSJSignalArrowLabels)||(UseSUSignals&&ShowSUSignalArrows&&ShowSUSignalArrowLabels)||(UseNCSignals&&ShowNCSignalArrows&&ShowNCSignalArrowLabels)||((IsPrimaryGroupModeActive()||IsSecondaryGroupModeActive())&&ShowGroupTriggerArrows&&ShowGroupTriggerArrowLabel);
			if (!anyText) RemoveProperties(col,"SignalArrowTextOffsetTicks");
		}

		private void ModifySignalBackBrushProperties(PropertyDescriptorCollection col)
		{
			if (!(IsPrimaryGroupModeActive()||IsSecondaryGroupModeActive())) RemoveProperties(col,"EnableGroupTriggerBackBrush","GroupTriggerBackBrush");
			else if (!EnableGroupTriggerBackBrush) RemoveProperties(col,"GroupTriggerBackBrush");
		}

		#region Properties
		// ── Indicator Information ─────────────────────────────────────────────────
		[ReadOnly(true)]
		[Display(Name="Indicator Version", GroupName="Indicator Information", Order=0)]
		public string IndicatorVersion => _version;

		[ReadOnly(true)]
		[Display(Name="Author", GroupName="Indicator Information", Order=1)]
		public string Author => "GreyBeard";

		[ReadOnly(true)]
		[Display(Name="Website", GroupName="Indicator Information", Order=2)]
		public string Website => "https://greybeardconsulting.net/";

		// ── Signals: Set 1 ───────────────────────────────────────────────────────
		[NinjaScriptProperty][Range(1,6)][Display(Name="Set 1 Required Count",Order=0,GroupName="Signals",Description="Number of enabled Set 1 signals that must align on the same bar.")]
		public int GroupTriggerSet1RequiredCount{get;set;}

		[NinjaScriptProperty][Range(0,25)][Display(Name="Confirmation Bars",Order=-1,GroupName="Signals",Description="Bars to wait after the group trigger fires before publishing the signal. On the Nth bar, if price is still moving in the signal direction, the arrow and alert fire. 0 = immediate (default).")]
		public int ConfirmationBars{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use KingOrderBlock",Order=10,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UseKOSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require KingOrderBlock",Order=11,GroupName="Signals",Description="When enabled, KingOrderBlock must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequireKOSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 KingOrderBlock Long Operator",Order=12,GroupName="Signals")]
		public GodZukiSignalOperator KO_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 KingOrderBlock Long Value",Order=13,GroupName="Signals")]
		public int KO_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 KingOrderBlock Short Operator",Order=14,GroupName="Signals")]
		public GodZukiSignalOperator KO_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 KingOrderBlock Short Value",Order=15,GroupName="Signals")]
		public int KO_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use PANAKanal",Order=20,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UsePASignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require PANAKanal",Order=21,GroupName="Signals",Description="When enabled, PANAKanal must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequirePASignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 PANAKanal Long Operator",Order=22,GroupName="Signals")]
		public GodZukiSignalOperator PA_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 PANAKanal Long Value",Order=23,GroupName="Signals")]
		public int PA_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 PANAKanal Short Operator",Order=24,GroupName="Signals")]
		public GodZukiSignalOperator PA_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 PANAKanal Short Value",Order=25,GroupName="Signals")]
		public int PA_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use ThunderZilla",Order=30,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UseTHSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require ThunderZilla",Order=31,GroupName="Signals",Description="When enabled, ThunderZilla must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequireTHSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 ThunderZilla Long Operator",Order=32,GroupName="Signals")]
		public GodZukiSignalOperator TH_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 ThunderZilla Long Value",Order=33,GroupName="Signals")]
		public int TH_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 ThunderZilla Short Operator",Order=34,GroupName="Signals")]
		public GodZukiSignalOperator TH_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 ThunderZilla Short Value",Order=35,GroupName="Signals")]
		public int TH_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use SuperJumpBoost",Order=40,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UseSJSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require SuperJumpBoost",Order=41,GroupName="Signals",Description="When enabled, SuperJumpBoost must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequireSJSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SuperJumpBoost Long Operator",Order=42,GroupName="Signals")]
		public GodZukiSignalOperator SJ_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SuperJumpBoost Long Value",Order=43,GroupName="Signals")]
		public int SJ_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SuperJumpBoost Short Operator",Order=44,GroupName="Signals")]
		public GodZukiSignalOperator SJ_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SuperJumpBoost Short Value",Order=45,GroupName="Signals")]
		public int SJ_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use SumoPullback",Order=50,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UseSUSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require SumoPullback",Order=51,GroupName="Signals",Description="When enabled, SumoPullback must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequireSUSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SumoPullback Long Operator",Order=52,GroupName="Signals")]
		public GodZukiSignalOperator SU_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SumoPullback Long Value",Order=53,GroupName="Signals")]
		public int SU_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SumoPullback Short Operator",Order=54,GroupName="Signals")]
		public GodZukiSignalOperator SU_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 SumoPullback Short Value",Order=55,GroupName="Signals")]
		public int SU_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 1 Use NobleCloud",Order=60,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool UseNCSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 Require NobleCloud",Order=61,GroupName="Signals",Description="When enabled, NobleCloud must be among the agreeing signals for Set 1 to trigger.")]
		public bool RequireNCSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 NobleCloud Long Operator",Order=62,GroupName="Signals")]
		public GodZukiSignalOperator NC_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 NobleCloud Long Value",Order=63,GroupName="Signals")]
		public int NC_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 NobleCloud Short Operator",Order=64,GroupName="Signals")]
		public GodZukiSignalOperator NC_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 1 NobleCloud Short Value",Order=65,GroupName="Signals")]
		public int NC_ShortValue{get;set;}

		// ── Signals: Set 2 ───────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Enable Set 2",Order=70,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool EnableGroupTriggerSet2{get;set;}
		[NinjaScriptProperty][Range(1,6)][Display(Name="Set 2 Required Count",Order=71,GroupName="Signals")]
		public int GroupTriggerSet2RequiredCount{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use KingOrderBlock",Order=80,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UseKOSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require KingOrderBlock",Order=81,GroupName="Signals",Description="When enabled, KingOrderBlock must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequireKOSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 KO Long Operator",Order=82,GroupName="Signals")]  public GodZukiSignalOperator G2_KO_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 KO Long Value",Order=83,GroupName="Signals")]      public int G2_KO_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 KO Short Operator",Order=84,GroupName="Signals")]  public GodZukiSignalOperator G2_KO_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 KO Short Value",Order=85,GroupName="Signals")]     public int G2_KO_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use PANAKanal",Order=90,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UsePASignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require PANAKanal",Order=91,GroupName="Signals",Description="When enabled, PANAKanal must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequirePASignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 PA Long Operator",Order=92,GroupName="Signals")]   public GodZukiSignalOperator G2_PA_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 PA Long Value",Order=93,GroupName="Signals")]       public int G2_PA_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 PA Short Operator",Order=94,GroupName="Signals")]   public GodZukiSignalOperator G2_PA_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 PA Short Value",Order=95,GroupName="Signals")]      public int G2_PA_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use ThunderZilla",Order=100,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UseTHSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require ThunderZilla",Order=101,GroupName="Signals",Description="When enabled, ThunderZilla must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequireTHSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 TH Long Operator",Order=102,GroupName="Signals")]  public GodZukiSignalOperator G2_TH_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 TH Long Value",Order=103,GroupName="Signals")]      public int G2_TH_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 TH Short Operator",Order=104,GroupName="Signals")] public GodZukiSignalOperator G2_TH_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 TH Short Value",Order=105,GroupName="Signals")]     public int G2_TH_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use SuperJumpBoost",Order=110,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UseSJSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require SuperJumpBoost",Order=111,GroupName="Signals",Description="When enabled, SuperJumpBoost must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequireSJSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SJ Long Operator",Order=112,GroupName="Signals")]  public GodZukiSignalOperator G2_SJ_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SJ Long Value",Order=113,GroupName="Signals")]      public int G2_SJ_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SJ Short Operator",Order=114,GroupName="Signals")] public GodZukiSignalOperator G2_SJ_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SJ Short Value",Order=115,GroupName="Signals")]     public int G2_SJ_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use SumoPullback",Order=120,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UseSUSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require SumoPullback",Order=121,GroupName="Signals",Description="When enabled, SumoPullback must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequireSUSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SU Long Operator",Order=122,GroupName="Signals")]  public GodZukiSignalOperator G2_SU_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SU Long Value",Order=123,GroupName="Signals")]      public int G2_SU_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SU Short Operator",Order=124,GroupName="Signals")] public GodZukiSignalOperator G2_SU_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 SU Short Value",Order=125,GroupName="Signals")]     public int G2_SU_ShortValue{get;set;}

		[NinjaScriptProperty][Display(Name="Set 2 Use NobleCloud",Order=130,GroupName="Signals")][RefreshProperties(RefreshProperties.All)]
		public bool G2_UseNCSignals{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 Require NobleCloud",Order=131,GroupName="Signals",Description="When enabled, NobleCloud must be among the agreeing signals for Set 2 to trigger.")]
		public bool G2_RequireNCSignal{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 NC Long Operator",Order=132,GroupName="Signals")]  public GodZukiSignalOperator G2_NC_LongOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 NC Long Value",Order=133,GroupName="Signals")]      public int G2_NC_LongValue{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 NC Short Operator",Order=134,GroupName="Signals")] public GodZukiSignalOperator G2_NC_ShortOperator{get;set;}
		[NinjaScriptProperty][Display(Name="Set 2 NC Short Value",Order=135,GroupName="Signals")]     public int G2_NC_ShortValue{get;set;}

		// ── Filters ───────────────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Enable EMA Filter",Order=0,GroupName="Filters",Description="Longs require short EMA above long EMA. Shorts require short EMA below long EMA.")][RefreshProperties(RefreshProperties.All)]
		public bool EnableEmaFilter{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Short EMA Period",Order=1,GroupName="Filters")]
		public int EmaShortPeriod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Long EMA Period",Order=2,GroupName="Filters")]
		public int EmaLongPeriod{get;set;}

		// ── Indicator Settings ────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Show Indicator Settings",Order=0,GroupName="Indicator Settings",Description="Reveals per-indicator parameter groups below.")][RefreshProperties(RefreshProperties.All)]
		public bool ShowIndicatorSettings{get;set;}

		// ── Indicator: KingOrderBlock ─────────────────────────────────────────────
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Swing Point: Neighborhood",Order=0,GroupName="Indicator: KingOrderBlock")]
		public int King_SwingPointNeighborhood{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Imbalance: Qualifying (Bars)",Order=10,GroupName="Indicator: KingOrderBlock")]
		public int King_ImbalanceQualifying{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Order Block: Finding BOS/CHoCH Period",Order=20,GroupName="Indicator: KingOrderBlock")]
		public int King_OrderBlockFindingBosChochPeriod{get;set;}
		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Order Block: Age (Bars)",Order=30,GroupName="Indicator: KingOrderBlock")]
		public int King_OrderBlockAge{get;set;}
		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Order Blocks: Same Direction Offset (Ticks)",Order=40,GroupName="Indicator: KingOrderBlock")]
		public int King_OrderBlocksSameDirectionOffset{get;set;}
		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Order Blocks: Diff Direction Offset (Ticks)",Order=50,GroupName="Indicator: KingOrderBlock")]
		public int King_OrderBlocksDifferenceDirectionOffset{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Trade: Quantity Per OB",Order=60,GroupName="Indicator: KingOrderBlock")]
		public int King_SignalTradeQuantityPerOrderBlock{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Trade: Split (Bars)",Order=70,GroupName="Indicator: KingOrderBlock")]
		public int King_SignalTradeSplitBars{get;set;}

		// ── Indicator: PANAKanal ──────────────────────────────────────────────────
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Period",Order=0,GroupName="Indicator: PANAKanal")]
		public int Pana_Period{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Factor",Order=10,GroupName="Indicator: PANAKanal")]
		public double Pana_Factor{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Middle Period",Order=20,GroupName="Indicator: PANAKanal")]
		public int Pana_MiddlePeriod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Break Split (Bars)",Order=30,GroupName="Indicator: PANAKanal")]
		public int Pana_SignalBreakSplit{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Pullback Finding Period",Order=40,GroupName="Indicator: PANAKanal")]
		public int Pana_SignalPullbackFindingPeriod{get;set;}

		// ── Indicator: ThunderZilla ───────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Trend: MA Type",Order=0,GroupName="Indicator: ThunderZilla")]
		public gbThunderZilla_MAType Thunder_TrendMAType{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Trend: Period",Order=10,GroupName="Indicator: ThunderZilla")]
		public int Thunder_TrendPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Trend: Smoothing Enabled",Order=20,GroupName="Indicator: ThunderZilla")]
		public bool Thunder_TrendSmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Trend: Smoothing Method",Order=30,GroupName="Indicator: ThunderZilla")]
		public gbThunderZilla_MAType Thunder_TrendSmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Trend: Smoothing Period",Order=40,GroupName="Indicator: ThunderZilla")]
		public int Thunder_TrendSmoothingPeriod{get;set;}
		[NinjaScriptProperty][Range(0.0,double.MaxValue)][Display(Name="Stop: Offset Multiplier (Ticks)",Order=50,GroupName="Indicator: ThunderZilla")]
		public double Thunder_StopOffsetMultiplierStop{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal: Quantity Per Flat",Order=60,GroupName="Indicator: ThunderZilla")]
		public int Thunder_SignalQuantityPerFlat{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal: Quantity Per Trend",Order=70,GroupName="Indicator: ThunderZilla")]
		public int Thunder_SignalQuantityPerTrend{get;set;}

		// ── Indicator: SuperJumpBoost ─────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Sensitive Mode Enabled",Order=0,GroupName="Indicator: SuperJumpBoost")]
		public bool SJ_SensitiveModeEnabled{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Offset Level 1",Order=10,GroupName="Indicator: SuperJumpBoost")]
		public double SJ_OffsetLevel1{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Offset Level 2",Order=11,GroupName="Indicator: SuperJumpBoost")]
		public double SJ_OffsetLevel2{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Offset Level 3",Order=12,GroupName="Indicator: SuperJumpBoost")]
		public double SJ_OffsetLevel3{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Offset Level 4",Order=13,GroupName="Indicator: SuperJumpBoost")]
		public double SJ_OffsetLevel4{get;set;}
		[NinjaScriptProperty][Range(0.01,double.MaxValue)][Display(Name="Offset Base",Order=14,GroupName="Indicator: SuperJumpBoost")]
		public double SJ_OffsetBase{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Reference Price Period",Order=20,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_ReferencePricePeriod{get;set;}
		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Line Levels Offset",Order=30,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_LineLevelsOffset{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Extreme Neighborhood",Order=40,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_ExtremeNeighborhood{get;set;}
		[NinjaScriptProperty][Range(1,100)][Display(Name="Signal Close Threshold (%)",Order=50,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_SignalCloseThreshold{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Quantity Per Zone",Order=60,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_SignalQuantityPerZone{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Split (Bars)",Order=70,GroupName="Indicator: SuperJumpBoost")]
		public int SJ_SignalSplit{get;set;}

		// ── Indicator: SumoPullback ───────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Slow MA: Type",Order=0,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_SlowMAType{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Slow MA: Period",Order=1,GroupName="Indicator: SumoPullback")]
		public int SU_SlowMAPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Slow MA: Smoothing Enabled",Order=2,GroupName="Indicator: SumoPullback")]
		public bool SU_SlowMASmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Slow MA: Smoothing Method",Order=3,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_SlowMASmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Slow MA: Smoothing Period",Order=4,GroupName="Indicator: SumoPullback")]
		public int SU_SlowMASmoothingPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #1: Type",Order=10,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA1Type{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #1: Period",Order=11,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA1Period{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #1: Smoothing Enabled",Order=12,GroupName="Indicator: SumoPullback")]
		public bool SU_FastMA1SmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #1: Smoothing Method",Order=13,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA1SmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #1: Smoothing Period",Order=14,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA1SmoothingPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #2: Type",Order=20,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA2Type{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #2: Period",Order=21,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA2Period{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #2: Smoothing Enabled",Order=22,GroupName="Indicator: SumoPullback")]
		public bool SU_FastMA2SmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #2: Smoothing Method",Order=23,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA2SmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #2: Smoothing Period",Order=24,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA2SmoothingPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #3: Type",Order=30,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA3Type{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #3: Period",Order=31,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA3Period{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #3: Smoothing Enabled",Order=32,GroupName="Indicator: SumoPullback")]
		public bool SU_FastMA3SmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Fast MA #3: Smoothing Method",Order=33,GroupName="Indicator: SumoPullback")]
		public gbSumoPullback_MAType SU_FastMA3SmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Fast MA #3: Smoothing Period",Order=34,GroupName="Indicator: SumoPullback")]
		public int SU_FastMA3SmoothingPeriod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Split: First",Order=40,GroupName="Indicator: SumoPullback")]
		public int SU_SignalSplitFirst{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Signal Split: Second",Order=41,GroupName="Indicator: SumoPullback")]
		public int SU_SignalSplitSecond{get;set;}

		// ── Indicator: NobleCloud ─────────────────────────────────────────────────
		[NinjaScriptProperty][Range(0.0,double.MaxValue)][Display(Name="Sensitivity",Order=0,GroupName="Indicator: NobleCloud")]
		public double NC_Sensitivity{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Smoothness",Order=1,GroupName="Indicator: NobleCloud")]
		public int NC_Smoothness{get;set;}
		[NinjaScriptProperty][Display(Name="Baseline: MA Type",Order=10,GroupName="Indicator: NobleCloud")]
		public gbNobleCloud_MAType NC_BaselineMAType{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Baseline: Period",Order=11,GroupName="Indicator: NobleCloud")]
		public int NC_BaselinePeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Baseline: Smoothing Enabled",Order=12,GroupName="Indicator: NobleCloud")]
		public bool NC_BaselineSmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Baseline: Smoothing Method",Order=13,GroupName="Indicator: NobleCloud")]
		public gbNobleCloud_MAType NC_BaselineSmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Baseline: Smoothing Period",Order=14,GroupName="Indicator: NobleCloud")]
		public int NC_BaselineSmoothingPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Kernel: MA Type",Order=20,GroupName="Indicator: NobleCloud")]
		public gbNobleCloud_MAType NC_KernelMAType{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Kernel: Period",Order=21,GroupName="Indicator: NobleCloud")]
		public int NC_KernelPeriod{get;set;}
		[NinjaScriptProperty][Display(Name="Kernel: Smoothing Enabled",Order=22,GroupName="Indicator: NobleCloud")]
		public bool NC_KernelSmoothingEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Kernel: Smoothing Method",Order=23,GroupName="Indicator: NobleCloud")]
		public gbNobleCloud_MAType NC_KernelSmoothingMethod{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Kernel: Smoothing Period",Order=24,GroupName="Indicator: NobleCloud")]
		public int NC_KernelSmoothingPeriod{get;set;}
		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Signal Split (Bars)",Order=30,GroupName="Indicator: NobleCloud")]
		public int NC_SignalSplit{get;set;}
		[NinjaScriptProperty][Display(Name="Filter: Enabled",Order=40,GroupName="Indicator: NobleCloud")]
		public bool NC_FilterEnabled{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Filter: Bar Min",Order=41,GroupName="Indicator: NobleCloud")]
		public int NC_FilterBarMin{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Filter: Bar Max",Order=42,GroupName="Indicator: NobleCloud")]
		public int NC_FilterBarMax{get;set;}

		// ── Display ───────────────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Show Dashboard",Order=0,GroupName="Display",Description="Show the SharpDX signal panel. Turn off for stability mode.")][RefreshProperties(RefreshProperties.All)]
		public bool ShowDashboard{get;set;}
		[NinjaScriptProperty][Display(Name="Dashboard Position",Order=1,GroupName="Display")]
		public GodZukiHudCorner DashboardPosition{get;set;}
		[NinjaScriptProperty][Display(Name="Dashboard Size",Order=2,GroupName="Display")]
		public GodZukiHudSize DashboardSize{get;set;}

		[NinjaScriptProperty][Display(Name="KingOrderBlock: Show Signal Arrows",Order=8,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowKOSignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="KingOrderBlock: Show Arrow Label",Order=9,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowKOSignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="KingOrderBlock: Arrow Label Text",Order=10,GroupName="Display")]
		public string KOSignalArrowText{get;set;}
		[XmlIgnore][Display(Name="KingOrderBlock: Indicator Color",Order=11,GroupName="Display")]
		public Brush KO_Brush{get;set;}
		[Browsable(false)] public string KO_BrushSerialize{get{return Serialize.BrushToString(KO_Brush);}set{KO_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="KingOrderBlock: Arrow Color",Order=12,GroupName="Display")]
		public Brush KOSignalArrowBrush{get;set;}
		[Browsable(false)] public string KOSignalArrowBrushSerialize{get{return Serialize.BrushToString(KOSignalArrowBrush);}set{KOSignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="PANAKanal: Show Signal Arrows",Order=18,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowPASignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="PANAKanal: Show Arrow Label",Order=19,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowPASignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="PANAKanal: Arrow Label Text",Order=20,GroupName="Display")]
		public string PASignalArrowText{get;set;}
		[XmlIgnore][Display(Name="PANAKanal: Indicator Color",Order=21,GroupName="Display")]
		public Brush PA_Brush{get;set;}
		[Browsable(false)] public string PA_BrushSerialize{get{return Serialize.BrushToString(PA_Brush);}set{PA_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="PANAKanal: Arrow Color",Order=22,GroupName="Display")]
		public Brush PASignalArrowBrush{get;set;}
		[Browsable(false)] public string PASignalArrowBrushSerialize{get{return Serialize.BrushToString(PASignalArrowBrush);}set{PASignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="ThunderZilla: Show Signal Arrows",Order=28,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowTHSignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="ThunderZilla: Show Arrow Label",Order=29,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowTHSignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="ThunderZilla: Arrow Label Text",Order=30,GroupName="Display")]
		public string THSignalArrowText{get;set;}
		[XmlIgnore][Display(Name="ThunderZilla: Indicator Color",Order=31,GroupName="Display")]
		public Brush TH_Brush{get;set;}
		[Browsable(false)] public string TH_BrushSerialize{get{return Serialize.BrushToString(TH_Brush);}set{TH_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="ThunderZilla: Arrow Color",Order=32,GroupName="Display")]
		public Brush THSignalArrowBrush{get;set;}
		[Browsable(false)] public string THSignalArrowBrushSerialize{get{return Serialize.BrushToString(THSignalArrowBrush);}set{THSignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="SuperJumpBoost: Show Signal Arrows",Order=38,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowSJSignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="SuperJumpBoost: Show Arrow Label",Order=39,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowSJSignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="SuperJumpBoost: Arrow Label Text",Order=40,GroupName="Display")]
		public string SJSignalArrowText{get;set;}
		[XmlIgnore][Display(Name="SuperJumpBoost: Indicator Color",Order=41,GroupName="Display")]
		public Brush SJ_Brush{get;set;}
		[Browsable(false)] public string SJ_BrushSerialize{get{return Serialize.BrushToString(SJ_Brush);}set{SJ_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="SuperJumpBoost: Arrow Color",Order=42,GroupName="Display")]
		public Brush SJSignalArrowBrush{get;set;}
		[Browsable(false)] public string SJSignalArrowBrushSerialize{get{return Serialize.BrushToString(SJSignalArrowBrush);}set{SJSignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="SumoPullback: Show Signal Arrows",Order=48,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowSUSignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="SumoPullback: Show Arrow Label",Order=49,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowSUSignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="SumoPullback: Arrow Label Text",Order=50,GroupName="Display")]
		public string SUSignalArrowText{get;set;}
		[XmlIgnore][Display(Name="SumoPullback: Indicator Color",Order=51,GroupName="Display")]
		public Brush SU_Brush{get;set;}
		[Browsable(false)] public string SU_BrushSerialize{get{return Serialize.BrushToString(SU_Brush);}set{SU_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="SumoPullback: Arrow Color",Order=52,GroupName="Display")]
		public Brush SUSignalArrowBrush{get;set;}
		[Browsable(false)] public string SUSignalArrowBrushSerialize{get{return Serialize.BrushToString(SUSignalArrowBrush);}set{SUSignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="NobleCloud: Show Signal Arrows",Order=58,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowNCSignalArrows{get;set;}
		[NinjaScriptProperty][Display(Name="NobleCloud: Show Arrow Label",Order=59,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowNCSignalArrowLabels{get;set;}
		[NinjaScriptProperty][Display(Name="NobleCloud: Arrow Label Text",Order=60,GroupName="Display")]
		public string NCSignalArrowText{get;set;}
		[XmlIgnore][Display(Name="NobleCloud: Indicator Color",Order=61,GroupName="Display")]
		public Brush NC_Brush{get;set;}
		[Browsable(false)] public string NC_BrushSerialize{get{return Serialize.BrushToString(NC_Brush);}set{NC_Brush=Serialize.StringToBrush(value);}}
		[XmlIgnore][Display(Name="NobleCloud: Arrow Color",Order=62,GroupName="Display")]
		public Brush NCSignalArrowBrush{get;set;}
		[Browsable(false)] public string NCSignalArrowBrushSerialize{get{return Serialize.BrushToString(NCSignalArrowBrush);}set{NCSignalArrowBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="Group: Show Trigger Arrows",Order=65,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowGroupTriggerArrows{get;set;}
		[NinjaScriptProperty][Display(Name="Group: Show Trigger Arrow Label",Order=66,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool ShowGroupTriggerArrowLabel{get;set;}
		[NinjaScriptProperty][Display(Name="Group: Arrow Label Text",Order=67,GroupName="Display")]
		public string GroupTriggerArrowText{get;set;}
		[XmlIgnore][Display(Name="Group: Trigger Arrow Color",Order=68,GroupName="Display")]
		public Brush GroupTriggerBrush{get;set;}
		[Browsable(false)] public string GroupTriggerBrushSerialize{get{return Serialize.BrushToString(GroupTriggerBrush);}set{GroupTriggerBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Display(Name="Group: Enable Trigger BackBrush",Order=69,GroupName="Display")][RefreshProperties(RefreshProperties.All)]
		public bool EnableGroupTriggerBackBrush{get;set;}
		[XmlIgnore][Display(Name="Group: Trigger BackBrush",Order=70,GroupName="Display")]
		public Brush GroupTriggerBackBrush{get;set;}
		[Browsable(false)] public string GroupTriggerBackBrushSerialize{get{return Serialize.BrushToString(GroupTriggerBackBrush);}set{GroupTriggerBackBrush=Serialize.StringToBrush(value);}}

		[NinjaScriptProperty][Range(0,int.MaxValue)][Display(Name="Arrow Offset (Ticks)",Order=75,GroupName="Display")]
		public int ArrowOffset{get;set;}
		[NinjaScriptProperty][Range(1,int.MaxValue)][Display(Name="Arrow Text Offset (Ticks)",Order=76,GroupName="Display")]
		public int SignalArrowTextOffsetTicks{get;set;}

		// ── Audio Alerts ──────────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Enable Signal Audio Alerts",Order=0,GroupName="Audio Alerts")][RefreshProperties(RefreshProperties.All)]
		public bool EnableSignalAudioAlerts{get;set;}
		[NinjaScriptProperty][Display(Name="Enable Individual Signal Alerts",Order=1,GroupName="Audio Alerts")][RefreshProperties(RefreshProperties.All)]
		public bool EnableIndividualSignalAudioAlerts{get;set;}
		[NinjaScriptProperty]
		[Display(Name="Individual Signal Alert Sound", Order=2, GroupName="Audio Alerts", Description="Sound file for individual indicator signal alerts. Click the field to browse for a .wav file.")]
		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter="WAV files (*.wav)|*.wav|All files (*.*)|*.*")]
		public string IndividualSignalAlertSound{get;set;}
		[NinjaScriptProperty][Display(Name="Enable Group Signal Alerts",Order=3,GroupName="Audio Alerts")][RefreshProperties(RefreshProperties.All)]
		public bool EnableGroupSignalAudioAlerts{get;set;}
		[NinjaScriptProperty]
		[Display(Name="Group Signal Alert Sound", Order=4, GroupName="Audio Alerts", Description="Sound file for group trigger signal alerts. Click the field to browse for a .wav file.")]
		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter="WAV files (*.wav)|*.wav|All files (*.*)|*.*")]
		public string GroupSignalAlertSound{get;set;}

		// ── Logging ───────────────────────────────────────────────────────────────
		[NinjaScriptProperty][Display(Name="Log Alerts to CSV",Order=0,GroupName="Logging",Description="Writes GodZuki_[Instrument]_datetime.csv to the NT8 UserDataDir.")][RefreshProperties(RefreshProperties.All)]
		public bool LogEnabled{get;set;}
		[NinjaScriptProperty][Display(Name="Enable Debug Output",Order=1,GroupName="Logging")]
		public bool EnableDebug{get;set;}

		// ── Output signal series ──────────────────────────────────────────────────
		// Values[2-17] drive the Data Box (via ShowTransparentPlotsInDataBox = true)
		// and are exposed here for programmatic access from strategies.
		// -1 = short  |  0 = flat / inactive  |  1 = long
		// 1 = bullish (short > long) | -1 = bearish | 0 = filter off or warming up
		[Browsable(false)][XmlIgnore] public Series<double> EmaSignal   => Values[2];
		[Browsable(false)][XmlIgnore] public Series<double> Set1Signal  => Values[3];
		[Browsable(false)][XmlIgnore] public Series<double> Set2Signal  => Values[4];
		[Browsable(false)][XmlIgnore] public Series<double> BothSignal  => Values[5];
		// Set1 individual signals (0 when signal is disabled)
		[Browsable(false)][XmlIgnore] public Series<double> S1KOSignal  => Values[6];
		[Browsable(false)][XmlIgnore] public Series<double> S1PASignal  => Values[7];
		[Browsable(false)][XmlIgnore] public Series<double> S1THSignal  => Values[8];
		[Browsable(false)][XmlIgnore] public Series<double> S1SJSignal  => Values[9];
		[Browsable(false)][XmlIgnore] public Series<double> S1SUSignal  => Values[10];
		[Browsable(false)][XmlIgnore] public Series<double> S1NCSignal  => Values[11];
		// Set2 individual signals (0 when Set2 is disabled or signal is disabled)
		[Browsable(false)][XmlIgnore] public Series<double> S2KOSignal  => Values[12];
		[Browsable(false)][XmlIgnore] public Series<double> S2PASignal  => Values[13];
		[Browsable(false)][XmlIgnore] public Series<double> S2THSignal  => Values[14];
		[Browsable(false)][XmlIgnore] public Series<double> S2SJSignal  => Values[15];
		[Browsable(false)][XmlIgnore] public Series<double> S2SUSignal  => Values[16];
		[Browsable(false)][XmlIgnore] public Series<double> S2NCSignal  => Values[17];
		#endregion
	}
}
