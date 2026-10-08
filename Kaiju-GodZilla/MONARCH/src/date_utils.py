"""
MONARCH Intelligence Report System – Date Utilities
=====================================================
Trading-day boundary logic, week math, and missing-report detection.

Trading day rule:
  6:00 PM ET = start of a new session.
  Session date = the CALENDAR DAY on which the session ENDS.

  Examples:
    2026-05-21 22:10 ET  →  session date 2026-05-22  (trades after 6pm belong to next day)
    2026-05-22 10:00 ET  →  session date 2026-05-22  (trades before 6pm stay on same day)

Weekend guardrail:
  Saturday → Friday of that same week
  Sunday   → Friday of that same week
  (Prevents empty weekend runs from creating blank reports)
"""

from datetime import date, datetime, timedelta
from pathlib import Path
from typing import List


# ── Core date helpers ─────────────────────────────────────────────────────────

def trading_day_for(open_dt: datetime, boundary_hour: int = 18) -> date:
    """Return the session date for a trade's open time.

    boundary_hour is the hour (0-23, in the chart's local time) at which a new
    trading session begins. Default 18 (6 PM), correct when NT8 charts render in
    ET. Override it (via config.json or --session-hour) if your charts use a
    different timezone — e.g. 17 for CT, 15 for PT.
    """
    if open_dt.hour >= boundary_hour:
        return (open_dt + timedelta(days=1)).date()
    return open_dt.date()


def get_report_date(d: date = None) -> date:
    """
    Return the effective report date for a given calendar date.
    Saturday and Sunday both map to the preceding Friday.
    Defaults to today.
    """
    if d is None:
        d = date.today()
    weekday = d.weekday()   # 0=Mon … 4=Fri, 5=Sat, 6=Sun
    if weekday == 5:         # Saturday → Friday
        return d - timedelta(days=1)
    if weekday == 6:         # Sunday   → Friday
        return d - timedelta(days=2)
    return d


def is_friday(d: date) -> bool:
    return d.weekday() == 4


def get_friday_of_week(d: date) -> date:
    """Return the Friday of the week containing d."""
    days_to_friday = 4 - d.weekday()   # can be negative (already past Friday)
    return d + timedelta(days=days_to_friday)


def week_dates(friday: date) -> List[date]:
    """Return [Mon, Tue, Wed, Thu, Fri] for the week ending on friday."""
    mon = friday - timedelta(days=4)
    return [mon + timedelta(days=i) for i in range(5)]


# ── Cross-platform date formatting ────────────────────────────────────────────

def fmt_month_day(d: date) -> str:
    """Return 'May 5' style (no leading zero, cross-platform)."""
    return d.strftime('%b') + ' ' + str(d.day)


def fmt_weekday_month_day(d: date) -> str:
    """Return 'Mon May 5' style."""
    return d.strftime('%a') + ' ' + fmt_month_day(d)


def fmt_month_day_year(d: date) -> str:
    """Return 'May 5, 2026' style."""
    return d.strftime('%B') + ' ' + str(d.day) + ', ' + str(d.year)


def fmt_weekday_full(d: date) -> str:
    """Return 'Monday, May 5, 2026' style."""
    return d.strftime('%A') + ', ' + fmt_month_day_year(d)


# ── Missing-report detection ──────────────────────────────────────────────────

def get_trading_days_with_data(all_trades: list) -> List[date]:
    """Return sorted list of session dates that have at least one trade."""
    return sorted({t['session'] for t in all_trades})


def get_missing_daily_dates(reports_dir: Path, trading_days: List[date]) -> List[date]:
    """
    Return trading days that have live data but no daily report file yet.
    Always excludes today (caller handles today separately).
    """
    today = date.today()
    missing = []
    for d in trading_days:
        if d >= today:
            continue
        fname = f"daily_{d.strftime('%Y%m%d')}.html"
        if not (reports_dir / fname).exists():
            missing.append(d)
    return missing


def get_weeks_with_data(trading_days: List[date]) -> List[date]:
    """Return sorted list of week-end Fridays that have at least one trading day."""
    fridays = {get_friday_of_week(d) for d in trading_days}
    return sorted(fridays)


def get_missing_weekly_dates(reports_dir: Path, weeks: List[date]) -> List[date]:
    """Return Fridays that have data but no weekly report file yet."""
    today = date.today()
    missing = []
    for friday in weeks:
        if friday > today:
            continue
        fname = f"weekly_{friday.strftime('%Y%m%d')}.html"
        if not (reports_dir / fname).exists():
            missing.append(friday)
    return missing
