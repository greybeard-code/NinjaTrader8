#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.TradeSaber_SignalMod
{
	public class gbSaberADXFilter : Indicator
	{
		private Series<double>		dmPlus;
		private Series<double>		dmMinus;
		private Series<double>		sumDmPlus;
		private Series<double>		sumDmMinus;
		private Series<double>		sumTr;
		private Series<double>		tr;
		private bool 				GoTrade;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"An ADX indicator that uses a threshold to trigger tags for Trade Saber Reversal Predator ";
				Name										= "gbSaberADXFilter";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Period					= 14;
				Threshold				= 35;
				AddPlot(Brushes.DarkCyan, "ADXLine");
				AddLine(Brushes.Red, Threshold, "Threshold");
				AddLine(Brushes.SkyBlue, 25, "Lower");
				AddLine(Brushes.Orange, 75, "Upper");
				
				TradeOn					= @"TradeOn";
				TradeOff				= @"TradeOff";
				GoTrade					= false;
			}
			else if (State == State.Configure)
			{
				Lines[0].Value = Threshold;
			}
			else if (State == State.DataLoaded)
			{
				dmPlus		= new Series<double>(this);
				dmMinus		= new Series<double>(this);
				sumDmPlus	= new Series<double>(this);
				sumDmMinus	= new Series<double>(this);
				sumTr		= new Series<double>(this);
				tr			= new Series<double>(this);

			}
		}

		protected override void OnBarUpdate()
		{
			//ADX with threshold and tags. Thanks to PatternDaveTrader for the insparation. writen by GreyBeard
			
			double high0	= High[0];
			double low0		= Low[0];

			if (CurrentBar == 0)
			{
				tr[0]				= high0 - low0;
				dmPlus[0]			= 0;
				dmMinus[0]			= 0;
				sumTr[0]			= tr[0];
				sumDmPlus[0]		= dmPlus[0];
				sumDmMinus[0]		= dmMinus[0];
				Value[0]			= 50;
			}
			else
			{
				double high1		= High[1];
				double low1			= Low[1];
				double close1		= Close[1];

				tr[0]				= Math.Max(Math.Abs(low0 - close1), Math.Max(high0 - low0, Math.Abs(high0 - close1)));
				dmPlus[0]			= high0 - high1 > low1 - low0 ? Math.Max(high0 - high1, 0) : 0;
				dmMinus[0]			= low1 - low0 > high0 - high1 ? Math.Max(low1 - low0, 0) : 0;

				if (CurrentBar < Period)
				{
					sumTr[0]		= sumTr[1] + tr[0];
					sumDmPlus[0]	= sumDmPlus[1] + dmPlus[0];
					sumDmMinus[0]	= sumDmMinus[1] + dmMinus[0];
				}
				else
				{
					double sumTr1		= sumTr[1];
					double sumDmPlus1	= sumDmPlus[1];
					double sumDmMinus1	= sumDmMinus[1];

					sumTr[0]			= sumTr1 - sumTr1 / Period + tr[0];
					sumDmPlus[0]		= sumDmPlus1 - sumDmPlus1 / Period + dmPlus[0];
					sumDmMinus[0]		= sumDmMinus1 - sumDmMinus1 / Period + dmMinus[0];
				}

				double sumTr0		= sumTr[0];
				double diPlus		= 100 * (sumTr0.ApproxCompare(0) == 0 ? 0 : sumDmPlus[0] / sumTr[0]);
				double diMinus		= 100 * (sumTr0.ApproxCompare(0) == 0 ? 0 : sumDmMinus[0] / sumTr[0]);
				double diff			= Math.Abs(diPlus - diMinus);
				double sum			= diPlus + diMinus;

				Value[0]			= sum.ApproxCompare(0) == 0 ? 50 : ((Period - 1) * Value[1] + 100 * diff / sum) / Period;
				
				 // Set 1
				if ((Value[0] > Threshold)  && (GoTrade == false))
				{
					Draw.Text(this, Convert.ToString(TradeOn) + " " + Convert.ToString(CurrentBars[0]), @"On", 0, (Close[0] + (-20 * TickSize)) );
					GoTrade = true;
				}
				
				 // Set 2
				// if ((CrossBelow( Value[0], Threshold, 1))	 && (GoTrade == true))
				if ((Value[0] < Threshold)  && (GoTrade == true))
				{
					Draw.Text(this, Convert.ToString(TradeOff) + " " + Convert.ToString(CurrentBars[0]), @"Off", 0, (Close[0] + (-20 * TickSize)) );
					GoTrade = false;
				}
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Description="Number of bars for ADX calculation.", Order=1, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Threshold", Description="Threshold that when ADX is above turns trades on", Order=2, GroupName="Parameters")]
		public int Threshold
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ADXFilter
		{
			get { return Values[0]; }
		}
		[NinjaScriptProperty]
		[Display(Name="Trade On Tag",Description="Tag for trades on", Order=3, GroupName="Parameters")]
		public string TradeOn
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Trade Off Tag", Description="Tag for trades off", Order=4, GroupName="Parameters")]
		public string TradeOff
		{ get; set; }

		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradeSaber_SignalMod.gbSaberADXFilter[] cachegbSaberADXFilter;
		public TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(int period, int threshold, string tradeOn, string tradeOff)
		{
			return gbSaberADXFilter(Input, period, threshold, tradeOn, tradeOff);
		}

		public TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(ISeries<double> input, int period, int threshold, string tradeOn, string tradeOff)
		{
			if (cachegbSaberADXFilter != null)
				for (int idx = 0; idx < cachegbSaberADXFilter.Length; idx++)
					if (cachegbSaberADXFilter[idx] != null && cachegbSaberADXFilter[idx].Period == period && cachegbSaberADXFilter[idx].Threshold == threshold && cachegbSaberADXFilter[idx].TradeOn == tradeOn && cachegbSaberADXFilter[idx].TradeOff == tradeOff && cachegbSaberADXFilter[idx].EqualsInput(input))
						return cachegbSaberADXFilter[idx];
			return CacheIndicator<TradeSaber_SignalMod.gbSaberADXFilter>(new TradeSaber_SignalMod.gbSaberADXFilter(){ Period = period, Threshold = threshold, TradeOn = tradeOn, TradeOff = tradeOff }, input, ref cachegbSaberADXFilter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(int period, int threshold, string tradeOn, string tradeOff)
		{
			return indicator.gbSaberADXFilter(Input, period, threshold, tradeOn, tradeOff);
		}

		public Indicators.TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(ISeries<double> input , int period, int threshold, string tradeOn, string tradeOff)
		{
			return indicator.gbSaberADXFilter(input, period, threshold, tradeOn, tradeOff);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(int period, int threshold, string tradeOn, string tradeOff)
		{
			return indicator.gbSaberADXFilter(Input, period, threshold, tradeOn, tradeOff);
		}

		public Indicators.TradeSaber_SignalMod.gbSaberADXFilter gbSaberADXFilter(ISeries<double> input , int period, int threshold, string tradeOn, string tradeOff)
		{
			return indicator.gbSaberADXFilter(input, period, threshold, tradeOn, tradeOff);
		}
	}
}

#endregion
