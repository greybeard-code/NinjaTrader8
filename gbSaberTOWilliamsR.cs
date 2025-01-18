#region Using declarations
using System;
using Microsoft.CSharp;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;
using System.Globalization;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.TradeSaber_SignalMod
{
    public class gbSaberTOWilliamsR : Indicator
    {
        private WilliamsR williamsR;

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name="Williams %R Period", Order=1, GroupName="Parameters")]
        public int Period { get; set; } = 14;

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name="Tick Interval", Order=2, GroupName="Parameters")]
        public int TickInterval { get; set; } = 2000;

        [Range(-100, 0), NinjaScriptProperty]
        [Display(Name="Upper Threshold", Order=3, GroupName="Parameters")]
        public double UpperThreshold { get; set; } = -20;

        [Range(-100, 0), NinjaScriptProperty]
        [Display(Name="Lower Threshold", Order=4, GroupName="Parameters")]
        public double LowerThreshold { get; set; } = -80;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Williams %R with customizable tick interval and threshold lines, calculated on price update";
                Name = "gbSaberTOWilliamsR";
                Calculate = Calculate.OnPriceChange;
                AddPlot(Brushes.Orange, "WilliamsR");
                AddLine(Brushes.Green, UpperThreshold, "UpperThresholdLine");
                AddLine(Brushes.Red, LowerThreshold, "LowerThresholdLine");
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Tick, TickInterval);
            }
            else if (State == State.DataLoaded)
            {
                williamsR = WilliamsR(Period);
            }
        }

        protected override void OnBarUpdate()
        {
            // Check that both primary and secondary series have enough bars before accessing them
            if (BarsInProgress == 1 && CurrentBars[0] > Period && CurrentBars[1] > Period)
            {
                // Update the Williams %R value on the plot
                Values[0][0] = williamsR[0];
				
				if (
				// Basic Long PreReqs
				//Ensures there is a green bar on the Renko
				(Close[0] > Open[0])
				// Ensures that the 2k Tick WilliamsR starts below and Crosses above the -20 Line (not just randomly above)
				 && (williamsR[1] < UpperThreshold)
				 && (williamsR[0] > UpperThreshold) )
			{
				//Print(string.Format("{2} Up Triger- {0}  {1} ", williamsR[0] , Close[0] , Time[0] ));
				Draw.Text(this, "TOWilR-Long" + Convert.ToString(CurrentBars[0]), @"Long", 0, (Close[0] + (+20 * TickSize)) );
			}
			if (
				// Basic Short PreReqs
				//Ensures there is a red bar on the Renko
				(Close[0] < Open[0])
			
				// Ensures that the 2k Tick WilliamsR starts Above and Crosses below the -80 Line (not just randomly below)
				&& (williamsR[1] > LowerThreshold)
				&& (williamsR[0] < LowerThreshold))
			{
				
				//Print(string.Format("{2} Down Triger - {0}  {1} ", williamsR[0] , Close[0] , Time[0] ));
				Draw.Text(this,  "TOWilR-Short" + Convert.ToString(CurrentBars[0]), @"Short", 0, (Close[0] + (-20 * TickSize)) );
			}
				
				
				
				
            }
			
			
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradeSaber_SignalMod.gbSaberTOWilliamsR[] cachegbSaberTOWilliamsR;
		public TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			return gbSaberTOWilliamsR(Input, period, tickInterval, upperThreshold, lowerThreshold);
		}

		public TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(ISeries<double> input, int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			if (cachegbSaberTOWilliamsR != null)
				for (int idx = 0; idx < cachegbSaberTOWilliamsR.Length; idx++)
					if (cachegbSaberTOWilliamsR[idx] != null && cachegbSaberTOWilliamsR[idx].Period == period && cachegbSaberTOWilliamsR[idx].TickInterval == tickInterval && cachegbSaberTOWilliamsR[idx].UpperThreshold == upperThreshold && cachegbSaberTOWilliamsR[idx].LowerThreshold == lowerThreshold && cachegbSaberTOWilliamsR[idx].EqualsInput(input))
						return cachegbSaberTOWilliamsR[idx];
			return CacheIndicator<TradeSaber_SignalMod.gbSaberTOWilliamsR>(new TradeSaber_SignalMod.gbSaberTOWilliamsR(){ Period = period, TickInterval = tickInterval, UpperThreshold = upperThreshold, LowerThreshold = lowerThreshold }, input, ref cachegbSaberTOWilliamsR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			return indicator.gbSaberTOWilliamsR(Input, period, tickInterval, upperThreshold, lowerThreshold);
		}

		public Indicators.TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(ISeries<double> input , int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			return indicator.gbSaberTOWilliamsR(input, period, tickInterval, upperThreshold, lowerThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			return indicator.gbSaberTOWilliamsR(Input, period, tickInterval, upperThreshold, lowerThreshold);
		}

		public Indicators.TradeSaber_SignalMod.gbSaberTOWilliamsR gbSaberTOWilliamsR(ISeries<double> input , int period, int tickInterval, double upperThreshold, double lowerThreshold)
		{
			return indicator.gbSaberTOWilliamsR(input, period, tickInterval, upperThreshold, lowerThreshold);
		}
	}
}

#endregion
