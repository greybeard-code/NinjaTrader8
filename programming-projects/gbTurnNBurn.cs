#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

// File path suggestion:
// Documents\NinjaTrader 8\bin\Custom\Indicators\GreyBeard\gbTurnNBurn.cs

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard
{
    [CategoryOrder("Developer", 0)]
    public class gbTurnNBurn : Indicator
    {
        private NinjaTrader.NinjaScript.Indicators.LivewireTradingSuite.LivewireWaddahExplosion LivewireWaddahExplosion1;

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(
            Name        = "EntryOffsetTicks",
            GroupName   = "gbTurnNBurn",
            Order       = 10,
            Description = "Ticks beyond the reversal candle for suggested stop-entry.")]
        public int EntryOffsetTicks { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "gbTurnNBurn";
                Description = "Reversal candle + Waddah Explosion confluence signal (indicator only, no orders).";

                Calculate       = Calculate.OnEachTick;
                IsOverlay       = true;
                DrawOnPricePanel = true;
                DisplayInDataBox = true;

                // 1 = long signal, -1 = short, 0 = none
                AddPlot(Brushes.Transparent, "Signal");

                BarsRequiredToPlot = 4;
                EntryOffsetTicks   = 2;
            }
            else if (State == State.DataLoaded)
            {
                // Same Waddah settings you were using in the strategy
                LivewireWaddahExplosion1 =
                   LivewireWaddahExplosion(Close, false, false, 150, false, CustomTimeFrame.Tick, 1000, 160, 20, 40, 20, 2, false, 80, false, false, 14, 20, 10);
            }
        }

        protected override void OnBarUpdate()
        {
            // Need at least bars [0..3]
            if (CurrentBar < 3)
            {
                Values[0][0] = 0;
                return;
            }

            // Keep behavior as “once per bar”
            if (!IsFirstTickOfBar)
                return;

            double signal = 0;

            // LONG signal logic:
            // - Waddah up > down and above deadzone on bar [1]
            // - Bullish reversal: [1] green, [2] red, [3] red
            bool longSignal =
                LivewireWaddahExplosion1.TrendUp[1]   > LivewireWaddahExplosion1.TrendDown[1] &&
                LivewireWaddahExplosion1.TrendUp[1]   > LivewireWaddahExplosion1.DeadZoneLine[1] &&
                Close[1] > Open[1] &&
                Close[2] < Open[2] &&
                Close[3] < Open[3];

            // SHORT signal logic:
            // - Waddah down > up and above deadzone on bar [1]
            // - Bearish reversal: [1] red, [2] green, [3] green
            bool shortSignal =
                LivewireWaddahExplosion1.TrendDown[1] > LivewireWaddahExplosion1.TrendUp[1] &&
                LivewireWaddahExplosion1.TrendDown[1] > LivewireWaddahExplosion1.DeadZoneLine[1] &&
                Close[1] < Open[1] &&
                Close[2] > Open[2] &&
                Close[3] > Open[3];

            if (longSignal)
            {
                signal = 1;
                double suggestedEntry = High[1] + EntryOffsetTicks * TickSize;

                Print("");
                Print("===== gbTurnNBurn LONG =====");
                Print("Time (bar 0): " + Time[0]);
                Print("Reversal [1] O: " + Open[1] + " H: " + High[1] + " L: " + Low[1] + " C: " + Close[1]);
                Print("Prev [2]      O: " + Open[2] + " C: " + Close[2]);
                Print("Prev [3]      O: " + Open[3] + " C: " + Close[3]);
                Print("Waddah TrendUp[1]:   " + LivewireWaddahExplosion1.TrendUp[1]);
                Print("Waddah TrendDown[1]: " + LivewireWaddahExplosion1.TrendDown[1]);
                Print("Waddah DeadZone[1]:  " + LivewireWaddahExplosion1.DeadZoneLine[1]);
                Print("Suggested stop-entry (EntryOffsetTicks above reversal high): " + suggestedEntry);
                Print("===================================");

                Draw.ArrowUp(
                    this,
                    "gbTurnNBurnLong_" + CurrentBar,
                    false,
                    1,                              // reversal candle
                    Low[1] - 2 * TickSize,
                    Brushes.LimeGreen);
            }

            if (shortSignal)
            {
                signal = -1;
                double suggestedEntry = Low[1] - EntryOffsetTicks * TickSize;

                Print("");
                Print("===== gbTurnNBurn SHORT =====");
                Print("Time (bar 0): " + Time[0]);
                Print("Reversal [1] O: " + Open[1] + " H: " + High[1] + " L: " + Low[1] + " C: " + Close[1]);
                Print("Prev [2]      O: " + Open[2] + " C: " + Close[2]);
                Print("Prev [3]      O: " + Open[3] + " C: " + Close[3]);
                Print("Waddah TrendUp[1]:   " + LivewireWaddahExplosion1.TrendUp[1]);
                Print("Waddah TrendDown[1]: " + LivewireWaddahExplosion1.TrendDown[1]);
                Print("Waddah DeadZone[1]:  " + LivewireWaddahExplosion1.DeadZoneLine[1]);
                Print("Suggested stop-entry (EntryOffsetTicks below reversal low): " + suggestedEntry);
                Print("====================================");

                Draw.ArrowDown(
                    this,
                    "gbTurnNBurnShort_" + CurrentBar,
                    false,
                    1,                              // reversal candle
                    High[1] + 2 * TickSize,
                    Brushes.Red);
            }

            Values[0][0] = signal;
        }

        #region Properties

        [Display(Name = "Author",  Order = 0, GroupName = "Developer")]
        public string Author  => "GreyBeard";

        [Display(Name = "Version", Order = 1, GroupName = "Developer")]
        public string Version => "1.0";

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Signal
        {
            get { return Values[0]; }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.gbTurnNBurn[] cachegbTurnNBurn;
		public GreyBeard.gbTurnNBurn gbTurnNBurn(int entryOffsetTicks)
		{
			return gbTurnNBurn(Input, entryOffsetTicks);
		}

		public GreyBeard.gbTurnNBurn gbTurnNBurn(ISeries<double> input, int entryOffsetTicks)
		{
			if (cachegbTurnNBurn != null)
				for (int idx = 0; idx < cachegbTurnNBurn.Length; idx++)
					if (cachegbTurnNBurn[idx] != null && cachegbTurnNBurn[idx].EntryOffsetTicks == entryOffsetTicks && cachegbTurnNBurn[idx].EqualsInput(input))
						return cachegbTurnNBurn[idx];
			return CacheIndicator<GreyBeard.gbTurnNBurn>(new GreyBeard.gbTurnNBurn(){ EntryOffsetTicks = entryOffsetTicks }, input, ref cachegbTurnNBurn);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.gbTurnNBurn gbTurnNBurn(int entryOffsetTicks)
		{
			return indicator.gbTurnNBurn(Input, entryOffsetTicks);
		}

		public Indicators.GreyBeard.gbTurnNBurn gbTurnNBurn(ISeries<double> input , int entryOffsetTicks)
		{
			return indicator.gbTurnNBurn(input, entryOffsetTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.gbTurnNBurn gbTurnNBurn(int entryOffsetTicks)
		{
			return indicator.gbTurnNBurn(Input, entryOffsetTicks);
		}

		public Indicators.GreyBeard.gbTurnNBurn gbTurnNBurn(ISeries<double> input , int entryOffsetTicks)
		{
			return indicator.gbTurnNBurn(input, entryOffsetTicks);
		}
	}
}

#endregion
