"""
MONARCH Intelligence Report System – Daily Report Generator
============================================================
Builds MONARCH_Daily_YYYY-MM-DD.html for a single trading session.
"""

from datetime import date
from pathlib import Path
from typing import List

from config import get_account_label, get_account_grade, VERSION, WEBSITE
from log_parser import (
    compute_stats, grade_stats, ko_stats, combo_stats, account_stats,
    instrument_label, ticker_stats, register_daily,
)
from date_utils import fmt_weekday_full, fmt_month_day
from templates import (
    page_wrap, kpi_card, bar_stat_row, grade_tag,
    pnl_fmt, pnl_class, pf_fmt, rr_fmt, now_stamp,
)


# ── Hub relative path from reports/ ──────────────────────────────────────────
HUB_REL = '../CastleBravo.html'


def build_daily_html(session_date: date, trades: List[dict], index: dict) -> str:
    """Generate the full HTML string for one daily report."""
    stats    = compute_stats(trades)
    by_acct  = account_stats(trades)
    by_tkr   = ticker_stats(trades)
    gstats   = grade_stats(trades)
    with_ko, without_ko = ko_stats(trades)
    combos   = combo_stats(trades)
    cumul    = index.get('cumulative', {})
    instr    = instrument_label(trades)

    day_name    = fmt_weekday_full(session_date)
    accts_active = sorted({get_account_label(t['account']) for t in trades})
    acct_str    = " + ".join(accts_active) if accts_active else "No accounts"

    # ── Badge ──────────────────────────────────────────────────────────────────
    if not trades:
        badge = '<span class="badge badge-none">No Trades</span>'
    elif stats['pnl'] >= 0:
        badge = f'<span class="badge badge-pos">{pnl_fmt(stats["pnl"])} Day</span>'
    else:
        badge = f'<span class="badge badge-neg">{pnl_fmt(stats["pnl"])} Day</span>'

    # ── Header ─────────────────────────────────────────────────────────────────
    header = f"""<div class="header">
  <div>
    <h1><span class="monarch-badge">MONARCH</span>Daily Report {badge}</h1>
    <div class="meta">{day_name}</div>
  </div>
  <div class="nav"><a href="{HUB_REL}">← Castle Bravo</a></div>
</div>"""

    # ── KPI Grid ───────────────────────────────────────────────────────────────
    if trades:
        wr_str = f"{stats['win_rate']*100:.1f}%"
        kpis = f"""<div class="kpi-grid">
  {kpi_card("Net P&L",       pnl_fmt(stats['pnl']),       pnl_class(stats['pnl']),   f"{stats['trades']} trades")}
  {kpi_card("Win Rate",      wr_str, 'green' if stats['win_rate'] >= 0.7 else 'yellow', f"{stats['wins']}W / {stats['losses']}L")}
  {kpi_card("Profit Factor", pf_fmt(stats['profit_factor']), 'green' if stats['profit_factor'] > 1.5 else ('yellow' if stats['profit_factor'] > 1 else 'red'), "Target: >1.5")}
  {kpi_card("Avg Win",       f"${stats['avg_win']:.2f}",  'green')}
  {kpi_card("Avg Loss",      f"${stats['avg_loss']:.2f}", 'red',  "Target: <$100")}
  {kpi_card("R:R Ratio",     rr_fmt(stats['rr']),         'green' if stats['rr'] >= 1 else 'red', "Target: >1.0")}
  {kpi_card("Longs",  f"{stats['long_wins']}/{stats['long_trades']}",  pnl_class(stats['long_pnl']),  pnl_fmt(stats['long_pnl']))}
  {kpi_card("Shorts", f"{stats['short_wins']}/{stats['short_trades']}", pnl_class(stats['short_pnl']), pnl_fmt(stats['short_pnl']))}
</div>"""
    else:
        kpis = ('<div class="callout callout-yellow">'
                '<h3>No trades recorded for this session</h3>'
                '<p>Check NT8 log folder – GodZilla CSVs may not have been written yet.</p>'
                '</div>')

    # ── Account Breakdown ──────────────────────────────────────────────────────
    acct_section = ''
    if by_acct:
        cards_html = ''
        for acct, s in sorted(by_acct.items()):
            label   = get_account_label(acct)
            grade   = get_account_grade(acct)
            pnl_cls = pnl_class(s['pnl'])
            wr_cls  = 'green' if s['win_rate'] >= 0.7 else 'yellow'
            grade_line = f' – {grade} Threshold' if grade != '?' else ''
            cards_html += f"""<div class="acct-card">
  <h3>{acct} – {label}{grade_line}</h3>
  <div class="acct-stat"><span class="k">Trades</span><span>{s['trades']}</span></div>
  <div class="acct-stat"><span class="k">Win Rate</span><span class="{wr_cls}">{s['win_rate']*100:.0f}% ({s['wins']}W/{s['losses']}L)</span></div>
  <div class="acct-stat"><span class="k">Net P&L</span><span class="{pnl_cls}">{pnl_fmt(s['pnl'])}</span></div>
  <div class="acct-stat"><span class="k">Avg Win</span><span class="green">${s['avg_win']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Avg Loss</span><span class="red">${s['avg_loss']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Profit Factor</span><span class="{pnl_cls}">{pf_fmt(s['profit_factor'])}</span></div>
</div>"""
        col = 'two-col' if len(by_acct) == 2 else ('three-col' if len(by_acct) >= 3 else '')
        acct_section = f'<div class="section"><h2>Account Breakdown</h2><div class="{col}">{cards_html}</div></div>'

    # ── Symbol Breakdown ───────────────────────────────────────────────────────
    symbol_section = ''
    if by_tkr:
        tkr_cards = ''
        for tkr, s in by_tkr.items():
            pnl_cls = pnl_class(s['pnl'])
            wr_cls  = 'green' if s['win_rate'] >= 0.7 else 'yellow'
            tkr_cards += f"""<div class="acct-card">
  <h3>{tkr}</h3>
  <div class="acct-stat"><span class="k">Trades</span><span>{s['trades']}</span></div>
  <div class="acct-stat"><span class="k">Win Rate</span><span class="{wr_cls}">{s['win_rate']*100:.0f}% ({s['wins']}W/{s['losses']}L)</span></div>
  <div class="acct-stat"><span class="k">Net P&L</span><span class="{pnl_cls}">{pnl_fmt(s['pnl'])}</span></div>
  <div class="acct-stat"><span class="k">Avg Win</span><span class="green">${s['avg_win']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Avg Loss</span><span class="red">${s['avg_loss']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Profit Factor</span><span class="{pnl_cls}">{pf_fmt(s['profit_factor'])}</span></div>
</div>"""
        col = 'two-col' if len(by_tkr) == 2 else ('three-col' if len(by_tkr) >= 3 else '')
        symbol_section = f'<div class="section"><h2>Symbol Breakdown</h2><div class="{col}">{tkr_cards}</div></div>'

    # ── Trade Log Table ────────────────────────────────────────────────────────
    trade_table = ''
    if trades:
        rows = ''
        for i, t in enumerate(sorted(trades, key=lambda x: x['open_dt']), 1):
            row_cls  = 'win-row' if t['result'] == 'WIN' else 'loss-row'
            fast_str = ' ⚡' if t['is_fast'] else ''
            dir_tag  = f'<span class="tag tag-{"long" if t["direction"]=="Long" else "short"}">{t["direction"]}</span>'
            res_tag  = f'<span class="tag tag-{"win" if t["result"]=="WIN" else "loss"}">{t["result"]}</span>'
            ko_tag   = '<span class="tag tag-ko">KO</span>' if t['has_ko'] else '<span class="muted">–</span>'
            pnl_col  = f'<span class="{pnl_class(t["pnl"])}">{pnl_fmt(t["pnl"])}</span>'
            acct_lbl = get_account_label(t['account'])
            rows += f"""<tr class="{row_cls}">
  <td>{i}</td>
  <td>{t['open_dt'].strftime('%H:%M')}</td>
  <td>{fmt_month_day(t['open_dt'].date())}</td>
  <td>{acct_lbl}</td>
  <td>{dir_tag}</td>
  <td>{t['duration']}{fast_str}</td>
  <td>{grade_tag(t['grade'])}</td>
  <td>{ko_tag}</td>
  <td><code>{t['combo_clean']}</code></td>
  <td>{res_tag}</td>
  <td>{pnl_col}</td>
</tr>"""
        trade_table = f"""<div class="section"><h2>Trade Log – {len(trades)} Trade{'s' if len(trades)!=1 else ''}</h2>
<table><thead><tr>
  <th>#</th><th>Time</th><th>Date</th><th>Acct</th><th>Dir</th>
  <th>Duration</th><th>Grade</th><th>KO</th><th>Signals</th><th>Result</th><th>P&L</th>
</tr></thead><tbody>{rows}</tbody></table>
<p style="font-size:11px;color:var(--muted);margin-top:8px">⚡ = closed in ≤10 seconds (ATM TP hit immediately)</p>
</div>"""

    # ── Breakdowns ─────────────────────────────────────────────────────────────
    breakdowns = ''
    if trades:
        dir_card = f"""<div class="card"><h3>Direction</h3>
  {bar_stat_row('Long',  stats['long_wins'],  stats['long_trades'],  stats['long_pnl'])}
  {bar_stat_row('Short', stats['short_wins'], stats['short_trades'], stats['short_pnl'])}
</div>"""
        grade_rows = ''.join(
            bar_stat_row(grade_tag(g), s['wins'], s['trades'], s['pnl'])
            for g, s in gstats.items()
        )
        grade_card = f'<div class="card"><h3>Signal Grade</h3>{grade_rows}</div>'
        ko_card = f"""<div class="card"><h3>KO Signal</h3>
  {bar_stat_row('With KO',    with_ko['wins'],    with_ko['trades'],    with_ko['pnl'])}
  {bar_stat_row('Without KO', without_ko['wins'], without_ko['trades'], without_ko['pnl'])}
</div>"""
        breakdowns = f'<div class="section"><h2>Breakdowns</h2><div class="three-col">{dir_card}{grade_card}{ko_card}</div></div>'

    # ── Signal Combo Analysis ──────────────────────────────────────────────────
    combo_section = ''
    if trades:
        combo_rows = ''
        for c in combos[:10]:
            row_cls = 'win-row' if c['pnl'] >= 0 else 'loss-row'
            combo_rows += f"""<tr class="{row_cls}">
  <td><code>{c['combo']}</code></td>
  <td>{c['trades']}</td>
  <td>{c['wins']}/{c['losses']}</td>
  <td class="{pnl_class(c['pnl'])}">{pnl_fmt(c['pnl'])}</td>
  <td>${c['avg_win']:.2f}</td>
  <td>${c['avg_loss']:.2f}</td>
</tr>"""
        combo_section = f"""<div class="section"><h2>Signal Combo Analysis</h2>
<table><thead><tr>
  <th>Signals</th><th>Trades</th><th>W/L</th><th>Net P&L</th><th>Avg Win</th><th>Avg Loss</th>
</tr></thead><tbody>{combo_rows}</tbody></table></div>"""

    # ── Cumulative Context ─────────────────────────────────────────────────────
    cumul_section = ''
    if cumul.get('trades'):
        c = cumul
        cumul_section = f"""<div class="section"><h2>Cumulative Live Performance</h2>
<div class="kpi-grid">
  {kpi_card("All-Time P&L",  pnl_fmt(c.get('pnl',0)),        pnl_class(c.get('pnl',0)), f"{c.get('trades',0)} trades")}
  {kpi_card("Win Rate",      f"{c.get('win_rate',0)*100:.1f}%",'green' if c.get('win_rate',0)>=.7 else 'yellow', f"{c.get('wins',0)}W/{c.get('losses',0)}L")}
  {kpi_card("Profit Factor", pf_fmt(c.get('profit_factor',0)), 'green' if c.get('profit_factor',0)>1.5 else 'yellow')}
  {kpi_card("Avg Win",       f"${c.get('avg_win',0):.2f}",    'green')}
  {kpi_card("Avg Loss",      f"${c.get('avg_loss',0):.2f}",   'red')}
  {kpi_card("R:R Ratio",     rr_fmt(c.get('rr',0)),           'red' if c.get('rr',0) < 1 else 'green')}
</div></div>"""

    # ── Footer ─────────────────────────────────────────────────────────────────
    year       = session_date.year
    instr_part = f' · {instr}' if instr else ''
    footer = (f'<div class="footer">Generated {now_stamp()} · MONARCH Intelligence Report System v{VERSION}{instr_part}'
              f' · <a href="{HUB_REL}" style="color:var(--accent)">← Castle Bravo</a>'
              f'<br>Copyright &copy; {year} GreyBeard Consulting &nbsp;·&nbsp; '
              f'<a href="{WEBSITE}" style="color:var(--accent)">{WEBSITE}</a></div>')

    body = '\n'.join([header, kpis, acct_section, symbol_section, trade_table, breakdowns, combo_section, cumul_section, footer])
    return page_wrap(f"MONARCH Daily – {session_date.strftime('%Y-%m-%d')}", body)


def generate_daily(session_date: date, all_trades: List[dict],
                   index: dict, reports_dir: Path) -> Path:
    """Build and write the daily report. Returns the output path."""
    from log_parser import group_by_session
    by_day     = group_by_session(all_trades)
    day_trades = by_day.get(session_date, [])

    print(f"  Daily {session_date}: {len(day_trades)} trades")

    html  = build_daily_html(session_date, day_trades, index)
    fname = f"daily_{session_date.strftime('%Y%m%d')}.html"
    out   = reports_dir / fname
    with open(out, 'w', encoding='utf-8') as f:
        f.write(html)

    register_daily(index, session_date, day_trades)
    print(f"  → {out}")
    return out
