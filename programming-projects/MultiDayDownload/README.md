# MultiDayDownload — NinjaTrader 8 Market Replay Downloaders

Two NinjaScript AddOns that download Market Replay `.nrd` files in bulk, without using NinjaTrader's built-in one-day-at-a-time UI. Both tools appear under **Tools** in the Control Center menu.

---

## MultidayReplayDownloaderWindowCN.cs — Multiday Replay Downloader

A flexible downloader for any futures instrument over any date range.

### Features

- Select any futures instrument from a dropdown (or type one in)
- Instruments are pre-populated with their current active contract (handles quarterly, bi-monthly, and monthly roll schedules)
- Accepts contract notation in either numeric (`NQ 03-26`) or letter-code (`NQ H26` / `NQH26`) format — auto-normalized on Load
- Pick start/end dates manually, from a calendar popup, or with **Last 7 / 30 / 90 Days** quick-select buttons
- **Skip existing replay files** checkbox (default on) avoids re-downloading data you already have
- Sundays included; only Saturdays skipped
- Sequential downloads with live progress bar and ETA
- Cancel mid-run; summary shows Completed / Failed / Canceled counts

### Supported Instruments (pre-populated)

| Category | Symbols |
|---|---|
| Index futures | ES, NQ, YM, RTY |
| Micro index | MES, MNQ, MYM, M2K |
| Commodities | CL (front month +1), GC, SI |
| Currencies | 6E, 6J, 6B |
| Treasuries | ZB, ZN, ZF |

### Usage

1. Select or type an instrument in the dropdown, then click **Load**
2. Set a date range (or use a quick-select button)
3. Click **Download**

---

## WeeklyUpdate.cs — Weekly Update

A zero-configuration downloader that grabs the last 14 days of replay data for a fixed set of commonly traded instruments in a single click.

### Features

- No instrument selection — automatically resolves the current active contract for each symbol at launch
- Fixed 14-day lookback from today
- Skips already-downloaded files automatically (no checkbox needed)
- Sundays included; only Saturdays skipped
- Same sequential download engine and progress bar as the full downloader
- Cancel mid-run supported

### Instruments

| Symbol | Description | Contract Roll |
|---|---|---|
| ES | E-mini S&P 500 | Quarterly (Mar/Jun/Sep/Dec) |
| MES | Micro E-mini S&P 500 | Quarterly |
| NQ | E-mini Nasdaq-100 | Quarterly |
| MNQ | Micro E-mini Nasdaq-100 | Quarterly |
| YM | E-mini Dow Jones | Quarterly |
| MYM | Micro E-mini Dow Jones | Quarterly |
| GC | Gold futures | Bi-monthly (Feb/Apr/Jun/Aug/Oct/Dec) |
| MGC | Micro Gold futures | Bi-monthly |
| CL | Crude Oil | Monthly (front month + 1) |
| MCL | Micro Crude Oil | Monthly (front month + 1) |

### Usage

Open **Tools → Weekly Update** and click **Download**. That's it.

---

## Installation

1. Copy the `.cs` file(s) into your NinjaTrader 8 AddOns folder:
   `Documents\NinjaTrader 8\bin\Custom\AddOns\`
2. In NinjaTrader, open **NinjaScript Editor** and compile (F5), or restart NinjaTrader
3. The tool(s) appear under **Tools** in the Control Center

## Requirements

- NinjaTrader 8
- An active data connection that supports historical Market Replay data (e.g., Tradovate, Rithmic/Continuum)
- The connection must be logged in before clicking Download

## Notes

- Downloaded files are stored in `%USERPROFILE%\Documents\NinjaTrader 8\db\replay\<InstrumentFullName>\`
- Files are named `YYYYMMDD.nrd`
- Both tools use NinjaTrader's internal `RequestMarketReplay` API via reflection, the same mechanism the built-in downloader uses
