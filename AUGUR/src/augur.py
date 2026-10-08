"""
AUGUR Trade Intelligence
=========================
Standalone Windows executable entry point.

Parses NT8 Executions export CSVs, reconstructs round-trip trades,
and generates HTML reports (daily, weekly, and Templum hub).

Usage:
  AUGUR.exe                                  Standard run (process all CSVs in imports/)
  AUGUR.exe --import "path\\to\\export.csv"  Copy and import a specific NT8 export
  AUGUR.exe --date 2026-05-28                Force-regenerate a specific daily report
  AUGUR.exe --weekly                         Force-regenerate this week's summary
  AUGUR.exe --backfill                       Generate ALL missing reports
  AUGUR.exe --augur-path "D:\\AUGUR"         Override the AUGUR folder location
  AUGUR.exe --dry-run                        Preview actions, write nothing
  AUGUR.exe -d / --daemon                    Daemon mode: no pause at exit
  AUGUR.exe --version                        Print version, author, website
"""

import sys
import shutil
import argparse
from datetime import date
from pathlib import Path

if getattr(sys, 'frozen', False):
    _src = Path(sys.executable).parent
else:
    _src = Path(__file__).parent

if str(_src) not in sys.path:
    sys.path.insert(0, str(_src))

from config import (APP_NAME, VERSION, AUTHOR, EMAIL, WEBSITE,
                    find_augur_dir, init_augur_dirs)
from log_parser import (
    parse_all_executions, reconstruct_trades, group_by_session,
    load_index, save_index, update_cumulative,
)
from date_utils import (
    get_report_date, is_friday, get_friday_of_week, week_dates,
    get_trading_days_with_data, get_missing_daily_dates,
    get_weeks_with_data, get_missing_weekly_dates,
)
from daily_report  import generate_daily
from weekly_report import generate_weekly
from hub           import generate_hub


def parse_args():
    p = argparse.ArgumentParser(
        prog='AUGUR',
        description=f'{APP_NAME} v{VERSION}',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""Examples:
  AUGUR.exe                                  # standard run
  AUGUR.exe --import "NinjaTrader Grid.csv"  # import a specific export
  AUGUR.exe --date 2026-05-28                # regenerate specific date
  AUGUR.exe --weekly                         # force this week's summary
  AUGUR.exe --backfill                       # fill all missing reports
  AUGUR.exe -d                               # daemon mode (no pause)
""")
    p.add_argument('--import',      dest='import_file', metavar='PATH',
                   help='Copy and import a specific NT8 Executions export CSV')
    p.add_argument('--date',        metavar='YYYY-MM-DD',
                   help='Force-generate report for this date')
    p.add_argument('--weekly',      action='store_true',
                   help="Force-generate this week's summary")
    p.add_argument('--backfill',    action='store_true',
                   help='Generate all missing daily and weekly reports')
    p.add_argument('--augur-path',  metavar='PATH',
                   help='Path to AUGUR data folder (default: Documents/AUGUR)')
    p.add_argument('--dry-run',     action='store_true',
                   help='Show what would be generated without writing files')
    p.add_argument('-d', '--daemon', action='store_true',
                   help='Daemon mode: no pause at exit (use for Task Scheduler)')
    p.add_argument('--version',     action='store_true',
                   help='Show version, author, and contact info then exit')
    return p.parse_args()


def main():
    args = parse_args()

    if args.version:
        print(f"\n  {APP_NAME}  v{VERSION}")
        print(f"  Author  :  {AUTHOR}")
        print(f"  Web     :  {WEBSITE}")
        print(f"  Email   :  {EMAIL}\n")
        sys.exit(0)

    print(f"\n{'='*60}")
    print(f"  {APP_NAME}  v{VERSION}")
    print(f"{'='*60}\n")

    # ── 1. Locate / initialise AUGUR folder ───────────────────────────────────
    augur_dir, imports_dir, reports_dir = init_augur_dirs(
        find_augur_dir(getattr(args, 'augur_path', None))
    )
    print(f"  AUGUR folder : {augur_dir}")
    print(f"  Imports      : {imports_dir}")
    print(f"  Reports      : {reports_dir}")

    # ── 2. Import a specific file if requested ────────────────────────────────
    if args.import_file:
        src = Path(args.import_file)
        if not src.exists():
            print(f"\n[ERROR] Import file not found: {src}")
            sys.exit(1)
        dest = imports_dir / src.name
        if not args.dry_run:
            shutil.copy2(src, dest)
            print(f"\n[IMPORT] Copied {src.name} → imports/")
        else:
            print(f"\n[IMPORT] (dry-run) Would copy {src.name} → imports/")

    # ── 3. Parse all execution CSVs ───────────────────────────────────────────
    print("\n[PARSE] Reading execution CSVs...")
    csv_files = list(imports_dir.glob('*.csv'))
    if not csv_files:
        print("  No CSV files found in imports/.")
        print(f"  Drop NT8 Executions exports into: {imports_dir}")
        print(f"  Or use: AUGUR.exe --import \"path\\to\\export.csv\"")
        _end_of_run(args.daemon)
        return

    print(f"  Found {len(csv_files)} CSV file(s)")
    executions = parse_all_executions(imports_dir)
    print(f"  Parsed {len(executions)} unique executions")

    # ── 4. Reconstruct trades ─────────────────────────────────────────────────
    print("\n[TRADES] Reconstructing round-trip trades...")
    all_trades = reconstruct_trades(executions)
    accounts   = sorted({t['account'] for t in all_trades})
    print(f"  Total: {len(all_trades)} trades  |  Accounts: {len(accounts)}")
    for acct in accounts:
        n = sum(1 for t in all_trades if t['account'] == acct)
        live_flag = '' if acct.lower().startswith('sim') else ' [LIVE]'
        print(f"    {acct}{live_flag}: {n} trades")

    if not all_trades:
        print("\n  No complete trades found.")
        print("  (Trades still open, or no Entry/Exit pairs in the data.)")
        _end_of_run(args.daemon)
        return

    # ── 5. Load index ─────────────────────────────────────────────────────────
    index = load_index(reports_dir)
    update_cumulative(index, all_trades)

    # ── 6. Determine target date ──────────────────────────────────────────────
    if args.date:
        try:
            target_date = date.fromisoformat(args.date)
        except ValueError:
            print(f"[ERROR] Invalid date format '{args.date}'. Use YYYY-MM-DD.")
            sys.exit(1)
    else:
        target_date = get_report_date()

    current_friday = get_friday_of_week(target_date)
    print(f"\n[DATE]  Report date : {target_date}  |  Week ending : {current_friday}")

    # ── 7. Decide which reports to generate ───────────────────────────────────
    trading_days = get_trading_days_with_data(all_trades)
    all_weeks    = get_weeks_with_data(trading_days)

    daily_to_generate  = get_missing_daily_dates(reports_dir, trading_days)
    weekly_to_generate = get_missing_weekly_dates(reports_dir, all_weeks)

    if daily_to_generate or weekly_to_generate:
        print(f"\n[FILL]  Missing daily  : {len(daily_to_generate)}")
        print(f"[FILL]  Missing weekly : {len(weekly_to_generate)}")

    if args.backfill:
        daily_to_generate  = list(trading_days)
        weekly_to_generate = list(all_weeks)
        print(f"\n[BACKFILL] Regenerating all {len(daily_to_generate)} daily"
              f" and {len(weekly_to_generate)} weekly reports")

    if target_date not in daily_to_generate:
        daily_to_generate.append(target_date)

    if args.weekly and current_friday not in weekly_to_generate:
        weekly_to_generate.append(current_friday)
    elif is_friday(target_date) and current_friday not in weekly_to_generate:
        weekly_to_generate.append(current_friday)

    daily_to_generate  = sorted(set(daily_to_generate))
    weekly_to_generate = sorted(set(weekly_to_generate))

    # ── 8. Generate daily reports ─────────────────────────────────────────────
    if daily_to_generate:
        print(f"\n[DAILY] Generating {len(daily_to_generate)} daily report(s)...")
        for d in daily_to_generate:
            if args.dry_run:
                has_data = any(t['session'] == d for t in all_trades)
                print(f"  (dry-run) daily_{d.strftime('%Y%m%d')}.html"
                      f"  – {'has data' if has_data else 'no trades'}")
            else:
                generate_daily(d, all_trades, index, reports_dir)

    # ── 9. Generate weekly reports ────────────────────────────────────────────
    if weekly_to_generate:
        print(f"\n[WEEKLY] Generating {len(weekly_to_generate)} weekly report(s)...")
        for friday in weekly_to_generate:
            if args.dry_run:
                print(f"  (dry-run) weekly_{friday.strftime('%Y%m%d')}.html")
            else:
                generate_weekly(friday, all_trades, index, reports_dir)

    # ── 10. Regenerate Templum hub ────────────────────────────────────────────
    print("\n[HUB] Updating Templum...")
    if not args.dry_run:
        generate_hub(all_trades, index, augur_dir, reports_dir)
        save_index(index, reports_dir)

    # ── Done ──────────────────────────────────────────────────────────────────
    print(f"\n{'='*60}")
    if args.dry_run:
        print("  Dry run complete – no files written.")
    else:
        hub_path = augur_dir / 'Templum.html'
        print(f"  Done. Open your hub:")
        print(f"  {hub_path}")
    print(f"{'='*60}\n")

    _end_of_run(args.daemon)


def _end_of_run(daemon: bool):
    if daemon:
        return

    import threading
    WAIT = 60
    print(f"\nClosing in {WAIT}s – press Enter to exit now...")

    entered = threading.Event()

    def _wait_for_enter():
        try:
            sys.stdin.readline()
        except Exception:
            pass
        entered.set()

    t = threading.Thread(target=_wait_for_enter, daemon=True)
    t.start()

    for remaining in range(WAIT, 0, -1):
        if entered.wait(timeout=1):
            break
        print(f"\r  {remaining:2d}s ", end='', flush=True)

    print()


if __name__ == '__main__':
    main()
