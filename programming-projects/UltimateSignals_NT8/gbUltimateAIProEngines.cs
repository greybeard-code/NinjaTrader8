#region Using declarations
using System;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//  gbUltimateAIProEngines v1.0.0 — support types for gbUltimateAIPro
//  ---------------------------------------------------------------------------
//  REQUIRES gbUltimateSignalsEngines.cs (GbUsMaMode, GbUsMa) — both live in
//  NinjaTrader.NinjaScript.Indicators.GreyBeard.
//
//  UltimateAIProV3 uses a DIFFERENT ZigZag from UltimateSignals: WiZigZagHighLowTOS1v0, a
//  running high/low state machine, rather than TosZigZagHighLow's threshold comparison. The two
//  produce different pivots, so GbUsZigZagHighLow cannot be substituted — this is a separate port.
//
//  Deliberate simplifications, behaviour-preserving:
//
//   * WiCurrentBar collapsed to 0. The original computes
//         Math.Max(0, ((State != Historical || IsTickReplay) && Calculate != OnBarClose ? 1 : 0)
//                     + CurrentBarOffset - 1)
//     but CurrentBarOffset is never assigned anywhere in the source, so the expression evaluates
//     to max(0, 1-1) = 0 in real time and max(0, 0-1) = 0 historically — always 0. Every
//     `state[WiCurrentBar + 1]` was therefore just `state[1]`. Substituting 0 changes nothing and
//     removes a mechanism that reads as if it does something.
//
//   * WilderMA1v0(ATR(1), n) replaced by ATR(n). ATR(1) is true range; Wilder-smoothing true range
//     over n IS NT8's ATR(n) — the recurrences are algebraically identical once warm. This removes
//     the need to port WilderMA1v0.cs at all.
//
//   * `lastBar` dropped — assigned, never read.
//  ---------------------------------------------------------------------------

// GLOBAL NAMESPACE, DELIBERATELY — see the note in gbUltimateSignalsEngines.cs. NinjaTrader's
// wrapper generator emits [NinjaScriptProperty] enum types unqualified, so an enum inside
// ...Indicators.GreyBeard cannot resolve and NT8 appends a duplicate wrapper region every compile.

/// <summary>Price source for the ZigZag high/low tracks.</summary>
public enum GbUaiPrice
{
	Open,
	High,
	Low,
	Close,
	HL2,
	HLC3,
	OHLC4
}

/// <summary>Higher-timeframe bar type for Ultimate Zones. Values intentionally match
/// NinjaTrader's BarsPeriodType so the cast in AddDataSeries stays valid — do not renumber.</summary>
public enum GbUaiHtfType
{
	Second	= 3,
	Minute	= 4,
	Day		= 5,
	Week	= 6,
	Month	= 7,
	Year	= 8
}

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public static class GbUaiPriceSeries
	{
		public static ISeries<double> Get(Indicator owner, GbUaiPrice mode, int barsInProgress = 0)
		{
			switch (mode)
			{
				case GbUaiPrice.Open:	return owner.Opens[barsInProgress];
				case GbUaiPrice.High:	return owner.Highs[barsInProgress];
				case GbUaiPrice.Low:	return owner.Lows[barsInProgress];
				case GbUaiPrice.HL2:	return owner.Medians[barsInProgress];
				case GbUaiPrice.HLC3:	return owner.Typicals[barsInProgress];
				case GbUaiPrice.OHLC4:	return owner.Weighteds[barsInProgress];
				default:				return owner.Closes[barsInProgress];
			}
		}
	}

	/// <summary>
	/// Port of WiZigZagHighLowTOS1v0 — the ToS ZigZagHighLow state machine.
	///
	/// Tracks a running maximum and minimum and flips state when price reverses past a threshold
	/// built from a percentage, a fixed absolute amount, a tick amount and an ATR multiple. State
	/// is -2 (seeking), +1 (in an up leg) or -1 (in a down leg).
	///
	/// Publishes a confirmed pivot in <see cref="ZzDot"/> and a provisional one in
	/// <see cref="ZzPot"/>. The provisional pivot is cleared and republished on every bar, which is
	/// the engine's repaint surface — <see cref="Retractions"/> counts how often a published
	/// provisional pivot moves.
	/// </summary>
	public class GbUaiZigZagHighLow
	{
		private readonly Indicator owner;
		private readonly int bip;					// which data series this instance tracks
		private readonly ISeries<double> priceH;
		private readonly ISeries<double> priceL;
		private readonly ISeries<double> atr;

		private readonly Series<double>	maxPriceH, minPriceL;
		private readonly Series<int>	state;
		private readonly Series<bool>	newState, newMax, newMin;
		private readonly Series<int>	prevPointBar, lastPot, lastPointBar, lastHighPointBar, lastLowPointBar;
		private readonly Series<double>	prevPointY;

		private readonly Series<double>	zz, zzLine, zzDot, zzPot;

		public double PercentageReversal	{ get; set; }
		public double AbsoluteReversal		{ get; set; }
		public double AtrReversal			{ get; set; }
		public int    TickReversal			{ get; set; }

		public int Retractions { get; private set; }

		/// <param name="barsInProgress">Data series index this engine tracks. Pass 1 for a
		/// higher-timeframe series; every internal series binds to that Bars object so indexing
		/// stays relative to it.</param>
		public GbUaiZigZagHighLow(Indicator owner, ISeries<double> priceH, ISeries<double> priceL, ISeries<double> atr, int barsInProgress = 0)
		{
			this.owner	= owner;
			this.bip	= barsInProgress;
			this.priceH	= priceH;
			this.priceL	= priceL;
			this.atr	= atr;

			Bars bars = owner.BarsArray[barsInProgress];

			maxPriceH			= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			minPriceL			= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			state				= new Series<int>(bars, MaximumBarsLookBack.Infinite);
			newState			= new Series<bool>(bars, MaximumBarsLookBack.Infinite);
			newMax				= new Series<bool>(bars, MaximumBarsLookBack.Infinite);
			newMin				= new Series<bool>(bars, MaximumBarsLookBack.Infinite);
			prevPointBar		= new Series<int>(bars, MaximumBarsLookBack.Infinite);
			prevPointY			= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			lastPot				= new Series<int>(bars, MaximumBarsLookBack.Infinite);
			lastPointBar		= new Series<int>(bars, MaximumBarsLookBack.Infinite);
			lastHighPointBar	= new Series<int>(bars, MaximumBarsLookBack.Infinite);
			lastLowPointBar		= new Series<int>(bars, MaximumBarsLookBack.Infinite);

			zz		= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			zzLine	= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			zzDot	= new Series<double>(bars, MaximumBarsLookBack.Infinite);
			zzPot	= new Series<double>(bars, MaximumBarsLookBack.Infinite);
		}

		private int CurBar => owner.CurrentBars[bip];

		/// <summary>Pivot value series — the vendor's EI[c] / EI.ZZ[c].</summary>
		public Series<double> Zz		=> zz;
		/// <summary>Confirmed pivot marker — the vendor's EI.ZZDot.</summary>
		public Series<double> ZzDot		=> zzDot;
		/// <summary>Provisional pivot marker — republished every bar.</summary>
		public Series<double> ZzPot		=> zzPot;
		public Series<double> ZzLine	=> zzLine;

		public double this[int barsAgo]				=> zz[barsAgo];
		public bool IsValidDataPoint(int barsAgo)	=> zz.IsValidDataPoint(barsAgo);

		public void OnBarUpdate()
		{
			if (CurBar <= 1)
				return;

			// Clear the previous provisional pivot before recomputing it.
			if (owner.IsFirstTickOfBar)
			{
				if (lastPot[1] != 0)
				{
					ResetAt(zzPot, CurBar - lastPot[1]);
					ResetAt(zz,    CurBar - lastPot[1]);
				}
			}
			else
			{
				if (lastPot[0] != 0)
				{
					ResetAt(zzPot, CurBar - lastPot[0]);
					ResetAt(zz,    CurBar - lastPot[0]);
					Retractions++;
				}
				ResetAt(zzPot, 0);
				ResetAt(zz, 0);
			}

			prevPointBar[0]		= prevPointBar[1];
			prevPointY[0]		= prevPointY[1];
			lastPot[0]			= lastPot[1];
			lastPointBar[0]		= lastPointBar[1];
			lastHighPointBar[0]	= lastHighPointBar[1];
			lastLowPointBar[0]	= lastLowPointBar[1];

			state[0]		= 0;
			maxPriceH[0]	= 0.0;
			minPriceL[0]	= 0.0;
			newState[0]		= false;
			newMax[0]		= false;
			newMin[0]		= false;

			double absRev = AbsoluteReversal != 0.0 ? AbsoluteReversal : TickReversal * owner.TickSize;
			double pctRev = AtrReversal != 0.0
				? PercentageReversal / 100.0 + atr[0] / owner.Closes[bip][0] * AtrReversal
				: PercentageReversal / 100.0;

			double prevMax = maxPriceH[1];
			double prevMin = minPriceL[1];

			if (state[1] == 0)
			{
				maxPriceH[0]	= priceH[0];
				minPriceL[0]	= priceL[0];
				newMax[0]		= true;
				newMin[0]		= true;
				state[0]		= -2;
			}
			else if (state[1] == -2)
			{
				if (priceH[0] >= prevMax)
				{
					state[0] = 1;  maxPriceH[0] = priceH[0]; minPriceL[0] = prevMin;
					newMax[0] = true;  newMin[0] = false;
				}
				else if (priceL[0] <= prevMin)
				{
					state[0] = -1; maxPriceH[0] = prevMax;   minPriceL[0] = priceL[0];
					newMax[0] = false; newMin[0] = true;
				}
				else
				{
					state[0] = -2; maxPriceH[0] = prevMax;   minPriceL[0] = prevMin;
					newMax[0] = false; newMin[0] = false;
				}
			}
			else if (state[1] == 1)
			{
				if (priceL[0] <= prevMax - prevMax * pctRev - absRev)
				{
					state[0] = -1; maxPriceH[0] = prevMax; minPriceL[0] = priceL[0];
					newMax[0] = false; newMin[0] = true;
				}
				else
				{
					state[0] = 1;
					if (priceH[0] >= prevMax)	{ maxPriceH[0] = priceH[0]; newMax[0] = true;  }
					else						{ maxPriceH[0] = prevMax;   newMax[0] = false; }
					minPriceL[0] = prevMin;
					newMin[0] = false;
				}
			}
			else if (priceH[0] >= prevMin + prevMin * pctRev + absRev)
			{
				state[0] = 1; maxPriceH[0] = priceH[0]; minPriceL[0] = prevMin;
				newMax[0] = true; newMin[0] = false;
			}
			else
			{
				state[0] = -1; maxPriceH[0] = prevMax; newMax[0] = false;
				if (priceL[0] <= prevMin)	{ minPriceL[0] = priceL[0]; newMin[0] = true;  }
				else						{ minPriceL[0] = prevMin;   newMin[0] = false; }
			}

			newState[0] = state[0] != state[1];

			bool atHigh = state[0] ==  1 && priceH[0] == maxPriceH[0];
			bool atLow  = state[0] == -1 && priceL[0] == minPriceL[0];

			double highPivot = double.NaN;
			if (atHigh)
				lastHighPointBar[0] = CurBar;
			if (newState[0] || newMax[0])
				highPivot = newMax[0] ? double.NaN : priceH[CurBar - lastHighPointBar[0]];

			double lowPivot = double.NaN;
			if (atLow)
				lastLowPointBar[0] = CurBar;
			if (newState[0] || newMin[0])
				lowPivot = newMin[0] ? double.NaN : priceL[CurBar - lastLowPointBar[0]];

			if (lastPointBar[0] == 0)
			{
				if (state[0] == 1)	zzDot[0] = priceL[0];
				else				highPivot = priceH[0];
				lastPointBar[0] = CurBar;
				return;
			}

			int pivotBarsAgo = -1;

			if (!double.IsNaN(highPivot))
			{
				pivotBarsAgo = CurBar - lastHighPointBar[0];
				zzDot[pivotBarsAgo] = highPivot;
			}
			else if (!double.IsNaN(lowPivot))
			{
				pivotBarsAgo = CurBar - lastLowPointBar[0];
				zzDot[pivotBarsAgo] = lowPivot;
			}

			if (pivotBarsAgo >= 0)
			{
				DrawLeg(CurBar - prevPointBar[0], prevPointY[0], pivotBarsAgo, zzDot[pivotBarsAgo]);
				prevPointBar[0]	= CurBar - pivotBarsAgo;
				prevPointY[0]	= zzDot[pivotBarsAgo];
			}
			else
			{
				lastPot[0]		= (lastHighPointBar[0] > lastLowPointBar[0] ? lastHighPointBar : lastLowPointBar)[0];
				pivotBarsAgo	= CurBar - lastPot[0];
			}

			if (pivotBarsAgo < 0)
				return;

			if (!atHigh && (state[0] != -1 || priceL[0] <= minPriceL[0]))
			{
				if (atLow || (state[0] == 1 && priceH[0] < maxPriceH[0]))
					zzPot[pivotBarsAgo] = priceH[pivotBarsAgo];
			}
			else
			{
				zzPot[pivotBarsAgo] = priceL[pivotBarsAgo];
			}

			double endY = lastHighPointBar[0] > lastLowPointBar[0] ? priceH[0] : priceL[0];
			DrawLeg(CurBar - prevPointBar[0], prevPointY[0], pivotBarsAgo, zzPot[pivotBarsAgo]);
			DrawLeg(pivotBarsAgo, zzPot[pivotBarsAgo], 0, endY);
		}

		private static void ResetAt(Series<double> series, int barsAgo)
		{
			if (barsAgo < 0)
				return;
			series[barsAgo] = 0.0;
			series.Reset(barsAgo);
		}

		private void DrawLeg(int startBarsAgo, double startY, int endBarsAgo, double endY)
		{
			if (startBarsAgo < 0 || endBarsAgo < 0)
				return;

			int span = Math.Abs(endBarsAgo - startBarsAgo);
			double step = span != 0 ? (startY - endY) / span : 0.0;

			zz[startBarsAgo] = startY;

			for (int i = 0; i <= span; i++)
			{
				int idx = Math.Min(CurBar, endBarsAgo + i);
				zzLine[idx] = endY + step * i;
			}
		}
	}
}
