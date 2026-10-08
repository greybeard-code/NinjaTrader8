"""
AUGUR Trade Intelligence – Daily Report Generator
===================================================
Builds daily_YYYYMMDD.html for a single trading session.
"""

from datetime import date
from pathlib import Path
from typing import List

from config import get_account_label, VERSION, WEBSITE
from log_parser import (
    compute_stats, exit_type_stats, account_stats,
    instrument_label, ticker_stats, group_by_session, register_daily,
)
from date_utils import fmt_weekday_full, fmt_month_day
from templates import (
    page_wrap, kpi_card, bar_stat_row, exit_type_tag,
    pnl_fmt, pnl_class, pf_fmt, rr_fmt, now_stamp,
)

HUB_REL = '../Templum.html'


def build_daily_html(session_date: date, trades: List[dict], index: dict) -> str:
    stats    = compute_stats(trades)
    by_acct  = account_stats(trades)
    by_tkr   = ticker_stats(trades)
    by_exit  = exit_type_stats(trades)
    cumul    = index.get('cumulative', {})
    instr    = instrument_label(trades)
    day_name = fmt_weekday_full(session_date)

    if not trades:
        badge = '<span class="badge badge-none">No Trades</span>'
    elif stats['pnl'] >= 0:
        badge = f'<span class="badge badge-pos">{pnl_fmt(stats["pnl"])} Day</span>'
    else:
        badge = f'<span class="badge badge-neg">{pnl_fmt(stats["pnl"])} Day</span>'

    header = f"""<div class="header">
  <div>
    <h1><span class="augur-badge">AUGUR</span>Daily Report {badge}</h1>
    <div class="meta">{day_name}</div>
  </div>
  <div class="nav"><a href="{HUB_REL}">← Templum</a></div>
</div>"""

    # ── KPI Grid ───────────────────────────────────────────────────────────────
    if trades:
        wr_str = f"{stats['win_rate']*100:.1f}%"
        kpis = f"""<div class="kpi-grid">
  {kpi_card("Net P&L",       pnl_fmt(stats['pnl']),       pnl_class(stats['pnl']),   f"{stats['trades']} trades")}
  {kpi_card("Gross P&L",     pnl_fmt(stats['gross_pnl']), pnl_class(stats['gross_pnl']), "before commissions")}
  {kpi_card("Commission",    f"−${stats['commission']:.2f}", 'red', f"{stats['trades']} trades")}
  {kpi_card("Win Rate",      wr_str, 'green' if stats['win_rate']>=.7 else 'yellow', f"{stats['wins']}W / {stats['losses']}L")}
  {kpi_card("Profit Factor", pf_fmt(stats['profit_factor']), 'green' if stats['profit_factor']>1.5 else ('yellow' if stats['profit_factor']>1 else 'red'), "Target: >1.5")}
  {kpi_card("Avg Win",       f"${stats['avg_win']:.2f}",   'green')}
  {kpi_card("Avg Loss",      f"${stats['avg_loss']:.2f}",  'red',   "Target: <$100")}
  {kpi_card("R:R Ratio",     rr_fmt(stats['rr']),          'green' if stats['rr']>=1 else 'red', "Target: >1.0")}
  {kpi_card("Longs",  f"{stats['long_wins']}/{stats['long_trades']}",  pnl_class(stats['long_pnl']),  pnl_fmt(stats['long_pnl']))}
  {kpi_card("Shorts", f"{stats['short_wins']}/{stats['short_trades']}", pnl_class(stats['short_pnl']), pnl_fmt(stats['short_pnl']))}
</div>"""
    else:
        kpis = ('<div class="callout callout-yellow">'
                '<h3>No trades recorded for this session</h3>'
                '<p>Import an NT8 Executions export CSV containing trades for this date.</p>'
                '</div>')

    # ── Account Breakdown ──────────────────────────────────────────────────────
    acct_section = ''
    if by_acct:
        cards_html = ''
        for acct, s in sorted(by_acct.items()):
            label   = get_account_label(acct)
            pnl_cls = pnl_class(s['pnl'])
            wr_cls  = 'green' if s['win_rate'] >= 0.7 else 'yellow'
            cards_html += f"""<div class="acct-card">
  <h3>{acct} ({label})</h3>
  <div class="acct-stat"><span class="k">Trades</span><span>{s['trades']}</span></div>
  <div class="acct-stat"><span class="k">Win Rate</span><span class="{wr_cls}">{s['win_rate']*100:.0f}% ({s['wins']}W/{s['losses']}L)</span></div>
  <div class="acct-stat"><span class="k">Net P&L</span><span class="{pnl_cls}">{pnl_fmt(s['pnl'])}</span></div>
  <div class="acct-stat"><span class="k">Gross P&L</span><span class="{pnl_class(s['gross_pnl'])}">{pnl_fmt(s['gross_pnl'])}</span></div>
  <div class="acct-stat"><span class="k">Commission</span><span class="red">−${s['commission']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Avg Win</span><span class="green">${s['avg_win']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Avg Loss</span><span class="red">${s['avg_loss']:.2f}</span></div>
  <div class="acct-stat"><span class="k">Profit Factor</span><span class="{pnl_cls}">{pf_fmt(s['profit_factor'])}</span></div>
</div>"""
        col = 'two-col' if len(by_acct) == 2 else ('three-col' if len(by_acct) >= 3 else '')
        acct_section = (f'<div class="section"><h2>Account Breakdown</h2>'
                        f'<div class="{col}">{cards_html}</div></div>')

    # ── Symbol Breakdown ───────────────────────────────────────────────────────
    symbol_section = ''
    if len(by_tkr) > 1:
        tkr_cards = ''
        for tkr, s in by_tkr.items():
            pnl_cls = pnl_class(s['pnl'])
            wr_cls  = 'green' if s['win_rate'] >= 0.7 else 'yellow'
            tkr_cards += f"""<div class="acct-card">
  <h3>{tkr}</h3>
  <div class="acct-stat"><span class="k">Trades</span><span>{s['trades']}</span></div>
  <div class="acct-stat"><span class="k">Win Rate</span><span class="{wr_cls}">{s['win_rate']*100:.0f}% ({s['wins']}W/{s['losses']}L)</span></div>
  <div class="acct-stat"><span class="k">Net P&L</span><span class="{pnl_cls}">{pnl_fmt(s['pnl'])}</span></div>
  <div class="acct-stat"><span class="k">Profit Factor</span><span class="{pnl_cls}">{pf_fmt(s['profit_factor'])}</span></div>
</div>"""
        col = 'two-col' if len(by_tkr) == 2 else ('three-col' if len(by_tkr) >= 3 else '')
        symbol_section = (f'<div class="section"><h2>Symbol Breakdown</h2>'
                          f'<div class="{col}">{tkr_cards}</div></div>')

    # ── Trade Log Table ────────────────────────────────────────────────────────
    trade_table = ''
    if trades:
        rows = ''
        for i, t in enumerate(sorted(trades, key=lambda x: x['open_dt']), 1):
            row_cls  = ('win-row' if t['result'] == 'WIN'
                        else ('loss-row' if t['result'] == 'LOSS' else 'flat-row'))
            dir_tag  = (f'<span class="tag tag-{"long" if t["direction"]=="Long" else "short"}">'
                        f'{t["direction"]}</span>')
            res_tag  = (f'<span class="tag tag-{t["result"].lower()}">{t["result"]}</span>')
            et_tag   = exit_type_tag(t['exit_type'])
            pnl_col  = f'<span class="{pnl_class(t["pnl"])}">{pnl_fmt(t["pnl"])}</span>'
            comm_col = f'<span class="red">−${t["commission"]:.2f}</span>' if t['commission'] else '<span class="muted">–</span>'
            acct_lbl = get_account_label(t['account'])
            instr_s  = t['instrument'].split()[0] if t['instrument'] else '?'
            rows += f"""<tr class="{row_cls}">
  <td>{i}</td>
  <td>{t['open_dt'].strftime('%H:%M')}</td>
  <td>{fmt_month_day(t['open_dt'].date())}</td>
  <td>{acct_lbl}</td>
  <td>{instr_s}</td>
  <td>{dir_tag}</td>
  <td>{t['qty']}</td>
  <td>{t['entry_price']:.2f}</td>
  <td>{t['duration']}</td>
  <td>{et_tag}</td>
  <td>{comm_col}</td>
  <td>{res_tag}</td>
  <td>{pnl_col}</td>
</tr>"""
        trade_table = f"""<div class="section"><h2>Trade Log – {len(trades)} Trade{'s' if len(trades)!=1 else ''}</h2>
<table><thead><tr>
  <th>#</th><th>Time</th><th>Date</th><th>Acct</th><th>Symbol</th><th>Dir</th>
  <th>Qty</th><th>Entry</th><th>Duration</th><th>Exit Type</th><th>Comm</th><th>Result</th><th>Net P&L</th>
</tr></thead><tbody>{rows}</tbody></table>
</div>"""

    # ── Breakdowns ─────────────────────────────────────────────────────────────
    breakdowns = ''
    if trades:
        dir_card = f"""<div class="card"><h3>Direction</h3>
  {bar_stat_row('Long',  stats['long_wins'],  stats['long_trades'],  stats['long_pnl'])}
  {bar_stat_row('Short', stats['short_wins'], stats['short_trades'], stats['short_pnl'])}
</div>"""

        exit_rows = ''.join(
            bar_stat_row(exit_type_tag(et), s['wins'], s['trades'], s['pnl'])
            for et, s in by_exit.items() if s['trades'] > 0
        )
        exit_card = f'<div class="card"><h3>Exit Type</h3>{exit_rows}</div>'

        breakdowns = f'<div class="section"><h2>Breakdowns</h2><div class="two-col">{dir_card}{exit_card}</div></div>'

    # ── Cumulative Context ─────────────────────────────────────────────────────
    cumul_section = ''
    if cumul.get('trades'):
        c = cumul
        cumul_section = f"""<div class="section"><h2>Cumulative Performance</h2>
<div class="kpi-grid">
  {kpi_card("All-Time P&L",  pnl_fmt(c.get('pnl',0)),        pnl_class(c.get('pnl',0)),   f"{c.get('trades',0)} trades")}
  {kpi_card("Win Rate",      f"{c.get('win_rate',0)*100:.1f}%",'green' if c.get('win_rate',0)>=.7 else 'yellow', f"{c.get('wins',0)}W/{c.get('losses',0)}L")}
  {kpi_card("Profit Factor", pf_fmt(c.get('profit_factor',0)), 'green' if c.get('profit_factor',0)>1.5 else 'yellow')}
  {kpi_card("Avg Win",       f"${c.get('avg_win',0):.2f}",    'green')}
  {kpi_card("Avg Loss",      f"${c.get('avg_loss',0):.2f}",   'red')}
  {kpi_card("Total Comm",    f"−${c.get('commission',0):.2f}", 'red', "all-time drag")}
</div></div>"""

    year       = session_date.year
    instr_part = f' · {instr}' if instr else ''
    footer = (f'<div class="footer">Generated {now_stamp()} · AUGUR Trade Intelligence v{VERSION}{instr_part}'
              f' · <a href="{HUB_REL}" style="color:var(--accent)">← Templum</a>'
              f'<br>Copyright &copy; {year} GreyBeard Consulting &nbsp;·&nbsp; '
              f'<a href="{WEBSITE}" style="color:var(--accent)">{WEBSITE}</a></div>')

    body = '\n'.join([header, kpis, acct_section, symbol_section,
                      trade_table, breakdowns, cumul_section, footer])
    return page_wrap(f"AUGUR Daily – {session_date.strftime('%Y-%m-%d')}", body)


def generate_daily(session_date: date, all_trades: List[dict],
                   index: dict, reports_dir: Path) -> Path:
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
