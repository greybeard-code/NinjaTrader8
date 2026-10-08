//
// Copyright (C) 2021, NinjaTrader LLC <www.ninjatrader.com>
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component
// Coded by NinjaTrader_ChelseaB
// Modified and improved by GreyBeard for Red Folder trades -https://www.fspfutures.com/
//
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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies.GreyBeard
{
	public class gbRedFolder : Strategy
	{
		private bool				exitOnCloseWait;
		private Order				longStopEntry, shortStopEntry, entryOrder ;
		private string				ocoString;
		private SessionIterator		sessionIterator;
		private bool 				orderPlaced, atmPlaced;
		private int					expireTimer;
		private DateTime			expireTime;
		
        public enum OrderDirection   // Enum for order direction options
        {
            UpOnly,
            DownOnly,
            BothOCO
        }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Point Offset", Order = 1, GroupName = "Parameters")]
        public int PointOffset { get; set; } = 25; // Default to 25 points

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Stop Loss (ticks)", Order = 2, GroupName = "Parameters")]
        public int StopLossTicks { get; set; } = 400;  // Default to 00 ticks

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Profit Target (ticks)", Order = 3, GroupName = "Parameters")]
        public int ProfitTargetTicks { get; set; } = 62;  // Default to 62 ticks

     	[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Event Time-Order will place 1 bar before", Order = 4, GroupName = "Parameters")]
        public DateTime TriggerTime { get; set; } = DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture); // Default to 8:30 AM
			
//		[NinjaScriptProperty]
//		[Display(Name = "Order will place 1 bar before", Order = 5, GroupName = "Parameters")]
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Order Qty", Order = 5, GroupName = "Parameters")]
        public int OrderQty { get; set; } = 10;  // Default to 10 contracts
		
		[NinjaScriptProperty]
        [Display(Name = "Order Direction", Order = 6, GroupName = "Parameters")]
        public OrderDirection SelectedOrderDirection { get; set; } = OrderDirection.BothOCO;


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description								= @"Places an OCO buy and sell limit order 30 points above and below the current price at 8:29 AM, with configurable stop loss and profit target.";
				Name									= "gbRedFolder";
				Calculate								= Calculate.OnPriceChange;
				IsExitOnSessionCloseStrategy			= true;
				ExitOnSessionCloseSeconds				= 30;
				IsUnmanaged								= true;
				orderPlaced 							= false; // Used to ensure only one order is placed
				atmPlaced 								= false; // used to make sure the ATM is only placed once
				expireTimer								= 5;    // wait 5 minutes and then cancle the initial order
			}
			else if (State == State.Historical)
			{
				sessionIterator		= new SessionIterator(Bars);
				exitOnCloseWait		= false;
			}
			else if (State == State.Realtime)
			{
				// this needs to be run at least once before orders start getting placed.
				// I could do this when CurrentBar is 0 in OnBarUpdate,
				// but since this script only runs in real-time, I can trigger it once as the script transistions to real-time
				sessionIterator.GetNextSession(Time[0], true);
			}
		}

		
		protected override void OnBarUpdate() { }
		
		private void AssignOrderToVariable(ref Order order)
		{
			// Assign Order variable from OnOrderUpdate() to ensure the assignment occurs when expected.
			// This is more reliable than assigning the return Order object from the submission method as the assignment is not guaranteed to be complete if it is referenced immediately after submitting
			if (order.Name == "longStopEntry" && longStopEntry != order)
				longStopEntry = order;

			if (order.Name == "shortStopEntry" && shortStopEntry != order)
				shortStopEntry = order;
		}

		// prevents entry orders after the exit on close until the start of the new session
		private bool ExitOnCloseWait(DateTime tickTime)
		{
			// the sessionIterator only needs to be updated when the session changes (after its first update)
			if (Bars.IsFirstBarOfSession)
				sessionIterator.GetNextSession(Time[0], true);

			// if after the exit on close, prevent new orders until the new session
			if (tickTime >= sessionIterator.ActualSessionEnd.AddSeconds(-ExitOnSessionCloseSeconds) && tickTime <= sessionIterator.ActualSessionEnd)
				exitOnCloseWait = true;

			// an exit on close occurred in the previous session, reset for a new entry on the first bar of a new session
			if (exitOnCloseWait && Bars.IsFirstBarOfSession)
				exitOnCloseWait = false;

			return exitOnCloseWait;
		}

		protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity,
			Cbi.MarketPosition marketPosition, string orderId, DateTime time)
		{
			
			entryOrder 	= execution.Order ; // This variable holds an object representing our entry order.
			// if the long entry filled, place a profit target and stop loss to protect the order. But not if stops are already present
			if (longStopEntry != null && execution.Order == longStopEntry && !atmPlaced)
			{
				// generate a new oco string for the protective stop and target
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				Print (string.Format("LongStop Ready {0} -- {1}  # {2} ",Time[0], Close[0], OrderQty ));
				// submit a protective profit target order
				SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Limit,      OrderQty, (High[0] + ProfitTargetTicks * TickSize), 0, ocoString, "longProfitTarget");
				// submit a protective stop loss order
				SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.StopMarket, OrderQty, 0, (Low[0] - StopLossTicks * TickSize), ocoString, "longStopLoss");
				atmPlaced = true ; 
			}

			// reverse the order types and prices for a short  But not if stops are already present
			else if (shortStopEntry != null && execution.Order == shortStopEntry && !atmPlaced)
			{
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				Print (string.Format("ShortStop Ready {0} -- {1}  # {2} ",Time[0], Close[0], OrderQty ) );
				SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.Limit, OrderQty, (Low[0] - ProfitTargetTicks * TickSize), 0, ocoString, "shortProfitTarget");
				SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.StopMarket, OrderQty, 0, (High[0] + StopLossTicks * TickSize), ocoString, "shortStopLoss");
				atmPlaced = true ; 

			}

			// I didn't use Order variables to track the stop loss and profit target, but I could have
			// Instead, I detect the orders when the fill by their signalName
			// (the execution.Name is the signalName provided with the order)

			// when the long profit or stop fills, set the long entry to null to allow a new entry
			else if (execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss")
			{
				longStopEntry	= null;
				shortStopEntry	= null;
			}
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			// only places orders in real time
			if (State != State.Realtime || ExitOnCloseWait(marketDataUpdate.Time))
				return;			
			
			// Calculate the trigger time 30 seconds before the set TriggerTime
//            DateTime triggerTimeAdjusted = TriggerTime.AddSeconds(-30);
			
			DateTime triggerTimeAdjusted = TriggerTime;
			expireTime = TriggerTime.AddMinutes(expireTimer); // add the expire timer to the triger time to get expire time
			// Calculate the trigger time one bar before TriggerTime. Ninjatrader only tracks the bar time with Time[0}
			// Determine adjustment based on bar type
            if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute || BarsPeriod.BarsPeriodType == BarsPeriodType.Second)
            {
                int barSize = BarsPeriod.Value;
				
                triggerTimeAdjusted = TriggerTime.AddSeconds(-barSize * (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute ? 60 : 1));
            }
            else
            {
                Print("This strategy currently only supports time-based bars (Minute or Second).");
                return;
            }
//			Print (string.Format("Time Calc - {0}  Time {1}  BarTime {2} ",triggerTimeAdjusted, Time[0], Bars.GetTime(CurrentBars[0]) ) );

            // Check if the current bar's time matches the adjusted trigger time
          	// require both entry orders to be null to begin the entry bracket. Time must be event time
			// entry orders are set to null if the entry is cancelled due to oco or when the exit order exits the trade
			// if the Order variables for the entries are null, no trade is in progress, place a new order in real time
			if ((longStopEntry == null && shortStopEntry == null) && 
				(Time[0].Hour == triggerTimeAdjusted.Hour && Time[0].Minute == triggerTimeAdjusted.Minute && Time[0].Second >= triggerTimeAdjusted.Second))
			{
				// generate a unique oco string based on the time
				// oco means that when one entry fills, the other entry is automatically cancelled
				// in OnExecution we will protect these orders with our version of a stop loss and profit target when one of the entry orders fills
				ocoString		= string.Format("unmanagedentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				Print (string.Format("Order Ready {0} -- {1} Direction {2}  ",Time[0], Close[0], SelectedOrderDirection ));
				
				// Place orders based on the selected direction
                switch (SelectedOrderDirection)
                    {
                        case OrderDirection.UpOnly:
							longStopEntry	= SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, OrderQty, 0, (High[0] + PointOffset), ocoString, "longStopEntry");
							orderPlaced = true;
							break;

                        case OrderDirection.DownOnly:
							shortStopEntry	= SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.StopMarket, OrderQty, 0, (Low[0] - PointOffset ), ocoString, "shortStopEntry");
                            orderPlaced = true;
							break;

                        case OrderDirection.BothOCO:
                            // Place both Buy and Sell OCO Limit Orders
			  				longStopEntry	= SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, OrderQty, 0, (High[0] + PointOffset), ocoString, "longStopEntry");
							shortStopEntry	= SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.StopMarket, OrderQty, 0, (Low[0] - PointOffset ), ocoString, "shortStopEntry");
							orderPlaced = true;
                            break;
                    }
				
			}
			if ((orderPlaced == true) && (atmPlaced == false) && // Did we place the order and we have not added a ATM ( the limit didn't hit)
				(Time[0].Hour == expireTime.Hour && Time[0].Minute == expireTime.Minute)   )  //and it's expire time
			{
				//cancel order
				Print (string.Format("Limit Order Expired {0} -- {1}  ",Time[0], Close[0] ));
				CancelOrder(longStopEntry);
				CancelOrder(shortStopEntry);
			}
		}

		protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice,
			int quantity, int filled, double averageFillPrice,
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			AssignOrderToVariable(ref order);
			// when both orders are cancelled set to null for a new entry
			// if the exit on close fills, also reset for a new entry
			if ((longStopEntry != null && longStopEntry.OrderState == OrderState.Cancelled && shortStopEntry != null && shortStopEntry.OrderState == OrderState.Cancelled) || (order.Name == "Exit on session close" && order.OrderState == OrderState.Filled))
			{
				longStopEntry	= null;
				shortStopEntry	= null;
				 
			}
		}
	}
}
