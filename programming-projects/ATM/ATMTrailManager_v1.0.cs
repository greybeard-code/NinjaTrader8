

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ATMTrailManager : Indicator
    {
        #region Constants
        private const string PanelName    = "ATM Trail Manager";
        private const string PanelVersion = "1.0.0";
        #endregion

        #region WPF fields
        private Gui.Chart.Chart                      _chartWindow;
        private System.Windows.Controls.Border       _floatingPanel;

        private System.Windows.Controls.StackPanel   _bodyStack;
        private System.Windows.Controls.Border       _pillBtn;
        private bool                                 _isPilled      = false;
        private System.Windows.Point                 _dragStart;
        private bool                                 _isDragging    = false;
        private System.Windows.Controls.StackPanel   _tightenSection;
        private System.Windows.Controls.TextBlock    _trailOffMsg;

        // Status labels
        private System.Windows.Controls.TextBlock    _posStatusLabel;
        private System.Windows.Controls.TextBlock    _atmNameLabel;
        private System.Windows.Controls.TextBlock    _atmStructureLabel;
        private System.Windows.Controls.TextBlock    _stopLabel;
        private System.Windows.Controls.TextBlock    _trailLabel;
        private System.Windows.Controls.TextBlock    _multLabel;
        private System.Windows.Controls.TextBlock    _tightenLabel;
        private System.Windows.Controls.ProgressBar  _tightenBar;
        private System.Windows.Controls.Button       _autoTrailBtn;

        // Settings managed via indicator properties panel
        #endregion

        #region Trail state
        private volatile bool  _autoTrailEnabled = false;
        private MarketPosition _trailDir         = MarketPosition.Flat;
        private double         _entryPrice       = 0.0;
        private int            _initialStopTicks = 0;
        private double         _lastAppliedStop  = 0.0;
        private bool           _trailActivated   = false;
        private double         _peakFavTicks     = 0.0;
        private double         _cachedLiveStop   = 0.0;
        private int            _lastKnownQty     = 0;  // tracks qty for leg exit detection
        private DateTime       _entryTime        = DateTime.MinValue;

        // UI snapshot
        private double         _uiCurPrice       = 0.0;
        private double         _uiEntryPrice     = 0.0;
        private double         _uiLiveStop       = 0.0;
        private double         _uiTrailStop      = 0.0;
        private double         _uiCurrentMult    = 0.0;
        private volatile int   _uiTightenStep    = 0;
        private volatile int   _uiDirInt         = 0;
        private double         _uiFavTicks       = 0.0;
        private double         _uiLockedTicks    = 0.0;
        private string         _uiAtmName        = "—";
        private int            _uiAtmQty         = 0;
        private int            _uiDetStopTicks   = 0;
        private string         _uiAtmStructure   = "—";

        private volatile Account _cachedAccount  = null;
        private ATR              _atrIndicator   = null;

        // Locked ticks — ticks booked by T1/T2 fills during this trade
        private double           _lockedTicks    = 0.0;
        #endregion

        #region Drawing state
        private enum ExitType { None, Target, Stop, Trail }

        private struct LegRecord
        {
            public int      LegNum;
            public double   EntryPrice;
            public double   ExitPrice;
            public ExitType Exit;
            public int      EntryBar;
            public int      ExitBar;
            public bool     Drawn;
        }

        private LegRecord[] _legs           = new LegRecord[3];
        private int         _tradeTag       = 0;
        private int         _entryBar       = -1;
        private bool        _entryDrawn     = false;
        private double[]    _legInitStop    = new double[3];
        private string[]    _legStopName    = { "Stop1", "Stop2", "Stop3" };
        private string[]    _legTargetName  = { "Target1", "Target2", "Target3" };

        private static readonly SimpleFont _fontEntry = new SimpleFont("Arial", 9) { Bold = true  };
        private static readonly SimpleFont _fontExit  = new SimpleFont("Arial", 8) { Bold = false };
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "ATM Trail Manager v1.0 — ATR chandelier trailing stop for your runner leg";
                Name        = "ATM Trail Manager";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = true;
                IsSuspendedWhileInactive = false;

                // Panel
                PanelLeft  = 20;
                PanelTop   = 80;
                PanelWidth = 280;

                // Runner
                RunnerStopName   = "Stop3";
                // ATR
                AtrPeriod          = 14;
                AtrMultiplier      = 2.0;
                AtrActivationTicks = 0;

                // 4 tighten steps
                Step1Ticks = 50;   Step1Mult = 1.75;
                Step2Ticks = 100;  Step2Mult = 1.50;
                Step3Ticks = 150;  Step3Mult = 1.25;
                Step4Ticks = 200;  Step4Mult = 1.00;

                // Safety
                MinStopDistanceTicks = 4;

                // Misc
                ShowTradeDrawings = true;
                PrintDebug        = false;
            }
            else if (State == State.Configure)
            {
                AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Runner");
            }
            else if (State == State.DataLoaded)
            {
                _atrIndicator = ATR(AtrPeriod);
                if (ChartControl != null)
                    ChartControl.Dispatcher.InvokeAsync(CreateWPFControls);
            }
            else if (State == State.Terminated)
            {
                if (ChartControl != null)
                    ChartControl.Dispatcher.InvokeAsync(DisposeWPFControls);
            }
        }

        protected override void OnBarUpdate()
        {
            PlotBrushes[0][0] = Brushes.Transparent;
            if (CurrentBar < AtrPeriod + 1) return;

            double atr = _atrIndicator[0];
            if (atr <= 0) return;

            double highestHigh = MAX(High, AtrPeriod)[0];
            double lowestLow   = MIN(Low,  AtrPeriod)[0];

            // ── Account / position ────────────────────────────────────────────────
            if (State == State.Realtime)
            {
                Account acct = _cachedAccount
                    ?? Account.All.FirstOrDefault(a => a.ConnectionStatus == ConnectionStatus.Connected);
                if (acct != null)
                {
                    _cachedAccount = acct;
                    Instrument instr = Instrument;
                    if (instr != null)
                    {
                        Position pos = acct.Positions.FirstOrDefault(
                            p => p.Instrument?.FullName == instr.FullName);
                        bool hasPos = pos != null && pos.MarketPosition != MarketPosition.Flat;
                        bool dirFlip = hasPos && _trailDir != MarketPosition.Flat
                            && pos.MarketPosition != _trailDir;

                        // New position
                        if (hasPos && _trailDir == MarketPosition.Flat)
                        {
                            _trailDir         = pos.MarketPosition;
                            _entryPrice       = pos.AveragePrice;
                            _lastAppliedStop  = 0.0;
                            _trailActivated   = false;
                            _peakFavTicks     = 0.0;
                            _initialStopTicks = 0;
                            _lastKnownQty     = pos.Quantity;
                            _entryBar         = CurrentBar;
                            _entryTime = Time[0];
                            _entryDrawn       = false;
                            _tradeTag++;

                            for (int li = 0; li < 3; li++)
                            {
                                _legs[li] = new LegRecord
                                    { LegNum = li+1, EntryPrice = pos.AveragePrice,
                                      EntryBar = CurrentBar, Exit = ExitType.None };
                                _legInitStop[li] = 0.0;
                            }
                            foreach (Order o in acct.Orders)
                            {
                                if (o == null || o.Instrument?.FullName != instr.FullName) continue;
                                if (o.OrderState == OrderState.Cancelled || o.OrderState == OrderState.Filled) continue;
                                for (int li = 0; li < 3; li++)
                                    if ((o.Name ?? "").Equals(_legStopName[li], StringComparison.OrdinalIgnoreCase))
                                        _legInitStop[li] = o.StopPrice;
                            }

                            Order stopOrd = FindRunnerStop(acct, instr, pos);
                            if (stopOrd != null && stopOrd.StopPrice > 0 && _entryPrice > 0)
                            {
                                bool lng = _trailDir == MarketPosition.Long;
                                double dist = lng ? _entryPrice - stopOrd.StopPrice
                                                  : stopOrd.StopPrice - _entryPrice;
                                _initialStopTicks = (int)Math.Round(dist / instr.MasterInstrument.TickSize);
                                _cachedLiveStop   = stopOrd.StopPrice;
                                _lastAppliedStop  = stopOrd.StopPrice;
                                if (PrintDebug) Print($"[ATMTrail] SEEDED stop={_lastAppliedStop:F2} ({_initialStopTicks}t)");
                            }
                            else if (PrintDebug)
                                Print($"[ATMTrail] SEED FAILED — stopOrd={(stopOrd == null ? "null" : stopOrd.Name)} price={stopOrd?.StopPrice.ToString("F2") ?? "n/a"} entry={_entryPrice:F2}");
                            ScanAtmInfo(acct, instr, pos);
                            // Clear previous trade drawings before drawing new ones
                            ClearTradeDrawings(_tradeTag - 1);
                            DrawTradeEntry();

                            if (PrintDebug)
                                Print($"[ATMTrail] ENTRY | Dir={_trailDir} Entry={_entryPrice:F2} Qty={_lastKnownQty} Stop={_cachedLiveStop:F2} ({_initialStopTicks}t)");
                        }

                        // Qty change — a leg exited
                        if (hasPos && pos.Quantity != _lastKnownQty && !dirFlip)
                        {
                            int legsExited = _lastKnownQty - pos.Quantity;
                            if (PrintDebug) Print($"[ATMTrail] QTY {_lastKnownQty}->{pos.Quantity} ({legsExited} leg(s))");
                            ScanLegExits(acct, instr, legsExited);
                            _lastKnownQty = pos.Quantity;
                        }

                        // Position closed / direction flip
                        if ((!hasPos || dirFlip) && _trailDir != MarketPosition.Flat)
                        {
                            if (PrintDebug) Print($"[ATMTrail] CLOSED | qty={_lastKnownQty} flip={dirFlip}");
                            ScanLegExits(acct, instr);
                            _lastKnownQty = 0;
                            ResetState();
                            UpdatePanelAsync();
                        }
                    }
                }
            }

            // ── Plot & trail ──────────────────────────────────────────────────────
            if (_trailDir == MarketPosition.Flat) { UpdatePanelAsync(); return; }

            bool   isLong = _trailDir == MarketPosition.Long;
            double tickSz = Instrument.MasterInstrument.TickSize;
            double price  = Close[0];

            double favTicks = isLong ? (price - _entryPrice) / tickSz
                                     : (_entryPrice - price) / tickSz;
            if (favTicks > _peakFavTicks) _peakFavTicks = favTicks;

            // 4-step multiplier (uses peak ticks — one-way ratchet)
            double mult = AtrMultiplier;
            int    step = 0;
            if      (_peakFavTicks >= Step4Ticks) { mult = Step4Mult; step = 4; }
            else if (_peakFavTicks >= Step3Ticks) { mult = Step3Mult; step = 3; }
            else if (_peakFavTicks >= Step2Ticks) { mult = Step2Mult; step = 2; }
            else if (_peakFavTicks >= Step1Ticks) { mult = Step1Mult; step = 1; }

            // Chandelier candidate
            double candidate = isLong ? highestHigh - atr * mult
                                      : lowestLow   + atr * mult;
            candidate = Instrument.MasterInstrument.RoundToTickSize(candidate);

            // Ratchet off previous bar
            double prevPlot = CurrentBar > AtrPeriod + 1 ? Values[0][1] : 0.0;
            double initialStop = _cachedLiveStop > 0 ? _cachedLiveStop
                : (_initialStopTicks > 0
                    ? (isLong ? _entryPrice - _initialStopTicks * tickSz
                              : _entryPrice + _initialStopTicks * tickSz)
                    : 0.0);

            double trailStop;
            if (!_trailActivated || prevPlot <= 0)
                trailStop = initialStop > 0 ? initialStop : candidate;
            else
                trailStop = isLong ? Math.Max(candidate, prevPlot)
                                   : Math.Min(candidate, prevPlot);

            trailStop = Instrument.MasterInstrument.RoundToTickSize(trailStop);

            if (isLong  && trailStop >= price) { UpdatePanelAsync(); return; }
            if (!isLong && trailStop <= price) { UpdatePanelAsync(); return; }

            Values[0][0]      = trailStop;
            PlotBrushes[0][0] = isLong ? Brushes.DodgerBlue : Brushes.OrangeRed;
            _trailActivated   = true;

            // Order submission — only when solo
            if (State == State.Realtime)
            {
                Account acct2 = _cachedAccount;
                Instrument instr2 = Instrument;
                if (acct2 != null && instr2 != null)
                {
                    Position pos2 = acct2.Positions.FirstOrDefault(
                        p => p.Instrument?.FullName == instr2.FullName);
                    if (pos2 != null && pos2.MarketPosition != MarketPosition.Flat)
                    {
                        Order stopOrd = FindRunnerStop(acct2, instr2, pos2);
                        double liveStop = stopOrd != null ? stopOrd.StopPrice : _lastAppliedStop;
                        if (liveStop > 0) _cachedLiveStop = liveStop;

                        bool solo = true; // trail runs regardless of qty — targeting specific named stop
                        if (_autoTrailEnabled && stopOrd != null && favTicks >= AtrActivationTicks)
                        {
                            ApplyTrail(acct2, instr2, stopOrd, isLong, trailStop, tickSz);
                        }
                        else if (PrintDebug)
                        {
                            if (!_autoTrailEnabled)   Print("[ATMTrail] Trail SKIP — AUTO TRAIL is OFF");
                            else if (stopOrd == null) Print("[ATMTrail] Trail SKIP — stop order not found");
                            else                      Print($"[ATMTrail] Trail SKIP — favTicks={favTicks:F1} < activation={AtrActivationTicks}");
                        }


                        _uiLiveStop = liveStop;
                    }
                }
            }

            _uiCurPrice     = price;
            _uiEntryPrice   = _entryPrice;
            _uiTrailStop    = trailStop;
            _uiCurrentMult  = mult;
            _uiTightenStep  = step;
            _uiDirInt       = isLong ? 1 : -1;
            _uiFavTicks     = Math.Max(0, favTicks);
            _uiLockedTicks  = _lockedTicks;
            _uiDetStopTicks = _initialStopTicks;
            UpdatePanelAsync();
        }

        #region Trail helpers
        private void ResetState()
        {
            _lockedTicks = 0.0;
            _trailDir         = MarketPosition.Flat;
            _entryPrice       = 0.0;
            _lastAppliedStop  = 0.0;
            _trailActivated   = false;
            _cachedLiveStop   = 0.0;
            _initialStopTicks = 0;
            _peakFavTicks     = 0.0;
            _lastKnownQty     = 0;
            _entryBar         = -1;
            _entryTime        = DateTime.MinValue;
            _entryDrawn       = false;
            for (int li = 0; li < 3; li++) { _legs[li] = new LegRecord(); _legInitStop[li] = 0; }
            _uiDirInt = 0; _uiAtmName = "—"; _uiAtmStructure = "—"; _uiAtmQty = 0;
            _uiDetStopTicks = 0; _uiFavTicks = 0; _uiLiveStop = 0; _uiTrailStop = 0;
            _uiCurrentMult = 0; _uiTightenStep = 0;
        }

        private void ScanAtmInfo(Account acct, Instrument instr, Position pos)
        {
            if (pos == null) return;
            _uiAtmQty = pos.Quantity;
            string atmName = "—", atmStruct = "—";
            try
            {
                var ords = acct.Orders.Where(o => o != null
                    && o.Instrument?.FullName == instr.FullName
                    && o.OrderState != OrderState.Cancelled
                    && o.OrderState != OrderState.Filled
                    && o.OrderState != OrderState.Rejected).ToList();
                var stops   = ords.Where(o => o.OrderType == OrderType.StopMarket || o.OrderType == OrderType.StopLimit).ToList();
                var targets = ords.Where(o => o.OrderType == OrderType.Limit).ToList();
                if (stops.Count > 0 || targets.Count > 0)
                    atmStruct = string.Join("  ", new[]
                        { stops.Count > 0 ? $"S:{stops.Count}" : "",
                          targets.Count > 0 ? $"T:{targets.Count}" : "" }
                        .Where(s => s != ""));
                var named = ords.FirstOrDefault(o => !string.IsNullOrEmpty(o.Name) && o.Name.Contains(" "));
                if (named != null) { var p = named.Name.Split(' '); if (p.Length >= 2) atmName = p[0]; }
            }
            catch { }
            _uiAtmName = atmName; _uiAtmStructure = atmStruct;
        }

        private Order FindRunnerStop(Account acct, Instrument instr, Position pos)
        {
            if (acct == null || instr == null || pos == null) return null;
            bool isLong = pos.MarketPosition == MarketPosition.Long;
            string name = (RunnerStopName ?? "").Trim();

            if (PrintDebug)
            {
                // Only scan-log working/accepted orders — not the entire history
                var active = acct.Orders.Where(o => o != null
                    && o.Instrument?.FullName == instr.FullName
                    && o.OrderState != OrderState.Filled
                    && o.OrderState != OrderState.Cancelled
                    && o.OrderState != OrderState.Rejected);
                foreach (var dbg in active)
                    Print($"[ATMTrail] SCAN | \"{dbg.Name}\" {dbg.OrderType} {dbg.OrderAction} {dbg.OrderState} Stop={dbg.StopPrice:F2}");
            }

            foreach (Order o in acct.Orders)
            {
                if (o == null || o.Instrument?.FullName != instr.FullName) continue;
                // Only consider working orders — never touch filled/cancelled historical orders
                if (o.OrderState != OrderState.Working
                    && o.OrderState != OrderState.Accepted
                    && o.OrderState != OrderState.PartFilled) continue;
                if (o.OrderType != OrderType.StopMarket && o.OrderType != OrderType.StopLimit) continue;
                bool isExit = isLong
                    ? (o.OrderAction == OrderAction.Sell || o.OrderAction == OrderAction.SellShort)
                    : (o.OrderAction == OrderAction.BuyToCover || o.OrderAction == OrderAction.Buy);
                if (!isExit) continue;
                // Must be placed after our entry — removed time filter, state filter is sufficient
                // (historical orders are Filled/Cancelled, never Working/Accepted)
                if (string.IsNullOrEmpty(name) || (o.Name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return o;
            }
            return null;
        }

        private void ApplyTrail(Account acct, Instrument instr, Order stopOrder,
                                bool isLong, double trailPrice, double tickSz)
        {
            try
            {
                bool orderIsLongExit = stopOrder.OrderAction == OrderAction.Sell
                                    || stopOrder.OrderAction == OrderAction.SellShort;
                if (isLong != orderIsLongExit)
                {
                    if (PrintDebug) Print($"[ATMTrail] ApplyTrail BLOCKED — direction mismatch");
                    return;
                }
                double liveStop = stopOrder.StopPrice;
                double curPrice = Close[0];
                double minDist  = MinStopDistanceTicks * tickSz;

                // If we never seeded lastAppliedStop, use the live broker stop as baseline
                double baseline = _lastAppliedStop > 0 ? _lastAppliedStop : liveStop;

                if (PrintDebug)
                    Print($"[ATMTrail] ApplyTrail | \"{stopOrder.Name}\" curStop={liveStop:F2} cand={trailPrice:F2} price={curPrice:F2}");

                bool improved = isLong ? trailPrice > baseline : trailPrice < baseline;
                if (!improved) return;
                bool safe = isLong ? trailPrice < curPrice - minDist : trailPrice > curPrice + minDist;
                if (!safe) return;
                if (trailPrice == _lastAppliedStop) return;

                stopOrder.StopPriceChanged = trailPrice;
                acct.Change(new[] { stopOrder });
                _lastAppliedStop = trailPrice;
                if (PrintDebug) Print($"[ATMTrail] ✓ APPLIED {trailPrice:F2} (was {liveStop:F2})");
            }
            catch (Exception ex) { if (PrintDebug) Print($"[ATMTrail] ApplyTrail error: {ex.Message}"); }
        }
        #endregion

        #region Drawing
        private void ClearTradeDrawings(int tag)
        {
            if (tag <= 0) return;
            string[] suffixes = { "_line","_trail_ext","_lbl","_ext","_arr" };
            for (int li = 1; li <= 3; li++)
            {
                string baseTag = "GZleg" + tag + "_" + li;
                foreach (var s in suffixes)
                    try { RemoveDrawObject(baseTag + s); } catch { }
            }
            try { RemoveDrawObject("GZentry_" + tag); } catch { }
        }

        private void DrawTradeEntry()
        {
            if (!ShowTradeDrawings || _entryDrawn || _entryPrice <= 0 || _entryBar < 0) return;
            bool   isLong  = _trailDir == MarketPosition.Long;
            string tag     = "GZentry_" + _tradeTag;
            int    qty     = _lastKnownQty > 0 ? _lastKnownQty : 1;
            int    barsAgo = CurrentBar - _entryBar;

            string icon  = isLong ? "▲" : "▼";
            Brush  col   = isLong ? Brushes.Cyan : Brushes.Magenta;
            string label = icon + " Entry" + Environment.NewLine + qty + " @ " + _entryPrice.ToString("F2");

            Draw.Text(this, tag, false, label, barsAgo, _entryPrice, 0, col, _fontEntry,
                      System.Windows.TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
            _entryDrawn = true;
        }

        private void DrawLegExit(int li)
        {
            if (!ShowTradeDrawings) return;
            if (_legs[li].Exit == ExitType.None || _legs[li].ExitPrice <= 0) return;
            if (_legs[li].Drawn) return;

            LegRecord leg = _legs[li];
            leg.Drawn = true;
            _legs[li] = leg;

            bool   isLong    = _trailDir == MarketPosition.Long;
            string baseTag   = "GZleg" + _tradeTag + "_" + (li + 1);
            int    exitBA    = Math.Max(0, CurrentBar - leg.ExitBar);
            int    entryBA   = Math.Max(0, CurrentBar - leg.EntryBar);

            // Dotted connector from entry bar to exit bar
            if (entryBA > exitBA)
                Draw.Line(this, baseTag + "_line", false,
                    entryBA, leg.ExitPrice, exitBA, leg.ExitPrice,
                    Brushes.DimGray, DashStyleHelper.Dot, 1);

            // Trail extension to close the plot gap
            if (leg.Exit == ExitType.Trail || leg.Exit == ExitType.Stop)
                Draw.Line(this, baseTag + "_trail_ext", false,
                    exitBA + 1, leg.ExitPrice, exitBA, leg.ExitPrice,
                    isLong ? Brushes.DodgerBlue : Brushes.OrangeRed, DashStyleHelper.Solid, 2);

            string icon; Brush col; string exitName;
            switch (leg.Exit)
            {
                case ExitType.Target: icon = "✓"; col = Brushes.LimeGreen; exitName = "Target" + (li+1); break;
                case ExitType.Trail:  icon = "⊙"; col = Brushes.Orange;    exitName = "Trail Stop";       break;
                default:              icon = "✕"; col = Brushes.OrangeRed; exitName = "Stop"   + (li+1); break;
            }

            double pnl  = isLong ? (leg.ExitPrice - _entryPrice) / TickSize
                                 : (_entryPrice - leg.ExitPrice) / TickSize;
            string sign = pnl >= 0 ? "+" : "";
            string lbl  = icon + " " + exitName + Environment.NewLine
                        + "1 @ " + leg.ExitPrice.ToString("F2")
                        + "  (" + sign + pnl.ToString("F0") + "t)";

            Draw.Text(this, baseTag + "_lbl", false, lbl, exitBA, leg.ExitPrice, 0,
                      col, _fontExit,
                      System.Windows.TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void ScanLegExits(Account acct, Instrument instr, int legsJustExited = -1)
        {
            if (instr == null || acct == null) return;
            bool isLong = _trailDir == MarketPosition.Long;

            // Only orders from THIS trade — filter by entry time
            DateTime entryTime = _entryTime;

            int unclassified = 0;
            for (int li = 0; li < 3; li++)
                if (_legs[li].Exit == ExitType.None) unclassified++;
            if (legsJustExited < 0) legsJustExited = unclassified;

            // Step 1: stops by exact name
            for (int li = 0; li < 3; li++)
            {
                if (_legs[li].Exit != ExitType.None) continue;
                Order stopOrd = acct.Orders.FirstOrDefault(o =>
                    o != null && o.Instrument?.FullName == instr.FullName &&
                    (o.Name ?? "").Equals(_legStopName[li], StringComparison.OrdinalIgnoreCase) &&
                    o.OrderState == OrderState.Filled &&
                    o.Time >= entryTime);
                if (stopOrd == null) continue;

                bool isTrail = li == 2 && _trailActivated && _lastAppliedStop > 0
                    && Math.Abs(_lastAppliedStop - _legInitStop[li]) > TickSize;

                LegRecord lr = _legs[li];
                lr.ExitPrice = stopOrd.AverageFillPrice > 0 ? stopOrd.AverageFillPrice : stopOrd.StopPrice;
                lr.Exit    = isTrail ? ExitType.Trail : ExitType.Stop;
                lr.ExitBar = CurrentBar; lr.Drawn = false;
                _legs[li]  = lr; DrawLegExit(li); legsJustExited--;
                if (PrintDebug) Print($"[ATMTrail] LEG{li+1} STOP {lr.Exit} @ {lr.ExitPrice:F2}");
            }
            if (legsJustExited <= 0) return;

            // Step 2: targets by exact name
            int found = 0;
            for (int li = 0; li < 3 && found < legsJustExited; li++)
            {
                if (_legs[li].Exit != ExitType.None) continue;
                Order tgtOrd = acct.Orders.FirstOrDefault(o =>
                    o != null && o.Instrument?.FullName == instr.FullName &&
                    o.OrderType == OrderType.Limit && o.OrderState == OrderState.Filled &&
                    (o.Name ?? "").Equals(_legTargetName[li], StringComparison.OrdinalIgnoreCase) &&
                    o.Time >= entryTime);
                if (tgtOrd == null) continue;

                LegRecord lr = _legs[li];
                lr.ExitPrice = tgtOrd.AverageFillPrice > 0 ? tgtOrd.AverageFillPrice : tgtOrd.LimitPrice;
                lr.Exit    = ExitType.Target; lr.ExitBar = CurrentBar; lr.Drawn = false;
                _legs[li]  = lr; DrawLegExit(li); found++;

                double legPnl = isLong ? (lr.ExitPrice - _entryPrice) / TickSize
                                       : (_entryPrice - lr.ExitPrice) / TickSize;
                _lockedTicks  += legPnl;
                _uiLockedTicks = _lockedTicks;
                if (PrintDebug) Print("[ATMTrail] LEG" + (li+1) + " TARGET @ " + lr.ExitPrice.ToString("F2") + " +" + legPnl.ToString("F0") + "t locked");
            }
        }


        #endregion

        #region WPF Panel
        private void CreateWPFControls()
        {
            _chartWindow = Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;
            if (_chartWindow == null) return;
            _floatingPanel = BuildPanel();
            _floatingPanel.Visibility = Visibility.Visible;
            UserControlCollection.Add(_floatingPanel);
            _chartWindow.MainTabControl.SelectionChanged += OnTabChanged;
        }

        private void DisposeWPFControls()
        {
            if (_chartWindow != null)
                _chartWindow.MainTabControl.SelectionChanged -= OnTabChanged;
            if (_floatingPanel != null)
            {
                try { UserControlCollection.Remove(_floatingPanel); } catch { }
                _floatingPanel = null;
            }
        }

        private System.Windows.Controls.Border BuildPanel()
        {
            Brush bPanel  = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x1C));
            Brush bBorder = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x44));
            Brush bTitle  = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x14));
            Brush bDim    = new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x80));
            Brush bRed    = new SolidColorBrush(Color.FromRgb(0xFF, 0x50, 0x50));
            Brush bBlue   = new SolidColorBrush(Color.FromRgb(0x40, 0xA0, 0xFF));
            Brush bGold   = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x30));
            Brush bSep    = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
            Brush bInput  = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x26));

            System.Func<System.Windows.Controls.Border> sep = () =>
                new System.Windows.Controls.Border
                    { Height = 1, Margin = new Thickness(6,4,6,4), Background = bSep };

            var outer = new System.Windows.Controls.Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment   = VerticalAlignment.Top,
                Margin              = new Thickness(PanelLeft, PanelTop, 0, 0),
                Width               = PanelWidth,
                Background          = bPanel,
                BorderBrush         = bBorder,
                BorderThickness     = new Thickness(1.5),
                CornerRadius        = new CornerRadius(12),
                ClipToBounds        = true,
                Effect              = new DropShadowEffect
                    { Color = Color.FromRgb(0,120,255), BlurRadius = 14,
                      Opacity = 0.35, ShadowDepth = 0 }
            };

            // ── Title bar ────────────────────────────────────────────────────────
            var titleBar = new System.Windows.Controls.Border
            {
                Background   = bTitle, Height = 28,
                CornerRadius = new CornerRadius(11,11,0,0),
                Cursor       = System.Windows.Input.Cursors.SizeAll
            };
            var titleText = new System.Windows.Controls.TextBlock
            {
                Text                = "🐲 " + PanelName + " v" + PanelVersion,
                FontSize            = 9, FontWeight = FontWeights.Bold,
                Foreground          = new SolidColorBrush(Colors.Cyan),
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleBar.Child = titleText;
            titleBar.MouseLeftButtonDown += OnTitleMouseDown;
            titleBar.MouseMove           += OnTitleMouseMove;
            titleBar.MouseLeftButtonUp   += OnTitleMouseUp;

            // ── Body ─────────────────────────────────────────────────────────────
            _bodyStack = new System.Windows.Controls.StackPanel
                { Margin = new Thickness(0,0,0,8) };

            // Direction banner
            _posStatusLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "NO POSITION",
                FontSize            = 14, FontWeight = FontWeights.Bold,
                Foreground          = bDim,
                Margin              = new Thickness(8,8,8,2),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center
            };

            // ATM / contract info
            _atmNameLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "—",
                FontSize            = 10,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x90,0x90,0xB0)),
                Margin              = new Thickness(8,0,8,2),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center
            };

            // Hidden compat labels
            _atmStructureLabel = new System.Windows.Controls.TextBlock
                { Visibility = Visibility.Collapsed };
            _stopLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "Stop: —   Trail: —",
                FontSize            = 9, Foreground = bDim,
                Margin              = new Thickness(8,0,8,1),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center
            };
            _multLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "Mult: —   Step: 0/4",
                FontSize            = 9, Foreground = bGold,
                Margin              = new Thickness(8,0,8,6),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center
            };
            _trailLabel = new System.Windows.Controls.TextBlock
                { Visibility = Visibility.Collapsed };

            // Big tick counter
            _tightenLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "— t",
                FontSize            = 32, FontWeight = FontWeights.Bold,
                Foreground          = bDim,
                Margin              = new Thickness(8,4,8,0),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center
            };
            var tickSubLabel = new System.Windows.Controls.TextBlock
            {
                Text                = "ticks in favour",
                FontSize            = 9,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x70,0x70,0x90)),
                Margin              = new Thickness(8,0,8,4),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Trail label hidden — not needed in simplified dashboard
            _trailLabel = new System.Windows.Controls.TextBlock
                { Visibility = Visibility.Collapsed, Height = 0 };
            var trailBadge = new System.Windows.Controls.Border
                { Visibility = Visibility.Collapsed, Height = 0 };

            // ATR tighten section header
            var tightenHdr = new System.Windows.Controls.TextBlock
            {
                Text = "ATR TIGHTEN LEVELS",
                FontSize = 8, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x70,0x70,0x95)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8,4,8,3)
            };

            // Step labels: tick threshold (top row) + multiplier (bottom row)
            // Segment lights up when _peakFavTicks crosses the threshold
            // Col 0 = initial (active from start), Col 1 = Step1, Col 2 = Step2, Col 3 = Step3
            var stepLabelGrid = new System.Windows.Controls.Grid
                { Margin = new Thickness(8,0,8,2) };
            stepLabelGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            stepLabelGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            string[] stepTickTxt = {
                "entry",
                "+" + Step1Ticks + "t",
                "+" + Step2Ticks + "t",
                "+" + Step3Ticks + "t"
            };
            string[] stepMultTxt = {
                AtrMultiplier.ToString("F2") + "×",
                Step1Mult.ToString("F2") + "×",
                Step2Mult.ToString("F2") + "×",
                Step3Mult.ToString("F2") + "×"
            };
            string[] stepLblCol = { "#40A0FF","#FFC030","#FF8040","#FF5050" };
            for (int i = 0; i < 4; i++)
                stepLabelGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            for (int i = 0; i < 4; i++)
            {
                Color stepCol = (Color)System.Windows.Media.ColorConverter
                    .ConvertFromString(stepLblCol[i]);

                // Top row: tick trigger
                var tickLbl = new System.Windows.Controls.TextBlock
                {
                    Text = stepTickTxt[i], FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x90,0x90,0xB0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0,0,0,1)
                };
                System.Windows.Controls.Grid.SetColumn(tickLbl, i);
                System.Windows.Controls.Grid.SetRow(tickLbl, 0);
                stepLabelGrid.Children.Add(tickLbl);

                // Bottom row: multiplier in step colour
                var multLbl = new System.Windows.Controls.TextBlock
                {
                    Text = stepMultTxt[i], FontSize = 8, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(stepCol),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0,0,0,2)
                };
                System.Windows.Controls.Grid.SetColumn(multLbl, i);
                System.Windows.Controls.Grid.SetRow(multLbl, 1);
                stepLabelGrid.Children.Add(multLbl);
            }

            // Segment blocks
            var segGrid = new System.Windows.Controls.Grid
                { Height = 16, Margin = new Thickness(8,0,8,6), Name = "tightenSegs" };
            for (int i = 0; i < 4; i++)
            {
                segGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
                if (i < 3) segGrid.ColumnDefinitions.Add(
                    new System.Windows.Controls.ColumnDefinition { Width = new GridLength(3) });
            }
            for (int i = 0; i < 4; i++)
            {
                var seg = new System.Windows.Controls.Border
                {
                    Background   = new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x2E)),
                    CornerRadius = new CornerRadius(4),
                    Name         = "seg" + i
                };
                System.Windows.Controls.Grid.SetColumn(seg, i * 2);
                segGrid.Children.Add(seg);
            }
            _tightenBar = new System.Windows.Controls.ProgressBar
                { Height = 0, Visibility = Visibility.Collapsed };

            // Auto trail button
            _autoTrailBtn = new System.Windows.Controls.Button
            {
                Content         = "AUTO TRAIL: OFF",
                Height          = 30, Margin = new Thickness(8,4,8,2),
                FontSize        = 11, FontWeight = FontWeights.Bold,
                Foreground      = bDim,
                Background      = new SolidColorBrush(Color.FromRgb(0x18,0x18,0x28)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x35,0x35,0x55)),
                BorderThickness = new Thickness(1.5),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            _autoTrailBtn.Click += OnAutoTrailToggle;

            // Flatten button
            var flatBtn = new System.Windows.Controls.Button
            {
                Content         = "⚡ Flatten",
                Height          = 26, Margin = new Thickness(8,2,8,4),
                FontSize        = 10, FontWeight = FontWeights.Bold,
                Foreground      = bRed,
                Background      = new SolidColorBrush(Color.FromRgb(0x28,0x0A,0x0A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0xC8,0x20,0x20)),
                BorderThickness = new Thickness(1),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            flatBtn.Click += OnFlattenClick;

            // Stop name monitor label
            var stopMonitorLabel = new System.Windows.Controls.TextBlock
            {
                FontSize            = 10,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x70,0xA0,0xFF)),
                Margin              = new Thickness(8,2,8,4),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = System.Windows.TextAlignment.Center,
                Name                = "stopMonitorLbl"
            };
            stopMonitorLabel.Text = "▶  " + (RunnerStopName ?? "—");

            _bodyStack.Children.Add(_posStatusLabel);
            _bodyStack.Children.Add(_atmNameLabel);
            _bodyStack.Children.Add(stopMonitorLabel);
            _bodyStack.Children.Add(sep());
            _bodyStack.Children.Add(_tightenLabel);
            _bodyStack.Children.Add(tickSubLabel);
            // Tighten section — shown only when AUTO TRAIL is ON
            _tightenSection = new System.Windows.Controls.StackPanel();
            _tightenSection.Children.Add(sep());
            _tightenSection.Children.Add(tightenHdr);
            _tightenSection.Children.Add(stepLabelGrid);
            _tightenSection.Children.Add(segGrid);
            _tightenSection.Children.Add(_tightenBar);
            _tightenSection.Visibility = Visibility.Collapsed; // OFF by default

            // Message shown when auto trail is off
            _trailOffMsg = new System.Windows.Controls.TextBlock
            {
                Text                = "enable auto trail to activate",
                FontSize            = 9,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x60,0x60,0x85)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(8,6,8,6),
                FontStyle           = System.Windows.FontStyles.Italic
            };

            _bodyStack.Children.Add(_tightenSection);
            _bodyStack.Children.Add(_trailOffMsg);
            _bodyStack.Children.Add(sep());
            _bodyStack.Children.Add(_autoTrailBtn);
            _bodyStack.Children.Add(flatBtn);

            var outerStack = new System.Windows.Controls.StackPanel();
            outerStack.Children.Add(titleBar);
            outerStack.Children.Add(_bodyStack);
            outer.Child = outerStack;
            return outer;
        }

        private void UpdatePanelAsync()
        {
            if (ChartControl == null) return;
            ChartControl.Dispatcher.InvokeAsync(UpdatePanelUI);
        }

        private void UpdatePanelUI()
        {
            if (_posStatusLabel == null) return;

            if (_uiDirInt == 0)
            {
                _posStatusLabel.Text       = "NO POSITION";
                _posStatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x60,0x60,0x80));
                _atmNameLabel.Text         = "—";
                _tightenLabel.Text         = "— t";
                _tightenLabel.Foreground   = new SolidColorBrush(Color.FromRgb(0x60,0x60,0x80));
                _stopLabel.Text            = "Stop: —   Trail: —";
                _multLabel.Text            = "Mult: —   Step: 0/4";
                UpdateTickSubLabel(0, 0);
                SetTrailBadge(false);
                UpdateTightenSegments(-1);
                return;
            }

            bool isLong = _uiDirInt == 1;
            _posStatusLabel.Text = isLong ? "▲  LONG  ▲" : "▼  SHORT  ▼";
            _posStatusLabel.Foreground = isLong
                ? new SolidColorBrush(Color.FromRgb(0x30,0xFF,0x80))
                : new SolidColorBrush(Color.FromRgb(0xFF,0x60,0x60));

            _atmNameLabel.Text = _uiAtmName != "—"
                ? $"{_uiAtmName}  ·  {_uiAtmQty} ct  ·  {_uiAtmStructure}"
                : $"{_uiAtmQty} ct  ·  {_uiAtmStructure}";
            _atmNameLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xA0,0xA0,0xC0));

            double favT = _uiFavTicks;
            _tightenLabel.Text = favT > 0 ? $"+{favT:F0} t" : "0 t";
            Color tickCol = favT <= 0   ? Color.FromRgb(0x60,0x60,0x80)
                          : favT < 50  ? Color.FromRgb(0x40,0xC0,0xC0)
                          : favT < 100 ? Color.FromRgb(0x30,0xFF,0x80)
                          :               Color.FromRgb(0xFF,0xC0,0x30);
            _tightenLabel.Foreground = new SolidColorBrush(tickCol);

            UpdateTightenSegments(_uiTightenStep);
        }

        private void UpdateTickSubLabel(double favT, double lockedT)
        {
            if (_bodyStack == null) return;
            foreach (var child in _bodyStack.Children)
            {
                var sp = child as System.Windows.Controls.StackPanel;
                if (sp == null) continue;
                foreach (var inner in sp.Children)
                {
                    var tb = inner as System.Windows.Controls.TextBlock;
                    if (tb == null || tb.Name != "tickSubLabel") continue;
                    if (lockedT > 0)
                    {
                        tb.Text = "runner  |  locked: +" + lockedT.ToString("F0") + "t";
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(0x30,0xFF,0x80));
                    }
                    else
                    {
                        tb.Text = "ticks in favour";
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(0x60,0x60,0x80));
                    }
                    return;
                }
            }
        }

        private void SetTrailBadge(bool active)
        {
            if (_trailLabel == null) return;
            if (active)
            {
                _trailLabel.Text       = "TRAIL\nACTIVE";
                _trailLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x30,0xFF,0x80));
                var parent = _trailLabel.Parent as System.Windows.Controls.Border;
                if (parent != null)
                {
                    parent.Background  = new SolidColorBrush(Color.FromRgb(0x08,0x28,0x14));
                    parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x20,0x80,0x40));
                }
            }
            else
            {
                _trailLabel.Text       = _uiDirInt != 0 ? "TRAIL\nPENDING" : "NO\nTRAIL";
                _trailLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x60,0x60,0x80));
                var parent = _trailLabel.Parent as System.Windows.Controls.Border;
                if (parent != null)
                {
                    parent.Background  = new SolidColorBrush(Color.FromRgb(0x12,0x12,0x22));
                    parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x40,0x40,0x60));
                }
            }
        }

        private void UpdateTightenSegments(int activeStep)
        {
            if (_bodyStack == null) return;
            string[] segHex = { "#40A0FF","#FFC030","#FF8040","#FF5050" };

            // Walk all children recursively looking for Grid named "tightenSegs"
            System.Action<System.Windows.Controls.UIElementCollection> search = null;
            search = (children) =>
            {
                foreach (var child in children)
                {
                    var g = child as System.Windows.Controls.Grid;
                    if (g != null && g.Name == "tightenSegs")
                    {
                        int segIdx = 0;
                        foreach (var gc in g.Children)
                        {
                            if (gc is System.Windows.Controls.Border seg && segIdx < 4)
                            {
                                Color c = (Color)System.Windows.Media.ColorConverter
                                    .ConvertFromString(segHex[segIdx]);
                                seg.Background = (_uiDirInt != 0 && segIdx <= activeStep)
                                    ? new SolidColorBrush(c)
                                    : new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x2E));
                                segIdx++;
                            }
                        }
                        return;
                    }
                    var sp = child as System.Windows.Controls.StackPanel;
                    if (sp != null) search(sp.Children);
                }
            };
            search(_bodyStack.Children);
        }

        private void OnAutoTrailToggle(object s, RoutedEventArgs e)
        {
            _autoTrailEnabled = !_autoTrailEnabled;
            RefreshAutoTrailUI();
        }

        private void RefreshAutoTrailUI()
        {
            if (_autoTrailBtn == null) return;
            if (_autoTrailEnabled)
            {
                _autoTrailBtn.Content     = "AUTO TRAIL: ON  ✓";
                _autoTrailBtn.Foreground  = new SolidColorBrush(Color.FromRgb(0x00,0xFF,0x80));
                _autoTrailBtn.Background  = new SolidColorBrush(Color.FromRgb(0x08,0x28,0x14));
                _autoTrailBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x20,0x80,0x40));
                if (_tightenSection != null) _tightenSection.Visibility = Visibility.Visible;
                if (_trailOffMsg    != null) _trailOffMsg.Visibility    = Visibility.Collapsed;
            }
            else
            {
                _autoTrailBtn.Content     = "AUTO TRAIL: OFF";
                _autoTrailBtn.Foreground  = new SolidColorBrush(Color.FromRgb(0xC0,0xC0,0xC0));
                _autoTrailBtn.Background  = new SolidColorBrush(Color.FromRgb(0x18,0x18,0x28));
                _autoTrailBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x35,0x35,0x55));
                if (_tightenSection != null) _tightenSection.Visibility = Visibility.Collapsed;
                if (_trailOffMsg    != null) _trailOffMsg.Visibility    = Visibility.Visible;
            }
        }

        private void OnFlattenClick(object s, RoutedEventArgs e)
        {
            try
            {
                Account acct = _cachedAccount
                    ?? Account.All.FirstOrDefault(a => a.ConnectionStatus == ConnectionStatus.Connected);
                if (acct == null || Instrument == null) return;
                Position pos = acct.Positions.FirstOrDefault(
                    p => p.Instrument?.FullName == Instrument.FullName);
                if (pos == null || pos.MarketPosition == MarketPosition.Flat) return;

                var working = acct.Orders.Where(o => o != null
                    && o.Instrument?.FullName == Instrument.FullName
                    && (o.OrderState == OrderState.Working
                        || o.OrderState == OrderState.Accepted
                        || o.OrderState == OrderState.PartFilled)).ToList();
                foreach (var o in working) acct.Cancel(new[] { o });

                int qty = pos.Quantity;
                OrderAction closeAction = pos.MarketPosition == MarketPosition.Long
                    ? OrderAction.Sell : OrderAction.BuyToCover;
                acct.CreateOrder(Instrument, closeAction, OrderType.Market, OrderEntry.Manual,
                    TimeInForce.Day, qty, 0, 0, null, "RunnerFlatten", DateTime.MinValue, null);
            }
            catch (Exception ex) { if (PrintDebug) Print("[ATMTrail] Flatten error: " + ex.Message); }
        }

        private void OnTabChanged(object s, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_floatingPanel != null) _floatingPanel.Visibility = Visibility.Visible;
        }

        private void OnTitleMouseDown(object s, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_floatingPanel == null) return;
            _dragStart  = e.GetPosition(_floatingPanel.Parent as UIElement);
            _isDragging = true;
            (s as UIElement)?.CaptureMouse();
        }

        private void OnTitleMouseMove(object s, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging || _floatingPanel == null) return;
            var c = e.GetPosition(_floatingPanel.Parent as UIElement);
            var d = c - _dragStart;
            _dragStart = c;
            _floatingPanel.Margin = new Thickness(
                _floatingPanel.Margin.Left + d.X,
                _floatingPanel.Margin.Top  + d.Y, 0, 0);
        }

        private void OnTitleMouseUp(object s, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            (s as UIElement)?.ReleaseMouseCapture();
        }

        private void SetPillMode(bool pill)
        {
            _isPilled = pill;
            if (_bodyStack != null)
                _bodyStack.Visibility = pill ? Visibility.Collapsed : Visibility.Visible;
        }
        #endregion

        #region Properties
        [NinjaScriptProperty]
        [Display(Name="Panel Left (px)", Order=0, GroupName="1 Panel")]
        public int PanelLeft { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Panel Top (px)", Order=1, GroupName="1 Panel")]
        public int PanelTop { get; set; }

        [NinjaScriptProperty][Range(150,500)]
        [Display(Name="Panel Width (px)", Order=2, GroupName="1 Panel")]
        public int PanelWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Runner Stop Order Name", Order=0, GroupName="2 Runner")]
        public string RunnerStopName { get; set; }


        [NinjaScriptProperty][Range(1,200)]
        [Display(Name="ATR Period", Order=0, GroupName="3 Trail Settings")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty][Range(0.1,10.0)]
        [Display(Name="ATR Multiplier (Initial)", Order=1, GroupName="3 Trail Settings")]
        public double AtrMultiplier { get; set; }

        [NinjaScriptProperty][Range(0,int.MaxValue)]
        [Display(Name="Activation Ticks", Order=2, GroupName="3 Trail Settings")]
        public int AtrActivationTicks { get; set; }

        [NinjaScriptProperty][Range(1,int.MaxValue)]
        [Display(Name="Step 1 Trigger Ticks", Order=3, GroupName="3 Trail Settings")]
        public int Step1Ticks { get; set; }

        [NinjaScriptProperty][Range(0.1,10.0)]
        [Display(Name="Step 1 Multiplier", Order=4, GroupName="3 Trail Settings")]
        public double Step1Mult { get; set; }

        [NinjaScriptProperty][Range(1,int.MaxValue)]
        [Display(Name="Step 2 Trigger Ticks", Order=5, GroupName="3 Trail Settings")]
        public int Step2Ticks { get; set; }

        [NinjaScriptProperty][Range(0.1,10.0)]
        [Display(Name="Step 2 Multiplier", Order=6, GroupName="3 Trail Settings")]
        public double Step2Mult { get; set; }

        [NinjaScriptProperty][Range(1,int.MaxValue)]
        [Display(Name="Step 3 Trigger Ticks", Order=7, GroupName="3 Trail Settings")]
        public int Step3Ticks { get; set; }

        [NinjaScriptProperty][Range(0.1,10.0)]
        [Display(Name="Step 3 Multiplier", Order=8, GroupName="3 Trail Settings")]
        public double Step3Mult { get; set; }

        [NinjaScriptProperty][Range(1,int.MaxValue)]
        [Display(Name="Step 4 Trigger Ticks", Order=9, GroupName="3 Trail Settings")]
        public int Step4Ticks { get; set; }

        [NinjaScriptProperty][Range(0.1,10.0)]
        [Display(Name="Step 4 Multiplier", Order=10, GroupName="3 Trail Settings")]
        public double Step4Mult { get; set; }


        [NinjaScriptProperty][Range(1,50)]
        [Display(Name="Min Stop Distance Ticks", Order=0, GroupName="4 Safety")]
        public int MinStopDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Show Trade Drawings", Order=1, GroupName="4 Safety")]
        public bool ShowTradeDrawings { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Print Debug", Order=2, GroupName="4 Safety")]
        public bool PrintDebug { get; set; }
        #endregion
    }
}
