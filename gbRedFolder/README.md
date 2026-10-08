# gbRedFolder

Red Folder strategy for trading volatile news events. Places a limit/market order above or below
(or both) the current price at a specific time.

- Use the [Forex Factory calendar](https://www.forexfactory.com/calendar) to find a Red Folder event.
- Use a 30 Second chart: NinjaTrader can only look at the time of the current bar to trigger orders.
  The script places the order 30 seconds before the selected time on a 30 second chart.
- The script exits after 15 minutes if the initial orders are not filled.
- Works best with one chart set up as "up" and a separate account as "down".
- Inspiration: <https://youtu.be/OY7TqQvj4Bs>. Best events: Non-Farm (NFP), Core CPI, Core PPI.

Files: `gbRedFolder.cs`, `gbRedFolder.zip` (importable package).
