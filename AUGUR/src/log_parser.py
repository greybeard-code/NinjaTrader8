"""
AUGUR Trade Intelligence – Log Parser & Statistics
====================================================
Parses NT8 Executions export CSVs, reconstructs complete round-trip trades
from individual execution fills, and computes all statistics.

NT8 Executions CSV columns:
  Instrument, Action, Quantity, Price, Time, ID, E/X, Position,
  Order ID, Name, Commission, Rate, Account display name, Connection

Trade reconstruction:
  The Position column shows the running position AFTER each fill.
  A trade segment spans from the first fill after a flat ("-") state
  to the next fill where Position == "-".
"""

import csv
import json
from datetime import datetime, date
from pathlib import Path
from collections import defaultdict
from typing import List, Dict, Tuple

from config import get_account_label, get_point_value, is_live_account
from date_utils import trading_day_for


# ── Execution CSV parsing ─────────────────────────────────────────────────────

def parse_all_executions(imports_dir: Path) -> List[dict]:
    """
    Parse every *.csv in imports_dir.
    Returns a deduped list of execution dicts sorted by time ascending.
    """
    executions: List[dict] = []
    seen_keys: set = set()
    for fpath in sorted(imports_dir.glob('*.csv')):
        _parse_exec_file(fpath, executions, seen_keys)
    executions.sort(key=lambda e: e['time'])
    return executions


def _parse_exec_file(fpath: Path, executions: List[dict], seen_keys: set):
    try:
        with open(fpath, encoding='utf-8-sig') as f:
            reader = csv.DictReader(f)
            for row in reader:
                if not row.get('Time', '').strip():
                    continue

                ex_id = row.get('ID', '').strip()
                acct  = row.get('Account display name', '').strip()
                if not acct or not ex_id:
                    continue

                key = (acct, ex_id)
                if key in seen_keys:
                    continue
                seen_keys.add(key)

                try:
                    t = datetime.strptime(row['Time'].strip(), '%m/%d/%Y %I:%M:%S %p')
                except ValueError:
                    continue

                executions.append({
                    'instrument':  row.get('Instrument', '').strip(),
                    'action':      row.get('Action', '').strip(),
                    'qty':         _safe_int(row.get('Quantity')),
                    'price':       _safe_float(row.get('Price')),
                    'time':        t,
                    'ex_id':       ex_id,
                    'entry_exit':  row.get('E/X', '').strip(),
                    'position':    row.get('Position', '').strip(),
                    'order_id':    row.get('Order ID', '').strip(),
                    'name':        row.get('Name', '').strip(),
                    'commission':  _parse_commission(row.get('Commission', '$0')),
                    'account':     acct,
                    'source':      fpath.name,
                })
    except Exception as e:
        print(f"  [warn] Could not parse {fpath.name}: {e}")


# ── Trade reconstruction ──────────────────────────────────────────────────────

def reconstruct_trades(executions: List[dict]) -> List[dict]:
    """
    Group execution fills into complete round-trip trade dicts.
    Trades where Position never returns to '-' (open positions) are skipped.
    """
    streams: Dict[tuple, list] = defaultdict(list)
    for ex in executions:
        key = (ex['account'], ex['instrument'])
        streams[key].append(ex)

    trades: List[dict] = []
    for (acct, instr), fills in streams.items():
        fills.sort(key=lambda f: f['time'])
        _extract_trades_from_stream(acct, instr, fills, trades)

    trades.sort(key=lambda t: t['open_dt'])
    return trades


def _extract_trades_from_stream(acct: str, instr: str,
                                fills: List[dict], trades: List[dict]):
    # Within the same second, the flat ("-") fill must always be last.
    # NT8 exports don't have sub-second ordering so we enforce it here.
    fills.sort(key=lambda f: (f['time'], 1 if f['position'] == '-' else 0))

    buffer: List[dict] = []
    for fill in fills:
        buffer.append(fill)
        if fill['position'] == '-':
            trade = _build_trade(acct, instr, buffer)
            if trade:
                trades.append(trade)
            buffer = []
    # Remaining fills = still-open position; skip


def _build_trade(acct: str, instr: str, fills: List[dict]) -> dict | None:
    entry_fills = [f for f in fills if f['entry_exit'] == 'Entry']
    exit_fills  = [f for f in fills if f['entry_exit'] == 'Exit']

    if not entry_fills:
        return None

    direction = 'Long' if entry_fills[0]['action'] == 'Buy' else 'Short'
    dir_mult  = 1 if direction == 'Long' else -1

    total_entry_qty = sum(f['qty'] for f in entry_fills)
    if total_entry_qty == 0:
        return None

    entry_avg   = sum(f['qty'] * f['price'] for f in entry_fills) / total_entry_qty
    point_val   = get_point_value(instr)
    gross_pnl   = sum(
        (f['price'] - entry_avg) * f['qty'] * point_val * dir_mult
        for f in exit_fills
    )
    commission  = sum(f['commission'] for f in fills)
    net_pnl     = gross_pnl - commission

    if net_pnl > 0.005:
        result = 'WIN'
    elif net_pnl < -0.005:
        result = 'LOSS'
    else:
        result = 'FLAT'

    exit_names = [f['name'].lower() for f in exit_fills if f['name']]
    has_target = any('target' in n for n in exit_names)
    has_stop   = any('stop'   in n for n in exit_names)
    if has_target and has_stop:
        exit_type = 'mixed'
    elif has_target:
        exit_type = 'target'
    elif has_stop:
        exit_type = 'stop'
    else:
        exit_type = 'unknown'

    open_dt  = min(f['time'] for f in entry_fills)
    close_dt = fills[-1]['time']
    sources  = sorted({f['source'] for f in fills})

    return {
        'open_dt':     open_dt,
        'close_dt':    close_dt,
        'session':     trading_day_for(open_dt),
        'account':     acct,
        'instrument':  instr,
        'direction':   direction,
        'qty':         total_entry_qty,
        'entry_price': round(entry_avg, 4),
        'gross_pnl':   round(gross_pnl, 2),
        'commission':  round(commission, 2),
        'pnl':         round(net_pnl, 2),
        'result':      result,
        'exit_type':   exit_type,
        'exit_fills':  [{'qty': f['qty'], 'price': f['price'], 'name': f['name']}
                        for f in exit_fills],
        'duration':    _fmt_duration(open_dt, close_dt),
        'is_live':     is_live_account(acct),
        'source':      ', '.join(sources),
    }


# ── Grouping ──────────────────────────────────────────────────────────────────

def group_by_session(trades: List[dict]) -> Dict[date, list]:
    groups: Dict[date, list] = defaultdict(list)
    for t in trades:
        groups[t['session']].append(t)
    return dict(groups)


# ── Statistics ────────────────────────────────────────────────────────────────

_EMPTY_STATS = {
    'trades': 0, 'pnl': 0.0, 'gross_pnl': 0.0, 'commission': 0.0,
    'wins': 0, 'losses': 0, 'flat': 0, 'win_rate': 0.0,
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
    flats  = [t for t in trades if t['result'] == 'FLAT']
    longs  = [t for t in trades if t['direction'] == 'Long']
    shorts = [t for t in trades if t['direction'] == 'Short']

    pnl        = sum(t['pnl'] for t in trades)
    gross_pnl  = sum(t['gross_pnl'] for t in trades)
    commission = sum(t['commission'] for t in trades)
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
        'gross_pnl':     gross_pnl,
        'commission':    commission,
        'wins':          len(wins),
        'losses':        len(losses),
        'flat':          len(flats),
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


def exit_type_stats(trades: List[dict]) -> Dict[str, dict]:
    """Stats grouped by exit_type: 'target', 'stop', 'mixed', 'unknown'."""
    by_type: Dict[str, list] = defaultdict(list)
    for t in trades:
        by_type[t['exit_type']].append(t)
    return {et: compute_stats(ts) for et, ts in sorted(by_type.items())}


def account_stats(trades: List[dict]) -> Dict[str, dict]:
    by_acct: Dict[str, list] = defaultdict(list)
    for t in trades:
        by_acct[t['account']].append(t)
    return {a: compute_stats(ts) for a, ts in by_acct.items()}


def ticker_stats(trades: List[dict]) -> Dict[str, dict]:
    by_ticker: Dict[str, list] = defaultdict(list)
    for t in trades:
        instr  = t.get('instrument', '')
        ticker = instr.split()[0] if instr else 'Unknown'
        by_ticker[ticker].append(t)
    return {ticker: compute_stats(ts) for ticker, ts in sorted(by_ticker.items())}


def instrument_label(trades: List[dict]) -> str:
    instruments = sorted({t['instrument'] for t in trades if t.get('instrument')})
    return ' · '.join(instruments) if instruments else ''


# ── Index management ──────────────────────────────────────────────────────────

def load_index(reports_dir: Path) -> dict:
    index_file = reports_dir / 'index.json'
    if index_file.exists():
        try:
            with open(index_file, encoding='utf-8') as f:
                return json.load(f)
        except Exception:
            pass
    return {
        'reports':         {'daily': {}, 'weekly': {}},
        'cumulative':      {},
        'recommendations': [],
    }


def save_index(index: dict, reports_dir: Path):
    index_file = reports_dir / 'index.json'
    with open(index_file, 'w', encoding='utf-8') as f:
        json.dump(index, f, indent=2, default=str)


def update_cumulative(index: dict, all_trades: List[dict]):
    stats   = compute_stats(all_trades)
    by_acct = account_stats(all_trades)

    per_account = {
        acct: {
            'pnl':        s['pnl'],
            'trades':     s['trades'],
            'wins':       s['wins'],
            'losses':     s['losses'],
            'win_rate':   s['win_rate'],
            'commission': s['commission'],
        }
        for acct, s in by_acct.items()
    }

    index['cumulative'] = {
        'pnl':           stats['pnl'],
        'gross_pnl':     stats['gross_pnl'],
        'commission':    stats['commission'],
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
        'file':       f"daily_{session_date.strftime('%Y%m%d')}.html",
        'pnl':        stats['pnl'],
        'trades':     stats['trades'],
        'wins':       stats['wins'],
        'losses':     stats['losses'],
        'commission': stats['commission'],
        'accounts':   sorted({get_account_label(t['account']) for t in day_trades}),
    }


def register_weekly(index: dict, friday: date, week_trades: List[dict]):
    import datetime as _dt
    stats = compute_stats(week_trades)
    index['reports']['weekly'][friday.isoformat()] = {
        'file':       f"weekly_{friday.strftime('%Y%m%d')}.html",
        'pnl':        stats['pnl'],
        'trades':     stats['trades'],
        'wins':       stats['wins'],
        'losses':     stats['losses'],
        'week_start': (friday - _dt.timedelta(days=4)).isoformat(),
        'week_end':   friday.isoformat(),
    }


# ── Field helpers ─────────────────────────────────────────────────────────────

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


def _parse_commission(s: str) -> float:
    try:
        return float(str(s).strip().lstrip('$'))
    except (ValueError, TypeError):
        return 0.0


def _fmt_duration(open_dt: datetime, close_dt: datetime) -> str:
    secs = int((close_dt - open_dt).total_seconds())
    if secs < 0:
        secs = 0
    if secs < 60:
        return f"{secs}s"
    if secs < 3600:
        m, s = divmod(secs, 60)
        return f"{m}m {s}s"
    h, rem = divmod(secs, 3600)
    return f"{h}h {rem // 60}m"
