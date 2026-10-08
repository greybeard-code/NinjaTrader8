"""
AUGUR Trade Intelligence – Date Utilities
==========================================
Trading-day boundary logic, week math, and missing-report detection.

Trading day rule:
  6:00 PM ET = start of a new session.
  Session date = the CALENDAR DAY on which the session ENDS.

  Examples:
    2026-05-21 22:10 ET  →  session date 2026-05-22
    2026-05-22 10:00 ET  →  session date 2026-05-22

Weekend guardrail:
  Saturday → Friday of that same week
  Sunday   → Friday of that same week
"""

from datetime import date, datetime, timedelta
from pathlib import Path
from typing import List


def trading_day_for(open_dt: datetime) -> date:
    """Return the session date for a trade's open time."""
    if open_dt.hour >= 18:
        return (open_dt + timedelta(days=1)).date()
    return open_dt.date()


def get_report_date(d: date = None) -> date:
    """
    Return the effective report date for a given calendar date.
    Saturday and Sunday both map to the preceding Friday.
    """
    if d is None:
        d = date.today()
    weekday = d.weekday()
    if weekday == 5:
        return d - timedelta(days=1)
    if weekday == 6:
        return d - timedelta(days=2)
    return d


def is_friday(d: date) -> bool:
    return d.weekday() == 4


def get_friday_of_week(d: date) -> date:
    """Return the Friday of the week containing d."""
    days_to_friday = 4 - d.weekday()
    return d + timedelta(days=days_to_friday)


def week_dates(friday: date) -> List[date]:
    """Return [Mon, Tue, Wed, Thu, Fri] for the week ending on friday."""
    mon = friday - timedelta(days=4)
    return [mon + timedelta(days=i) for i in range(5)]


def fmt_month_day(d: date) -> str:
    return d.strftime('%b') + ' ' + str(d.day)


def fmt_weekday_month_day(d: date) -> str:
    return d.strftime('%a') + ' ' + fmt_month_day(d)


def fmt_month_day_year(d: date) -> str:
    return d.strftime('%B') + ' ' + str(d.day) + ', ' + str(d.year)


def fmt_weekday_full(d: date) -> str:
    return d.strftime('%A') + ', ' + fmt_month_day_year(d)


def get_trading_days_with_data(all_trades: list) -> List[date]:
    return sorted({t['session'] for t in all_trades})


def get_missing_daily_dates(reports_dir: Path, trading_days: List[date]) -> List[date]:
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
    fridays = {get_friday_of_week(d) for d in trading_days}
    return sorted(fridays)


def get_missing_weekly_dates(reports_dir: Path, weeks: List[date]) -> List[date]:
    today = date.today()
    missing = []
    for friday in weeks:
        if friday > today:
            continue
        fname = f"weekly_{friday.strftime('%Y%m%d')}.html"
        if not (reports_dir / fname).exists():
            missing.append(friday)
    return missing
