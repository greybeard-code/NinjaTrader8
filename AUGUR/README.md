# AUGUR Trade Intelligence

HTML trade reporting for NinjaTrader 8, powered by the NT8 Executions export.

Drop a CSV export from the NT8 Executions tab, run one command, open a browser.
AUGUR reconstructs your round-trip trades, calculates P&L, and generates a full
set of daily reports, weekly summaries, and a hub dashboard (Templum).

---

## What You Get

**Templum.html** — The hub. All-time cumulative KPIs, a 4-week clickable
calendar, and a recent sessions table. Everything links to the reports below.

**Daily reports** — One per trading session. Net P&L, gross P&L, commission
drag, win rate, profit factor, R:R. Trade log table with every round-trip trade:
entry time, direction, quantity, entry price, duration, exit type (Target /
Stop / Mixed), commission, result, and net P&L. Account and symbol breakdowns.
Direction and exit type bar charts. Cumulative all-time stats for context.

**Weekly reports** — Mon–Fri aggregated KPIs, per-day session table, biggest
wins and losses, direction and exit type breakdowns, account and symbol cards.

---

## Requirements

- Windows (NinjaTrader 8 runs on Windows)
- NinjaTrader 8 with at least one executed trade
- Python 3.8+ (only needed to run from source or to build the exe)

The compiled `AUGUR.exe` requires no Python on the target machine.

---

## Install

1. Download `AUGUR.exe` (or build it — see **Build Notes** at the bottom)
2. Create the folder `Documents\AUGUR` if it does not already exist
3. Copy `AUGUR.exe` into `Documents\AUGUR\`
4. Double-click `AUGUR.exe` once to let it create the `imports\` and `reports\`
   subfolders, then close the window

After that first run your folder will look like this:

```
Documents\AUGUR\
  AUGUR.exe
  imports\        ← drop your NT8 export CSVs here
  reports\
```

---

## Quick Start

### Step 1 – Export from NT8

1. Open a chart or the Control Center in NinjaTrader 8
2. Go to the **Executions** tab (bottom panel, or `Account → Executions`)
3. Right-click anywhere in the grid → **Export**
4. Save the CSV to `Documents\AUGUR\imports`

### Step 2 – Run AUGUR

Double-click `AUGUR.exe`, or from a command prompt:

```
AUGUR.exe
```

AUGUR automatically reads every CSV in `imports\`. No flags needed once the
file is in the right place.

If you saved the CSV somewhere else, use `--import` to copy it in:

```
AUGUR.exe --import "C:\Users\You\Desktop\NinjaTrader Grid 2026-05-28.csv"
```

### Step 3 – Open your hub

AUGUR prints the path at the end of each run:
```
  Done. Open your hub:
  C:\Users\You\Documents\AUGUR\Templum.html
```

Open `Templum.html` in any browser. Click any calendar cell or session row
to drill into a daily report.

---

## Data Folder Layout

AUGUR creates and manages this folder automatically:

```
Documents\AUGUR\
  Templum.html          Hub / index page
  imports\              All NT8 export CSVs live here
  reports\
    daily_YYYYMMDD.html
    weekly_YYYYMMDD.html
    index.json          Cumulative stats and report registry
```

Once a CSV is imported into `imports\`, it stays there. Every subsequent run
re-reads all CSVs together and deduplicates by execution ID, so importing
multiple exports that overlap in time is safe — no double-counting.

---

## All Commands

```
AUGUR.exe                                  Process all CSVs in imports\, update all reports
AUGUR.exe --import "path\to\export.csv"   Copy a CSV into imports\ and run
AUGUR.exe --date 2026-05-28               Force-regenerate a specific daily report
AUGUR.exe --weekly                         Force-regenerate this week's summary
AUGUR.exe --backfill                       Regenerate every daily and weekly report
AUGUR.exe --augur-path "D:\MyData\AUGUR"  Use a custom folder instead of Documents\AUGUR
AUGUR.exe --dry-run                        Preview what would be generated; write nothing
AUGUR.exe -d / --daemon                   Exit immediately after finishing (Task Scheduler)
AUGUR.exe --version                        Print version, author, and contact info
```

`--dry-run` is safe to run at any time. It parses all data and reports what
it would generate without touching any files.

---

## Workflow Tips

**Daily use:** Export from NT8 at end of session, run `AUGUR.exe --import ...`,
open Templum. The report for today auto-generates; historical reports fill in
automatically if any are missing.

**Multiple exports:** You can import as many CSVs as you like. AUGUR merges them
all and deduplicates. If you export every day you build up a complete history.

**Friday:** A weekly summary generates automatically when the target date is a
Friday, or use `--weekly` to force one at any time.

**Task Scheduler:** Schedule a daily run with `-d` (daemon mode) so the window
doesn't linger. The exe can be found at `Documents\AUGUR\AUGUR.exe` after build.

```
Action    : Start a program
Program   : C:\Users\<user>\Documents\AUGUR\AUGUR.exe
Arguments : -d
```

Saturday and Sunday runs are safe — they map to the preceding Friday without
creating blank weekend reports.

**Custom data folder:** If your Documents folder is on OneDrive or a network
drive and you want AUGUR data elsewhere, use `--augur-path` or set the
`AUGUR_PATH` environment variable.

---

## Accounts and Sim Data

AUGUR reads the **Account display name** column from the NT8 export.

- Accounts whose name starts with `Sim` are treated as simulation accounts
- All other accounts (LFE*, APEX*, brokerage IDs) are treated as live

All accounts appear in reports and statistics. Sim accounts are visually
distinguished in account breakdown cards.

---

## Instruments and P&L Calculation

NT8 Executions exports do not include P&L — AUGUR calculates it from prices.

The calculation per trade:
```
entry_avg  = weighted average price of all Entry fills
per exit:  pnl = (exit_price − entry_avg) × qty × point_value × direction
net_pnl    = sum(exit pnls) − total commissions
```

Point values are configured in `src/config.py`. Current table:

| Symbol | $/point | Notes |
|--------|---------|-------|
| MNQ    | $2.00   | Micro E-mini NASDAQ-100 |
| NQ     | $20.00  | E-mini NASDAQ-100 |
| ES     | $50.00  | E-mini S&P 500 |
| MES    | $5.00   | Micro E-mini S&P 500 |
| YM     | $5.00   | E-mini Dow Jones |
| MYM    | $0.50   | Micro E-mini Dow Jones |
| RTY    | $50.00  | E-mini Russell 2000 |
| M2K    | $5.00   | Micro E-mini Russell 2000 |
| GC     | $100.00 | Gold |
| MGC    | $10.00  | Micro Gold |
| CL     | $1000.00| Crude Oil (WTI) |

If AUGUR encounters an unknown instrument it prints a warning and uses $1/point.
Add new instruments to `POINT_VALUES` in `src/config.py`.

---

## Trading Day Boundary

6:00 PM ET is the start of a new session. Trades opened at or after 6 PM belong
to the next calendar day's session.

```
Trade at 10:30 PM Tuesday  →  Wednesday's report
Trade at  9:45 AM Wednesday →  Wednesday's report
```

---

## Recommendations (Hub Callouts)

The hub supports manual recommendation cards — colored callouts for observations
about your trading. Edit `reports\index.json` and add entries to the
`"recommendations"` array:

```json
{
  "type":  "positive",
  "title": "Short setups outperforming this week",
  "body":  "Shorts: 8W/2L (+$620). Longs: 4W/6L (−$180). Consider bias toward short entries."
}
```

Types: `critical` (red), `warn` (yellow), `positive` (green), `info` (blue).

---

## Author

**GreyBeard** — [greybeardconsulting.net](https://greybeardconsulting.net) — greybeard@greybeardconsulting.net

---

---

## Build Notes

This section is for building `AUGUR.exe` from source.

### Prerequisites

- Python 3.10.x with Nuitka installed (the same setup used by MONARCH)
- Or any Python 3.8+ if you only want to run from source

```powershell
# Install Nuitka under the correct interpreter
& "C:\Users\<user>\.pyenv\pyenv-win\versions\3.10.5\python.exe" -m pip install nuitka
```

### Build

```powershell
cd C:\Dev\AUGUR

# One-time: generate the exe icon
python make_icon.py

# Compile and deploy
.\build.ps1
```

`build.ps1` will:
1. Locate the Python interpreter that owns Nuitka via `pip show nuitka`
2. Read `VERSION` from `src/config.py` for exe metadata
3. Clean the previous `dist/` folder
4. Compile with Nuitka `--onefile --windows-console-mode=force`
5. Copy `dist\AUGUR.exe` to `Documents\AUGUR\AUGUR.exe`
6. Prompt to run the exe immediately

First build downloads MinGW-w64 (~120 MB, one-time) if Visual Studio Build
Tools are absent. Build time: 2–5 min first compile, 30–90 sec on rebuilds.

`dist/` is in `.gitignore`. Never commit it.

### Icon

`make_icon.py` generates `augur.ico` (auto-installs Pillow if missing).
The generated `augur.ico` is in `.gitignore` since it's reproducible from
source. `build.ps1` silently skips the icon if the file is absent.

### Running from Source

```powershell
cd C:\Dev\AUGUR
python src\augur.py --import "path\to\export.csv"
python src\augur.py --dry-run
python src\augur.py --backfill
```

Any Python 3.8+ works for running from source. The 3.10.x restriction applies
only to Nuitka compilation.

### Bumping the Version

Change `VERSION` in `src/config.py` only. The build script reads it from there
and passes it to Nuitka as exe metadata. Do not change it anywhere else.
