# GodZilla Suite — NinjaTrader 8

**Namespace:** `NinjaTrader.NinjaScript.Indicators.GreyBeard`

A NinjaTrader 8 trading system built around six specialized signal indicators unified under a common namespace and signal contract. The suite has three layers: a set of purpose-built sub-indicators that generate numeric `Signal_Trade` outputs, a pure signal indicator (GodZuki) for visual monitoring, and a fully automated ATM trading strategy (GodZillaKilla) that acts on those signals.

---

## Components

### GodZillaKilla — ATM Trading Strategy
*Current version: 1.9.4*

Automated NinjaTrader 8 strategy that reads signals from all six GodZilla Suite sub-indicators and executes ATM or Fixed-Ticks trades based on configurable confluence rules. Includes session filters, EMA filter, news filter, daily PnL limits, martingale recovery, a full SharpDX dashboard, and a floating "noble" dark navy control panel with drag, scale, and minimize support.

→ [GodZillaKilla.md](documents/GodZillaKilla.md)

---

### MONARCH — Intelligence Report System
*Current version: 1.0.2*

Standalone Windows executable that turns GodZilla trade logs into browser-based HTML reports — no Python required. MONARCH reads `GodZilla_*.csv` files directly from your NinjaTrader 8 folder, generates a daily performance report for every session, a weekly summary every Friday, and a **Castle Bravo** hub page with a 4-week calendar and all-time cumulative stats. Supports multiple accounts (live, prop firm, and Sim) side by side. Runs automatically via Windows Task Scheduler.

→ [MONARCH/MONARCH.md](MONARCH/MONARCH.md)

---

### GodZuki — Signal Indicator
*Current version: 1.4.1*

Pure signal indicator version of GodZillaKilla. No trading — add GodZuki to any chart to visualize the same confluence signals, trigger audio alerts, log signal history to CSV, and expose all signal values in the NT8 Data Box. Signal Set 1 and Set 2 draw independently on the same bar. Per-indicator **Require** flags (Set 1 and Set 2) enforce that a named indicator must be among the signals that actually fired before a group trigger is counted. Useful for monitoring, backtesting signal quality, and driving custom strategies via public `Series<double>` outputs.

→ [GodZuki.md](documents/GodZuki.md)

---

### GodZilla Indicators — Signal Engine
*Six sub-indicators powering both GodZillaKilla and GodZuki — current version: 1.1.1*

| Indicator | Short Name | What it detects |
|---|---|---|
| gbKingOrderBlock | KO | Institutional order blocks via BOS/CHoCH structure breaks |
| gbPANAKanal | PA | Adaptive Keltner channel trend, breaks, and pullbacks |
| gbThunderZilla | TH | Dual-system trend + multi-oscillator pullback and slowdown |
| gbSuperJumpBoost | SJ | ATR-derived multi-level supply/demand zones |
| gbSumoPullback | SU | Multi-MA cloud pullback pattern detector |
| gbNobleCloud | NC | Kernel-envelope cloud with re-entry trade signals |

All six expose a `Signal_Trade` series using a consistent **−1 / 0 / +1** (or extended integer) numeric contract that GodZillaKilla and GodZuki read directly.

→ [Indicators.md](documents/Indicators.md)

---

## Quick Start

1. Import the `GodZilla_Family.zip` file into NinjaTrader 8 — this compiles all six sub-indicators, GodZuki, and GodZillaKilla together.
2. Add **GodZuki** to a chart to verify signal output before enabling live trading.
3. Add **GodZillaKilla** to a separate chart and configure your ATM template, signal set, and session times.

**Upgrading from a manual install?** Run the cleanup script first to remove any misplaced files before importing the new package.

→ [Remove-GodZilla-README.md](Remove-GodZilla-README.md)

---

## File Index

### NinjaScript (NT8)

| File | Purpose |
|---|---|
| `GodZillaKilla.cs` | ATM trading strategy (v1.9.4) |
| `GodZuki.cs` | Signal visualization indicator (v1.4.1) |
| `gbKingOrderBlock.cs` | KO sub-indicator |
| `gbPANAKanal.cs` | PA sub-indicator |
| `gbThunderZilla.cs` | TH sub-indicator |
| `gbSuperJumpBoost.cs` | SJ sub-indicator |
| `gbSumoPullback.cs` | SU sub-indicator |
| `gbNobleCloud.cs` | NC sub-indicator |
| `gbBarStatus.cs` | Bar status utility indicator |
| `NewsSignals.cs` | Economic calendar signal source |

### MONARCH (Python / Windows exe)

| File | Purpose |
|---|---|
| `MONARCH/src/monarch.py` | Entry point — CLI, orchestrator |
| `MONARCH/src/config.py` | NT8 path detection, account helpers, version constants |
| `MONARCH/src/log_sync.py` | Move `GodZilla_*.csv` files from NT8 tree to `logs/` |
| `MONARCH/src/log_parser.py` | Parse CSVs, compute statistics, manage index |
| `MONARCH/src/date_utils.py` | Trading-day math, week helpers, missing-report detection |
| `MONARCH/src/daily_report.py` | Generate one day's HTML report |
| `MONARCH/src/weekly_report.py` | Generate Mon–Fri weekly summary HTML |
| `MONARCH/src/hub.py` | Generate Castle Bravo hub page |
| `MONARCH/src/templates.py` | Shared dark-theme CSS and HTML component helpers |
| `MONARCH/build.ps1` | PowerShell build script — compiles exe and deploys |
| `MONARCH/make_icon.py` | One-time icon generator (writes `monarch.ico`) |
| `MONARCH/make_version_file.py` | Generates `version_info.txt` for exe Properties metadata |
