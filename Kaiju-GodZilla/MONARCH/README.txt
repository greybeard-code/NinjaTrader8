MONARCH Intelligence Report System
====================================
Standalone Windows trade report generator for NinjaTrader 8 / GodZilla strategy.
Just run MONARCH.exe — no Python, no installation, nothing else required.


WHAT YOU NEED
-------------
- Windows with NinjaTrader 8
- The GodZillaKilla strategy (or GodZuki) writing GodZilla_*.csv trade logs
- MONARCH.exe  (included in this zip)


QUICK START
-----------
1. Unzip MONARCH.exe (and this README) anywhere you like.
2. Double-click MONARCH.exe  (or run it from a terminal / Task Scheduler).
3. MONARCH finds your NinjaTrader 8 folder, imports your GodZilla trade logs,
   and builds the reports. When it finishes, open the hub page in your browser:
      Documents\NinjaTrader 8\MONARCH\CastleBravo.html

Run it again any day to refresh the reports, or schedule it (see SCHEDULING).
If MONARCH cannot find NinjaTrader 8 automatically, point it at the folder with
--nt8-path (see USAGE).


FOLDER STRUCTURE CREATED AUTOMATICALLY
---------------------------------------
After first run, MONARCH creates this inside your NinjaTrader 8 folder:

  NinjaTrader 8\
    MONARCH\
      CastleBravo.html       <- Hub / index page (open this in your browser)
      logs\                  <- GodZilla CSV files are copied here
      reports\
        daily_YYYYMMDD.html  <- One per trading day
        weekly_YYYYMMDD.html <- One per Friday (Mon-Fri summary)
        index.json           <- Internal metadata


USAGE
-----
MONARCH.exe                         Normal daily run
MONARCH.exe --backfill              Generate ALL missing reports
MONARCH.exe --date 2026-05-22       Regenerate a specific date
MONARCH.exe --weekly                Force-regenerate this week's summary
MONARCH.exe --nt8-path "D:\NT8"     Override NT8 folder location
MONARCH.exe --dry-run               Preview what would be generated
MONARCH.exe --session-hour 17       Session-start hour in your chart timezone
MONARCH.exe --version               Show version


SCHEDULING (Windows Task Scheduler)
-------------------------------------
Recommended schedule:  Mon-Fri at 5:00 PM
Action:    Start a program
Program:   full path to MONARCH.exe  (wherever you unzipped it — e.g.
           C:\Users\<you>\Documents\NinjaTrader 8\MONARCH\MONARCH.exe)
Arguments: -d      <- daemon mode: skips the 60-second pause when scheduled

Friday 5pm will automatically generate both the daily AND weekly summary.
Saturday/Sunday runs map to Friday — safe to schedule daily without gaps.


TRADING DAY RULES
------------------
- Session boundary: 6:00 PM ET = start of new session
- Session date = END of the session (next calendar day for evening trades)
  Example:  Trade at 10:22 PM on Tuesday  ->  Wednesday's report
            Trade at  9:45 AM on Wednesday ->  Wednesday's report
- Saturday run = Friday's report
- Sunday run   = Friday's report

Timezone note: the 6 PM boundary assumes your NT8 charts render in ET (that is
the timezone your CSV timestamps are written in). If your charts use another
timezone, set the session-start hour to match — e.g. 17 for CT, 15 for PT:
  - For every run:   add  "session_boundary_hour": 17  to
                     NinjaTrader 8\MONARCH\config.json
  - For one run:     MONARCH.exe --session-hour 17


GODZILLA LOG FORMAT
-------------------
MONARCH reads GodZilla_*.csv trade logs by column header, so it supports both:
  - GodZillaKilla 1.9.2+  (11 columns: ... RealizedPnL, TradeResult)
  - GodZillaKilla <=1.9.1 (14 columns, adds SignalCombo/UsedSignals/LastTradeLine)
Old and new logs can be mixed freely. Signal combo and KO analysis fall back to
the Trigger column when the older columns are absent (they duplicated Trigger).
GodZuki_*.csv files are ignored (indicator state, not trade logs).


BUILDING YOUR OWN  (OPTIONAL — you already have MONARCH.exe)
------------------------------------------------------------
Most people never need this. The MONARCH.exe in this zip is ready to run and
needs no Python. Build from source only if you want to review or modify the
code, or compile the exe yourself.

Get the source from GitHub:
  https://github.com/greybeard-code/Kaiju-GodZilla
  (the MONARCH project is in the MONARCH\ folder)

Requirements: Python 3.10 with Nuitka to compile (any Python runs from source).

1. ACTIVATE THE BUILD ENVIRONMENT FIRST.
   build.ps1 uses whatever `python` / `pip` is active in your terminal, so make
   the Python 3.10 install that owns Nuitka the active one before building.
   This project uses pyenv-win:
     pyenv shell 3.10.5        <- activate 3.10.5 for this terminal
   The default `python` may be a newer version that does NOT own Nuitka. If
   Nuitka is missing from the active environment, build.ps1 installs it there.

2. Run directly from source (no build needed, any Python):
     python src\monarch.py

3. Compile the standalone exe:
     .\build.ps1               (or build.bat)  ->  dist\MONARCH.exe + deploy

build.ps1 locates the Nuitka-owning Python via `pip show nuitka`, reads VERSION
from config.py, compiles with Nuitka, and copies the exe into
NinjaTrader 8\MONARCH\. The exe needs no Python on the target machine.

Source layout (src\):
  monarch.py       Entry point / orchestrator (CLI args, run loop)
  config.py        NT8 path detection, config.json, account label/grade maps
  date_utils.py    Trading day math, week helpers, missing-report detection
  log_sync.py      Find & copy GodZilla CSVs to MONARCH\logs\
  log_parser.py    Parse CSVs into trade dicts, compute stats
  templates.py     Shared CSS theme and HTML component helpers
  daily_report.py  Build one day's HTML report
  weekly_report.py Build Mon-Fri weekly summary HTML
  hub.py           Build CastleBravo.html hub page


ACCOUNTS
---------
All accounts found in the logs are included and reported equally — live, Sim,
and any others. Sim accounts are intentionally scannable alongside live ones.

To give an account a short label and grade, edit config.json in your
NinjaTrader 8\MONARCH\ folder (created automatically on first run):

  {
    "session_boundary_hour": 18,
    "accounts": {
      "YOUR_ACCOUNT_ID": { "label": "084", "grade": "G4" }
    }
  }

This file stays in your NinjaTrader 8 folder (not the source repo), so your
account numbers are never shared. Accounts with no entry fall back to the last
6 characters of the account name as the label, and '?' for grade.


VERSION HISTORY
---------------
1.0.4  Support GodZillaKilla 1.9.2 logs (11-column format). Parser reads by
       header and falls back to the Trigger column for KO/combo analysis, so
       old (14-col) and new (11-col) logs both work. TradeResult falls back to
       the sign of PnL when blank. Configurable session-start hour for non-ET
       chart timezones (config.json session_boundary_hour / --session-hour).
       Resilience: a bad CSV row no longer drops the rest of a file, and a
       failed report no longer aborts the whole run. Account labels/grades moved
       to config.json (auto-created) so account numbers stay out of the repo.
       Removed dead is_live field.
1.0.3  Castle Bravo 4-week calendar anchors to the current week's Friday
       (current week no longer disappears Mon-Thu).
1.0.2  Symbol Breakdown section (stats grouped by base ticker: MNQ, NQ, ES...).
1.0.1  Any GodZilla_* file supported; account derived from filename; all
       accounts (prop firm + Sim) included; per-account KPI breakdown; daemon
       (-d); 60s countdown pause on manual runs; --version info.
1.0.0  Initial release — modular rewrite of generate_report.py
       New: standalone exe, auto log sync, backfill, Sat/Sun guardrail,
            CastleBravo hub, MONARCH branding
