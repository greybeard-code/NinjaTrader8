# NinjaTrader8
 GreyBeard's Repo of Ninjatrader scripts. Use at your own risk.
 
 Many of my scripts are for use with TradeSaber's Predator tool - https://tradesaber.com/
 I do this for the chalange, but if you want to say thanks, Buy Me a Coffee - https://buymeacoffee.com/greybeardcode
 All code is in the public domain, let me know if you want any changes.
# Current Indicators and Strategies

+ **gbSaberADXFilter.cs** - ADX Filer for Trade Saber Predator. 

+ **gbSaberTOWilliamsR.cs** Trader Oracle WilliamsR for Trade Saber Predator. 

+ **gbRedFolder.cs** - Red Folder strategy for trading volatile news events. Will place a limit market order above or below (or both) of current price at a specific time.  
 Use Forex Factory https://www.forexfactory.com/calendar to find a Red Folder event. Use a 30 Second chart because NT can only look at the time of the current bar to trigger orders. 
 Script will exit after 15 minutes if the initial market orders are not filled.
 It will place the order 30 seconds before the selected time if the chart is a 30 second chart. That's what I use.
 I think it works better if you set one chart as  up and a separate account as down.  
 The inspiration was https://youtu.be/OY7TqQvj4Bs?si=pGgVUE8X0ZEvWE9S  He says the best red folder events are  Non-Farm (NFP), Core CPI, & Core PPI.


+ **gbPaperFeet.cs** -  Modified version fo Trader Oracle's PaperFeet to place indicators on the chart for Trade Saber Predator https://youtu.be/HeJOxQ7_fhM?si=XDq6wDu_qhTnE7gV  
 Program Predator to Enter Long on "Long" and Short on "Short" Use "ExitLong" and "ExitShort" for the exit signal.
