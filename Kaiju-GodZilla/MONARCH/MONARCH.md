# MONARCH Intelligence Report System

**Automated HTML trade reports for NinjaTrader 8 / GodZilla strategy suite.**

MONARCH reads your GodZilla trade logs, generates daily and weekly performance
reports, and presents them through a browser-based command centre called
**Castle Bravo**. No Python installation required — MONARCH.exe is a single
self-contained file.

---

## Installation

1. Copy `MONARCH.exe` to any folder you prefer — for example:

   ```
   C:\Users\<you>\Documents\NinjaTrader 8\MONARCH\MONARCH.exe
   ```

2. Double-click `MONARCH.exe` to run it for the first time.

That's it. MONARCH automatically finds your NinjaTrader 8 folder, moves your
GodZilla log files into its own `logs\` folder, and generates your reports.

---

## What Gets Created

After the first run, MONARCH creates the following inside your NinjaTrader 8
Documents folder:

```
NinjaTrader 8\
  MONARCH\
    CastleBravo.html        ← Open this in your browser — it's your hub
    logs\                   ← GodZilla CSVs are moved here automatically
    reports\
      daily_YYYYMMDD.html   ← One report per trading day
      weekly_YYYYMMDD.html  ← One summary per week (Mon–Fri)
      index.json            ← Internal metadata (do not edit)
```

Open `CastleBravo.html` in any browser. All daily and weekly reports link
back to it.

---

## Running MONARCH

### Double-click (normal use)

Double-click `MONARCH.exe`. The console window shows progress and stays open
for 60 seconds so you can read the output. Press **Enter** at any time to
close it early.

### Command line

Open a terminal, navigate to the MONARCH folder, and run:

| Command | What it does |
|---|---|
| `MONARCH.exe` | Standard run — auto-fills any missing reports, updates today's daily and the hub |
| `MONARCH.exe --backfill` | Force-regenerate **all** reports (including ones that already exist) |
| `MONARCH.exe --date 2026-05-22` | Regenerate the report for a specific date |
| `MONARCH.exe --weekly` | Force-regenerate this week's summary |
| `MONARCH.exe --nt8-path "D:\NT8"` | Override the NinjaTrader 8 folder location |
| `MONARCH.exe --dry-run` | Preview what would be generated — writes nothing |
| `MONARCH.exe -d` | Daemon mode — no pause at exit (use with Task Scheduler) |
| `MONARCH.exe --version` | Show version, author, and contact info |

---

## Automatic Scheduling (Recommended)

Use Windows Task Scheduler to run MONARCH automatically every weekday afternoon
after the trading session ends.

**Recommended schedule:** Mon–Fri at 5:00 PM

**Setup:**

1. Open **Task Scheduler** (search for it in the Start menu)
2. Click **Create Basic Task**
3. Name it `MONARCH Daily Report`
4. Set the trigger: **Daily**, repeat Mon–Fri at 5:00 PM
5. Set the action:
   - **Program:** full path to `MONARCH.exe`
   - **Arguments:** `-d`

The `-d` flag (daemon mode) prevents the console window from lingering after
the scheduled run completes.

**Friday at 5:00 PM** automatically generates both the daily report **and**
the weekly summary — no extra setup needed.

**Saturday and Sunday** are safe to include in the schedule. Both map to
the preceding Friday — no blank weekend reports are ever created.

---

## Understanding Your Reports

### Castle Bravo (CastleBravo.html)

Your command centre. Open this page in a browser and bookmark it.

- **Cumulative Performance** — all-time KPIs across all accounts
- **Account Totals** — per-account P&L breakdown
- **Current Recommendations** — manually curated trading guidance
- **4-Week Calendar** — click any day to open its daily report; click a week
  total to open the weekly summary
- **Recent Sessions** — last 10 trading days at a glance

### Daily Report (daily\_YYYYMMDD.html)

One page per trading session.

- **KPI Grid** — Net P&L, Win Rate, Profit Factor, Avg Win, Avg Loss, R:R,
  Longs, Shorts
- **Account Breakdown** — per-account stats cards
- **Trade Log** — every trade with time, direction, duration, grade, KO signal,
  signal combo, result, and P&L. ⚡ marks trades closed in ≤10 seconds (ATM
  take-profit hit immediately)
- **Breakdowns** — Direction, Signal Grade, and KO Signal bar charts
- **Signal Combo Analysis** — top 10 signal combinations by frequency
- **Cumulative Performance** — all-time context at the bottom of every page

### Weekly Report (weekly\_YYYYMMDD.html)

One page per Mon–Fri week, generated automatically on Fridays.

- **KPI Grid** — same metrics, aggregated across the week
- **Session Breakdown** — per-day P&L table with links to daily reports
- **Account Breakdown** — per-account week totals
- **Biggest Wins / Biggest Losses** — top 3 each
- **Grade and Direction Breakdowns** — bar charts across the full week

---

## Trading Day Rules

MONARCH uses the same session boundary as NinjaTrader 8:

> **6:00 PM ET = start of a new trading session.**
> The session date is the calendar day on which the session **ends**.

| Trade time | Session date |
|---|---|
| Tuesday 10:22 PM | Wednesday |
| Wednesday 9:45 AM | Wednesday |
| Friday 4:59 PM | Friday |
| Friday 6:01 PM | Monday (next week) |

Weekend guardrail:
- Saturday run → Friday's report
- Sunday run → Friday's report

---

## Importing Multiple Weeks of Data

Drop any number of `GodZilla_*.csv` files into your NinjaTrader 8 folder before
running MONARCH. On the next run MONARCH will:

1. Move all of them into `MONARCH\logs\`
2. Generate a daily report for **every trading day** that has data
3. Generate a weekly summary for **every Friday** represented in that data
4. Update Castle Bravo with cumulative stats for the whole period

No flags needed — gap-filling is the default behaviour on every run.

`--backfill` is only needed if you want to **force-regenerate** reports that
already exist (for example, after fixing a bug or rolling the contract ticker).

---

## Accounts and Log Files

MONARCH automatically picks up **any** GodZilla log file (`GodZilla_*.csv`)
found anywhere in your NinjaTrader 8 folder and moves it into `MONARCH\logs\`.
The account name is read from the CSV data, with the filename used as a
fallback. Both live prop firm accounts and Sim accounts are included in reports.

Log files are **moved** out of NinjaTrader 8 on each run — they are not
duplicated. If a file with the same name already exists in `logs\`, it is
overwritten.

---

## Troubleshooting

**MONARCH can't find my NinjaTrader 8 folder**

Run from the command line with the path specified explicitly:

```
MONARCH.exe --nt8-path "C:\Users\<you>\Documents\NinjaTrader 8"
```

If your Documents folder is on OneDrive, MONARCH tries to detect this
automatically. If it still fails, use `--nt8-path` or set the `NT8_PATH`
environment variable.

**No trades are showing up**

Check that GodZilla CSVs exist in your NT8 folder. MONARCH looks for files
named `GodZilla_*.csv`. Files named `GodZuki_*.csv` are ignored (those are
strategy state files, not trade logs).

**Reports are missing for old dates**

MONARCH automatically generates missing reports on every run — just place the
relevant `GodZilla_*.csv` files in your NT8 folder and run MONARCH normally.

Use `--backfill` only if you want to **force-regenerate** reports that already
exist (e.g., after a contract ticker update):

```
MONARCH.exe --backfill
```

---

## Version and Support

| | |
|---|---|
| **Version** | 1.0.2 |
| **Author** | GreyBeard Consulting |
| **Website** | https://greybeardconsulting.net |
| **Email** | greybeard@greybeardconsulting.net |

Run `MONARCH.exe --version` to confirm the installed version.

---

## Version History

| Version | Changes |
|---|---|
| **1.0.2** | Symbol Breakdown section added to daily, weekly, and Castle Bravo reports — aggregates performance by base ticker (MNQ, NQ, ES, etc.) independent of contract date. Instrument label in headers and footers derived from log data; no quarterly maintenance required on contract rolls. |
| **1.0.1** | Dynamic account support — any `GodZilla_*.csv` file picked up automatically; account name derived from filename. Sim accounts included alongside live accounts (useful for copy-trade leaders). Per-account KPI breakdown on daily, weekly, and hub reports. Automatic gap-filling on every run — imports weeks of data in one shot. `--backfill` now means force-regenerate all existing reports. Log files moved (not copied) from NT8 tree; duplicates overwritten safely. Daemon mode (`-d`) for Task Scheduler. 60-second auto-close countdown on manual runs (press Enter to dismiss early). `--version` flag shows version, author, website, and email. Exe Properties metadata embedded via `--version-file`. Instrument label derived from log data — supports any futures symbol (MNQ, NQ, ES, MES, MYM, etc.) with no maintenance required on contract rolls. |
| **1.0.0** | Initial release. Modular Python rewrite compiled to a standalone exe. Automatic log sync, backfill, Sat/Sun weekend guardrail, Castle Bravo hub page, MONARCH branding, icon support. |

---

*Copyright &copy; 2026 GreyBeard Consulting. All rights reserved.*
