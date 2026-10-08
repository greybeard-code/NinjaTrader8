#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using SharpDX;
using Color = System.Windows.Media.Color;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class PanaZillaStrategy : Strategy
    {
        #region Enums

        private enum TradeState { Flat, Managing, Exiting }
        private enum TradeDir   { None, Long, Short }

        #endregion

        #region ContractInfo

        private class ContractInfo
        {
            public string   SignalName      { get; set; }
            public int      Quantity        { get; set; }
            public double   EntryPrice      { get; set; }
            public DateTime FillTime        { get; set; }
            public double   CurrentStopPrice { get; set; }
            public double   CurrentTpPrice  { get; set; }
            public bool     IsActive        { get; set; }
            public bool     IsFilled        { get; set; }
        }

        #endregion

        #region Signal name constants

        private const string InitLong  = "PZ_Init_L";
        private const string InitShort = "PZ_Init_S";

        // Add-on signal names (indexed 0-5 → A1-A6)
        private static readonly string[] AddonLongNames  = { "PZ_A1_L", "PZ_A2_L", "PZ_A3_L", "PZ_A4_L", "PZ_A5_L", "PZ_A6_L" };
        private static readonly string[] AddonShortNames = { "PZ_A1_S", "PZ_A2_S", "PZ_A3_S", "PZ_A4_S", "PZ_A5_S", "PZ_A6_S" };

        #endregion

        #region Private fields — Indicators

        private Indicators.ninZaPANAKanal              panaKanal;
        private Indicators.RenkoKings.RenkoKings_ThunderZilla thunderZilla;
        private Indicators.ninZaBarStatus              barStatus;

        // Reference level indicators
        private PriorDayOHLC    priorDayOHLC;
        private CurrentDayOHL   currentDayOHL;
        private Pivots          pivots;

        #endregion

        #region Private fields — Trade state

        private TradeState  tradeState;
        private TradeDir    tradeDir;
        private int         nextAddIndex;           // 0-5 for A1-A6
        private List<ContractInfo> activeContracts;

        // Pending add-on limit order tracking
        private Order pendingAddonOrder;

        #endregion

        #region Private fields — Reference levels

        private double overnightHigh, overnightLow;
        private double londonHigh,    londonLow;
        private double openHigh,      openLow;
        private bool   overnightTracking, londonTracking, openTracking;
        private List<double> referenceLevels;

        #endregion

        #region Private fields — Pullback counter

        private int    lastRenkoDir;     // +1 up, -1 down
        private int    pullbackCount;
        private int    dirChangeBar;     // bar index of last direction change

        #endregion

        #region Private fields — Session / Daily P&L

        private SessionIterator sessionIterator;
        private double  sessionStartCumPnl;
        private bool    sessionLocked;
        private bool    firstSession = true;
        private double  dailyPnl;
        private DateTime lastAddonTime;

        #endregion

        #region Private fields — TP velocity tracking

        private DateTime lastTpAdjustTime;
        private double   priceAtLastTpCheck;
        private double   currentVelocity;       // points per second toward TP
        private int      lastPanaSignalBar;      // bar where PANAKanal last fired +2/-2
        private int      lastTzSignalBar;        // bar where ThunderZilla last fired +3/-3

        #endregion

        #region Private fields — Signal debug strips

        private int stripLastPanaBar;
        private int stripLastPanaDir;   // +1 long, -1 short
        private int stripLastTzBar;
        private int stripLastTzDir;
        private Series<int> stripTzState;   // 0=none, 1=signal, 2=confluence
        private Series<int> stripPkState;

        // D2D brushes for strips
        private SharpDX.Direct2D1.SolidColorBrush dxStripGrayBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxStripPinkBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxStripGreenBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxStripBlueBrush;

        #endregion

        #region Private fields — Dashboard WPF (buttons only)

        private Grid chartGrid;
        private Border dashBorder;
        private StackPanel dashPanel;
        private Button btnAutoOnOff;
        private Button btnFlatten;
        private bool isAutoEnabled = true;
        private bool dashboardCreated;
        private bool isDashDragging;
        private bool dashPositionInitialized;
        private System.Windows.Point dashDragStart;
        private double dashX;
        private double dashY;
        private Window dashChartWindow;
        private int tradeCount;

        #endregion

        #region Private fields — SharpDX Dashboard

        private struct DashRow
        {
            public string Label;
            public string Value;
            public int ColorCode; // 0=white, 1=green, 2=red, 3=yellow, 4=cyan, 5=orange, 6=label(grey)
            public bool IsDivider;
            public bool IsHeader;
        }

        // Color code constants
        private const int DC_WHITE  = 0;
        private const int DC_GREEN  = 1;
        private const int DC_RED    = 2;
        private const int DC_YELLOW = 3;
        private const int DC_CYAN   = 4;
        private const int DC_ORANGE = 5;

        // D2D brushes
        private SharpDX.Direct2D1.SolidColorBrush dxBgBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxBorderBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxDividerBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxWhiteBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxGreenBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxRedBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxYellowBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxCyanBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxLabelBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxOrangeBrush;

        // DirectWrite text formats
        private SharpDX.DirectWrite.TextFormat dxLabelFont;
        private SharpDX.DirectWrite.TextFormat dxValueFont;
        private SharpDX.DirectWrite.TextFormat dxHeaderFont;

        // Cached table dimensions
        private float dashCachedTableW;
        private float dashCachedTableH;
        private float dashCachedLabelW;
        private float dashCachedValueW;

        #endregion

        #region OnStateChange

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name                        = "PanaZillaStrategy";
                Description                 = "Multi-indicator confluence strategy: PANAKanal break + ThunderZilla pullback with progressive SL, add-ons, partials, daily limits.";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;

                EntriesPerDirection          = 7;     // 1 initial + 6 max adds
                EntryHandling                = EntryHandling.UniqueEntries;
                StopTargetHandling           = StopTargetHandling.PerEntryExecution;

                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds    = 30;
                StartBehavior                = StartBehavior.WaitUntilFlat;
                TimeInForce                  = TimeInForce.Gtc;
                RealtimeErrorHandling        = RealtimeErrorHandling.IgnoreAllErrors;
                TraceOrders                  = false;
                IsInstantiatedOnEachOptimizationIteration = false;
                MaximumBarsLookBack          = MaximumBarsLookBack.Infinite;
                BarsRequiredToTrade          = 100;

                // --- Indicator params ---
                PanaKanalPeriod              = 20;
                PanaKanalFactor              = 4.0;
                PanaKanalMiddlePeriod        = 14;
                PanaKanalBreakSplit          = 20;
                PanaKanalPullbackPeriod      = 10;

                TzTrendMAType                = ThunderZillaMAType.SMA;
                TzTrendPeriod                = 100;
                TzTrendSmoothingEnabled      = false;
                TzTrendSmoothingMethod       = ThunderZillaMAType.EMA;
                TzTrendSmoothingPeriod       = 10;
                TzStopOffsetMultiplier       = 60.0;
                TzSignalQtyFlat              = 2;
                TzSignalQtyTrend             = 999;

                BarStatusBoundOffset         = 0;

                // --- Position sizing ---
                InitialQty                   = 2;
                AddonQty                     = 1;
                MaxExposureDollars           = 250.0;

                // --- Risk ---
                InitialSlTicks               = 100;
                InitialTpTicks               = 100;

                // --- Add-on ---
                AddProximityPoints           = 8.0;
                AddMinSeconds                = 10;
                MaxTotalRiskTicks            = 500;

                // --- Partial close ---
                PartialCloseTicks            = 40;
                PullbackThreshold            = 3;

                // --- Daily limits ---
                DailyProfitGoal              = 500.0;
                DailyLossLimit               = 300.0;

                // --- Debug ---
                DebugMode                    = false;

                // --- Dashboard ---
                ShowDashboard                = true;
                DashboardOpacity             = 85;

            }
            else if (State == State.Configure)
            {
                // panaKanal and barStatus use only the primary series — safe to add to chart here.
                // AddChartIndicator() must be called in State.Configure per NT8 requirements.
                panaKanal = ninZaPANAKanal(PanaKanalPeriod, PanaKanalFactor, PanaKanalMiddlePeriod, PanaKanalBreakSplit, PanaKanalPullbackPeriod);
                AddChartIndicator(panaKanal);

                barStatus = ninZaBarStatus(BarStatusBoundOffset);
                AddChartIndicator(barStatus);
            }
            else if (State == State.DataLoaded)
            {
                // ThunderZilla: manual bootstrap first (SetInput pins it to the primary series,
                // preventing NT8's framework from registering secondary data series that would
                // have insufficient bar history at startup). AddChartIndicator is then safe to
                // call on the already-initialised instance — it registers it for rendering only,
                // without re-triggering secondary series creation.
                thunderZilla = CreateThunderZillaIndicator();
                // Reference level indicators — data sources only (no AddChartIndicator).
                // PriorDayOHLC, CurrentDayOHL, and Pivots internally use a secondary daily series.
                // When added via AddChartIndicator, NT8 creates that series, which may have fewer
                // bars than expected at bar 100, triggering an out-of-range error.
                priorDayOHLC  = PriorDayOHLC();
                currentDayOHL = CurrentDayOHL();
                pivots        = Pivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20);

                // Initialize collections
                activeContracts = new List<ContractInfo>();
                referenceLevels = new List<double>();
                lastAddonTime   = DateTime.MinValue;

                // Signal debug strip series
                stripTzState = new Series<int>(this);
                stripPkState = new Series<int>(this);

                // Create WPF dashboard on UI thread
                if (ShowDashboard && ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        try { CreateDashboard(); dashboardCreated = true; }
                        catch (Exception ex) { Print("PZ Dashboard error: " + ex.Message); }
                    });
                }
            }
            else if (State == State.Historical)
            {
                sessionIterator = new SessionIterator(Bars);
            }
            else if (State == State.Realtime)
            {
                // Re-snapshot so only real-time trades count
                sessionStartCumPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                dailyPnl           = 0;
                sessionLocked      = false;

                // Reset stale state from historical processing
                if (Position.MarketPosition == MarketPosition.Flat)
                    ResetTradeState();
                pullbackCount = 0;
                lastRenkoDir  = 0;
                tradeCount    = 0;
            }
            else if (State == State.Terminated)
            {
                DisposeDxResources();

                if (ChartControl != null && dashboardCreated)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        try { DisposeDashboard(); dashboardCreated = false; }
                        catch { }
                    });
                }
            }
        }

        #endregion

        #region Indicator helpers

        private Indicators.RenkoKings.RenkoKings_ThunderZilla CreateThunderZillaIndicator()
        {
            Indicators.RenkoKings.RenkoKings_ThunderZilla indicator = new Indicators.RenkoKings.RenkoKings_ThunderZilla
            {
                TrendMAType              = TzTrendMAType,
                TrendPeriod              = TzTrendPeriod,
                TrendSmoothingEnabled    = TzTrendSmoothingEnabled,
                TrendSmoothingMethod     = TzTrendSmoothingMethod,
                TrendSmoothingPeriod     = TzTrendSmoothingPeriod,
                StopOffsetMultiplierStop = TzStopOffsetMultiplier,
                SignalQuantityPerFlat    = TzSignalQtyFlat,
                SignalQuantityPerTrend   = TzSignalQtyTrend
            };

            indicator.Parent = this;
            indicator.SetState(State.Configure);
            indicator.SetInput(Input);

            lock (NinjaScripts)
                NinjaScripts.Add(indicator);

            // Register with chart BEFORE DataLoaded so indicator's isCharting=true
            // (ThunderZilla sets isCharting = ChartControl != null in DataLoaded;
            //  without chart registration first, ChartControl is null → no marker rendering)
            if (ChartControl != null)
                AddChartIndicator(indicator);

            indicator.SetState(State.DataLoaded);
            return indicator;
        }

        #endregion

        #region OnBarUpdate

        protected override void OnBarUpdate()
        {
            // 1. Guard: warmup
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // 2. Session management — detect new session
            if (Bars.IsFirstBarOfSession || firstSession)
            {
                firstSession = false;
                sessionIterator.GetNextSession(Time[0], true);

                sessionStartCumPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                dailyPnl           = 0;
                sessionLocked      = false;

                // Reset session-level tracking
                overnightHigh    = double.MinValue;
                overnightLow     = double.MaxValue;
                londonHigh       = double.MinValue;
                londonLow        = double.MaxValue;
                openHigh         = double.MinValue;
                openLow          = double.MaxValue;
                overnightTracking = false;
                londonTracking    = false;
                openTracking      = false;

                pullbackCount    = 0;
                lastRenkoDir     = 0;
                tradeCount       = 0;

                // Reset stuck trade state at session boundary (handles rejected/stale entries from prior session)
                if (Position.MarketPosition == MarketPosition.Flat && tradeState != TradeState.Flat)
                    ResetTradeState();

                if (DebugMode)
                    Print(string.Format("{0} [PZ] New session — cumPnl baseline={1:F2}", Time[0], sessionStartCumPnl));
            }
            else
            {
                // 3. Update daily P&L
                dailyPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumPnl;

                // Include unrealized P&L for risk checks
                if (Position.MarketPosition != MarketPosition.Flat)
                    dailyPnl += Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);
            }

            // Signal debug strips — update every bar regardless of session lock
            UpdateSignalStrips();

            // 4. Check daily limits
            if (!sessionLocked && CheckDailyLimits())
                return;

            // 5. Track session levels (Overnight, London, Open H/L)
            TrackSessionLevels();

            // 6. Update sorted reference levels
            UpdateReferenceLevels();

            // 7. Update pullback counter
            UpdatePullbackCounter();

            // 8. If session locked, only manage existing positions
            if (sessionLocked)
            {
                if (tradeState == TradeState.Managing)
                {
                    double panaTrail = panaKanal.TrailingStop[0];
                    ManageStopLoss(panaTrail);
                    ManageTakeProfit();
                }
                return;
            }

            // 9. Read signals + track last signal bars for proximity matching
            double panaSignal = panaKanal.Signal_Trade[0];
            double tzSignal   = thunderZilla.Signal_Trade[0];
            double panaTrailingStop = panaKanal.TrailingStop[0];

            if (Math.Abs(panaSignal) == 2) lastPanaSignalBar = CurrentBar;
            if (Math.Abs(tzSignal) == 3)   lastTzSignalBar = CurrentBar;

            if (DebugMode)
            {
                // Print every bar in realtime, every 50 bars in historical
                bool shouldPrint = (State == State.Realtime) || (CurrentBar % 50 == 0);
                // Always print when any signal is non-zero
                if (panaSignal != 0 || tzSignal != 0) shouldPrint = true;

                if (shouldPrint)
                    Print(string.Format("{0} [PZ] bar={1} state={2} dir={3} pana={4} tz={5} pnl=${6:F2} auto={7} locked={8} lastPana={9} lastTz={10}",
                        Time[0], CurrentBar, tradeState, tradeDir, panaSignal, tzSignal, dailyPnl,
                        isAutoEnabled, sessionLocked, lastPanaSignalBar, lastTzSignalBar));
            }

            // 10. State dispatch
            switch (tradeState)
            {
                case TradeState.Flat:
                    CheckEntry(panaSignal, tzSignal);
                    break;

                case TradeState.Managing:
                    ManageStopLoss(panaTrailingStop);
                    ManageTakeProfit();
                    CheckAddonOpportunity();
                    CheckPartialClose();
                    break;

                case TradeState.Exiting:
                    FlattenAll("PZ_Exit");
                    break;
            }

            // 11. Visual SL/TP lines
            if (tradeState == TradeState.Managing && activeContracts != null && activeContracts.Count > 0)
            {
                var initContract = activeContracts.FirstOrDefault(c => c.IsActive && c.IsFilled);
                if (initContract != null)
                {
                    if (initContract.CurrentStopPrice > 0)
                        Draw.HorizontalLine(this, "PZ_SL_Line", initContract.CurrentStopPrice, Brushes.Red, DashStyleHelper.Dash, 2);
                    if (initContract.CurrentTpPrice > 0)
                        Draw.HorizontalLine(this, "PZ_TP_Line", initContract.CurrentTpPrice, Brushes.LimeGreen, DashStyleHelper.Dash, 2);
                }
            }
            else
            {
                RemoveDrawObject("PZ_SL_Line");
                RemoveDrawObject("PZ_TP_Line");
            }

            // 12. Force chart repaint for SharpDX dashboard
            if (ShowDashboard && ChartControl != null)
            {
                try { ChartControl.InvalidateVisual(); }
                catch { }
            }

            // 13. Safe flat detection — only if position has been flat for 3+ bars
            // (gives time for async fills, prevents premature reset on entry bar)
            if (tradeState != TradeState.Flat && Position.MarketPosition == MarketPosition.Flat)
            {
                bool hasActiveFilled = activeContracts != null && activeContracts.Any(c => c.IsActive && c.IsFilled);
                bool hasPendingEntry = activeContracts != null && activeContracts.Any(c => c.IsActive && !c.IsFilled);
                if (!hasActiveFilled && !hasPendingEntry)
                {
                    ResetTradeState();
                }
            }
        }

        #endregion

        #region Entry Logic

        private void CheckEntry(double panaSignal, double tzSignal)
        {
            if (!isAutoEnabled)
                return;
            if (Position.MarketPosition != MarketPosition.Flat)
                return;

            const int signalProximityBars = 3; // signals can be within 3 bars of each other

            // Check if PANAKanal break fired recently (current bar or within lookback)
            bool panaBreakLong  = (panaSignal == 2)  || (lastPanaSignalBar > 0 && CurrentBar - lastPanaSignalBar <= signalProximityBars && panaKanal.Signal_Trade[CurrentBar - lastPanaSignalBar] == 2);
            bool panaBreakShort = (panaSignal == -2) || (lastPanaSignalBar > 0 && CurrentBar - lastPanaSignalBar <= signalProximityBars && panaKanal.Signal_Trade[CurrentBar - lastPanaSignalBar] == -2);

            // Check if ThunderZilla pullback fired recently
            bool tzPullLong  = (tzSignal == 3)  || (lastTzSignalBar > 0 && CurrentBar - lastTzSignalBar <= signalProximityBars && thunderZilla.Signal_Trade[CurrentBar - lastTzSignalBar] == 3);
            bool tzPullShort = (tzSignal == -3) || (lastTzSignalBar > 0 && CurrentBar - lastTzSignalBar <= signalProximityBars && thunderZilla.Signal_Trade[CurrentBar - lastTzSignalBar] == -3);

            // Long confluence: PANAKanal +2 AND ThunderZilla +3 (same bar or within proximity)
            if (panaBreakLong && tzPullLong)
            {
                // Set SL/TP BEFORE entry
                SetStopLoss(InitLong, CalculationMode.Ticks, InitialSlTicks, false);
                SetProfitTarget(InitLong, CalculationMode.Ticks, InitialTpTicks);

                EnterLong(InitialQty, InitLong);

                tradeState = TradeState.Managing;
                tradeDir   = TradeDir.Long;
                nextAddIndex = 0;
                pullbackCount = 0;
                lastRenkoDir = 0;

                // Add pending (unfilled) contract so flat detection doesn't reset immediately
                activeContracts.Add(new ContractInfo
                {
                    SignalName = InitLong, Quantity = InitialQty,
                    IsActive = true, IsFilled = false
                });

                if (DebugMode)
                    Print(string.Format("{0} [PZ] LONG ENTRY: pana={1} tz={2} qty={3}",
                        Time[0], panaSignal, tzSignal, InitialQty));
            }
            // Short confluence: PANAKanal -2 AND ThunderZilla -3 (same bar or within proximity)
            else if (panaBreakShort && tzPullShort)
            {
                SetStopLoss(InitShort, CalculationMode.Ticks, InitialSlTicks, false);
                SetProfitTarget(InitShort, CalculationMode.Ticks, InitialTpTicks);

                EnterShort(InitialQty, InitShort);

                tradeState = TradeState.Managing;
                tradeDir   = TradeDir.Short;
                nextAddIndex = 0;
                pullbackCount = 0;
                lastRenkoDir = 0;

                activeContracts.Add(new ContractInfo
                {
                    SignalName = InitShort, Quantity = InitialQty,
                    IsActive = true, IsFilled = false
                });

                if (DebugMode)
                    Print(string.Format("{0} [PZ] SHORT ENTRY: pana={1} tz={2} qty={3}",
                        Time[0], panaSignal, tzSignal, InitialQty));
            }
        }

        #endregion

        #region Signal Debug Strips

        private void UpdateSignalStrips()
        {
            double panaSignal = panaKanal.Signal_Trade[0];
            double tzSignal   = thunderZilla.Signal_Trade[0];

            // Track last ACTIVE bar for each signal (matches entry logic's lastPanaSignalBar behavior)
            if (Math.Abs(panaSignal) == 2) { stripLastPanaBar = CurrentBar; stripLastPanaDir = panaSignal > 0 ? 1 : -1; }
            if (Math.Abs(tzSignal) == 3)   { stripLastTzBar = CurrentBar;   stripLastTzDir   = tzSignal > 0 ? 1 : -1; }

            // Edge detection: only the FIRST bar of each signal burst (transition from inactive)
            bool panaEdge = Math.Abs(panaSignal) == 2 && (CurrentBar == 0 || Math.Abs(panaKanal.Signal_Trade[1]) != 2);
            bool tzEdge   = Math.Abs(tzSignal) == 3   && (CurrentBar == 0 || Math.Abs(thunderZilla.Signal_Trade[1]) != 3);

            // Confluence: both signals active within 3 bars (matches CheckEntry lookback)
            const int lookback = 3;
            bool panaRecent = stripLastPanaBar > 0 && CurrentBar - stripLastPanaBar <= lookback;
            bool tzRecent   = stripLastTzBar > 0   && CurrentBar - stripLastTzBar <= lookback;
            bool confluence = panaRecent && tzRecent && stripLastPanaDir == stripLastTzDir;

            // Store state: 0=grey, 1=signal edge (pink/green), 2=confluence (blue)
            stripTzState[0] = confluence ? 2 : (tzEdge ? 1 : 0);
            stripPkState[0] = confluence ? 2 : (panaEdge ? 1 : 0);
        }

        private void RenderSignalStrips(ChartControl chartControl)
        {
            if (dxStripGrayBrush == null || ChartBars == null)
                return;

            int fromIdx = ChartBars.FromIndex;
            int toIdx   = ChartBars.ToIndex;
            if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx)
                return;

            float panelBottom = ChartPanel.Y + ChartPanel.H;
            float stripH = 7f;
            float gap = 2f;
            float tzY = panelBottom - stripH;               // TZ strip (bottom)
            float pkY = panelBottom - stripH * 2 - gap;     // PK strip (above TZ)

            for (int i = fromIdx; i <= toIdx; i++)
            {
                if (i < 0 || i > CurrentBar)
                    continue;

                int barsAgo = CurrentBar - i;
                if (barsAgo < 0 || barsAgo >= stripTzState.Count)
                    continue;

                float barX = chartControl.GetXByBarIndex(ChartBars, i);
                float halfW = (float)chartControl.BarWidth / 2f + 0.5f;
                float x1 = barX - halfW;
                float x2 = barX + halfW;

                // TZ strip
                int tzVal = stripTzState.GetValueAt(i);
                SharpDX.Direct2D1.SolidColorBrush tzBrush =
                    tzVal == 2 ? dxStripBlueBrush :
                    tzVal == 1 ? dxStripPinkBrush : dxStripGrayBrush;
                RenderTarget.FillRectangle(new RectangleF(x1, tzY, x2 - x1, stripH), tzBrush);

                // PK strip
                int pkVal = stripPkState.GetValueAt(i);
                SharpDX.Direct2D1.SolidColorBrush pkBrush =
                    pkVal == 2 ? dxStripBlueBrush :
                    pkVal == 1 ? dxStripGreenBrush : dxStripGrayBrush;
                RenderTarget.FillRectangle(new RectangleF(x1, pkY, x2 - x1, stripH), pkBrush);
            }

            // Labels on the left edge
            float labelX = ChartPanel.X + 2f;
            using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Consolas", 8f))
            {
                using (var lay = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, "TZ", fmt, 30f, stripH))
                    RenderTarget.DrawTextLayout(new Vector2(labelX, tzY), lay, dxWhiteBrush);
                using (var lay = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, "PK", fmt, 30f, stripH))
                    RenderTarget.DrawTextLayout(new Vector2(labelX, pkY), lay, dxWhiteBrush);
            }
        }

        #endregion

        #region Phase 2 — Progressive SL Trailing

        private void ManageStopLoss(double panaTrailingStop)
        {
            if (activeContracts == null || activeContracts.Count == 0)
                return;

            double tickSize  = TickSize;
            double tickValue = Instrument.MasterInstrument.PointValue * tickSize; // $0.50 for MNQ

            foreach (var ci in activeContracts)
            {
                if (!ci.IsActive || !ci.IsFilled)
                    continue;

                // Only manage contracts matching current trade direction
                bool ciIsLong = ci.SignalName.EndsWith("_L");
                if (tradeDir == TradeDir.Long && !ciIsLong) continue;
                if (tradeDir == TradeDir.Short && ciIsLong) continue;

                double entryPrice = ci.EntryPrice;
                double currentPrice = Close[0];
                bool isLong = tradeDir == TradeDir.Long;

                // Ticks of profit
                double profitTicks = isLong
                    ? (currentPrice - entryPrice) / tickSize
                    : (entryPrice - currentPrice) / tickSize;

                double newStop = ci.CurrentStopPrice;

                // Base trail: use panaKanal.TrailingStop (only tighten)
                if (panaTrailingStop > 0)
                {
                    if (isLong && panaTrailingStop > newStop)
                        newStop = panaTrailingStop;
                    else if (!isLong && panaTrailingStop < newStop && panaTrailingStop > 0)
                        newStop = panaTrailingStop;
                }

                // +80 tick rule: when profit >= 80t, SL must be within 80 ticks of ENTRY (max 80t risk)
                if (profitTicks >= 80)
                {
                    double maxRiskStop = isLong
                        ? entryPrice - 80 * tickSize
                        : entryPrice + 80 * tickSize;

                    // Tighten to max 80 ticks risk from entry
                    if (isLong && maxRiskStop > newStop)
                        newStop = maxRiskStop;
                    else if (!isLong && maxRiskStop < newStop)
                        newStop = maxRiskStop;
                }

                // +100 tick rule: minimum BE + 20 ticks
                if (profitTicks >= 100)
                {
                    double bePlus20 = isLong
                        ? entryPrice + 20 * tickSize
                        : entryPrice - 20 * tickSize;

                    if (isLong && bePlus20 > newStop)
                        newStop = bePlus20;
                    else if (!isLong && bePlus20 < newStop)
                        newStop = bePlus20;
                }

                // Only tighten — never loosen
                bool shouldUpdate = false;
                if (isLong && newStop > ci.CurrentStopPrice)
                    shouldUpdate = true;
                else if (!isLong && newStop < ci.CurrentStopPrice)
                    shouldUpdate = true;

                if (shouldUpdate)
                {
                    // Validate stop is on correct side of market
                    double mktPrice = Close[0];
                    bool validStop = isLong ? (newStop < mktPrice) : (newStop > mktPrice);
                    if (!validStop)
                    {
                        if (DebugMode)
                            Print(string.Format("{0} [PZ] SL SKIP {1}: newStop={2:F2} would be on wrong side of market={3:F2}",
                                Time[0], ci.SignalName, newStop, mktPrice));
                        continue;
                    }

                    ci.CurrentStopPrice = newStop;
                    SetStopLoss(ci.SignalName, CalculationMode.Price, newStop, false);

                    if (DebugMode)
                        Print(string.Format("{0} [PZ] SL update {1}: new={2:F2} profit={3:F0}t",
                            Time[0], ci.SignalName, newStop, profitTicks));
                }
            }
        }

        #endregion

        #region Phase 3 — Reference Levels + TP

        private void TrackSessionLevels()
        {
            TimeSpan tod = Time[0].TimeOfDay;
            double h = High[0];
            double l = Low[0];

            // Overnight: 18:00 - 09:30
            if (tod >= new TimeSpan(18, 0, 0) || tod < new TimeSpan(9, 30, 0))
            {
                overnightTracking = true;
                if (h > overnightHigh) overnightHigh = h;
                if (l < overnightLow)  overnightLow  = l;
            }
            else
            {
                overnightTracking = false;
            }

            // London: 03:00 - 08:00
            if (tod >= new TimeSpan(3, 0, 0) && tod < new TimeSpan(8, 0, 0))
            {
                londonTracking = true;
                if (h > londonHigh) londonHigh = h;
                if (l < londonLow)  londonLow  = l;
            }
            else
            {
                londonTracking = false;
            }

            // Open range: 09:30 - 10:00
            if (tod >= new TimeSpan(9, 30, 0) && tod < new TimeSpan(10, 0, 0))
            {
                openTracking = true;
                if (h > openHigh) openHigh = h;
                if (l < openLow)  openLow  = l;
            }
            else
            {
                openTracking = false;
            }
        }

        private void UpdateReferenceLevels()
        {
            referenceLevels.Clear();

            // Prior day
            if (priorDayOHLC.PriorHigh[0] > 0)  referenceLevels.Add(priorDayOHLC.PriorHigh[0]);
            if (priorDayOHLC.PriorLow[0] > 0)   referenceLevels.Add(priorDayOHLC.PriorLow[0]);

            // Current day open
            if (currentDayOHL.CurrentOpen[0] > 0) referenceLevels.Add(currentDayOHL.CurrentOpen[0]);

            // Pivots
            if (pivots.Pp[0] > 0)  referenceLevels.Add(pivots.Pp[0]);
            if (pivots.R1[0] > 0)  referenceLevels.Add(pivots.R1[0]);
            if (pivots.R2[0] > 0)  referenceLevels.Add(pivots.R2[0]);
            if (pivots.S1[0] > 0)  referenceLevels.Add(pivots.S1[0]);
            if (pivots.S2[0] > 0)  referenceLevels.Add(pivots.S2[0]);

            // Session H/L (only add if they've been set)
            if (overnightHigh > double.MinValue) referenceLevels.Add(overnightHigh);
            if (overnightLow  < double.MaxValue) referenceLevels.Add(overnightLow);
            if (londonHigh    > double.MinValue) referenceLevels.Add(londonHigh);
            if (londonLow     < double.MaxValue) referenceLevels.Add(londonLow);
            if (openHigh      > double.MinValue) referenceLevels.Add(openHigh);
            if (openLow       < double.MaxValue) referenceLevels.Add(openLow);

            referenceLevels.Sort();
        }

        private double GetNextLevelAbove(double price)
        {
            for (int i = 0; i < referenceLevels.Count; i++)
            {
                if (referenceLevels[i] > price + TickSize)
                    return referenceLevels[i];
            }
            return 0; // no level found
        }

        private double GetNextLevelBelow(double price)
        {
            for (int i = referenceLevels.Count - 1; i >= 0; i--)
            {
                if (referenceLevels[i] < price - TickSize)
                    return referenceLevels[i];
            }
            return 0; // no level found
        }

        private void ManageTakeProfit()
        {
            if (activeContracts == null || activeContracts.Count == 0)
                return;

            double tickSize = TickSize;
            bool isLong = tradeDir == TradeDir.Long;
            double currentPrice = Close[0];

            // --- Velocity tracking ---
            DateTime now = (State == State.Realtime) ? DateTime.Now : Time[0];
            double elapsedSec = (now - lastTpAdjustTime).TotalSeconds;

            // Compute velocity: points moved per second since last check
            if (elapsedSec > 0 && priceAtLastTpCheck > 0)
            {
                double pointsMoved = Math.Abs(currentPrice - priceAtLastTpCheck);
                currentVelocity = pointsMoved / elapsedSec;
            }

            // Adaptive interval: fast market = 2s, slow market = 5s
            double adjustIntervalSec = (currentVelocity > 2.0) ? 2.0 : (currentVelocity > 0.5) ? 3.0 : 5.0;
            bool tpAdjustAllowed = elapsedSec >= adjustIntervalSec;

            if (tpAdjustAllowed)
            {
                lastTpAdjustTime = now;
                priceAtLastTpCheck = currentPrice;
            }

            foreach (var ci in activeContracts)
            {
                if (!ci.IsActive || !ci.IsFilled)
                    continue;

                // Only manage contracts matching current trade direction
                bool ciIsLong = ci.SignalName.EndsWith("_L");
                if (isLong && !ciIsLong) continue;
                if (!isLong && ciIsLong) continue;

                // --- TP adjustment (velocity-gated, incremental) ---
                if (tpAdjustAllowed)
                {
                    double minTpDist = InitialTpTicks * tickSize;
                    double nextLevel;

                    if (pullbackCount < PullbackThreshold)
                    {
                        // Clean move (< 3 pullbacks): push TP to next level ABOVE current price
                        // (one level at a time — incremental, not leapfrog)
                        nextLevel = isLong ? GetNextLevelAbove(currentPrice) : GetNextLevelBelow(currentPrice);

                        // Only push if the new level is FURTHER than current TP
                        if (nextLevel > 0 && ci.CurrentTpPrice > 0)
                        {
                            bool isFurther = isLong ? (nextLevel > ci.CurrentTpPrice) : (nextLevel < ci.CurrentTpPrice);
                            if (!isFurther)
                                nextLevel = 0; // don't move TP backward during clean run
                        }
                    }
                    else
                    {
                        // Stalling (3+ pullbacks): bring TP IN to nearest level ahead of price
                        nextLevel = isLong ? GetNextLevelAbove(currentPrice) : GetNextLevelBelow(currentPrice);

                        // Only pull if closer than current TP
                        if (nextLevel > 0 && ci.CurrentTpPrice > 0)
                        {
                            bool isCloser = isLong ? (nextLevel < ci.CurrentTpPrice) : (nextLevel > ci.CurrentTpPrice);
                            if (!isCloser)
                                nextLevel = 0; // don't push TP further during stall
                        }
                    }

                    if (nextLevel > 0)
                    {
                        double distFromEntry = isLong ? (nextLevel - ci.EntryPrice) : (ci.EntryPrice - nextLevel);

                        if (distFromEntry >= minTpDist && nextLevel != ci.CurrentTpPrice)
                        {
                            if (DebugMode)
                                Print(string.Format("{0} [PZ] TP {1} {2}: old={3:F2} new={4:F2} vel={5:F2}pt/s pullbacks={6}",
                                    Time[0], pullbackCount < PullbackThreshold ? "PUSH" : "PULL",
                                    ci.SignalName, ci.CurrentTpPrice, nextLevel, currentVelocity, pullbackCount));

                            ci.CurrentTpPrice = nextLevel;
                            SetProfitTarget(ci.SignalName, CalculationMode.Price, nextLevel);
                        }
                    }
                }

                // --- Near-TP SL tightening: within 80 ticks of TP AND at least 80 ticks profit ---
                if (ci.CurrentTpPrice > 0)
                {
                    double distToTp = isLong
                        ? (ci.CurrentTpPrice - currentPrice) / tickSize
                        : (currentPrice - ci.CurrentTpPrice) / tickSize;
                    double profitTicks = isLong
                        ? (currentPrice - ci.EntryPrice) / tickSize
                        : (ci.EntryPrice - currentPrice) / tickSize;

                    if (distToTp <= 80 && distToTp > 0 && profitTicks >= 80)
                    {
                        double bePlus20 = isLong
                            ? ci.EntryPrice + 20 * tickSize
                            : ci.EntryPrice - 20 * tickSize;

                        bool shouldTighten = isLong
                            ? bePlus20 > ci.CurrentStopPrice
                            : bePlus20 < ci.CurrentStopPrice;

                        if (shouldTighten)
                        {
                            bool validStop = isLong ? (bePlus20 < currentPrice) : (bePlus20 > currentPrice);
                            if (validStop)
                            {
                                ci.CurrentStopPrice = bePlus20;
                                SetStopLoss(ci.SignalName, CalculationMode.Price, bePlus20, false);

                                if (DebugMode)
                                    Print(string.Format("{0} [PZ] Near-TP SL tighten {1}: SL={2:F2} (BE+20)", Time[0], ci.SignalName, bePlus20));
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Phase 4 — Add-on Manager

        private void CheckAddonOpportunity()
        {
            // Guards
            if (nextAddIndex >= 6)
                return;
            if (sessionLocked)
                return;
            if (tradeState != TradeState.Managing)
                return;
            if (tradeDir == TradeDir.None)
                return;

            // Pacing: 10 seconds between add-ons
            DateTime now = State == State.Realtime ? DateTime.Now : Time[0];
            if ((now - lastAddonTime).TotalSeconds < AddMinSeconds)
                return;

            // Risk budget check
            if (ComputeTotalRiskTicks() + InitialSlTicks > MaxTotalRiskTicks)
                return;

            // Proximity check: Close within AddProximityPoints of weighted avg entry
            double avgEntry = GetWeightedAvgEntry();
            if (avgEntry <= 0)
                return;

            double dist = Math.Abs(Close[0] - avgEntry);
            if (dist > AddProximityPoints)
                return;

            // Price must be at or past entry in favorable direction, OR in a small pullback/stall
            bool isLongDir = tradeDir == TradeDir.Long;
            bool priceFavorable = isLongDir ? (Close[0] >= avgEntry) : (Close[0] <= avgEntry);
            bool hasStall = HasCounterTrendBar(3);
            if (!priceFavorable && !hasStall)
                return;

            string signalName = isLongDir ? AddonLongNames[nextAddIndex] : AddonShortNames[nextAddIndex];

            // Set SL/TP for the add-on before entry
            SetStopLoss(signalName, CalculationMode.Ticks, InitialSlTicks, false);
            SetProfitTarget(signalName, CalculationMode.Ticks, InitialTpTicks);

            // Place stop-market at next Renko bar boundary (continuation direction)
            // Long: buy stop at UpperBound (triggers when price rises to next up bar)
            // Short: sell stop at LowerBound (triggers when price drops to next down bar)
            // Enter add-on at market — each Renko bar close IS a boundary event.
            // Conditions already verified: proximity, pacing, risk budget, favorable price.
            if (isLongDir)
                EnterLong(AddonQty, signalName);
            else
                EnterShort(AddonQty, signalName);

            lastAddonTime = now;

            if (DebugMode)
                Print(string.Format("{0} [PZ] ADD-ON {1}: market @ {2:F2} qty={3} riskTicks={4}",
                    Time[0], signalName, Close[0], AddonQty, ComputeTotalRiskTicks()));
        }

        private double GetWeightedAvgEntry()
        {
            if (activeContracts == null || activeContracts.Count == 0)
                return 0;

            double totalValue = 0;
            int totalQty = 0;

            foreach (var ci in activeContracts)
            {
                if (!ci.IsActive || !ci.IsFilled)
                    continue;
                totalValue += ci.EntryPrice * ci.Quantity;
                totalQty   += ci.Quantity;
            }

            return totalQty > 0 ? totalValue / totalQty : 0;
        }

        private int ComputeTotalRiskTicks()
        {
            if (activeContracts == null)
                return 0;

            int totalRisk = 0;
            bool isLong = tradeDir == TradeDir.Long;

            foreach (var ci in activeContracts)
            {
                if (!ci.IsActive)
                    continue;

                if (ci.IsFilled)
                {
                    // Filled: risk = distance from entry to current stop
                    double riskPerContract = isLong
                        ? (ci.EntryPrice - ci.CurrentStopPrice) / TickSize
                        : (ci.CurrentStopPrice - ci.EntryPrice) / TickSize;

                    if (riskPerContract > 0)
                        totalRisk += (int)(riskPerContract * ci.Quantity);
                }
                else
                {
                    // Pending (unfilled): assume worst case InitialSlTicks risk
                    totalRisk += InitialSlTicks * ci.Quantity;
                }
            }

            return totalRisk;
        }

        private bool HasCounterTrendBar(int lookback)
        {
            bool isLong = tradeDir == TradeDir.Long;

            for (int i = 0; i < lookback && i <= CurrentBar; i++)
            {
                bool isBearish = Close[i] < Open[i];
                bool isBullish = Close[i] > Open[i];

                if (isLong && isBearish)
                    return true;
                if (!isLong && isBullish)
                    return true;
            }

            return false;
        }

        #endregion

        #region Phase 5 — Partial Close

        private void UpdatePullbackCounter()
        {
            // Direction of current Renko bar
            int dir = 0;
            if (Close[0] > Open[0]) dir = 1;
            else if (Close[0] < Open[0]) dir = -1;

            if (dir != 0 && dir != lastRenkoDir)
            {
                if (lastRenkoDir != 0)
                {
                    pullbackCount++;
                    dirChangeBar = CurrentBar;
                }
                lastRenkoDir = dir;
            }
        }

        private void CheckPartialClose()
        {
            if (pullbackCount < PullbackThreshold)
                return;
            if (activeContracts == null || activeContracts.Count == 0)
                return;

            bool isLong = tradeDir == TradeDir.Long;
            double tickSize = TickSize;
            double currentPrice = Close[0];

            // Find contracts with 40+ tick profit and exit them
            foreach (var ci in activeContracts.ToList()) // ToList for safe modification
            {
                if (!ci.IsActive || !ci.IsFilled)
                    continue;

                // Only manage contracts matching current trade direction
                bool ciIsLong = ci.SignalName.EndsWith("_L");
                if (isLong && !ciIsLong) continue;
                if (!isLong && ciIsLong) continue;

                double profitTicks = isLong
                    ? (currentPrice - ci.EntryPrice) / tickSize
                    : (ci.EntryPrice - currentPrice) / tickSize;

                if (profitTicks >= PartialCloseTicks)
                {
                    if (isLong)
                        ExitLong(ci.Quantity, "PZ_Partial", ci.SignalName);
                    else
                        ExitShort(ci.Quantity, "PZ_Partial", ci.SignalName);

                    ci.IsActive = false;

                    if (DebugMode)
                        Print(string.Format("{0} [PZ] PARTIAL CLOSE {1}: profit={2:F0}t pullbacks={3}",
                            Time[0], ci.SignalName, profitTicks, pullbackCount));
                }
            }
        }

        #endregion

        #region Phase 6 — Daily Limits + Session

        /// <summary>
        /// Returns true if session is now locked (caller should return early).
        /// </summary>
        private bool CheckDailyLimits()
        {
            dailyPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumPnl;
            if (Position.MarketPosition != MarketPosition.Flat)
                dailyPnl += Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);

            if (dailyPnl >= DailyProfitGoal)
            {
                if (!sessionLocked)
                {
                    sessionLocked = true;
                    if (DebugMode)
                        Print(string.Format("{0} [PZ] DAILY PROFIT GOAL reached: ${1:F2} >= ${2:F2}",
                            Time[0], dailyPnl, DailyProfitGoal));
                    FlattenAll("PZ_DailyProfit");
                }
                return true;
            }

            if (dailyPnl <= -DailyLossLimit)
            {
                if (!sessionLocked)
                {
                    sessionLocked = true;
                    if (DebugMode)
                        Print(string.Format("{0} [PZ] DAILY LOSS LIMIT hit: ${1:F2} <= -${2:F2}",
                            Time[0], dailyPnl, DailyLossLimit));
                    FlattenAll("PZ_DailyLoss");
                }
                return true;
            }

            return false;
        }

        private void FlattenAll(string reason)
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                foreach (var ci in activeContracts)
                {
                    if (ci.IsActive)
                        ExitLong(ci.Quantity, reason, ci.SignalName);
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                foreach (var ci in activeContracts)
                {
                    if (ci.IsActive)
                        ExitShort(ci.Quantity, reason, ci.SignalName);
                }
            }

            tradeState = TradeState.Exiting;
        }

        #endregion

        #region OnExecutionUpdate

        protected override void OnExecutionUpdate(Execution execution, string executionId,
            double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution == null || execution.Order == null)
                return;

            string name = execution.Order.Name;
            OrderAction action = execution.Order.OrderAction;

            // Track entry fills in ContractInfo list
            if (action == OrderAction.Buy || action == OrderAction.SellShort)
            {
                // Check if this is one of our signal names
                if (IsOurSignal(name))
                {
                    double fillPrice = execution.Order.AverageFillPrice;

                    // Check if we already track this signal
                    var existing = activeContracts.FirstOrDefault(c => c.SignalName == name);
                    if (existing != null)
                    {
                        existing.EntryPrice = fillPrice;
                        existing.Quantity   = quantity;
                        existing.FillTime   = time;
                        existing.IsFilled   = true;
                        existing.IsActive   = true;

                        // Initialize stop/tp from entry
                        bool isLong = action == OrderAction.Buy;
                        existing.CurrentStopPrice = isLong
                            ? fillPrice - InitialSlTicks * TickSize
                            : fillPrice + InitialSlTicks * TickSize;
                        existing.CurrentTpPrice = isLong
                            ? fillPrice + InitialTpTicks * TickSize
                            : fillPrice - InitialTpTicks * TickSize;
                    }
                    else
                    {
                        bool isLong = action == OrderAction.Buy;
                        var ci = new ContractInfo
                        {
                            SignalName      = name,
                            Quantity        = quantity,
                            EntryPrice      = fillPrice,
                            FillTime        = time,
                            IsActive        = true,
                            IsFilled        = true,
                            CurrentStopPrice = isLong
                                ? fillPrice - InitialSlTicks * TickSize
                                : fillPrice + InitialSlTicks * TickSize,
                            CurrentTpPrice = isLong
                                ? fillPrice + InitialTpTicks * TickSize
                                : fillPrice - InitialTpTicks * TickSize,
                        };
                        activeContracts.Add(ci);
                    }

                    // Track trade count: only initial entries count as new trades
                    if (name == InitLong || name == InitShort)
                        tradeCount++;

                    // Advance add index if this was an add-on fill
                    if (name.StartsWith("PZ_A"))
                        nextAddIndex = Math.Min(nextAddIndex + 1, 6);

                    if (DebugMode)
                        Print(string.Format("{0} [PZ] FILL: {1} @ {2:F2} qty={3}", time, name, fillPrice, quantity));
                }
            }

            // Detect position fully closed
            if (tradeState != TradeState.Flat && marketPosition == MarketPosition.Flat)
            {
                dailyPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumPnl;

                if (DebugMode)
                    Print(string.Format("{0} [PZ] Position FLAT. Daily PnL=${1:F2}", time, dailyPnl));

                ResetTradeState();

                // Check daily limits after close
                CheckDailyLimits();
            }
        }

        #endregion

        #region OnOrderUpdate

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice,
            int quantity, int filled, double averageFillPrice, OrderState orderState,
            DateTime time, ErrorCode error, string comment)
        {
            if (order == null)
                return;

            // Handle rejections
            if (orderState == OrderState.Rejected)
            {
                Print(string.Format("{0} [PZ] ORDER REJECTED: {1} | Action={2} | Type={3} | Limit={4:F2} | Stop={5:F2} | Qty={6} | Error={7} | Comment={8}",
                    time, order.Name, order.OrderAction, order.OrderType, limitPrice, stopPrice, quantity, error, comment));

                // If a stop order is rejected while in position, emergency flatten
                if ((order.Name == "Stop loss" || order.OrderType == OrderType.StopMarket)
                    && Position.MarketPosition != MarketPosition.Flat)
                {
                    Print(string.Format("{0} [PZ] CRITICAL: Stop rejected while in position — emergency flatten!", time));
                    FlattenAll("PZ_EmergencyFlatten");
                }
            }

            // Handle rejected/cancelled ENTRY orders — deactivate ContractInfo to prevent stuck state
            if ((orderState == OrderState.Rejected || orderState == OrderState.Cancelled) && IsOurSignal(order.Name))
            {
                var ci = activeContracts?.FirstOrDefault(c => c.SignalName == order.Name && !c.IsFilled);
                if (ci != null)
                    ci.IsActive = false;

                if (DebugMode)
                    Print(string.Format("{0} [PZ] Entry order {1}: {2}", time, orderState, order.Name));

                // If position is flat and no active contracts remain, reset trade state
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    bool hasActive = activeContracts != null && activeContracts.Any(c => c.IsActive);
                    if (!hasActive)
                        ResetTradeState();
                }
            }
        }

        #endregion

        #region Phase 7 — Dashboard (SharpDX rendering + WPF buttons)

        // --- SharpDX resource management ---

        private void DisposeDxResources()
        {
            if (dxBgBrush != null) { dxBgBrush.Dispose(); dxBgBrush = null; }
            if (dxBorderBrush != null) { dxBorderBrush.Dispose(); dxBorderBrush = null; }
            if (dxDividerBrush != null) { dxDividerBrush.Dispose(); dxDividerBrush = null; }
            if (dxWhiteBrush != null) { dxWhiteBrush.Dispose(); dxWhiteBrush = null; }
            if (dxGreenBrush != null) { dxGreenBrush.Dispose(); dxGreenBrush = null; }
            if (dxRedBrush != null) { dxRedBrush.Dispose(); dxRedBrush = null; }
            if (dxYellowBrush != null) { dxYellowBrush.Dispose(); dxYellowBrush = null; }
            if (dxCyanBrush != null) { dxCyanBrush.Dispose(); dxCyanBrush = null; }
            if (dxLabelBrush != null) { dxLabelBrush.Dispose(); dxLabelBrush = null; }
            if (dxOrangeBrush != null) { dxOrangeBrush.Dispose(); dxOrangeBrush = null; }
            if (dxStripGrayBrush != null) { dxStripGrayBrush.Dispose(); dxStripGrayBrush = null; }
            if (dxStripPinkBrush != null) { dxStripPinkBrush.Dispose(); dxStripPinkBrush = null; }
            if (dxStripGreenBrush != null) { dxStripGreenBrush.Dispose(); dxStripGreenBrush = null; }
            if (dxStripBlueBrush != null) { dxStripBlueBrush.Dispose(); dxStripBlueBrush = null; }
            if (dxLabelFont != null) { dxLabelFont.Dispose(); dxLabelFont = null; }
            if (dxValueFont != null) { dxValueFont.Dispose(); dxValueFont = null; }
            if (dxHeaderFont != null) { dxHeaderFont.Dispose(); dxHeaderFont = null; }
        }

        public override void OnRenderTargetChanged()
        {
            base.OnRenderTargetChanged();

            DisposeDxResources();

            if (RenderTarget != null)
            {
                float bgAlpha = DashboardOpacity / 100f;
                dxBgBrush      = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0f, 0f, 0f, bgAlpha));
                dxBorderBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(60, 60, 60, 255));
                dxDividerBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(50, 50, 50, 255));
                dxWhiteBrush   = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 255, 255, 255));
                dxGreenBrush   = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(0, 200, 80, 255));
                dxRedBrush     = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(230, 50, 50, 255));
                dxYellowBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(230, 200, 40, 255));
                dxCyanBrush    = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(80, 200, 230, 255));
                dxLabelBrush   = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(180, 180, 180, 255));
                dxOrangeBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 140, 0, 255));

                // Signal strip brushes
                dxStripGrayBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(60, 60, 60, 180));
                dxStripPinkBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 105, 180, 255));
                dxStripGreenBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(0, 230, 80, 255));
                dxStripBlueBrush  = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(30, 144, 255, 255));

                dxLabelFont  = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Consolas", 10f);
                dxValueFont  = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Consolas",
                    SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f);
                dxHeaderFont = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Trebuchet MS",
                    SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 13f);
            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            if (RenderTarget == null || ChartPanel == null)
                return;
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Signal debug strips — always render (even if dashboard is off)
            if (stripTzState != null && stripPkState != null)
                RenderSignalStrips(chartControl);

            if (!ShowDashboard || dxLabelFont == null)
                return;

            // --- Build dashboard rows ---
            var rows = new List<DashRow>();

            // 1. Header
            string stateStr;
            int stateColor;
            if (tradeDir == TradeDir.Long)       { stateStr = "LONG";  stateColor = DC_GREEN; }
            else if (tradeDir == TradeDir.Short)  { stateStr = "SHORT"; stateColor = DC_RED; }
            else                                  { stateStr = "Flat";  stateColor = DC_WHITE; }
            rows.Add(new DashRow { Label = "PANAZILLA", Value = stateStr, ColorCode = stateColor, IsHeader = true });

            // 2. Divider
            rows.Add(new DashRow { IsDivider = true });

            // 3. Position section
            int totalQty = 0;
            double avgEntry = 0;
            double unrealizedPnl = 0;
            if (activeContracts != null)
            {
                double totalValue = 0;
                int totalQ = 0;
                foreach (var ci in activeContracts)
                {
                    if (ci.IsActive && ci.IsFilled)
                    {
                        totalValue += ci.EntryPrice * ci.Quantity;
                        totalQ += ci.Quantity;
                    }
                }
                totalQty = totalQ;
                avgEntry = totalQ > 0 ? totalValue / totalQ : 0;
            }
            if (Position.MarketPosition != MarketPosition.Flat)
                unrealizedPnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);

            rows.Add(new DashRow { Label = "Contracts", Value = totalQty.ToString(), ColorCode = totalQty > 0 ? DC_GREEN : DC_WHITE });
            rows.Add(new DashRow { Label = "Avg Entry", Value = avgEntry > 0 ? Instrument.MasterInstrument.FormatPrice(avgEntry) : "---", ColorCode = DC_WHITE });
            rows.Add(new DashRow { Label = "Unrealized", Value = unrealizedPnl != 0 ? string.Format("${0:F2}", unrealizedPnl) : "---",
                ColorCode = unrealizedPnl > 0 ? DC_GREEN : unrealizedPnl < 0 ? DC_RED : DC_WHITE });

            // 4. Divider
            rows.Add(new DashRow { IsDivider = true });

            // 5. Risk section
            int riskTicks = ComputeTotalRiskTicks();
            double riskDollars = riskTicks * Instrument.MasterInstrument.PointValue * TickSize;
            double exposurePct = MaxExposureDollars > 0 ? (riskDollars / MaxExposureDollars) * 100.0 : 0;

            rows.Add(new DashRow { Label = "Risk", Value = string.Format("{0}t / ${1:F0}", riskTicks, riskDollars),
                ColorCode = riskTicks > 300 ? DC_YELLOW : DC_WHITE });
            rows.Add(new DashRow { Label = "Exposure", Value = string.Format("{0:F0}%", exposurePct),
                ColorCode = exposurePct > 80 ? DC_RED : exposurePct > 50 ? DC_YELLOW : DC_WHITE });

            // SL / TP
            double initSl = 0, initTp = 0;
            if (activeContracts != null)
            {
                string initName = tradeDir == TradeDir.Long ? InitLong : InitShort;
                var initCi = activeContracts.FirstOrDefault(c => c.SignalName == initName && c.IsActive);
                if (initCi != null)
                {
                    initSl = initCi.CurrentStopPrice;
                    initTp = initCi.CurrentTpPrice;
                }
            }
            rows.Add(new DashRow { Label = "SL", Value = initSl > 0 ? Instrument.MasterInstrument.FormatPrice(initSl) : "---", ColorCode = DC_RED });
            rows.Add(new DashRow { Label = "TP", Value = initTp > 0 ? Instrument.MasterInstrument.FormatPrice(initTp) : "---", ColorCode = DC_GREEN });

            // 6. Divider
            rows.Add(new DashRow { IsDivider = true });

            // 7. Signal section
            double panaSignalVal = panaKanal.Signal_Trade[0];
            double tzSignalVal   = thunderZilla.Signal_Trade[0];
            bool confluence = (panaSignalVal == 2 && tzSignalVal == 3) || (panaSignalVal == -2 && tzSignalVal == -3);

            rows.Add(new DashRow { Label = "PANAKanal", Value = GetPanaKanalDesc(panaSignalVal),
                ColorCode = panaSignalVal > 0 ? DC_GREEN : panaSignalVal < 0 ? DC_RED : DC_WHITE });
            rows.Add(new DashRow { Label = "ThunderZilla", Value = GetThunderZillaDesc(tzSignalVal),
                ColorCode = tzSignalVal > 0 ? DC_GREEN : tzSignalVal < 0 ? DC_RED : DC_WHITE });
            rows.Add(new DashRow { Label = "Confluence", Value = confluence ? "YES" : "---",
                ColorCode = confluence ? DC_GREEN : DC_WHITE });

            // 8. Divider
            rows.Add(new DashRow { IsDivider = true });

            // 9. Session section
            rows.Add(new DashRow { Label = "Daily P&L", Value = string.Format("${0:F2}", dailyPnl),
                ColorCode = dailyPnl >= 0 ? DC_GREEN : DC_RED });
            rows.Add(new DashRow { Label = "Trades", Value = tradeCount.ToString(), ColorCode = DC_WHITE });
            rows.Add(new DashRow { Label = "Pullbacks", Value = pullbackCount.ToString(),
                ColorCode = pullbackCount >= PullbackThreshold ? DC_YELLOW : DC_WHITE });
            rows.Add(new DashRow { Label = "Velocity", Value = string.Format("{0:F2} pt/s", currentVelocity), ColorCode = DC_CYAN });

            // Status
            string statusStr;
            int statusColor;
            if (!isAutoEnabled)                                   { statusStr = "PAUSED";      statusColor = DC_ORANGE; }
            else if (sessionLocked)                               { statusStr = "LOCKED";      statusColor = DC_RED; }
            else if (Position.MarketPosition != MarketPosition.Flat) { statusStr = "IN POSITION"; statusColor = DC_YELLOW; }
            else                                                  { statusStr = "ACTIVE";      statusColor = DC_GREEN; }
            rows.Add(new DashRow { Label = "Status", Value = statusStr, ColorCode = statusColor });

            // --- Measure column widths ---
            float rowH = 18f;
            float divH = 8f;
            float headerH = 22f;
            float padX = 10f;
            float padY = 8f;
            float gapCol = 12f;
            float marginPanel = 10f;

            float maxLabelW = 0f;
            float maxValW = 0f;
            for (int k = 0; k < rows.Count; k++)
            {
                if (!rows[k].IsDivider)
                {
                    SharpDX.DirectWrite.TextFormat font = rows[k].IsHeader ? dxHeaderFont : dxLabelFont;
                    using (var lblLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, rows[k].Label ?? "", font, 400f, rowH))
                        maxLabelW = Math.Max(maxLabelW, lblLayout.Metrics.Width);
                    SharpDX.DirectWrite.TextFormat vFont = rows[k].IsHeader ? dxHeaderFont : dxValueFont;
                    using (var valLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, rows[k].Value ?? "", vFont, 400f, rowH))
                        maxValW = Math.Max(maxValW, valLayout.Metrics.Width);
                }
            }

            float totalH = padY;
            for (int l = 0; l < rows.Count; l++)
            {
                if (rows[l].IsDivider) totalH += divH;
                else if (rows[l].IsHeader) totalH += headerH;
                else totalH += rowH;
            }
            totalH += padY;

            float tableW = padX + maxLabelW + gapCol + maxValW + padX;
            float tableH = totalH;
            // Default position: top-right of chart panel (panel-local coordinates)
            if (!dashPositionInitialized)
            {
                dashX = ChartPanel.W - tableW - marginPanel;
                dashY = marginPanel;
                dashPositionInitialized = true;
            }
            float tableX = (float)dashX;
            float tableY = (float)dashY;

            // Cache dimensions for WPF button positioning
            dashCachedTableW = tableW;
            dashCachedTableH = tableH;
            dashCachedLabelW = maxLabelW;
            dashCachedValueW = maxValW;

            // --- Draw background and border ---
            RectangleF bgRect = new RectangleF(tableX, tableY, tableW, tableH);
            RenderTarget.FillRectangle(bgRect, dxBgBrush);
            RenderTarget.DrawRectangle(bgRect, dxBorderBrush, 1f);

            // --- Draw rows ---
            float curY = tableY + padY;
            for (int m = 0; m < rows.Count; m++)
            {
                DashRow dr = rows[m];
                if (!dr.IsDivider)
                {
                    SharpDX.DirectWrite.TextFormat lblFont = dr.IsHeader ? dxHeaderFont : dxLabelFont;
                    SharpDX.DirectWrite.TextFormat valFont = dr.IsHeader ? dxHeaderFont : dxValueFont;
                    float rH = dr.IsHeader ? headerH : rowH;

                    // Value brush by color code
                    SharpDX.Direct2D1.Brush valueBrush;
                    if      (dr.ColorCode == DC_GREEN)  valueBrush = dxGreenBrush;
                    else if (dr.ColorCode == DC_RED)    valueBrush = dxRedBrush;
                    else if (dr.ColorCode == DC_YELLOW) valueBrush = dxYellowBrush;
                    else if (dr.ColorCode == DC_CYAN)   valueBrush = dxCyanBrush;
                    else if (dr.ColorCode == DC_ORANGE) valueBrush = dxOrangeBrush;
                    else                                valueBrush = dxWhiteBrush;

                    // Header label = cyan, data labels = grey
                    SharpDX.Direct2D1.Brush labelBrush = dr.IsHeader ? dxCyanBrush : dxLabelBrush;

                    using (var lblLay = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, dr.Label ?? "", lblFont, maxLabelW + 10f, rH))
                        RenderTarget.DrawTextLayout(new Vector2(tableX + padX, curY), lblLay, labelBrush);

                    using (var valLay = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, dr.Value ?? "", valFont, maxValW + 10f, rH))
                    {
                        valLay.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
                        RenderTarget.DrawTextLayout(new Vector2(tableX + tableW - padX - maxValW - 10f, curY), valLay, valueBrush);
                    }

                    curY += rH;
                }
                else
                {
                    float divY = curY + divH / 2f;
                    RenderTarget.DrawLine(new Vector2(tableX + 4f, divY), new Vector2(tableX + tableW - 4f, divY), dxDividerBrush, 1f);
                    curY += divH;
                }
            }

            // --- Reposition WPF button panel below the SharpDX dashboard ---
            // D2D renders in ChartPanel-local coords; WPF Margin is in chartGrid (ChartControl) coords.
            // Add ChartPanel.X/Y to convert.
            if (dashboardCreated && dashBorder != null && !isDashDragging)
            {
                try
                {
                    double wpfX = ChartPanel.X + tableX;
                    double wpfY = ChartPanel.Y + tableY + tableH;
                    double wpfW = tableW;
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (dashBorder != null)
                            {
                                dashBorder.Margin = new Thickness(wpfX, wpfY, 0, 0);
                                dashBorder.Width = wpfW;
                            }
                        }
                        catch { }
                    });
                }
                catch { }
            }
        }

        // --- WPF: interactive buttons only ---

        private void CreateDashboard()
        {
            chartGrid = FindChartGrid();
            if (chartGrid == null)
            {
                Print("PZ: Could not find chart grid for dashboard");
                return;
            }

            dashChartWindow = Window.GetWindow(ChartControl.Parent);

            dashPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // --- Auto On/Off button ---
            btnAutoOnOff = new Button
            {
                Content = "AUTO: ON",
                Height = 28,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new LinearGradientBrush(
                    Color.FromRgb(60, 160, 60), Color.FromRgb(30, 110, 30),
                    new System.Windows.Point(0, 0), new System.Windows.Point(0, 1)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 180, 80)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 3, 4, 2),
                Cursor = Cursors.Hand
            };
            btnAutoOnOff.Click += OnAutoOnOffClick;
            dashPanel.Children.Add(btnAutoOnOff);

            // --- Flatten button ---
            btnFlatten = new Button
            {
                Content = "FLATTEN",
                Height = 28,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new LinearGradientBrush(
                    Color.FromRgb(200, 80, 80), Color.FromRgb(140, 40, 40),
                    new System.Windows.Point(0, 0), new System.Windows.Point(0, 1)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 100, 100)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 2, 4, 4),
                Cursor = Cursors.Hand
            };
            btnFlatten.Click += OnFlattenClick;
            dashPanel.Children.Add(btnFlatten);

            byte bgAlpha = (byte)(255 * DashboardOpacity / 100);

            dashBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(bgAlpha, 28, 28, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = dashPanel,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0),  // positioned by OnRender after first frame
                Cursor = Cursors.SizeAll
            };
            dashBorder.MouseLeftButtonDown += OnDashMouseDown;
            dashBorder.MouseLeftButtonUp += OnDashMouseUp;
            dashBorder.MouseMove += OnDashMouseMove;

            chartGrid.Children.Add(dashBorder);
        }

        private void DisposeDashboard()
        {
            if (dashBorder != null)
            {
                dashBorder.MouseLeftButtonDown -= OnDashMouseDown;
                dashBorder.MouseLeftButtonUp -= OnDashMouseUp;
                dashBorder.MouseMove -= OnDashMouseMove;
            }
            if (btnAutoOnOff != null)
                btnAutoOnOff.Click -= OnAutoOnOffClick;
            if (btnFlatten != null)
                btnFlatten.Click -= OnFlattenClick;
            if (chartGrid != null && dashBorder != null)
                chartGrid.Children.Remove(dashBorder);
            dashBorder = null;
            dashPanel = null;
            btnAutoOnOff = null;
            btnFlatten = null;
            dashChartWindow = null;
        }

        private string GetPanaKanalDesc(double signal)
        {
            if (signal == 1) return "Trend Up";
            if (signal == -1) return "Trend Dn";
            if (signal == 2) return "BREAK UP";
            if (signal == -2) return "BREAK DN";
            if (signal == 3) return "Pull Up";
            if (signal == -3) return "Pull Dn";
            return "---";
        }

        private string GetThunderZillaDesc(double signal)
        {
            if (signal == 1) return "Trend Up";
            if (signal == -1) return "Trend Dn";
            if (signal == 2) return "Slow Dn";
            if (signal == -2) return "Slow Up";
            if (signal == 3) return "Pull Up";
            if (signal == -3) return "Pull Dn";
            if (signal == 4) return "MoveSL Up";
            if (signal == -4) return "MoveSL Dn";
            return "---";
        }

        private void OnAutoOnOffClick(object sender, RoutedEventArgs e)
        {
            isAutoEnabled = !isAutoEnabled;
            if (btnAutoOnOff != null)
            {
                btnAutoOnOff.Content = isAutoEnabled ? "AUTO: ON" : "AUTO: OFF";
                if (isAutoEnabled)
                {
                    btnAutoOnOff.Background = new LinearGradientBrush(
                        Color.FromRgb(60, 160, 60), Color.FromRgb(30, 110, 30),
                        new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));
                    btnAutoOnOff.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 180, 80));
                }
                else
                {
                    btnAutoOnOff.Background = new LinearGradientBrush(
                        Color.FromRgb(220, 120, 30), Color.FromRgb(170, 70, 10),
                        new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));
                    btnAutoOnOff.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 140, 50));
                }
            }
        }

        private void OnFlattenClick(object sender, RoutedEventArgs e)
        {
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                FlattenAll("PZ_Manual");
                ResetTradeState();
            }
        }

        private void OnDashMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDashDragging = true;
            dashDragStart = dashChartWindow != null ? e.GetPosition(dashChartWindow) : e.GetPosition(dashBorder);
            dashBorder.CaptureMouse();
            e.Handled = true;
        }

        private void OnDashMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDashDragging)
            {
                isDashDragging = false;
                dashBorder.ReleaseMouseCapture();
                // dashX/dashY already updated in OnDashMouseMove
                e.Handled = true;
            }
        }

        private void OnDashMouseMove(object sender, MouseEventArgs e)
        {
            if (isDashDragging && dashChartWindow != null)
            {
                System.Windows.Point pos = e.GetPosition(dashChartWindow);
                double dx = pos.X - dashDragStart.X;
                double dy = pos.Y - dashDragStart.Y;
                double newX = dashX + dx;
                double newY = dashY + dy;
                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;
                if (newX > dashChartWindow.ActualWidth - 220) newX = dashChartWindow.ActualWidth - 220;
                if (newY > dashChartWindow.ActualHeight - 400) newY = dashChartWindow.ActualHeight - 400;
                dashX = newX;
                dashY = newY;
                // WPF buttons follow below D2D table — convert panel-local to chartGrid coords
                dashBorder.Margin = new Thickness(ChartPanel.X + dashX, ChartPanel.Y + dashY + dashCachedTableH, 0, 0);
                dashDragStart = pos;
                ChartControl.InvalidateVisual(); // redraw D2D at new position
                e.Handled = true;
            }
        }

        private Grid FindChartGrid()
        {
            try
            {
                if (ChartControl == null) return null;
                var child1 = System.Windows.Media.VisualTreeHelper.GetChild(ChartControl, 0) as Grid;
                if (child1 != null)
                {
                    var child2 = System.Windows.Media.VisualTreeHelper.GetChild(child1, 0) as Grid;
                    if (child2 != null) return child2;
                    return child1;
                }
                return FindChildGrid(ChartControl);
            }
            catch { return null; }
        }

        private Grid FindChildGrid(DependencyObject parent)
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is Grid g) return g;
                var result = FindChildGrid(child);
                if (result != null) return result;
            }
            return null;
        }

        #endregion

        #region Helpers

        private bool IsOurSignal(string name)
        {
            if (name == InitLong || name == InitShort)
                return true;
            for (int i = 0; i < AddonLongNames.Length; i++)
            {
                if (name == AddonLongNames[i] || name == AddonShortNames[i])
                    return true;
            }
            return false;
        }

        private void ResetTradeState()
        {
            tradeState   = TradeState.Flat;
            tradeDir     = TradeDir.None;
            nextAddIndex = 0;
            pullbackCount = 0;
            lastRenkoDir  = 0;

            if (activeContracts != null)
                activeContracts.Clear();

            pendingAddonOrder = null;
            lastPanaSignalBar = 0;
            lastTzSignalBar   = 0;
            lastTpAdjustTime  = DateTime.MinValue;
            priceAtLastTpCheck = 0;
            currentVelocity   = 0;

            if (DebugMode)
                Print(string.Format("{0} [PZ] Trade state RESET to Flat", Time[0]));
        }

        #endregion

        #region NinjaScriptProperty Parameters

        // --- Indicator params ---
        [NinjaScriptProperty]
        [Display(Name = "PANAKanal Period", Order = 1, GroupName = "1. Indicators")]
        public int PanaKanalPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PANAKanal Factor", Order = 2, GroupName = "1. Indicators")]
        public double PanaKanalFactor { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PANAKanal Middle Period", Order = 3, GroupName = "1. Indicators")]
        public int PanaKanalMiddlePeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PANAKanal Break Split", Order = 4, GroupName = "1. Indicators")]
        public int PanaKanalBreakSplit { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PANAKanal Pullback Period", Order = 5, GroupName = "1. Indicators")]
        public int PanaKanalPullbackPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Trend MA Type", Order = 10, GroupName = "1. Indicators")]
        public ThunderZillaMAType TzTrendMAType { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Trend Period", Order = 11, GroupName = "1. Indicators")]
        public int TzTrendPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Trend Smoothing Enabled", Order = 12, GroupName = "1. Indicators")]
        public bool TzTrendSmoothingEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Trend Smoothing Method", Order = 13, GroupName = "1. Indicators")]
        public ThunderZillaMAType TzTrendSmoothingMethod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Trend Smoothing Period", Order = 14, GroupName = "1. Indicators")]
        public int TzTrendSmoothingPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Stop Offset Multiplier", Order = 15, GroupName = "1. Indicators")]
        public double TzStopOffsetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Signal Qty Flat", Order = 16, GroupName = "1. Indicators")]
        public int TzSignalQtyFlat { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TZ Signal Qty Trend", Order = 17, GroupName = "1. Indicators")]
        public int TzSignalQtyTrend { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "BarStatus Bound Offset", Order = 20, GroupName = "1. Indicators")]
        public int BarStatusBoundOffset { get; set; }

        // --- Position sizing ---
        [NinjaScriptProperty]
        [Display(Name = "Initial Quantity", Order = 1, GroupName = "2. Position")]
        public int InitialQty { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Add-on Quantity", Order = 2, GroupName = "2. Position")]
        public int AddonQty { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Exposure ($)", Order = 3, GroupName = "2. Position")]
        public double MaxExposureDollars { get; set; }

        // --- Risk ---
        [NinjaScriptProperty]
        [Display(Name = "Initial SL (ticks)", Order = 1, GroupName = "3. Risk")]
        public int InitialSlTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Initial TP (ticks)", Order = 2, GroupName = "3. Risk")]
        public int InitialTpTicks { get; set; }

        // --- Add-on ---
        [NinjaScriptProperty]
        [Display(Name = "Add Proximity (points)", Order = 1, GroupName = "4. Add-on")]
        public double AddProximityPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Add Min Seconds", Order = 2, GroupName = "4. Add-on")]
        public int AddMinSeconds { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Risk (ticks)", Order = 3, GroupName = "4. Add-on")]
        public int MaxTotalRiskTicks { get; set; }

        // --- Partial close ---
        [NinjaScriptProperty]
        [Display(Name = "Partial Close Ticks", Order = 1, GroupName = "5. Partial Close")]
        public int PartialCloseTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Pullback Threshold", Order = 2, GroupName = "5. Partial Close")]
        public int PullbackThreshold { get; set; }

        // --- Daily limits ---
        [NinjaScriptProperty]
        [Display(Name = "Daily Profit Goal ($)", Order = 1, GroupName = "6. Daily Limits")]
        public double DailyProfitGoal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Daily Loss Limit ($)", Order = 2, GroupName = "6. Daily Limits")]
        public double DailyLossLimit { get; set; }

        // --- Debug ---
        [NinjaScriptProperty]
        [Display(Name = "Debug Mode", Order = 1, GroupName = "7. Debug")]
        public bool DebugMode { get; set; }

        // --- Dashboard ---
        [NinjaScriptProperty]
        [Display(Name = "Show Dashboard", Order = 1, GroupName = "8. Dashboard")]
        public bool ShowDashboard { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Dashboard Opacity (%)", Order = 2, GroupName = "8. Dashboard")]
        public int DashboardOpacity { get; set; }

        #endregion
    }
}
