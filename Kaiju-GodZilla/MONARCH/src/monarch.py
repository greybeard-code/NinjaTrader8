"""
MONARCH Intelligence Report System
====================================
Standalone Windows executable entry point.

Compiled with Nuitka (see build.ps1) into a single MONARCH.exe
that requires no Python installation on the target machine.

Usage:
  MONARCH.exe                        Run: sync logs, fill gaps, update hub
  MONARCH.exe --date 2026-05-22      Force-regenerate a specific daily date
  MONARCH.exe --weekly               Force-regenerate this week's summary
  MONARCH.exe --backfill             Generate ALL missing reports (no limit)
  MONARCH.exe --nt8-path "D:\\NT8"   Override NT8 folder location
  MONARCH.exe --dry-run              Show what would be generated, don't write

Weekend guardrail:
  Saturday and Sunday both map to the preceding Friday.
  No blank weekend reports are ever created.
"""

import sys
import argparse
from datetime import date, timedelta
from pathlib import Path

# ── ensure src/ is on the path when running from source ──────────────────────
if getattr(sys, 'frozen', False):
    # Running as compiled exe (Nuitka sets sys.frozen = True)
    _src = Path(sys.executable).parent
else:
    _src = Path(__file__).parent

if str(_src) not in sys.path:
    sys.path.insert(0, str(_src))

from config import (
    APP_NAME, VERSION, AUTHOR, EMAIL, WEBSITE,
    find_nt8_folder, init_monarch_dirs,
    load_local_config, ensure_local_config_template,
    apply_account_config, get_session_boundary_hour,
)
from log_sync import sync_logs
from log_parser import (
    parse_all_trades, group_by_session,
    load_index, save_index, update_cumulative,
)
from date_utils import (
    get_report_date, is_friday, get_friday_of_week, week_dates,
    get_trading_days_with_data, get_missing_daily_dates,
    get_weeks_with_data, get_missing_weekly_dates,
)
from daily_report import generate_daily
from weekly_report import generate_weekly
from hub import generate_hub


# ── CLI ───────────────────────────────────────────────────────────────────────

def parse_args():
    p = argparse.ArgumentParser(
        prog='MONARCH',
        description=f'{APP_NAME} v{VERSION}',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""Examples:
  MONARCH.exe                      # standard daily run
  MONARCH.exe --weekly             # force this week's summary
  MONARCH.exe --date 2026-05-22    # regenerate a specific date
  MONARCH.exe --backfill           # fill all missing reports
  MONARCH.exe --nt8-path "D:\\NT8" # custom NT8 location
  MONARCH.exe -d                   # daemon mode (no pause at exit)
""")
    p.add_argument('--date',     metavar='YYYY-MM-DD',
                   help='Force-generate report for this date (daily + weekly if Friday)')
    p.add_argument('--weekly',   action='store_true',
                   help='Force-generate this week\'s summary report')
    p.add_argument('--backfill', action='store_true',
                   help='Generate all missing daily and weekly reports')
    p.add_argument('--nt8-path', metavar='PATH',
                   help='Path to NinjaTrader 8 Documents folder')
    p.add_argument('--dry-run',  action='store_true',
                   help='Show what would be generated without writing files')
    p.add_argument('--session-hour', type=int, metavar='0-23', default=None,
                   help='Hour a new trading session starts, in your chart timezone '
                        '(default 18 = 6 PM ET). Overrides config.json for this run.')
    p.add_argument('-d', '--daemon', action='store_true',
                   help='Daemon mode: no pause at exit (use for Task Scheduler)')
    p.add_argument('--version',  action='store_true',
                   help='Show version, author, and contact info then exit')
    return p.parse_args()


# ── Main orchestrator ─────────────────────────────────────────────────────────

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

    # ── 1. Locate NT8 folder ───────────────────────────────────────────────────
    try:
        nt8 = find_nt8_folder(getattr(args, 'nt8_path', None))
    except FileNotFoundError as e:
        print(f"[ERROR] {e}")
        sys.exit(1)
    print(f"  NT8 folder : {nt8}")

    # ── 2. Initialise MONARCH directory structure ──────────────────────────────
    monarch_dir, logs_dir, reports_dir = init_monarch_dirs(nt8)
    print(f"  MONARCH    : {monarch_dir}")

    # Local per-installation config lives in NT8\MONARCH\config.json — OUTSIDE
    # the source repo — so account numbers and other personal settings stay
    # private. Create a starter template on first run (skipped on --dry-run).
    if not args.dry_run:
        ensure_local_config_template(monarch_dir)
    local_cfg = load_local_config(monarch_dir)
    apply_account_config(local_cfg)   # populate account label/grade maps

    # Session-boundary hour: --session-hour overrides config.json overrides default 18.
    if args.session_hour is not None and not (0 <= args.session_hour <= 23):
        print(f"[ERROR] --session-hour must be 0-23 (got {args.session_hour}).")
        sys.exit(1)
    boundary_hour = args.session_hour if args.session_hour is not None \
        else get_session_boundary_hour(local_cfg)
    if boundary_hour != 18:
        print(f"  Session start hour : {boundary_hour}:00 (chart-local)")

    # ── 3. Sync log files ──────────────────────────────────────────────────────
    print("\n[SYNC] Moving GodZilla log files...")
    if not args.dry_run:
        moved, failed = sync_logs(nt8, logs_dir)
        print(f"  Moved: {moved}" + (f"  |  Failed: {failed}" if failed else ""))
    else:
        print("  (dry-run – skipped)")

    # ── 4. Parse all trades ────────────────────────────────────────────────────
    print("\n[PARSE] Reading log files...")
    all_trades = parse_all_trades(logs_dir, boundary_hour)
    accounts   = sorted({t['account'] for t in all_trades})
    print(f"  Total: {len(all_trades)} trades  |  Accounts: {len(accounts)}")

    if not all_trades:
        print("\n  No trade data found. Nothing to generate.")
        _end_of_run(args.daemon)
        return

    # ── 5. Load index and update cumulative stats ──────────────────────────────
    index = load_index(reports_dir)
    update_cumulative(index, all_trades)

    # ── 6. Determine target date (apply Sat/Sun → Friday guardrail) ────────────
    if args.date:
        try:
            target_date = date.fromisoformat(args.date)
        except ValueError:
            print(f"[ERROR] Invalid date format '{args.date}'. Use YYYY-MM-DD.")
            sys.exit(1)
    else:
        target_date = get_report_date()   # applies weekend guardrail

    current_friday = get_friday_of_week(target_date)

    print(f"\n[DATE]  Report date : {target_date}  |  Week ending : {current_friday}")

    # ── 7. Decide which reports to generate ───────────────────────────────────
    trading_days = get_trading_days_with_data(all_trades)
    all_weeks    = get_weeks_with_data(trading_days)

    # Always fill in every missing daily and weekly — this is the default behaviour.
    # Any date that has log data but no HTML report gets generated automatically.
    daily_to_generate  = get_missing_daily_dates(reports_dir, trading_days)
    weekly_to_generate = get_missing_weekly_dates(reports_dir, all_weeks)

    if daily_to_generate or weekly_to_generate:
        print(f"\n[FILL]  Missing daily reports  : {len(daily_to_generate)}")
        print(f"[FILL]  Missing weekly reports : {len(weekly_to_generate)}")

    # --backfill: force-regenerate ALL reports, not just missing ones
    if args.backfill:
        daily_to_generate  = list(trading_days)
        weekly_to_generate = list(all_weeks)
        print(f"\n[BACKFILL] Regenerating all {len(daily_to_generate)} daily"
              f" and {len(weekly_to_generate)} weekly reports")

    # Always regenerate today's daily (catches intraday updates)
    if target_date not in daily_to_generate:
        daily_to_generate.append(target_date)

    # Weekly: generate for this week's Friday if it's Friday, or if forced
    if args.weekly and current_friday not in weekly_to_generate:
        weekly_to_generate.append(current_friday)
    elif is_friday(target_date) and current_friday not in weekly_to_generate:
        weekly_to_generate.append(current_friday)

    # Sort chronologically
    daily_to_generate  = sorted(set(daily_to_generate))
    weekly_to_generate = sorted(set(weekly_to_generate))

    # ── 8. Generate daily reports ──────────────────────────────────────────────
    if daily_to_generate:
        print(f"\n[DAILY] Generating {len(daily_to_generate)} daily report(s)...")
        for d in daily_to_generate:
            if args.dry_run:
                has_data = any(t['session'] == d for t in all_trades)
                print(f"  (dry-run) Would generate daily_{d.strftime('%Y%m%d')}.html"
                      f"  – {'has data' if has_data else 'no trades'}")
            else:
                # Isolate each report: one bad day must not abort the whole run
                # (remaining reports, the hub, and index save still proceed).
                try:
                    generate_daily(d, all_trades, index, reports_dir)
                except Exception as e:
                    print(f"  [warn] Failed to generate daily_{d.strftime('%Y%m%d')}.html: {e}")

    # ── 9. Generate weekly reports ─────────────────────────────────────────────
    if weekly_to_generate:
        print(f"\n[WEEKLY] Generating {len(weekly_to_generate)} weekly report(s)...")
        for friday in weekly_to_generate:
            if args.dry_run:
                print(f"  (dry-run) Would generate weekly_{friday.strftime('%Y%m%d')}.html")
            else:
                try:
                    generate_weekly(friday, all_trades, index, reports_dir)
                except Exception as e:
                    print(f"  [warn] Failed to generate weekly_{friday.strftime('%Y%m%d')}.html: {e}")

    # ── 10. Regenerate hub ─────────────────────────────────────────────────────
    print("\n[HUB] Updating Castle Bravo...")
    if not args.dry_run:
        # Isolate the hub so a hub failure still lets the index (with updated
        # cumulative stats) be saved.
        try:
            generate_hub(all_trades, index, monarch_dir, reports_dir)
        except Exception as e:
            print(f"  [warn] Failed to generate the hub: {e}")
        save_index(index, reports_dir)

    # ── Done ───────────────────────────────────────────────────────────────────
    print(f"\n{'='*60}")
    if args.dry_run:
        print("  Dry run complete – no files written.")
    else:
        hub_path = monarch_dir / 'CastleBravo.html'
        print(f"  Done. Open your hub:")
        print(f"  {hub_path}")
    print(f"{'='*60}\n")

    _end_of_run(args.daemon)


def _end_of_run(daemon: bool):
    """
    Post-run pause behaviour:
      daemon=True  – Task Scheduler launch: return immediately, no pause.
      daemon=False – Manual or double-click launch: 60-second countdown
                     so the user can read output. Press Enter to exit early.
    """
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
