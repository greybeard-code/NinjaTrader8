#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

//  gbSignalProbe v1.2.0
//  ---------------------------------------------------------------------------
//  Late-bound probe for ANY third-party indicator on the chart. It answers the
//  three questions a closed-source vendor DLL will not:
//
//    1. IS THE SIGNAL CAPTURABLE?  A condition builder (Infinity Algo Engine,
//       PredatorX, NT8's own Strategy Builder) can only see series registered
//       as PLOTS. A vendor can also expose a public Series<double> that is NOT
//       a plot — visible from C#, invisible to every builder. The probe prints
//       Values.Length vs Plots.Length and flags the gap. That gap is the single
//       most likely reason a signal "can't be captured".
//
//    2. WHAT IS THE PLOT MAP?  Live plot index, Name, Brush and PlotStyle.
//
//    3. DOES IT REPAINT?  Write-once csv row per plot per bar, plus a REVISION
//       row every time an already-recorded value later changes.
//
//  v1.1.0 — NO COMPILE-TIME REFERENCE to the vendor assembly. It resolves the
//  target through ChartControl.Indicators by name, so it compiles and runs on an
//  install where the vendor indicator is not present or not licensed. Set
//  "Target Indicator" to any substring of the indicator's name, or leave it
//  blank to inventory every indicator on the chart.
//
//  USAGE: add the vendor indicator to the chart FIRST, then add this probe to
//  the same chart and the same data series. Read the Output window.
//
//  Output: <NinjaTrader 8>\gbSignalProbe\gbSignalProbe_<instr>_<stamp>.csv
//  ---------------------------------------------------------------------------

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
	public class gbSignalProbe : Indicator
	{
		private NinjaTrader.Gui.NinjaScript.IndicatorRenderBase	target;
		private bool				inventoryPrinted;
		private bool				mapPrinted;
		private bool				resolveFailureLogged;

		private StreamWriter		writer;
		private string				filePath		= string.Empty;
		private int					plotCount;
		private string[]			plotNames;

		// absolute bar index -> snapshot of every value at the moment that bar was first recorded
		private Dictionary<int, double[]> firstSeen = new Dictionary<int, double[]>();

		private int					revisionCount;
		private int					rowCount;

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Target Indicator", Description = "Substring of the indicator's name to probe, e.g. UltimateSignals. Leave BLANK to just inventory every indicator on the chart and stop.", GroupName = "1. Probe", Order = 0)]
		public string TargetIndicator { get; set; }

		[NinjaScriptProperty]
		[Range(0, 200)]
		[Display(Name = "Revision Lookback Bars", Description = "How many closed bars to re-check for revised values on every new bar. 20 is plenty for a ZigZag; raise it if revisions keep appearing at the edge of the window.", GroupName = "1. Probe", Order = 1)]
		public int LookbackBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Write CSV", Description = "Write the write-once / revision log to disk. Turn off to use the Output window only.", GroupName = "1. Probe", Order = 2)]
		public bool WriteCsv { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Log Every Bar", Description = "Log every bar. Off = log only bars on which at least one series is active (much smaller file).", GroupName = "1. Probe", Order = 3)]
		public bool LogEveryBar { get; set; }

		// GreyBeard developer block — read-only, informational, never serialized
		// (no setter, no [NinjaScriptProperty]). "0. Developer" sorts first in the grid.
		[Display(Name = "Author", GroupName = "0. Developer", Order = 0)]
		public string Author => "GreyBeard";

		[Display(Name = "Version", GroupName = "0. Developer", Order = 1)]
		public string Version => "1.2.0";

		[Display(Name = "Website", GroupName = "0. Developer", Order = 2)]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "gbSignalProbe";
				Description						= "Late-bound plot-map, capturability and repaint probe for any third-party indicator on the chart.";
				Calculate						= Calculate.OnBarClose;
				IsOverlay						= false;
				DisplayInDataBox				= false;
				DrawOnPricePanel				= false;
				IsSuspendedWhileInactive		= false;
				ShowTransparentPlotsInDataBox	= true;
				PaintPriceMarkers				= false;

				TargetIndicator					= "UltimateSignals";
				LookbackBars					= 20;
				WriteCsv						= true;
				LogEveryBar						= false;
			}
			else if (State == State.Historical)
			{
				if (WriteCsv)
					OpenWriter();
			}
			else if (State == State.Terminated)
			{
				CloseWriter();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1)
				return;

			if (!inventoryPrinted)
				PrintInventory();

			if (target == null)
			{
				ResolveTarget();
				if (target == null)
					return;					// keep retrying — the chart may still be building
			}

			if (!mapPrinted)
				PrintPlotMap();

			if (plotCount == 0)
				return;

			RecordBar(CurrentBar);
			CheckForRevisions();
			PruneSnapshots();
		}

		// ------------------------------------------------------------------
		//  Discovery — what is actually on this chart, and what can be seen
		// ------------------------------------------------------------------
		private void PrintInventory()
		{
			if (ChartControl == null)
				return;					// not on a chart (Market Analyzer / Strategy Analyzer) — try again next bar

			inventoryPrinted = true;

			Print("");
			Print("===== gbSignalProbe — indicators on this chart =====");
			Print("Instrument : " + Instrument.FullName + "   Bars: " + Bars.BarsPeriod);
			Print("");

			try
			{
				foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase ind in ChartControl.Indicators)
				{
					if (ind == null || object.ReferenceEquals(ind, this))
						continue;

					int nValues = 0;
					int nPlots  = 0;
					try { nValues = ind.Values == null ? 0 : ind.Values.Length; } catch { }
					try { nPlots  = ind.Plots  == null ? 0 : ind.Plots.Length;  } catch { }

					Print(string.Format("  {0,-40}  Values={1,-3} Plots={2,-3} {3}",
						ind.Name,
						nValues,
						nPlots,
						nValues > nPlots ? "<-- " + (nValues - nPlots) + " series are NOT plots (invisible to condition builders)" : ""));
				}
			}
			catch (Exception ex)
			{
				Print("  could not enumerate ChartControl.Indicators — " + ex.Message);
			}

			Print("");
			Print("Set 'Target Indicator' to a substring of one of the names above.");
			Print("===================================================");
			Print("");
		}

		private void ResolveTarget()
		{
			if (string.IsNullOrEmpty(TargetIndicator) || ChartControl == null)
				return;

			try
			{
				foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase ind in ChartControl.Indicators)
				{
					if (ind == null || object.ReferenceEquals(ind, this))
						continue;

					string n = ind.Name ?? string.Empty;
					if (n.IndexOf(TargetIndicator, StringComparison.OrdinalIgnoreCase) < 0)
						continue;

					target = ind;
					Print("gbSignalProbe: target resolved -> " + n);
					return;
				}
			}
			catch { }

			if (!resolveFailureLogged && CurrentBar > 10)
			{
				resolveFailureLogged = true;
				Print("gbSignalProbe: no indicator on this chart matches '" + TargetIndicator
					+ "'. Add it to the chart first, or clear Target Indicator to inventory only.");
			}
		}

		// ------------------------------------------------------------------
		//  The capturability report
		// ------------------------------------------------------------------
		private void PrintPlotMap()
		{
			mapPrinted = true;

			int valueCount = 0;
			int declaredPlots = 0;
			try { valueCount    = target.Values == null ? 0 : target.Values.Length; } catch { }
			try { declaredPlots = target.Plots  == null ? 0 : target.Plots.Length;  } catch { }

			plotCount = valueCount;
			plotNames = new string[plotCount];

			Print("");
			Print("===== gbSignalProbe — plot map for " + target.Name + " =====");
			Print("Values.Length : " + valueCount + "   (series reachable from C#)");
			Print("Plots.Length  : " + declaredPlots + "   (series a condition builder can list)");

			if (valueCount > declaredPlots)
			{
				Print("");
				Print("*** CAPTURABILITY GAP: " + (valueCount - declaredPlots) + " series have no plot. ***");
				Print("*** Those cannot be selected in Infinity Algo Engine, PredatorX or the   ***");
				Print("*** NT8 Strategy Builder. A bridge indicator is required — see           ***");
				Print("*** UltimateSignals_Signals.md.                                          ***");
			}

			Print("");

			for (int i = 0; i < plotCount; i++)
			{
				string name  = "Values[" + i + "]";
				string brush = "-";
				string style = "-";
				bool   isPlot = false;

				try
				{
					if (target.Plots != null && i < target.Plots.Length && target.Plots[i] != null)
					{
						isPlot = true;
						if (!string.IsNullOrEmpty(target.Plots[i].Name))
							name = target.Plots[i].Name;
						brush = target.Plots[i].Brush == null ? "null" : target.Plots[i].Brush.ToString();
						style = target.Plots[i].PlotStyle.ToString();
					}
				}
				catch { }

				plotNames[i] = name;

				Print(string.Format("  [{0}] {1,-20} plot={2,-5} brush={3,-24} style={4}",
					i, name, isPlot ? "YES" : "NO", brush, style));
			}

			Print("");
			Print("A plot with no name, or a duplicate name, may also be unselectable in a");
			Print("condition builder even though it exists. Compare buy-side and sell-side rows.");
			Print("=========================================================");
			Print("");

			if (WriteCsv && writer != null)
			{
				writer.WriteLine("event,time,barIndex,plotIndex,plotName,value,isNaN");
				writer.Flush();
			}
		}

		// ------------------------------------------------------------------
		//  Write-once capture + revision detection
		// ------------------------------------------------------------------
		private void RecordBar(int bar)
		{
			if (firstSeen.ContainsKey(bar))
				return;

			double[] snap = new double[plotCount];
			bool anyActive = false;

			for (int i = 0; i < plotCount; i++)
			{
				double v = ReadValue(i, bar);
				snap[i] = v;
				if (!double.IsNaN(v))
					anyActive = true;
			}

			firstSeen[bar] = snap;

			if (!anyActive && !LogEveryBar)
				return;

			for (int i = 0; i < plotCount; i++)
			{
				if (!LogEveryBar && double.IsNaN(snap[i]))
					continue;
				WriteRow("FIRST", Time[CurrentBar - bar], bar, i, snap[i]);
			}
		}

		private void CheckForRevisions()
		{
			int max = Math.Min(LookbackBars, CurrentBar);

			for (int barsAgo = 1; barsAgo <= max; barsAgo++)
			{
				int bar = CurrentBar - barsAgo;

				double[] snap;
				if (!firstSeen.TryGetValue(bar, out snap))
					continue;

				for (int i = 0; i < plotCount; i++)
				{
					double now = ReadValue(i, bar);
					if (Same(snap[i], now))
						continue;

					revisionCount++;
					WriteRow("REVISION", Time[barsAgo], bar, i, now);

					Print(string.Format(
						"REPAINT  bar {0} ({1})  [{2}] {3}:  {4}  ->  {5}   (revisions: {6})",
						bar,
						Time[barsAgo].ToString("yyyy-MM-dd HH:mm:ss"),
						i,
						plotNames[i],
						Fmt(snap[i]),
						Fmt(now),
						revisionCount));

					snap[i] = now;			// re-baseline so one change is reported once
				}
			}
		}

		// Absolute bar index — avoids any barsAgo ambiguity between our series and the target's.
		private double ReadValue(int plotIndex, int absoluteBar)
		{
			try
			{
				Series<double> s = target.Values[plotIndex];
				if (s == null || absoluteBar < 0 || !s.IsValidDataPointAt(absoluteBar))
					return double.NaN;
				return s.GetValueAt(absoluteBar);
			}
			catch
			{
				return double.NaN;
			}
		}

		// NaN-aware equality: plain != would report every idle bar as a change,
		// because NaN != NaN is true.
		private static bool Same(double a, double b)
		{
			if (double.IsNaN(a) && double.IsNaN(b))	return true;
			if (double.IsNaN(a) || double.IsNaN(b))	return false;
			return Math.Abs(a - b) < 1e-9;
		}

		private static string Fmt(double v)
		{
			return double.IsNaN(v) ? "NaN" : v.ToString("0.########", CultureInfo.InvariantCulture);
		}

		private void PruneSnapshots()
		{
			int cutoff = CurrentBar - LookbackBars - 5;
			if (cutoff < 0 || firstSeen.Count < 5000)
				return;

			List<int> stale = new List<int>();
			foreach (int bar in firstSeen.Keys)
				if (bar < cutoff)
					stale.Add(bar);

			for (int i = 0; i < stale.Count; i++)
				firstSeen.Remove(stale[i]);
		}

		// ------------------------------------------------------------------
		//  CSV plumbing
		// ------------------------------------------------------------------
		private void OpenWriter()
		{
			try
			{
				string dir = Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "gbSignalProbe");
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				// Include the target substring: two probes on the same chart/instrument opened in the
				// same second (the normal case when comparing a vendor and a gb indicator side by side)
				// would otherwise both resolve to the same filename and one would clobber the other.
				string tag = string.IsNullOrEmpty(TargetIndicator) ? "all" : TargetIndicator;
				foreach (char c in Path.GetInvalidFileNameChars())
					tag = tag.Replace(c, '_');
				tag = tag.Replace(' ', '_');

				filePath = Path.Combine(dir, string.Format("gbSignalProbe_{0}_{1}_{2}.csv",
					Instrument.MasterInstrument.Name,
					tag,
					DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")));

				writer = new StreamWriter(filePath, false);
				Print("gbSignalProbe: logging to " + filePath);
			}
			catch (Exception ex)
			{
				Print("gbSignalProbe: could not open log — " + ex.Message);
				writer = null;
			}
		}

		private void WriteRow(string ev, DateTime t, int bar, int plotIndex, double value)
		{
			rowCount++;

			if (writer == null)
				return;

			try
			{
				writer.WriteLine(string.Join(",",
					ev,
					t.ToString("yyyy-MM-dd HH:mm:ss"),
					bar.ToString(CultureInfo.InvariantCulture),
					plotIndex.ToString(CultureInfo.InvariantCulture),
					plotNames[plotIndex],
					Fmt(value),
					double.IsNaN(value) ? "1" : "0"));

				writer.Flush();			// crash-safe: the log survives an NT8 restart
			}
			catch { }
		}

		private void CloseWriter()
		{
			if (writer == null)
				return;

			try
			{
				Print(string.Format("gbSignalProbe: {0} rows, {1} REVISIONS -> {2}",
					rowCount, revisionCount, filePath));
				writer.Flush();
				writer.Close();
			}
			catch { }
			finally
			{
				writer = null;
			}
		}
	}
}
