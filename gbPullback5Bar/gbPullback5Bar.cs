#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// Enum used as a [NinjaScriptProperty] type must live at global scope (no namespace) — NT8's
// generated-code region emits custom enum parameter types unqualified, so a namespaced enum
// can't resolve from there. See GreyBeard-Typical-NinjaTrader.md §6.1.
public enum GbPanelCorner
{
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight
}

namespace NinjaTrader.NinjaScript.Strategies.GreyBeard
{
	// 5-bar reversal/pullback continuation strategy. Waits for a bar against the prevailing
	// trend (the "pullback" bar), then enters in the trend direction if a later bar (within
	// PullbackBarLimit bars of the pullback bar) closes back through the pullback bar's open.
	// Stop goes beyond the pullback bar's extreme by StopOffsetTicks; target is a fixed
	// ProfitTargetTicks. Original implementation — not derived from any third-party source.
	public class gbPullback5Bar : Strategy, ICustomTypeDescriptor
	{
		private enum PendingDirection
		{
			None = 0,
			Up = 1,
			Down = -1
		}

		#region Developer

		[Display(Name = "Author", Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.1";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";

		#endregion

		#region Pullback state machine

		private PendingDirection pendingTrend = PendingDirection.None;
		private int barsSincePullback;
		private double pullbackBarOpen;
		private double pullbackBarHigh;
		private double pullbackBarLow;
		private int lastBarDirection;

		#endregion

		#region Trade tracking (shared with dashboard / manual commands)

		private string currentSignalName = "";
		private double entryPrice;
		private double currentStopPrice;
		private double currentTargetPrice;
		private int _onOrderRejects;

		#endregion

		#region Manual dashboard command flags (UI thread writes, strategy thread drains)

		private volatile bool _autoEnabled = true;
		private volatile bool _longEnabled = true;
		private volatile bool _shortEnabled = true;
		private volatile bool _pendingFlatten;
		private volatile bool _pendingBE;
		private volatile bool _pendingReverse;
		private int _pendingStopNudgeTicks;
		private int _pendingTargetNudgeTicks;

		#endregion

		#region Dashboard fields

		private Border _dashPanel;
		private Border _dashTitleBar;
		private StackPanel _dashBody;
		private StackPanel _dashTradeInfo;
		private Thumb _dragThumb;
		private System.Windows.Shapes.Path _pillPath;
		private Border _pillBtn;
		private TextBlock _dashStatus, _dashBias;
		private TextBlock _dashInstrument, _dashWindow, _dashWindowState, _dashDaily;
		private TextBlock _dashEntry, _dashStop, _dashTarget, _dashQty, _dashPnl;
		private bool _dashMinimized;
		private bool _uiInitialized;
		private volatile bool _dashTornDown;
		private DateTime _lastDashPushUtc = DateTime.MinValue;
		private const int DASH_PUSH_MIN_MS = 150;
		private volatile bool _dashPushInFlight;

		private Button _autoBtn, _longBtn, _shortBtn, _revBtn, _beBtn, _flattenBtn;
		private Button _slDownBtn, _slUpBtn, _tpDownBtn, _tpUpBtn;
		private StackPanel _nudgeRow;

		// Frozen palette — created once, shared, never mutated.
		private static readonly SolidColorBrush DashBg      = MakeFrozen(0xF0, 0x14, 0x18, 0x16);
		private static readonly SolidColorBrush DashBorder  = MakeFrozen(0xFF, 0x2A, 0x4A, 0x3C);
		private static readonly SolidColorBrush DashTitleBg = MakeFrozen(0xFF, 0x1A, 0x2E, 0x24);
		private static readonly SolidColorBrush DashTitleFg = MakeFrozen(0xFF, 0x6A, 0xE6, 0xB4);
		private static readonly SolidColorBrush DashDimFg   = MakeFrozen(0xFF, 0x9A, 0xA6, 0x9E);
		private static readonly SolidColorBrush DashSep     = MakeFrozen(0xFF, 0x28, 0x3A, 0x32);

		private static readonly SolidColorBrush BtnInactBg  = MakeFrozen(0xFF, 0x1E, 0x24, 0x20);
		private static readonly SolidColorBrush BtnInactBdr = MakeFrozen(0xFF, 0x3E, 0x4A, 0x42);
		private static readonly SolidColorBrush BtnLongBg   = MakeFrozen(0xFF, 0x0D, 0x30, 0x1A);
		private static readonly SolidColorBrush BtnLongBdr  = MakeFrozen(0xFF, 0x28, 0xC8, 0x60);
		private static readonly SolidColorBrush BtnShortBg  = MakeFrozen(0xFF, 0x30, 0x0D, 0x0D);
		private static readonly SolidColorBrush BtnShortBdr = MakeFrozen(0xFF, 0xC8, 0x20, 0x28);
		private static readonly SolidColorBrush BtnAutoBg   = MakeFrozen(0xFF, 0x0A, 0x3A, 0x2E);
		private static readonly SolidColorBrush BtnAutoBdr  = MakeFrozen(0xFF, 0x3A, 0xE6, 0xB0);
		private static readonly SolidColorBrush BtnFg       = MakeFrozen(0xFF, 0xD0, 0xDA, 0xD4);
		private static readonly SolidColorBrush BtnFlatFg   = MakeFrozen(0xFF, 0xFF, 0x60, 0x50);

		private static SolidColorBrush MakeFrozen(byte a, byte r, byte g, byte b)
		{
			var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
			br.Freeze();
			return br;
		}

		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Standalone 5-bar reversal/pullback continuation strategy." +
					" Waits for a bar against the prevailing trend (the 'pullback' bar), then" +
					" enters in the trend direction if a later bar (within PullbackBarLimit bars" +
					" of the pullback bar) closes back through the pullback bar's open. Stop goes" +
					" beyond the pullback bar's extreme by StopOffsetTicks; target is a fixed" +
					" ProfitTargetTicks.";
				Name = "gbPullback5Bar";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = false;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 5;
				IsInstantiatedOnEachOptimizationIteration = true;

				PullbackBarLimit = 5;
				StopOffsetTicks = 2;
				ProfitTargetTicks = 50;
				TakeLongs = true;
				TakeShorts = true;
				Contracts = 1;

				EnableSession = true;
				SessionStart = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				SessionEnd = DateTime.Parse("16:45", System.Globalization.CultureInfo.InvariantCulture);

				ManualNudgeTicks = 4;
				ManualBeOffsetTicks = 0;
				DailyProfitTarget = 0;
				DailyMaxLoss = 0;

				ShowDashboard = true;
				DashboardCorner = GbPanelCorner.TopLeft;
				DashboardStartMinimized = false;
			}
			else if (State == State.DataLoaded)
			{
				_longEnabled = TakeLongs;
				_shortEnabled = TakeShorts;
			}
			else if (State == State.Realtime)
			{
				if (ShowDashboard)
					CreateDashboard();
			}
			else if (State == State.Terminated)
			{
				RemoveDashboard();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if (Bars.IsFirstBarOfSession)
			{
				pendingTrend = PendingDirection.None;
				lastBarDirection = 0;
			}

			if (Position.MarketPosition == MarketPosition.Flat && currentSignalName.Length > 0)
				ResetTradeState();

			bool inSession = !EnableSession || IsInSession(Time[0]);
			if (!inSession)
				return;

			bool dailyLimitHit = IsDailyLimitHit();

			int currentDirection = Math.Sign(Close[0] - Open[0]);
			bool enteredThisBar = false;

			// 1) Age / check any pending pullback setup using the bar that just closed.
			if (pendingTrend != PendingDirection.None)
			{
				barsSincePullback++;

				if (barsSincePullback > PullbackBarLimit)
				{
					pendingTrend = PendingDirection.None;
				}
				else if (!dailyLimitHit && Position.MarketPosition == MarketPosition.Flat && _autoEnabled)
				{
					bool confirmedLong = pendingTrend == PendingDirection.Up && Close[0] > pullbackBarOpen;
					bool confirmedShort = pendingTrend == PendingDirection.Down && Close[0] < pullbackBarOpen;

					if (confirmedLong && TakeLongs && _longEnabled)
					{
						EnterDirection(1, pullbackBarHigh, pullbackBarLow);
						pendingTrend = PendingDirection.None;
						enteredThisBar = true;
					}
					else if (confirmedShort && TakeShorts && _shortEnabled)
					{
						EnterDirection(-1, pullbackBarHigh, pullbackBarLow);
						pendingTrend = PendingDirection.None;
						enteredThisBar = true;
					}
				}
			}

			// 2) Look for a brand-new reversal ("pullback") bar: a bar whose direction flips
			//    against the previously established bar direction. The bar that just triggered
			//    an entry is never itself a pullback bar — otherwise every confirmation bar would
			//    immediately arm the opposite-direction setup.
			if (pendingTrend == PendingDirection.None
				&& !enteredThisBar
				&& currentDirection != 0
				&& lastBarDirection != 0
				&& currentDirection != lastBarDirection)
			{
				pendingTrend = lastBarDirection > 0 ? PendingDirection.Up : PendingDirection.Down;
				pullbackBarOpen = Open[0];
				pullbackBarHigh = High[0];
				pullbackBarLow = Low[0];
				barsSincePullback = 1;
			}

			if (currentDirection != 0)
				lastBarDirection = currentDirection;

			UpdateDashboard();
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			try
			{
				if (State != State.Realtime || BarsInProgress != 0) return;
				if (marketDataUpdate.MarketDataType != MarketDataType.Last) return;

				// Manual dashboard commands run on the strategy thread, immediately — a panic
				// FLATTEN or BE move must act now, not wait for the next bar close.
				ProcessDashboardCommands(marketDataUpdate.Price);
				UpdateDashboard();
			}
			catch { /* never let a tick throw into NT8 */ }
		}

		// The profit target is set in ticks, so NT places it relative to the actual fill; the
		// Close[0]-based estimate from EnterDirection is replaced here with the real fill price
		// so the dashboard and the SL/TP nudge buttons work from the live target level.
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price,
			int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			Order o = execution.Order;
			if (o == null || currentSignalName.Length == 0 || o.Name != currentSignalName)
				return;
			if (o.OrderAction != OrderAction.Buy && o.OrderAction != OrderAction.SellShort)
				return;

			double fill = o.AverageFillPrice > 0 ? o.AverageFillPrice : price;
			if (fill <= 0) return;

			entryPrice = fill;
			currentTargetPrice = o.OrderAction == OrderAction.Buy
				? fill + ProfitTargetTicks * TickSize
				: fill - ProfitTargetTicks * TickSize;
			UpdateDashboard(true);
		}

		private void EnterDirection(int dir, double refHigh, double refLow)
		{
			string sig = dir > 0 ? "gbPullback5BarLong" : "gbPullback5BarShort";
			double stopPx = dir > 0
				? refLow - StopOffsetTicks * TickSize
				: refHigh + StopOffsetTicks * TickSize;

			SetStopLoss(sig, CalculationMode.Price, stopPx, false);
			SetProfitTarget(sig, CalculationMode.Ticks, ProfitTargetTicks);

			if (dir > 0) EnterLong(Contracts, sig);
			else EnterShort(Contracts, sig);

			DrawTriggerArrow(dir);

			currentSignalName = sig;
			entryPrice = Close[0];
			currentStopPrice = stopPx;
			currentTargetPrice = dir > 0
				? entryPrice + ProfitTargetTicks * TickSize
				: entryPrice - ProfitTargetTicks * TickSize;
		}

		// Marks the bar whose close confirmed entry — the trigger bar, not the pullback bar.
		private void DrawTriggerArrow(int dir)
		{
			string tag = "gbPullbackTrigger" + CurrentBar;
			double offset = 4 * TickSize;
			if (dir > 0)
				Draw.ArrowUp(this, tag, false, 0, Low[0] - offset, Brushes.LimeGreen);
			else
				Draw.ArrowDown(this, tag, false, 0, High[0] + offset, Brushes.Crimson);
		}

		private void ResetTradeState()
		{
			currentSignalName = "";
			entryPrice = 0.0;
			currentStopPrice = 0.0;
			currentTargetPrice = 0.0;
		}

		private bool IsInSession(DateTime barTime)
		{
			int t = ToTime(barTime);
			int start = ToTime(SessionStart);
			int end = ToTime(SessionEnd);
			return start <= end
				? t >= start && t <= end
				: t >= start || t <= end;
		}

		private bool IsDailyLimitHit()
		{
			if (DailyProfitTarget <= 0 && DailyMaxLoss <= 0)
				return false;

			double pnl = GetTodaysRealizedPnL();

			if (DailyProfitTarget > 0 && pnl >= DailyProfitTarget)
				return true;

			if (DailyMaxLoss > 0 && pnl <= -DailyMaxLoss)
				return true;

			return false;
		}

		// Live, only real-time trades count: AllTrades also holds the virtual trades from the
		// historical backfill, which would show a fake day P&L and could trip the daily limits.
		// Historical (backtest / backfill) keeps AllTrades so the simulation stays consistent.
		private double GetTodaysRealizedPnL()
		{
			double sum = 0;
			DateTime today = Time[0].Date;
			TradeCollection trades = State == State.Realtime
				? SystemPerformance.RealTimeTrades
				: SystemPerformance.AllTrades;

			for (int i = trades.Count - 1; i >= 0; i--)
			{
				Trade trade = trades[i];
				if (trade.Exit.Time.Date != today)
					break;

				sum += trade.ProfitCurrency;
			}

			return sum;
		}

		#region Manual stop/target management

		// Process the volatile flags the UI thread sets. Runs on the strategy thread (called
		// from OnMarketData) so it can act tick-by-tick rather than waiting for a bar close.
		private void ProcessDashboardCommands(double lastPrice)
		{
			if (_pendingFlatten)
			{
				_pendingFlatten = false;
				if (Position.MarketPosition != MarketPosition.Flat)
					FlattenAll("Manual");
				UpdateDashboard(true);
			}
			if (_pendingReverse)
			{
				_pendingReverse = false;
				if (Position.MarketPosition != MarketPosition.Flat)
					ReverseNow(lastPrice);
				UpdateDashboard(true);
			}
			if (_pendingBE)
			{
				_pendingBE = false;
				if (Position.MarketPosition != MarketPosition.Flat)
					MoveToBreakeven();
				UpdateDashboard(true);
			}
			DrainManualNudges();
		}

		// Drain the nudge-button clicks the UI thread accumulated (net ticks) and apply once.
		private void DrainManualNudges()
		{
			int sn = Interlocked.Exchange(ref _pendingStopNudgeTicks, 0);
			int tn = Interlocked.Exchange(ref _pendingTargetNudgeTicks, 0);
			if (Position.MarketPosition == MarketPosition.Flat) return;
			if (sn != 0) ApplyManualStop(NudgeStopBase() + sn * TickSize);
			if (tn != 0) ApplyManualTarget(NudgeTargetBase() + tn * TickSize);
		}

		private double NudgeStopBase() => currentStopPrice > 0 ? currentStopPrice : Position.AveragePrice;

		private double NudgeTargetBase() => currentTargetPrice > 0 ? currentTargetPrice : Position.AveragePrice;

		private bool ApplyManualStop(double price)
		{
			if (Position.MarketPosition == MarketPosition.Flat || currentSignalName.Length == 0) return false;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double px = Instrument.MasterInstrument.RoundToTickSize(price);
			double mk = Close[0];
			bool validSide = isLong ? px < mk : px > mk;
			if (!validSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[gbPullback5Bar] Manual SL ignored — wrong side of price (" + px.ToString("F2") + " vs " + mk.ToString("F2") + ").");
				return false;
			}
			SetStopLoss(currentSignalName, CalculationMode.Price, px, false);
			currentStopPrice = px;
			return true;
		}

		private bool ApplyManualTarget(double price)
		{
			if (Position.MarketPosition == MarketPosition.Flat || currentSignalName.Length == 0) return false;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double px = Instrument.MasterInstrument.RoundToTickSize(price);
			double mk = Close[0];
			bool profitSide = isLong ? px > mk : px < mk;
			if (!profitSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[gbPullback5Bar] Manual TP ignored — not on the profit side (" + px.ToString("F2") + " vs " + mk.ToString("F2") + ").");
				return false;
			}
			SetProfitTarget(currentSignalName, CalculationMode.Price, px);
			currentTargetPrice = px;
			return true;
		}

		// Manual "MOVE SL TO BE" — create-or-tighten the stop to entry +/- ManualBeOffsetTicks
		// (signed; negative locks in a small loss). Never drags the stop backward into more risk.
		private void MoveToBreakeven()
		{
			if (Position.MarketPosition == MarketPosition.Flat) return;
			double avg = Position.AveragePrice;
			if (avg <= 0) return;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double be = isLong ? avg + ManualBeOffsetTicks * TickSize : avg - ManualBeOffsetTicks * TickSize;
			be = Instrument.MasterInstrument.RoundToTickSize(be);
			double mk = Close[0];
			bool validSide = isLong ? be < mk : be > mk;
			if (!validSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[gbPullback5Bar] MOVE SL TO BE skipped — price already past breakeven.");
				return;
			}
			bool better = currentStopPrice <= 0 || (isLong ? be > currentStopPrice : be < currentStopPrice);
			if (!better)
			{
				if (_onOrderRejects++ < 10)
					Print("[gbPullback5Bar] MOVE SL TO BE skipped — current stop is already at or beyond that level.");
				return;
			}
			SetStopLoss(currentSignalName, CalculationMode.Price, be, false);
			currentStopPrice = be;
		}

		// Manual REV, applied on this tick. Managed EnterLong/EnterShort against an open
		// opposite position closes it and opens the new side in one step, so there is no
		// wait for a bar close. There is no pullback bar for the new side, so the stop
		// anchors on the last closed bar's extreme — widened to the live price if price has
		// already run past it — so the stop always lands on the valid side of the market.
		private void ReverseNow(double lastPrice)
		{
			int newDir = Position.MarketPosition == MarketPosition.Long ? -1 : 1;
			double px = lastPrice > 0 ? lastPrice : Close[0];
			double refHigh = Math.Max(High[0], px);
			double refLow = Math.Min(Low[0], px);
			EnterDirection(newDir, refHigh, refLow);
		}

		private void FlattenAll(string reason)
		{
			if (Position.MarketPosition == MarketPosition.Long) ExitLong("Flat_" + reason, "");
			else if (Position.MarketPosition == MarketPosition.Short) ExitShort("Flat_" + reason, "");
		}

		#endregion

		#region Dashboard build / teardown / update

		private void CreateDashboard()
		{
			if (ChartControl == null || _uiInitialized) return;

			_dashTornDown = false;
			_dashMinimized = DashboardStartMinimized;
			GbPanelCorner corner = DashboardCorner;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (_uiInitialized || _dashTornDown || State == State.Terminated)
						return;

					_dragThumb = new Thumb { Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
					var thumbFac = new FrameworkElementFactory(typeof(Border));
					thumbFac.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
					_dragThumb.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = thumbFac };
					_dragThumb.DragDelta += OnPanelDragDelta;

					var titleText = new TextBlock
					{
						Text = "gbPullback5Bar  v" + Version,
						Foreground = DashTitleFg,
						FontSize = 11,
						FontWeight = FontWeights.Bold,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(4, 0, 30, 0),
						IsHitTestVisible = false
					};

					_pillPath = new System.Windows.Shapes.Path
					{
						Stroke = DashDimFg,
						StrokeThickness = 1.5,
						Fill = null,
						StrokeLineJoin = PenLineJoin.Round,
						Opacity = _dashMinimized ? 0.9 : 0.5,
						IsHitTestVisible = false,
						Data = Geometry.Parse("M 3,0 L 15,0 A 3,3 0 0 1 15,6 L 3,6 A 3,3 0 0 1 3,0 Z"),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					};
					_pillBtn = new Border
					{
						Width = 22,
						Height = 12,
						Margin = new Thickness(0, 0, 8, 0),
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Background = Brushes.Transparent,
						Cursor = Cursors.Hand,
						ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize",
						Child = _pillPath
					};
					_pillBtn.MouseLeftButtonDown += OnPillMouseDown;
					_pillBtn.MouseLeftButtonUp += OnPillMouseUp;
					_pillBtn.MouseEnter += OnPillMouseEnter;
					_pillBtn.MouseLeave += OnPillMouseLeave;

					var titleGrid = new Grid();
					titleGrid.Children.Add(_dragThumb);
					titleGrid.Children.Add(titleText);
					titleGrid.Children.Add(_pillBtn);

					_dashTitleBar = new Border
					{
						Background = DashTitleBg,
						Height = 24,
						CornerRadius = new CornerRadius(8, 8, 0, 0),
						Child = titleGrid,
						ToolTip = "Drag to move"
					};

					var statusRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 2) };
					_dashStatus = new TextBlock { Text = "FLAT", Foreground = Brushes.DimGray, FontSize = 12, FontWeight = FontWeights.Bold };
					_dashBias = new TextBlock { Text = "", Foreground = Brushes.DimGray, FontSize = 12 };
					statusRow.Children.Add(_dashStatus);
					statusRow.Children.Add(_dashBias);

					_dashInstrument = MakeInfoRow(DashDimFg);
					_dashWindow = MakeInfoRow(Brushes.WhiteSmoke);
					_dashWindowState = MakeInfoRow(Brushes.Orange);
					_dashDaily = MakeInfoRow(Brushes.WhiteSmoke);

					_dashTradeInfo = new StackPanel { Orientation = Orientation.Vertical, Visibility = Visibility.Collapsed };
					_dashTradeInfo.Children.Add(MakeSeparator());
					_dashEntry = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashEntry);
					_dashStop = MakeInfoRow(Brushes.Salmon); _dashTradeInfo.Children.Add(_dashStop);
					_dashTarget = MakeInfoRow(Brushes.LimeGreen); _dashTradeInfo.Children.Add(_dashTarget);
					_dashQty = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashQty);
					_dashPnl = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashPnl);

					_autoBtn = MakeDashButton("AUTO: ON", 94, 26);
					_longBtn = MakeDashButton("LONG: ON", 94, 26);
					_shortBtn = MakeDashButton("SHORT: ON", 94, 26);
					RestyleToggle(_autoBtn, "AUTO", _autoEnabled, BtnAutoBg, BtnAutoBdr);
					RestyleToggle(_longBtn, "LONG", _longEnabled, BtnLongBg, BtnLongBdr);
					RestyleToggle(_shortBtn, "SHORT", _shortEnabled, BtnShortBg, BtnShortBdr);
					_longBtn.IsEnabled = _autoEnabled;
					_shortBtn.IsEnabled = _autoEnabled;
					_autoBtn.Click += OnAutoToggleClick;
					_longBtn.Click += OnLongToggleClick;
					_shortBtn.Click += OnShortToggleClick;
					var toggleRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
					toggleRow.Children.Add(_autoBtn);
					toggleRow.Children.Add(_longBtn);
					toggleRow.Children.Add(_shortBtn);

					_revBtn = MakeDashButton("REV", 294, 24);
					_revBtn.Click += OnReverseClick;

					_beBtn = MakeDashButton("MOVE SL TO BE", 294, 24);
					_beBtn.Click += OnBEClick;

					_nudgeRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
					_slDownBtn = MakeDashButton("SL ▼", 70, 24);
					_slUpBtn   = MakeDashButton("SL ▲", 70, 24);
					_tpDownBtn = MakeDashButton("TP ▼", 70, 24);
					_tpUpBtn   = MakeDashButton("TP ▲", 70, 24);
					_slDownBtn.Click += OnSlDownClick;
					_slUpBtn.Click   += OnSlUpClick;
					_tpDownBtn.Click += OnTpDownClick;
					_tpUpBtn.Click   += OnTpUpClick;
					_nudgeRow.Children.Add(_slDownBtn);
					_nudgeRow.Children.Add(_slUpBtn);
					_nudgeRow.Children.Add(_tpDownBtn);
					_nudgeRow.Children.Add(_tpUpBtn);

					_flattenBtn = MakeDashButton("FLATTEN ALL", 294, 26);
					_flattenBtn.Background = BtnShortBg;
					_flattenBtn.BorderBrush = BtnShortBdr;
					_flattenBtn.Foreground = BtnFlatFg;
					_flattenBtn.Click += OnFlattenClick;

					_dashBody = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210, Margin = new Thickness(0, 0, 0, 6) };
					_dashBody.Children.Add(statusRow);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(_dashInstrument);
					_dashBody.Children.Add(_dashWindow);
					_dashBody.Children.Add(_dashWindowState);
					_dashBody.Children.Add(_dashDaily);
					_dashBody.Children.Add(_dashTradeInfo);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(toggleRow);
					_dashBody.Children.Add(_revBtn);
					_dashBody.Children.Add(_beBtn);
					_dashBody.Children.Add(_nudgeRow);
					_dashBody.Children.Add(_flattenBtn);
					if (_dashMinimized)
						_dashBody.Visibility = Visibility.Collapsed;

					var main = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210 };
					main.Children.Add(_dashTitleBar);
					main.Children.Add(_dashBody);

					_dashPanel = new Border
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Margin = new Thickness(10, 10, 0, 0),
						Background = DashBg,
						BorderBrush = DashBorder,
						BorderThickness = new Thickness(2),
						CornerRadius = new CornerRadius(10),
						ClipToBounds = true,
						Child = main
					};

					EventHandler layoutHandler = null;
					layoutHandler = (ls, le) =>
					{
						if (_dashPanel == null) return;
						var parent = _dashPanel.Parent as FrameworkElement;
						if (parent == null || _dashPanel.ActualWidth <= 0 || parent.ActualWidth <= 0) return;
						double left = 10, top = 10;
						switch (corner)
						{
							case GbPanelCorner.TopRight:
								left = parent.ActualWidth - _dashPanel.ActualWidth - 10; break;
							case GbPanelCorner.BottomLeft:
								top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
							case GbPanelCorner.BottomRight:
								left = parent.ActualWidth - _dashPanel.ActualWidth - 10;
								top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
						}
						_dashPanel.Margin = new Thickness(Math.Max(0, left), Math.Max(0, top), 0, 0);
						_dashPanel.LayoutUpdated -= layoutHandler;
					};
					_dashPanel.LayoutUpdated += layoutHandler;

					UserControlCollection.Add(_dashPanel);
					_uiInitialized = true;
				}
				catch (Exception ex)
				{
					Print("[gbPullback5Bar] Dashboard create error: " + ex.Message);
				}
			});
		}

		private void RemoveDashboard()
		{
			_dashTornDown = true;
			if (ChartControl == null || (_dashPanel == null && _dragThumb == null))
				return;

			Action teardown = () =>
			{
				try
				{
					if (_dragThumb != null) _dragThumb.DragDelta -= OnPanelDragDelta;
					if (_pillBtn != null)
					{
						_pillBtn.MouseLeftButtonDown -= OnPillMouseDown;
						_pillBtn.MouseLeftButtonUp -= OnPillMouseUp;
						_pillBtn.MouseEnter -= OnPillMouseEnter;
						_pillBtn.MouseLeave -= OnPillMouseLeave;
					}
					if (_autoBtn != null) _autoBtn.Click -= OnAutoToggleClick;
					if (_longBtn != null) _longBtn.Click -= OnLongToggleClick;
					if (_shortBtn != null) _shortBtn.Click -= OnShortToggleClick;
					if (_revBtn != null) _revBtn.Click -= OnReverseClick;
					if (_beBtn != null) _beBtn.Click -= OnBEClick;
					if (_slDownBtn != null) _slDownBtn.Click -= OnSlDownClick;
					if (_slUpBtn != null) _slUpBtn.Click -= OnSlUpClick;
					if (_tpDownBtn != null) _tpDownBtn.Click -= OnTpDownClick;
					if (_tpUpBtn != null) _tpUpBtn.Click -= OnTpUpClick;
					if (_flattenBtn != null) _flattenBtn.Click -= OnFlattenClick;
					if (_dashPanel != null && UserControlCollection.Contains(_dashPanel))
						UserControlCollection.Remove(_dashPanel);
				}
				catch { }
				finally
				{
					_dragThumb = null;
					_pillBtn = null; _pillPath = null;
					_autoBtn = _longBtn = _shortBtn = _revBtn = _beBtn = _flattenBtn = null;
					_slDownBtn = _slUpBtn = _tpDownBtn = _tpUpBtn = null;
					_nudgeRow = null;
					_dashStatus = _dashBias = null;
					_dashInstrument = _dashWindow = _dashWindowState = _dashDaily = null;
					_dashEntry = _dashStop = _dashTarget = _dashQty = _dashPnl = null;
					_dashTradeInfo = null;
					_dashBody = null;
					_dashTitleBar = null;
					_dashPanel = null;
					_uiInitialized = false;
				}
			};

			try
			{
				if (ChartControl.Dispatcher.CheckAccess()) teardown();
				else ChartControl.Dispatcher.Invoke(teardown);
			}
			catch { /* Terminated must never throw */ }
		}

		private void UpdateDashboard(bool force = false)
		{
			if (ChartControl == null || !_uiInitialized) return;

			DateTime nowUtc = DateTime.UtcNow;
			if (!force && (nowUtc - _lastDashPushUtc).TotalMilliseconds < DASH_PUSH_MIN_MS)
				return;
			if (_dashPushInFlight)
				return;
			_lastDashPushUtc = nowUtc;
			_dashPushInFlight = true;

			bool isLong = Position.MarketPosition == MarketPosition.Long;
			bool isShort = Position.MarketPosition == MarketPosition.Short;
			bool inPos = isLong || isShort;

			string posText = inPos ? (isLong ? "LONG" : "SHORT") : "FLAT";
			Brush posBrush = isLong ? Brushes.LimeGreen : isShort ? Brushes.Crimson : Brushes.DimGray;

			string biasText = pendingTrend == PendingDirection.None
				? ""
				: string.Format("  |  Watching {0} pullback {1}/{2}",
					pendingTrend == PendingDirection.Up ? "UP" : "DOWN", barsSincePullback, PullbackBarLimit);
			Brush biasBrush = pendingTrend == PendingDirection.Up ? Brushes.LimeGreen
				: pendingTrend == PendingDirection.Down ? Brushes.Crimson : Brushes.DimGray;

			string instrText = "Instr  " + (Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "?")
				+ "   Acct  " + (Account != null ? Account.Name : "?");

			string windowText = EnableSession
				? string.Format("Window  {0:HH:mm}-{1:HH:mm}", SessionStart, SessionEnd)
				: "Window  always on";
			string windowStateText;
			Brush windowStateBrush;
			if (IsDailyLimitHit())
			{
				windowStateText = "BLOCKED — daily limit hit";
				windowStateBrush = Brushes.Crimson;
			}
			else if (!_autoEnabled)
			{
				windowStateText = "AUTO OFF — manual only";
				windowStateBrush = Brushes.Orange;
			}
			else
			{
				bool inWin = !EnableSession || (CurrentBar >= 0 && IsInSession(Time[0]));
				windowStateText = inWin ? "Armed — entries enabled" : "Outside window";
				windowStateBrush = inWin ? Brushes.LimeGreen : Brushes.Orange;
			}

			double dpnl = GetTodaysRealizedPnL();
			string dailyText = string.Format("Day PnL {0}${1:F0}", dpnl < 0 ? "-" : "+", Math.Abs(dpnl));
			Brush dailyBrush = dpnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;

			string entryText = "", stopText = "", tgtText = "", qtyText = "", pnlText = "";
			Brush pnlBrush = Brushes.WhiteSmoke;
			if (inPos)
			{
				double avg = Position.AveragePrice;
				entryText = string.Format("Entry   {0:F2}", avg);
				stopText = currentStopPrice > 0 ? string.Format("Stop    {0:F2}", currentStopPrice) : "Stop    none";
				tgtText = currentTargetPrice > 0 ? string.Format("Target  {0:F2}", currentTargetPrice) : "Target  none";
				qtyText = string.Format("Qty     {0}", Position.Quantity);
				double upnl = 0;
				try { upnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]); } catch { }
				pnlText = string.Format("uPnL    {0}${1:F0}", upnl < 0 ? "-" : "+", Math.Abs(upnl));
				pnlBrush = upnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;
			}

			string pt = posText, bt = biasText, it = instrText, wt = windowText, wst = windowStateText, dt = dailyText;
			string et = entryText, st = stopText, tgt = tgtText, qt = qtyText, plt = pnlText;
			Brush pb = posBrush, bb = biasBrush, wsb = windowStateBrush, plb = pnlBrush, db = dailyBrush;
			bool ip = inPos;
			bool autoOn = _autoEnabled, longOn = _longEnabled, shortOn = _shortEnabled;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (!_uiInitialized) return;
					if (_dashStatus != null) { _dashStatus.Text = pt; _dashStatus.Foreground = pb; }
					if (_dashBias != null) { _dashBias.Text = bt; _dashBias.Foreground = bb; }
					if (_dashInstrument != null) _dashInstrument.Text = it;
					if (_dashWindow != null) _dashWindow.Text = wt;
					if (_dashWindowState != null) { _dashWindowState.Text = wst; _dashWindowState.Foreground = wsb; }
					if (_dashDaily != null) { _dashDaily.Text = dt; _dashDaily.Foreground = db; }
					if (_dashTradeInfo != null)
						_dashTradeInfo.Visibility = ip ? Visibility.Visible : Visibility.Collapsed;
					if (ip)
					{
						if (_dashEntry != null) _dashEntry.Text = et;
						if (_dashStop != null) _dashStop.Text = st;
						if (_dashTarget != null) _dashTarget.Text = tgt;
						if (_dashQty != null) _dashQty.Text = qt;
						if (_dashPnl != null) { _dashPnl.Text = plt; _dashPnl.Foreground = plb; }
					}
					RestyleToggle(_autoBtn, "AUTO", autoOn, BtnAutoBg, BtnAutoBdr);
					RestyleToggle(_longBtn, "LONG", longOn, BtnLongBg, BtnLongBdr);
					RestyleToggle(_shortBtn, "SHORT", shortOn, BtnShortBg, BtnShortBdr);
					if (_longBtn != null) _longBtn.IsEnabled = autoOn;
					if (_shortBtn != null) _shortBtn.IsEnabled = autoOn;
					if (_revBtn != null) _revBtn.IsEnabled = ip;
					if (_slDownBtn != null) _slDownBtn.IsEnabled = ip;
					if (_slUpBtn != null) _slUpBtn.IsEnabled = ip;
					if (_tpDownBtn != null) _tpDownBtn.IsEnabled = ip;
					if (_tpUpBtn != null) _tpUpBtn.IsEnabled = ip;
				}
				catch { }
				finally { _dashPushInFlight = false; }
			});
		}

		#endregion

		#region Dashboard widgets + handlers

		private static Button MakeDashButton(string label, double width, double height)
		{
			var btn = new Button
			{
				Width = width,
				Height = height,
				Margin = new Thickness(2),
				MinWidth = 0,
				Cursor = Cursors.Hand,
				FocusVisualStyle = null,
				Padding = new Thickness(0),
				Background = BtnInactBg,
				BorderBrush = BtnInactBdr,
				BorderThickness = new Thickness(1),
				Foreground = BtnFg,
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				Content = label
			};

			var ct = new ControlTemplate(typeof(Button));
			var grid = new FrameworkElementFactory(typeof(Grid), "RootGrid");

			var bf = new FrameworkElementFactory(typeof(Border), "BaseBorder");
			bf.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
			bf.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
			bf.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
			bf.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
			grid.AppendChild(bf);

			var hf = new FrameworkElementFactory(typeof(Border), "HoverOverlay");
			hf.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0x40, 0xE0, 0xA8)));
			hf.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x40, 0xE0, 0xA8)));
			hf.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
			hf.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
			hf.SetValue(UIElement.OpacityProperty, 0.0);
			hf.SetValue(UIElement.IsHitTestVisibleProperty, false);
			grid.AppendChild(hf);

			var cp = new FrameworkElementFactory(typeof(TextBlock));
			cp.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			cp.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
			cp.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
			cp.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
			cp.SetValue(TextBlock.FontSizeProperty, new TemplateBindingExtension(Control.FontSizeProperty));
			cp.SetValue(TextBlock.FontWeightProperty, new TemplateBindingExtension(Control.FontWeightProperty));
			grid.AppendChild(cp);

			ct.VisualTree = grid;
			var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
			hoverTrigger.Setters.Add(new Setter { TargetName = "HoverOverlay", Property = UIElement.OpacityProperty, Value = 1.0 });
			ct.Triggers.Add(hoverTrigger);
			var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
			disabledTrigger.Setters.Add(new Setter { TargetName = "RootGrid", Property = UIElement.OpacityProperty, Value = 0.35 });
			ct.Triggers.Add(disabledTrigger);
			btn.Template = ct;
			return btn;
		}

		private static void RestyleToggle(Button btn, string label, bool on, Brush onBg, Brush onBdr)
		{
			if (btn == null) return;
			btn.Content = label + (on ? ": ON" : ": OFF");
			btn.Background = on ? onBg : BtnInactBg;
			btn.BorderBrush = on ? onBdr : BtnInactBdr;
		}

		private static TextBlock MakeInfoRow(Brush foreground)
		{
			return new TextBlock
			{
				Text = "",
				Foreground = foreground,
				FontSize = 11,
				FontFamily = new FontFamily("Consolas"),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(12, 1, 12, 1)
			};
		}

		private static Border MakeSeparator()
		{
			return new Border { Height = 1, Background = DashSep, Margin = new Thickness(6, 3, 6, 3) };
		}

		private void OnAutoToggleClick(object sender, RoutedEventArgs e)
		{
			_autoEnabled = !_autoEnabled;
			RestyleToggle(_autoBtn, "AUTO", _autoEnabled, BtnAutoBg, BtnAutoBdr);
			if (_longBtn != null) _longBtn.IsEnabled = _autoEnabled;
			if (_shortBtn != null) _shortBtn.IsEnabled = _autoEnabled;
		}

		private void OnLongToggleClick(object sender, RoutedEventArgs e)
		{
			_longEnabled = !_longEnabled;
			RestyleToggle(_longBtn, "LONG", _longEnabled, BtnLongBg, BtnLongBdr);
		}

		private void OnShortToggleClick(object sender, RoutedEventArgs e)
		{
			_shortEnabled = !_shortEnabled;
			RestyleToggle(_shortBtn, "SHORT", _shortEnabled, BtnShortBg, BtnShortBdr);
		}

		private void OnReverseClick(object sender, RoutedEventArgs e)
		{
			_pendingReverse = true;
		}

		private void OnBEClick(object sender, RoutedEventArgs e)
		{
			_pendingBE = true;
		}

		// Nudge clicks only accumulate net ticks here (UI thread); the strategy thread applies
		// them in DrainManualNudges. ▲ always raises the price, ▼ always lowers it.
		private void OnSlDownClick(object sender, RoutedEventArgs e)
		{ Interlocked.Add(ref _pendingStopNudgeTicks, -Math.Max(1, ManualNudgeTicks)); }
		private void OnSlUpClick(object sender, RoutedEventArgs e)
		{ Interlocked.Add(ref _pendingStopNudgeTicks, Math.Max(1, ManualNudgeTicks)); }
		private void OnTpDownClick(object sender, RoutedEventArgs e)
		{ Interlocked.Add(ref _pendingTargetNudgeTicks, -Math.Max(1, ManualNudgeTicks)); }
		private void OnTpUpClick(object sender, RoutedEventArgs e)
		{ Interlocked.Add(ref _pendingTargetNudgeTicks, Math.Max(1, ManualNudgeTicks)); }

		private void OnFlattenClick(object sender, RoutedEventArgs e)
		{
			_pendingFlatten = true;
			_autoEnabled = false;
			RestyleToggle(_autoBtn, "AUTO", false, BtnAutoBg, BtnAutoBdr);
			if (_longBtn != null) _longBtn.IsEnabled = false;
			if (_shortBtn != null) _shortBtn.IsEnabled = false;
		}

		private void OnPillMouseDown(object sender, MouseButtonEventArgs e) { e.Handled = true; }

		private void OnPillMouseUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			_dashMinimized = !_dashMinimized;
			if (_dashBody != null)
				_dashBody.Visibility = _dashMinimized ? Visibility.Collapsed : Visibility.Visible;
			if (_pillPath != null)
				_pillPath.Opacity = _dashMinimized ? 0.9 : 0.5;
			if (_pillBtn != null)
				_pillBtn.ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize";
		}

		private void OnPillMouseEnter(object sender, MouseEventArgs e)
		{
			if (_pillPath != null) _pillPath.Opacity = 1.0;
		}

		private void OnPillMouseLeave(object sender, MouseEventArgs e)
		{
			if (_pillPath != null) _pillPath.Opacity = _dashMinimized ? 0.9 : 0.5;
		}

		private void OnPanelDragDelta(object sender, DragDeltaEventArgs e)
		{
			if (_dashPanel == null) return;
			double newLeft = Math.Max(0, _dashPanel.Margin.Left + e.HorizontalChange);
			double newTop = Math.Max(0, _dashPanel.Margin.Top + e.VerticalChange);
			if (ChartControl != null)
			{
				newLeft = Math.Min(newLeft, Math.Max(0, ChartControl.ActualWidth - _dashPanel.ActualWidth));
				newTop = Math.Min(newTop, Math.Max(0, ChartControl.ActualHeight - _dashPanel.ActualHeight));
			}
			_dashPanel.Margin = new Thickness(newLeft, newTop, 0, 0);
		}

		#endregion

		#region ICustomTypeDescriptor - per-instance property hiding

		AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(GetType());
		string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(GetType());
		string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(GetType());
		TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(GetType());
		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(GetType());
		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(GetType());
		object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(GetType(), editorBaseType);
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(GetType());
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attrs) => TypeDescriptor.GetEvents(GetType(), attrs);
		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
			PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
			orig.CopyTo(arr, 0);
			PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);

			if (!EnableSession) RemoveProperties(col, nameof(SessionStart), nameof(SessionEnd));
			if (!ShowDashboard) RemoveProperties(col, nameof(DashboardCorner), nameof(DashboardStartMinimized));

			return col;
		}

		private void RemoveProperties(PropertyDescriptorCollection col, params string[] names)
		{
			foreach (string n in names)
				if (col[n] != null) col.Remove(col[n]);
		}

		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Pullback bar limit", Description = "Max bars (counting the pullback bar as bar 1) allowed before a confirmation must occur.", Order = 1, GroupName = "1. Strategy")]
		public int PullbackBarLimit { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Stop offset (ticks)", Description = "Extra ticks beyond the pullback bar's high/low used for the stop loss.", Order = 2, GroupName = "1. Strategy")]
		public int StopOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Profit target (ticks)", Description = "Fixed profit target distance in ticks.", Order = 3, GroupName = "1. Strategy")]
		public int ProfitTargetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Take longs", Order = 4, GroupName = "1. Strategy")]
		public bool TakeLongs { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Take shorts", Order = 5, GroupName = "1. Strategy")]
		public bool TakeShorts { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Contracts", Description = "Number of contracts per entry. Overrides the Order Properties quantity.", Order = 6, GroupName = "1. Strategy")]
		public int Contracts { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Enable Session Filter", Order = 1, GroupName = "2. Session")]
		public bool EnableSession { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Session Start", Order = 2, GroupName = "2. Session")]
		public DateTime SessionStart { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Session End", Order = 3, GroupName = "2. Session")]
		public DateTime SessionEnd { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Manual nudge (ticks)", Description = "SL/TP dashboard button step size.", Order = 1, GroupName = "3. Risk Management")]
		public int ManualNudgeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Manual BE offset (ticks)", Description = "Signed break-even offset; negative locks in a small loss.", Order = 2, GroupName = "3. Risk Management")]
		public int ManualBeOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Daily profit target ($)", Description = "0 = disabled. Blocks new entries once today's realized P&L reaches this.", Order = 3, GroupName = "3. Risk Management")]
		public double DailyProfitTarget { get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Daily max loss ($)", Description = "0 = disabled. Blocks new entries once today's realized loss reaches this.", Order = 4, GroupName = "3. Risk Management")]
		public double DailyMaxLoss { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Dashboard", Order = 1, GroupName = "4. Dashboard")]
		public bool ShowDashboard { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Corner", Order = 2, GroupName = "4. Dashboard")]
		public GbPanelCorner DashboardCorner { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Start Minimized", Order = 3, GroupName = "4. Dashboard")]
		public bool DashboardStartMinimized { get; set; }

		#endregion
	}
}
