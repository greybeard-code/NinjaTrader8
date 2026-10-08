"""
MONARCH Intelligence Report System – Log Parser
================================================
Parses GodZilla_*.csv trade files into normalised trade dicts and
computes all statistics used by the report generators.

CSV columns (as written by NinjaTrader):
  GodZillaKilla 1.9.2+ (current, 11 columns):
    OpenTime, Account, Instrument, OpenPrice, Qty, CloseTime,
    Trigger, Direction, AtmStrategyName, RealizedPnL, TradeResult
  GodZillaKilla <=1.9.1 (legacy, 14 columns) also added:
    SignalCombo, UsedSignals, LastTradeLine
Parsed via csv.DictReader (by header name), so both layouts are supported.
SignalCombo/UsedSignals fall back to Trigger when absent (they duplicated it).
"""

import csv
import re
from datetime import datetime, timedelta
from pathlib import Path
from collections import defaultdict
from typing import List, Dict, Tuple

from config import get_account_label
from date_utils import trading_day_for


# ── Individual field helpers ──────────────────────────────────────────────────

def parse_grade(trigger: str, used_signals: str) -> str:
    """Extract G3 / G4 / G5 from Trigger or UsedSignals."""
    for field in (trigger, used_signals):
        m = re.search(r'G(\d)', field)
        if m:
            return f"G{m.group(1)}"
    return 'G?'


def has_ko(used_signals: str) -> bool:
    return 'KO' in used_signals.upper()


def parse_duration(open_dt: datetime, close_dt: datetime) -> str:
    secs = int((close_dt - open_dt).total_seconds())
    if secs < 60:
        return f"{secs}s"
    if secs < 3600:
        m, s = divmod(secs, 60)
        return f"{m}m {s}s"
    h, rem = divmod(secs, 3600)
    return f"{h}h {rem // 60}m"


def is_fast_trade(open_dt: datetime, close_dt: datetime) -> bool:
    """True for trades closed in ≤10 seconds (ATM TP hit immediately)."""
    return (close_dt - open_dt).total_seconds() <= 10


def clean_combo(combo: str) -> str:
    """Strip the SET#-G# prefix from a signal combo / trigger.

    Handles both the old SignalCombo form ("SET1-G3-KO+PA+SU") and the
    Trigger form now used as the fallback ("SET1-G3:KO+PA+SU", incl. SET2).
    """
    return re.sub(r'^SET\d-G\d[-:]?', '', combo)


# ── File scanning & parsing ───────────────────────────────────────────────────

def _account_from_filename(fpath: Path) -> str:
    """
    Extract account name from filename: GodZilla_{ACCOUNT}_{...}.csv
    Returns empty string if the filename doesn't match the pattern.
    """
    m = re.match(r'^GodZilla_([^_]+)_', fpath.name)
    return m.group(1) if m else ''


def parse_all_trades(logs_dir: Path, boundary_hour: int = 18) -> List[dict]:
    """
    Parse every GodZilla_*.csv in logs_dir.
    Returns a list of trade dicts sorted by open_dt ascending.
    All accounts (prop firm, Sim, etc.) are included and treated equally.

    boundary_hour: hour (0-23, chart-local time) at which a new trading session
    begins — passed through to trading_day_for. Default 18 (6 PM ET).
    """
    trades: List[dict] = []
    seen_keys: set = set()

    for fpath in sorted(logs_dir.glob('GodZilla_*.csv')):
        _parse_file(fpath, trades, seen_keys, boundary_hour)

    trades.sort(key=lambda t: t['open_dt'])
    return trades


def _parse_file(fpath: Path, trades: List[dict], seen_keys: set, boundary_hour: int = 18):
    filename_account = _account_from_filename(fpath)
    try:
        with open(fpath, encoding='utf-8-sig') as f:
            reader = csv.DictReader(f)
            for row in reader:
                # Per-row isolation: one malformed line is skipped, not fatal to the
                # rest of the file.
                try:
                    _parse_row(row, fpath, filename_account, trades, seen_keys, boundary_hour)
                except Exception as e:
                    print(f"  [warn] Skipped a bad row in {fpath.name}: {e}")
    except Exception as e:
        print(f"  [warn] Could not parse {fpath.name}: {e}")


def _parse_row(row: dict, fpath: Path, filename_account: str,
               trades: List[dict], seen_keys: set, boundary_hour: int):
    """Parse a single CSV row and append a trade dict. Returns early (skips the
    row) on any missing/invalid required field."""
    if not row.get('OpenTime'):
        return

    # Account: prefer CSV column; fall back to filename-derived value
    acct = row.get('Account', '').strip().strip('"') or filename_account
    if not acct:
        return

    try:
        open_dt  = datetime.strptime(row['OpenTime'].strip(),  '%Y-%m-%d %H:%M:%S')
        close_dt = datetime.strptime(row['CloseTime'].strip(), '%Y-%m-%d %H:%M:%S')
    except ValueError:
        return

    # GodZillaKilla 1.9.2+ dropped the SignalCombo/UsedSignals/LastTradeLine
    # columns — they duplicated Trigger (e.g. "SET1-G3:KO+PA+SU"). Fall back
    # to Trigger so KO detection and combo grouping keep working on new logs
    # while older 14-column logs still use their dedicated columns.
    trigger = row.get('Trigger', '').strip()
    used    = row.get('UsedSignals', '').strip() or trigger
    combo   = row.get('SignalCombo', '').strip() or trigger

    # Dedup: same account + open time = same trade
    key = (acct, open_dt)
    if key in seen_keys:
        return
    seen_keys.add(key)

    try:
        pnl = float(row['RealizedPnL'].strip())
    except (ValueError, KeyError):
        pnl = 0.0

    # TradeResult (WIN/LOSS/FLAT) is authoritative when present, but fall
    # back to the sign of PnL if the column is missing or blank so win/loss
    # stats stay correct across any log variant.
    result = row.get('TradeResult', '').strip().upper()
    if result not in ('WIN', 'LOSS', 'FLAT'):
        result = 'WIN' if pnl > 0 else 'LOSS' if pnl < 0 else 'FLAT'

    trades.append({
        'open_dt':     open_dt,
        'close_dt':    close_dt,
        'session':     trading_day_for(open_dt, boundary_hour),
        'account':     acct,
        'instrument':  row.get('Instrument', '').strip(),
        'open_price':  _safe_float(row.get('OpenPrice')),
        'qty':         _safe_int(row.get('Qty')),
        'direction':   row.get('Direction', '').strip(),
        'atm':         row.get('AtmStrategyName', '').strip(),
        'pnl':         pnl,
        'result':      result,
        'trigger':     trigger,
        'used':        used,
        'combo':       combo,
        'combo_clean': clean_combo(combo),
        'grade':       parse_grade(trigger, used),
        'has_ko':      has_ko(used),
        'duration':    parse_duration(open_dt, close_dt),
        'is_fast':     is_fast_trade(open_dt, close_dt),
        'source':      fpath.name,
    })


def _safe_float(v) -> float:
    try:
        return float(v) if v else 0.0
    except (ValueError, TypeError):
        return 0.0


def _safe_int(v) -> int:
    try:
        return int(v) if v else 0
    except (ValueError, TypeError):
        return 0


# ── Grouping ──────────────────────────────────────────────────────────────────

def group_by_session(trades: List[dict]) -> Dict:
    groups = defaultdict(list)
    for t in trades:
        groups[t['session']].append(t)
    return dict(groups)


# ── Statistics ────────────────────────────────────────────────────────────────

_EMPTY_STATS = {
    'trades': 0, 'pnl': 0.0,
    'wins': 0, 'losses': 0, 'win_rate': 0.0,
    'profit_factor': 0.0, 'avg_win': 0.0, 'avg_loss': 0.0, 'rr': 0.0,
    'long_trades': 0, 'long_wins': 0, 'long_pnl': 0.0,
    'short_trades': 0, 'short_wins': 0, 'short_pnl': 0.0,
    'gross_win': 0.0, 'gross_loss': 0.0,
}


def compute_stats(trades: List[dict]) -> dict:
    if not trades:
        return dict(_EMPTY_STATS)

    wins   = [t for t in trades if t['result'] == 'WIN']
    losses = [t for t in trades if t['result'] == 'LOSS']
    longs  = [t for t in trades if t['direction'] == 'Long']
    shorts = [t for t in trades if t['direction'] == 'Short']

    pnl        = sum(t['pnl'] for t in trades)
    gross_win  = sum(t['pnl'] for t in wins)
    gross_loss = abs(sum(t['pnl'] for t in losses))
    avg_win    = gross_win  / len(wins)   if wins   else 0.0
    avg_loss   = gross_loss / len(losses) if losses else 0.0

    if gross_loss > 0:
        pf = gross_win / gross_loss
        rr = avg_win   / avg_loss
    elif gross_win > 0:
        pf = float('inf')
        rr = float('inf')
    else:
        pf = rr = 0.0

    return {
        'trades':        len(trades),
        'pnl':           pnl,
        'wins':          len(wins),
        'losses':        len(losses),
        'win_rate':      len(wins) / len(trades),
        'profit_factor': pf,
        'avg_win':       avg_win,
        'avg_loss':      avg_loss,
        'rr':            rr,
        'gross_win':     gross_win,
        'gross_loss':    gross_loss,
        'long_trades':   len(longs),
        'long_wins':     sum(1 for t in longs  if t['result'] == 'WIN'),
        'long_pnl':      sum(t['pnl'] for t in longs),
        'short_trades':  len(shorts),
        'short_wins':    sum(1 for t in shorts if t['result'] == 'WIN'),
        'short_pnl':     sum(t['pnl'] for t in shorts),
    }


def grade_stats(trades: List[dict]) -> Dict[str, dict]:
    by_grade = defaultdict(list)
    for t in trades:
        by_grade[t['grade']].append(t)
    return {g: compute_stats(ts) for g, ts in sorted(by_grade.items())}


def ko_stats(trades: List[dict]) -> Tuple[dict, dict]:
    with_ko    = [t for t in trades if t['has_ko']]
    without_ko = [t for t in trades if not t['has_ko']]
    return compute_stats(with_ko), compute_stats(without_ko)


def combo_stats(trades: List[dict]) -> List[dict]:
    by_combo = defaultdict(list)
    for t in trades:
        by_combo[t['combo_clean']].append(t)
    result = []
    for combo, ts in sorted(by_combo.items(), key=lambda x: -len(x[1])):
        s = compute_stats(ts)
        result.append({'combo': combo, **s})
    return result


def account_stats(trades: List[dict]) -> Dict[str, dict]:
    by_acct = defaultdict(list)
    for t in trades:
        by_acct[t['account']].append(t)
    return {a: compute_stats(ts) for a, ts in by_acct.items()}


def instrument_label(trades: List[dict]) -> str:
    """Return a display string of unique instruments found in trades (e.g. 'MNQ 06-26' or 'MNQ 06-26 · ES 06-26')."""
    instruments = sorted({t['instrument'] for t in trades if t.get('instrument')})
    return ' · '.join(instruments) if instruments else ''


def ticker_stats(trades: List[dict]) -> Dict[str, dict]:
    """Group trades by base ticker symbol (contract date stripped) and compute stats per symbol."""
    by_ticker: Dict[str, list] = defaultdict(list)
    for t in trades:
        instr  = t.get('instrument', '')
        ticker = instr.split()[0] if instr else 'Unknown'
        by_ticker[ticker].append(t)
    return {ticker: compute_stats(ts) for ticker, ts in sorted(by_ticker.items())}


# ── Index management ──────────────────────────────────────────────────────────

import json
from datetime import date


def load_index(reports_dir: Path) -> dict:
    index_file = reports_dir / 'index.json'
    if index_file.exists():
        try:
            with open(index_file, encoding='utf-8') as f:
                return json.load(f)
        except Exception:
            pass
    return {
        'reports':     {'daily': {}, 'weekly': {}},
        'cumulative':  {},
        'recommendations': [],
    }


def save_index(index: dict, reports_dir: Path):
    index_file = reports_dir / 'index.json'
    with open(index_file, 'w', encoding='utf-8') as f:
        json.dump(index, f, indent=2, default=str)


def update_cumulative(index: dict, all_trades: List[dict]):
    """Recompute all-time cumulative stats and write into index."""
    stats   = compute_stats(all_trades)
    by_acct = account_stats(all_trades)

    per_account = {
        acct: {'pnl': s['pnl'], 'trades': s['trades'],
               'wins': s['wins'], 'losses': s['losses'],
               'win_rate': s['win_rate']}
        for acct, s in by_acct.items()
    }

    index['cumulative'] = {
        'pnl':           stats['pnl'],
        'trades':        stats['trades'],
        'wins':          stats['wins'],
        'losses':        stats['losses'],
        'win_rate':      stats['win_rate'],
        'profit_factor': stats['profit_factor'],
        'avg_win':       stats['avg_win'],
        'avg_loss':      stats['avg_loss'],
        'rr':            stats['rr'],
        'per_account':   per_account,
        'last_updated':  date.today().isoformat(),
    }


def register_daily(index: dict, session_date: date, day_trades: List[dict]):
    stats = compute_stats(day_trades)
    index['reports']['daily'][session_date.isoformat()] = {
        'file':     f"daily_{session_date.strftime('%Y%m%d')}.html",
        'pnl':      stats['pnl'],
        'trades':   stats['trades'],
        'wins':     stats['wins'],
        'losses':   stats['losses'],
        'accounts': sorted({get_account_label(t['account']) for t in day_trades}),
    }


def register_weekly(index: dict, friday: date, week_trades: List[dict]):
    stats = compute_stats(week_trades)
    index['reports']['weekly'][friday.isoformat()] = {
        'file':       f"weekly_{friday.strftime('%Y%m%d')}.html",
        'pnl':        stats['pnl'],
        'trades':     stats['trades'],
        'wins':       stats['wins'],
        'losses':     stats['losses'],
        'week_start': (friday - __import__('datetime').timedelta(days=4)).isoformat(),
        'week_end':   friday.isoformat(),
    }
