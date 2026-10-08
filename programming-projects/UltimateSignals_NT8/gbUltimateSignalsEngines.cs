#region Using declarations
using System;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//  gbUltimateSignalsEngines v1.0.0 — support types for gbUltimateSignalsIndicator
//  ---------------------------------------------------------------------------
//  The vendor shipped a private ~1,300-line `UltimateSignalsNamespace` containing its own
//  IndicatorEngine hierarchy: EMA, SMA, HMA, WMA, WildersMA, ATR, MIN, MAX, a Stochastic and a
//  ZigZag. Nearly all of it re-derives NT8 built-ins that are numerically identical, so this port
//  keeps only what NT8 genuinely lacks:
//
//    * GbUsZigZagHighLow  — the ToS ZigZagHighLow reversal engine. NT8's ZigZag uses a different
//                           reversal model and cannot substitute.
//    * GbUsMa             — a mode switch over NT8's own MA indicators.
//
//  Dropped deliberately, with reasoning (see gbUltimateSignalsIndicator.cs header):
//    * EMA/SMA/HMA/WMA/ATR/MIN/MAX  — NT8's built-ins are formula-identical to the vendor's.
//    * Wilders MA                   — Wilder smoothing with period P is exactly EMA(2P-1); no
//                                     separate implementation is needed.
//    * TosStochastics               — NT8's Stochastics(D, K, smooth) is the same calculation at
//                                     the vendor's hard-coded defaults (High/Low/Close, SMA).
//    * TosArrowDraw / LineDraw      — replaced by real plots and NT8 drawing objects, which are
//                                     capturable by tag; SharpDX geometry is not.
//
//  Everything is prefixed Gb / GbUs: all NinjaScript in bin\Custom compiles into one assembly, so
//  bare type and enum names collide across scripts.
//  ---------------------------------------------------------------------------

// GLOBAL NAMESPACE, DELIBERATELY. Any enum used as a [NinjaScriptProperty] type must live here.
// NinjaTrader's code generator writes the wrapper region into `namespace
// NinjaTrader.NinjaScript.Indicators` and emits custom enum types UNQUALIFIED — it produces
// `GbUsMaMode macdAverageType`, not `GreyBeard.GbUsMaMode macdAverageType`. An enum declared inside
// ...Indicators.GreyBeard therefore fails to resolve (CS0246), and because the generated signature
// never matches the one in the file, NT8 appends a SECOND wrapper region on every compile
// (CS0102 / CS0229 duplicate-definition storms).
//
// The vendor hit this too — UltimateAIProV3_Enums.cs declares every enum in the global namespace
// for exactly this reason. Names are Gb-prefixed to stay unique across the shared Custom assembly.
public enum GbUsMaMode
{
	SMA,
	EMA,
	HMA,
	WMA,
	Wilders
}

/// <summary>Which series the ZigZag tracks: smoothed EMA(High/Low) or the raw extremes.</summary>
public enum GbUsZigZagPriceMethod
{
	Average,
	HighLow
}

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	/// <summary>
	/// On-demand weighted moving average over an arbitrary ISeries&lt;double&gt;, computed directly
	/// from the input rather than via NT8's built-in WMA indicator.
	///
	/// [EXPORT] NT8's "Export NinjaScript" tool auto-bundles referenced system indicators (SMA, EMA)
	/// but its dependency scanner misses WMA/HMA, and on this install those two aren't even offered
	/// as includable files in the export dialog — export fails before the "include system indicator?"
	/// prompt ever appears (confirmed against the shipped @WMA.cs/@HMA.cs; a known NT8 export gap,
	/// not a bug in this file). Computing the formula directly removes the dependency entirely, so
	/// gbUltimateSignalsIndicator exports standalone. Formula matches @WMA.cs exactly: weight P for
	/// the most recent bar in the window down to weight 1 for the oldest, normalized by P(P+1)/2.
	/// </summary>
	internal sealed class GbWmaSeries : ISeries<double>
	{
		private readonly Indicator owner;
		private readonly ISeries<double> input;
		private readonly int period;

		public GbWmaSeries(Indicator owner, ISeries<double> input, int period)
		{
			this.owner	= owner;
			this.input	= input;
			this.period	= Math.Max(1, period);
		}

		public int Count => owner.CurrentBar + 1;

		public double this[int barsAgo] => Compute(input, period, barsAgo, owner.CurrentBar);

		public double GetValueAt(int barIndex) => this[owner.CurrentBar - barIndex];
		public bool IsValidDataPoint(int barsAgo) => owner.CurrentBar - barsAgo >= 0;
		public bool IsValidDataPointAt(int barIndex) => barIndex >= 0 && barIndex <= owner.CurrentBar;

		internal static double Compute(ISeries<double> input, int period, int barsAgo, int curBar)
		{
			int n = Math.Min(period, curBar - barsAgo + 1);
			if (n <= 0)
				return input[barsAgo];

			double wsum = 0;
			for (int i = 0; i < n; i++)
				wsum += (n - i) * input[barsAgo + i];
			return wsum / (0.5 * n * (n + 1));
		}
	}

	/// <summary>
	/// On-demand Hull moving average, built on <see cref="GbWmaSeries"/>. Same [EXPORT] rationale —
	/// see GbWmaSeries. Formula matches @HMA.cs exactly: HMA = WMA(2*WMA(P/2) - WMA(P), sqrt(P)).
	/// Cost is O(sqrt(P) * P) per bar read, which is negligible at TOS-typical periods (P &lt;= ~30).
	/// </summary>
	internal sealed class GbHmaSeries : ISeries<double>
	{
		private readonly Indicator owner;
		private readonly ISeries<double> input;
		private readonly int period;
		private readonly int half;
		private readonly int sqrtPeriod;

		public GbHmaSeries(Indicator owner, ISeries<double> input, int period)
		{
			this.owner		= owner;
			this.input		= input;
			this.period		= Math.Max(1, period);
			half			= Math.Max(1, this.period / 2);
			sqrtPeriod		= Math.Max(1, (int)Math.Sqrt(this.period));
		}

		public int Count => owner.CurrentBar + 1;

		public double this[int barsAgo]
		{
			get
			{
				int curBar	= owner.CurrentBar;
				int n		= Math.Min(sqrtPeriod, curBar - barsAgo + 1);
				if (n <= 0)
					return input[barsAgo];

				double wsum = 0, wtotal = 0;
				for (int i = 0; i < n; i++)
				{
					int b		= barsAgo + i;
					double diff	= 2 * GbWmaSeries.Compute(input, half, b, curBar) - GbWmaSeries.Compute(input, period, b, curBar);
					int weight	= n - i;
					wsum		+= weight * diff;
					wtotal		+= weight;
				}
				return wtotal > 0 ? wsum / wtotal : 0;
			}
		}

		public double GetValueAt(int barIndex) => this[owner.CurrentBar - barIndex];
		public bool IsValidDataPoint(int barsAgo) => owner.CurrentBar - barsAgo >= 0;
		public bool IsValidDataPointAt(int barIndex) => barIndex >= 0 && barIndex <= owner.CurrentBar;
	}

	/// <summary>
	/// Maps a GbUsMaMode onto NT8's own moving averages. Wilders is expressed as EMA(2P-1), which is
	/// exact: Wilder's alpha is 1/P and EMA's is 2/(N+1), so N = 2P-1 gives identical smoothing.
	/// HMA/WMA are computed directly (GbHmaSeries/GbWmaSeries) rather than via owner.HMA()/owner.WMA()
	/// — see GbWmaSeries doc comment for why.
	/// </summary>
	public static class GbUsMa
	{
		public static ISeries<double> Create(Indicator owner, GbUsMaMode mode, ISeries<double> input, int period)
		{
			switch (mode)
			{
				case GbUsMaMode.SMA:		return owner.SMA(input, period);
				case GbUsMaMode.HMA:		return new GbHmaSeries(owner, input, period);
				case GbUsMaMode.WMA:		return new GbWmaSeries(owner, input, period);
				case GbUsMaMode.Wilders:	return owner.EMA(input, Math.Max(1, 2 * period - 1));
				default:					return owner.EMA(input, period);
			}
		}
	}

	/// <summary>
	/// Port of the vendor's TosZigZagHighLow (itself a port of ThinkOrSwim's ZigZagHighLow).
	///
	/// A pivot is confirmed only once price has reversed past a threshold built from a percentage of
	/// the last pivot, a fixed absolute amount, a tick amount, and an ATR multiple. Because that
	/// confirmation is inherently backward-looking, the engine can RETRACT a pivot it previously
	/// published — see Retractions and XLastChangedBar. Any consumer must treat published pivots as
	/// provisional until ConfirmationBars have passed.
	///
	/// Behaviour is faithful to the vendor's, with one deliberate change: the "was a prior pivot
	/// found" test uses an explicit bool rather than comparing a price against the 0.0 sentinel.
	/// </summary>
	public class GbUsZigZagHighLow
	{
		private readonly Indicator		owner;
		private readonly ISeries<double>	highSeries;
		private readonly ISeries<double>	lowSeries;
		private readonly ISeries<double>	atr;

		private readonly Series<double>	pivot;			// pivot price; Reset() where there is none
		private readonly Series<int>		extremumDir;	// +1 high pivot, -1 low pivot, 0 none

		private int lastSeenBar = -1;

		public double	PercentageReversal	{ get; set; }
		public double	AbsoluteReversal	{ get; set; }
		public double	TickReversal		{ get; set; }
		public double	AtrReversal			{ get; set; }

		/// <summary>Oldest absolute bar index touched by the most recent update — the rewrite floor.</summary>
		public int XLastChangedBar { get; private set; }

		/// <summary>Count of pivots retracted after publication. A direct measure of repaint.</summary>
		public int Retractions { get; private set; }

		public GbUsZigZagHighLow(Indicator owner, ISeries<double> highSeries, ISeries<double> lowSeries, ISeries<double> atr)
		{
			this.owner		= owner;
			this.highSeries	= highSeries;
			this.lowSeries	= lowSeries;
			this.atr		= atr;

			// Infinite lookback: the retraction path can reach back an arbitrary distance.
			pivot		= new Series<double>(owner, MaximumBarsLookBack.Infinite);
			extremumDir	= new Series<int>(owner, MaximumBarsLookBack.Infinite);
		}

		public Series<double> Pivot						=> pivot;
		public double this[int barsAgo]					=> pivot[barsAgo];
		public bool IsValidDataPoint(int barsAgo)		=> pivot.IsValidDataPoint(barsAgo);
		public int DirectionAt(int barsAgo)				=> extremumDir[barsAgo];

		/// <summary>
		/// Call once per OnBarUpdate. The engine only re-evaluates on a new bar — the forming bar
		/// cannot confirm a pivot, so ticking it would only produce churn.
		/// </summary>
		public void OnBarUpdate()
		{
			pivot.Reset(0);
			extremumDir[0] = 0;

			if (owner.CurrentBar == lastSeenBar)
				return;

			lastSeenBar = owner.CurrentBar;
			UpdateBar(owner.CurrentBar - 1);
		}

		private void UpdateBar(int absBar)
		{
			XLastChangedBar = absBar;

			if (absBar < 1)
				return;

			int barsAgo = owner.CurrentBar - absBar;

			pivot.Reset(barsAgo);
			extremumDir[barsAgo] = 0;

			// Walk back for the most recent published pivot.
			int		priorDir		= 0;
			int		priorAbsBar		= -1;
			double	priorPrice		= 0.0;
			bool	foundPrior		= false;

			for (int i = barsAgo + 1; i <= absBar; i++)
			{
				if (extremumDir[i] == 0)
					continue;

				priorDir	= extremumDir[i] > 0 ? 1 : -1;
				priorAbsBar	= owner.CurrentBar - i;
				priorPrice	= pivot[i];
				foundPrior	= true;
				break;
			}

			// Vendor used `if (num4 == 0.0)` — an exact float compare against a sentinel that a real
			// price could legitimately equal. An explicit flag is equivalent and safe.
			if (!foundPrior)
				priorPrice = owner.Input[0];

			double band = AbsoluteReversal
						+ TickReversal * owner.TickSize
						+ atr[barsAgo] * AtrReversal;

			double lowThreshold  = priorDir < 0 ? priorPrice : priorPrice * (1.0 - PercentageReversal * 0.01) - band;
			double highThreshold = priorDir > 0 ? priorPrice : priorPrice * (1.0 + PercentageReversal * 0.01) + band;

			bool newHigh = highSeries[barsAgo] > highThreshold;
			bool newLow  = lowSeries[barsAgo]  < lowThreshold;

			// A bar that clears both thresholds resolves in favour of the prevailing direction.
			if (newHigh && newLow)
			{
				if (priorDir > 0)	newLow  = false;
				else				newHigh = false;
			}

			if (newHigh)
			{
				pivot[barsAgo]			= highSeries[barsAgo];
				extremumDir[barsAgo]	= 1;
				PullBackRewriteFloor(absBar);

				// This high supersedes the previous high pivot — erase it.
				if (priorDir > 0 && priorAbsBar >= 0)
					Retract(priorAbsBar);
			}

			if (newLow)
			{
				pivot[barsAgo]			= lowSeries[barsAgo];
				extremumDir[barsAgo]	= -1;
				PullBackRewriteFloor(absBar);

				if (priorDir < 0 && priorAbsBar >= 0)
					Retract(priorAbsBar);
			}
		}

		private void Retract(int absBar)
		{
			int barsAgo = owner.CurrentBar - absBar;
			if (barsAgo < 0)
				return;

			pivot.Reset(barsAgo);
			extremumDir[barsAgo] = 0;
			Retractions++;
			PullBackRewriteFloor(absBar);
		}

		private void PullBackRewriteFloor(int absBar)
		{
			if (absBar < XLastChangedBar)
				XLastChangedBar = absBar;
		}
	}
}
