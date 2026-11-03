// GreyKo -- GreyBeard renko bar type: the ninZaRenko/KingRenko$ grid with a choice of OHLC.
//
// v1.0.3  2026-10-30
//   - the trace is no longer wired to the Output window (core.Diag stays null = silent); errors still log
//   - naming: the two modes are called Traded (dropdown entry "Close") and Full Brick ("Highs and lows",
//     ninZaRenko's OHLC); the selector name carries no mode text, it is always `GreyKo 64/4`
// v1.0.2  2026-10-30  (the dropdown is labelled just "OHLC Mode")
// v1.0.1  2026-10-30
//   - the OHLC mode is a real choice box: NT8 builds the Data Series dialog from BarsPeriod's own
//     settings and ignores a replacement property, so the mode now rides on the built-in
//     PointAndFigurePriceType dropdown (Close = Traded, Highs and lows = Full Brick), which NT8 lists after
//     Trend Threshold. The numeric BaseBarsPeriodValue field is hidden. (v1.0.0 used that number.)
// v1.0.0  2026-10-30
//   - OHLC mode: Traded (default; every OHLC value is a price that traded) or Full Brick (ninZaRenko's
//          synthetic open and body-B sweep bricks).
//   - own BarsPeriodType id (91003) and own class names.
//
// WHAT THIS IS
//   A renko bar with three settings:
//
//       Brick Size       B  (Value, ticks)
//       Trend Threshold  T  (Value2, ticks, 1 <= T <= B)
//       OHLC mode           (dropdown: Close = Traded, Highs and lows = Full Brick)
//
//   Geometry (all strict comparisons; "anchor" = close of the previous bar). It is the
//   ninZaRenko / KingRenko$ grid, identical in both OHLC modes, so every CLOSE agrees:
//       with-trend close   anchor +/- T            (a bar closes ON that level)
//       reversal close     anchor -/+ (2B - T)     (against the trend)
//       first bar of a run closes T away, either direction (body T, opens on the anchor)
//   The bar that completes is clamped to the level; the TICK THAT BROKE the level belongs
//   to the NEW bar.
//
//   OHLC mode 1 = Traded (dropdown "Close"; KingRenko$-style):
//       open = price of the first trade in the bar's span; high/low = real running
//       extremes widened to the clamped close; a trade that jumps several levels prints
//       one bar per level, the in-between bars FLAT (O=H=L=C) with volume 0.
//   OHLC mode 2 = Full Brick (dropdown "Highs and lows"; ninZaRenko-style, bodies drawn full brick size):
//       open = close -/+ B, i.e. anchor - dir*(B - T) for the forming bar (synthetic);
//       high/low = real extremes widened to that open and the clamped close; in-between
//       sweep bars have the standard body B and volume 0 (high = max(O,C), low = min(O,C)).
//   Indicators that read the open or the range (ATR, ...) see different numbers in the two
//   modes: the median MNQ 12/4 bar range is 7 ticks Traded, 12 Full Brick.
//
//   Re-anchor: a gap of more than 30 minutes between two trades (the daily 17:00-18:00 ET
//   halt, weekends) starts a fresh grid at the next trade. The forming bar stays as the
//   partial bar it already is; a seed bar that never completed is removed. "Break at EOD"
//   does NOT drive this; the gap rule is the only reset.
//
// PROVENANCE
//   Traded mode: measured from KingRenko$ chart exports. Full Brick mode: measured from ninZaRenko
//   chart exports. No vendor code and no decompiled source is used or copied.
//
// TESTING
//   The brick logic is the plain class GreyKoCore below (no NinjaTrader types), so it can be compiled
//   and tested on its own with GB_HARNESS defined.

#region Using declarations
using System;
#if !GB_HARNESS
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endif
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
	/// <summary>What the brick logic needs from a bar series.</summary>
	internal interface IGkBars
	{
		int Count { get; }
		double GetOpen(int index);
		double GetHigh(int index);
		double GetLow(int index);
		double GetClose(int index);
		DateTime GetTime(int index);
		void Add(double open, double high, double low, double close, DateTime time, long volume);
		/// <summary>Widen high/low to the given values, set close, ADD volume.</summary>
		void Update(double high, double low, double close, DateTime time, long volume);
		void RemoveLast();
	}

	/// <summary>
	/// The brick logic. Works in whole ticks, so thresholds never pick up float
	/// error and every level lands exactly on the tick grid.
	/// </summary>
	internal sealed class GreyKoCore
	{
		/// <summary>A gap strictly longer than this between two trades re-anchors.</summary>
		public static readonly long ResetGapTicks = 30L * 60L * TimeSpan.TicksPerSecond;

		private readonly double tickSize;
		private readonly int decimals;
		private readonly long trend;      // T, ticks
		private readonly long reversal;   // 2B - T, ticks
		private readonly long brick;      // B, ticks
		private readonly bool ninZa;      // OHLC mode 2: synthetic open, body-B sweep bricks

		private long anchor;              // close of the previous bar, ticks
		private int dir;                  // +1 / -1, 0 = no bar completed since the (re)seed
		private DateTime lastTime;
		private bool started;
		private int ownSeedIndex = -1;    // index of the seed bar THIS instance added, else -1

		/// <summary>Optional trace sink (NinjaTrader's Print). Null = silent.</summary>
		public Action<string> Diag;
		private const int DiagTicksAfterReset = 8;
		private int diagLeft;
		private long lastP;
		private bool haveLastP;
		private int expectedCount = -1;   // what the series held when this instance last touched it

		public GreyKoCore(double tickSize, int brickTicks, int trendTicks, bool ninZaOhlc = false)
		{
			if (!(tickSize > 0))
				throw new ArgumentException("tickSize must be positive");
			this.tickSize = tickSize;

			int d = 0;
			while (d < 10 && Math.Abs(tickSize * Math.Pow(10, d) - Math.Round(tickSize * Math.Pow(10, d))) > 1e-9)
				d++;
			decimals = d;

			// T >= 1 keeps every sweep step moving at least one tick toward the
			// price (T = 0 would loop forever); T <= B keeps the reversal at
			// least as far as a continuation.
			long b = Math.Max(1, brickTicks);
			trend = Math.Max(1, Math.Min(trendTicks, b));
			reversal = 2 * b - trend;
			brick = b;
			ninZa = ninZaOhlc;
		}

		public int Direction { get { return dir; } }

		public void OnTrade(IGkBars bars, DateTime time, double price, long volume)
		{
			OnTradeCore(bars, time, price, volume);
			expectedCount = bars.Count;
		}

		private void OnTradeCore(IGkBars bars, DateTime time, double price, long volume)
		{
			long p = ToTicks(price);
			if (!started)
			{
				started = true;
				if (bars.Count > 0)
					Resume(bars);
			}
			else if (bars.Count > 0 && (bars.Count != expectedCount
					 || bars.GetTime(bars.Count - 1) < lastTime - TimeSpan.FromSeconds(1)))
			{
				// NinjaTrader rolled the series back (bars removed, or the last bar older than
				// the last tick this instance saw) and is feeding the ticks again. The state held
				// here belongs to the discarded bars, so rebuild it from the bars that are left.
				if (Diag != null)
					Diag("RESYNC " + Fmt(time) + " p=" + price + ": series has " + bars.Count + " bars, this instance left "
						 + expectedCount + "; last bar t=" + Fmt(bars.GetTime(bars.Count - 1)) + ", last tick seen " + Fmt(lastTime));
				Resume(bars);
			}
			if (Diag != null)
			{
				// anything that could drag the grid somewhere odd: a big jump or time running backwards
				if (haveLastP && Math.Abs(p - lastP) > 200)
					Diag("JUMP " + Fmt(time) + " p=" + price + " v=" + volume + " from " + ToPrice(lastP)
						 + " (lastTick " + Fmt(lastTime) + ") | " + State(bars));
				if (bars.Count > 0 && time < lastTime)
					Diag("BACKWARDS " + Fmt(time) + " p=" + price + " lastTick " + Fmt(lastTime) + " | " + State(bars));
			}
			lastP = p;
			haveLastP = true;

			bool gap = bars.Count > 0 && (time - lastTime).Ticks > ResetGapTicks;
			if (Diag != null && diagLeft > 0 && !(bars.Count == 0 || gap))
			{
				diagLeft--;
				Diag("tick " + Fmt(time) + " p=" + price + " v=" + volume + " | " + State(bars));
			}
			if (bars.Count == 0 || gap)
			{
				if (Diag != null)
				{
					diagLeft = DiagTicksAfterReset;
					Diag((bars.Count == 0 ? "SEED " : "RESET ") + Fmt(time) + " p=" + price + " v=" + volume
						 + " lastTick=" + Fmt(lastTime) + " gap=" + (time - lastTime).TotalMinutes.ToString("F1")
						 + "min | " + State(bars));
				}
				// (Re)seed. A seed bar that never completed is abandoned, as in
				// a reference bar builder; a bar that had a direction stays as the
				// partial bar it already is (closed at the last trade).
				if (bars.Count > 0 && dir == 0 && ownSeedIndex == bars.Count - 1)
					bars.RemoveLast();
				anchor = p;
				dir = 0;
				bars.Add(price, price, price, price, Stamp(bars, time), volume);
				ownSeedIndex = bars.Count - 1;
				lastTime = time;
				if (Diag != null)
					Diag("  after seed: " + State(bars));
				return;
			}
			lastTime = time;

			bool closed = false;
			while (true)
			{
				long up = anchor + (dir < 0 ? reversal : trend);
				long down = anchor - (dir > 0 ? reversal : trend);
				bool breakUp = p > up;
				bool breakDown = !breakUp && p < down;
				if (!breakUp && !breakDown)
					break;

				long level = breakUp ? up : down;
				double levelPrice = ToPrice(level);
				DateTime stamp = Stamp(bars, time);
				if (!closed)
				{
					// Complete the forming bar on the level. The breakout tick is
					// NOT in it: no volume, and its price only enters via the clamp.
					int last = bars.Count - 1;
					bars.Update(Math.Max(bars.GetHigh(last), levelPrice),
								Math.Min(bars.GetLow(last), levelPrice),
								levelPrice, stamp, 0);
					closed = true;
				}
				else
				{
					// The same trade also clears the next level: an empty-span bar, no
					// volume. Traded: flat at its own level. Full Brick: the standard body B.
					if (ninZa)
					{
						double so = ToPrice(level - (breakUp ? 1 : -1) * brick);
						bars.Add(so, Math.Max(so, levelPrice), Math.Min(so, levelPrice), levelPrice, stamp, 0);
					}
					else
						bars.Add(levelPrice, levelPrice, levelPrice, levelPrice, stamp, 0);
				}
				anchor = level;
				dir = breakUp ? 1 : -1;
			}

			DateTime t = Stamp(bars, time);
			if (closed)
			{
				if (ninZa)
				{
					// ninZa: the forming bar opens at anchor - dir*(B - T), whether it later
					// continues or reverses (both give close -/+ B), not on the breakout trade.
					double fo = ToPrice(anchor - dir * (brick - trend));
					bars.Add(fo, Math.Max(fo, price), Math.Min(fo, price), price, t, volume);
				}
				else
					bars.Add(price, price, price, price, t, volume);   // opens ON the breakout trade
			}
			else
				bars.Update(price, price, price, t, volume);
		}

		/// <summary>
		/// The series already holds bars this instance did not build (NinjaTrader resumed it
		/// from stored bars): take the state from them. The last bar is the forming one, the
		/// one before it closed on the anchor, and the two closes before that give the trend.
		/// </summary>
		private void Resume(IGkBars bars)
		{
			int n = bars.Count;
			lastTime = bars.GetTime(n - 1);
			ownSeedIndex = -1;
			if (n == 1)
			{
				// the only bar is the opening seed: no brick has completed yet
				anchor = ToTicks(bars.GetOpen(0));
				dir = 0;
				ownSeedIndex = 0;
			}
			else
			{
				anchor = ToTicks(bars.GetClose(n - 2));
				long move = n >= 3
					? ToTicks(bars.GetClose(n - 2)) - ToTicks(bars.GetClose(n - 3))
					: ToTicks(bars.GetClose(0)) - ToTicks(bars.GetOpen(0));
				if (move == 0)    // a bar cut at a re-anchor can close where the one before did
					move = ToTicks(bars.GetClose(n - 2)) - ToTicks(bars.GetOpen(n - 2));
				dir = move > 0 ? 1 : move < 0 ? -1 : 0;
			}
			if (Diag != null)
				Diag("RESUME from " + n + " stored bars: " + State(bars));
		}

		private static string Fmt(DateTime t)
		{
			return t.ToString("MM-dd HH:mm:ss.fff");
		}

		private string State(IGkBars bars)
		{
			string last = "no bars";
			if (bars.Count > 0)
				last = "last O/H/L " + bars.GetOpen(bars.Count - 1) + "/" + bars.GetHigh(bars.Count - 1) + "/"
					   + bars.GetLow(bars.Count - 1) + " t=" + Fmt(bars.GetTime(bars.Count - 1));
			return "count=" + bars.Count + " anchor=" + ToPrice(anchor) + " dir=" + dir + " " + last;
		}

		private long ToTicks(double price)
		{
			return (long)Math.Round(price / tickSize, MidpointRounding.AwayFromZero);
		}

		private double ToPrice(long ticks)
		{
			return Math.Round(ticks * tickSize, decimals);
		}

		/// <summary>Bar times must not go backwards.</summary>
		private static DateTime Stamp(IGkBars bars, DateTime time)
		{
			if (bars.Count == 0)
				return time;
			DateTime last = bars.GetTime(bars.Count - 1);
			return time < last ? last : time;
		}
	}

#if !GB_HARNESS
	public class GreyKo : BarsType
	{
		// GreyBeard custom BarsPeriodType id block: 91000-91099 (this bar type is 91003).
		// Must not clash with NT8's own (0-16), ninZaRenko (12345), SaberRenko
		// (20821), KingRenko$ (678910) or the vendor TBars builds.
		private const int GbBarsPeriodTypeId = 91003;

		#region Developer
		[Display(Name = "Author", Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.3";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		private GreyKoCore core;
		private readonly BarsAdapter adapter;
		private bool faultLogged;

		public GreyKo()
		{
			adapter = new BarsAdapter(this);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "GreyBeard GreyKo - ninZaRenko/KingRenko grid; OHLC Mode: Close = Traded (real open), Highs and lows = Full Brick (ninZa synthetic open)";
				Name = "GreyKo";
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = (BarsPeriodType)GbBarsPeriodTypeId,
					BarsPeriodTypeName = Name
				};
				BuiltFrom = BarsPeriodType.Tick;
				DaysToLoad = 5;
				IsIntraday = true;
				IsTimeBased = false;
			}
			else if (State == State.Configure)
			{
				Properties.Remove(Properties.Find("BaseBarsPeriodType", true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue", true));
				Properties.Remove(Properties.Find("ReversalType", true));
				SetPropertyName("Value", "Brick Size");
				SetPropertyName("Value2", "Trend Threshold");
				// PointAndFigurePriceType stays visible: its two-entry dropdown is the OHLC mode.
				// NT8 lists it after Trend Threshold and keys its bar cache on it. Close is its
				// default, so a fresh series is the Traded build. (A replacement property object is
				// ignored by the Data Series dialog; only renaming and hiding work.)
				SetPropertyName("PointAndFigurePriceType", "OHLC Mode");

				// 1 <= T <= B. T = 0 would make a breakout stop moving the anchor.
				BarsPeriod.Value = Math.Max(1, BarsPeriod.Value);
				BarsPeriod.Value2 = Math.Max(1, Math.Min(BarsPeriod.Value2, BarsPeriod.Value));

				// What the bar selector and the chart title show ("KingRenko$ 64/4" for the vendor);
				// no mode text, by request.
				Name = string.Format("GreyKo {0}/{1}", BarsPeriod.Value, BarsPeriod.Value2);
			}
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{
			return 5;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close,
											DateTime time, long volume, bool isBar, double bid, double ask)
		{
			try
			{
				if (core == null || bars.Count == 0)
					core = new GreyKoCore(bars.Instrument.MasterInstrument.TickSize,
											   bars.BarsPeriod.Value, bars.BarsPeriod.Value2,
											   bars.BarsPeriod.PointAndFigurePriceType == PointAndFigurePriceType.HighsAndLows);
				adapter.Bars = bars;
				core.OnTrade(adapter, time, close, volume);
				bars.LastPrice = close;
			}
			catch (Exception ex)
			{
				// Stay alive so one bad data point cannot kill the chart, but say
				// so once instead of silently building bars from corrupt state.
				if (!faultLogged)
				{
					faultLogged = true;
					Log("GreyKo: " + ex, LogLevel.Error);
					Print("GreyKo EXCEPTION at " + time.ToString("MM-dd HH:mm:ss.fff") + ": " + ex);
				}
			}
		}

		public override bool IsRemoveLastBarSupported => true;

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 8;
			period.Value2 = 4;
		}

		public override string ChartLabel(DateTime dateTime)
		{
			return dateTime.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return 0.0;
		}

		/// <summary>Bridges the pure brick logic to NT8's protected AddBar/UpdateBar.</summary>
		private sealed class BarsAdapter : IGkBars
		{
			private readonly GreyKo owner;
			public Bars Bars;

			public BarsAdapter(GreyKo owner) { this.owner = owner; }

			public int Count => Bars.Count;
			public double GetOpen(int index) => Bars.GetOpen(index);
			public double GetHigh(int index) => Bars.GetHigh(index);
			public double GetLow(int index) => Bars.GetLow(index);
			public double GetClose(int index) => Bars.GetClose(index);
			public DateTime GetTime(int index) => Bars.GetTime(index);

			public void Add(double open, double high, double low, double close, DateTime time, long volume)
			{
				owner.AddBar(Bars, open, high, low, close, time, volume);
			}

			public void Update(double high, double low, double close, DateTime time, long volume)
			{
				owner.UpdateBar(Bars, high, low, close, time, volume);
			}

			public void RemoveLast()
			{
				owner.RemoveLastBar(Bars);
			}
		}
	}
#endif
}
