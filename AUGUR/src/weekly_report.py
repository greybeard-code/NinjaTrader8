"""
AUGUR Trade Intelligence – Weekly Report Generator
====================================================
Builds weekly_YYYYMMDD.html for the Mon–Fri window ending on a given Friday.
"""

from datetime import date
from pathlib import Path
from typing import List

from config import get_account_label, VERSION, WEBSITE
from log_parser import (
    compute_stats, account_stats, ticker_stats,
    instrument_label, group_by_session, register_weekly,
)
from date_utils import week_dates, fmt_month_day, fmt_weekday_month_day
from templates import (
    page_wrap, kpi_card, bar_stat_row, exit_type_tag,
    pnl_fmt, pnl_class, pf_fmt, rr_fmt, now_stamp,
)

HUB_REL = '../Templum.html'


def build_weekly_html(friday: date, all_trades: List[dict],
                      index: dict, reports_dir: Path) -> str:
    days        = week_dates(friday)
    monday      = days[0]
    by_day      = group_by_session(all_trades)
    week_trades = []
    for d in days:
        week_trades.extend(by_day.get(d, []))

    stats   = compute_stats(week_trades)
    by_acct = account_stats(week_trades)
    by_tkr  = ticker_stats(week_trades)
    instr   = instrument_label(week_trades)

    date_range = f"{fmt_month_day(monday)} – {fmt_month_day(friday)}, {friday.year}"

    if not week_trades:
        badge = '<span class="badge badge-none">No Trades</span>'
    elif stats['pnl'] >= 0:
        badge = f'<span class="badge badge-pos">{pnl_fmt(stats["pnl"])} Week</span>'
    else:
        badge = f'<span class="badge badge-neg">{pnl_fmt(stats["pnl"])} Week</span>'

    header = f"""<div class="header">
  <div>
    <h1><span class="augur-badge">AUGUR</span>Weekly Summary {badge}</h1>
    <div class="meta">Week of {date_range}</div>
  </div>
  <div class="nav"><a href="{HUB_REL}">← Templum</a></div>
</div>"""

    if week_trades:
        kpis = f"""<div class="kpi-grid">
  {kpi_card("Week P&L",      pnl_fmt(stats['pnl']),        pnl_class(stats['pnl']),   f"{stats['trades']} trades")}
  {kpi_card("Gross P&L",     pnl_fmt(stats['gross_pnl']), pnl_class(stats['gross_pnl']), "before commissions")}
  {kpi_card("Commission",    f"−${stats['commission']:.2f}", 'red')}
  {kpi_card("Win Rate",      f"{stats['win_rate']*100:.1f}%", 'green' if stats['win_rate']>=.7 else 'yellow', f"{stats['wins']}W / {stats['losses']}L")}
  {kpi_card("Profit Factor", pf_fmt(stats['profit_factor']), 'green' if stats['profit_factor']>1.5 else ('yellow' if stats['profit_factor']>1 else 'red'), "Target: >1.5")}
  {kpi_card("Avg Win",       f"${stats['avg_win']:.2f}",    'green')}
  {kpi_card("Avg Loss",      f"${stats['avg_loss']:.2f}",   'red')}
  {kpi_card("R:R",           rr_fmt(stats['rr']),           'green' if stats['rr']>=1 else 'red', "Target: >1.0")}
  {kpi_card("Longs",  f"{stats['long_wins']}/{stats['long_trades']}",  pnl_class(stats['long_pnl']),  pnl_fmt(stats['long_pnl']))}
  {kpi_card("Shorts", f"{stats['short_wins']}/{stats['short_trades']}", pnl_class(stats['short_pnl']), pnl_fmt(stats['short_pnl']))}
</div>"""
    else:
        kpis = ('<div class="callout callout-yellow">'
                '<h3>No trades this week</h3>'
                '<p>No imported NT8 data found for this period.</p>'
                '</div>')

    # ── Session Breakdown table ────────────────────────────────────────────────
    day_rows = ''
    for d in days:
        day_trades = by_day.get(d, [])
        s     = compute_stats(day_trades)
        dname = fmt_weekday_month_day(d)
        fname = f"daily_{d.strftime('%Y%m%d')}.html"
        has_report = (reports_dir / fname).exists()

        if not day_trades:
            day_rows += (f'<tr class="flat-row">'
                         f'<td>{"<a href=" + repr(fname) + " style=color:var(--accent)>" if has_report else ""}'
                         f'{dname}'
                         f'{"</a>" if has_report else ""}</td>'
                         f'<td class="muted">–</td><td class="muted">–</td>'
                         f'<td class="muted">–</td><td class="muted">–</td>'
                         f'<td class="muted">–</td><td class="muted">No trades</td></tr>')
        else:
            pnl_cls = pnl_class(s['pnl'])
            accts   = ', '.join(sorted({get_account_label(t['account']) for t in day_trades}))
            link_o  = f'<a href="{fname}" style="color:var(--accent)">' if has_report else ''
            link_c  = '</a>' if has_report else ''
            row_cls = 'win-row' if s['pnl'] >= 0 else 'loss-row'
            day_rows += (f'<tr class="{row_cls}">'
                         f'<td>{link_o}{dname}{link_c}</td>'
                         f'<td>{s["trades"]}</td>'
                         f'<td>{s["wins"]}/{s["losses"]}</td>'
                         f'<td class="{pnl_cls}">{pnl_fmt(s["pnl"])}</td>'
                         f'<td class="red">−${s["commission"]:.2f}</td>'
                         f'<td class="{pnl_cls}">{s["win_rate"]*100:.0f}%</td>'
                         f'<td class="muted">{accts}</td></tr>')

    day_table = f"""<div class="section"><h2>Session Breakdown</h2>
<table><thead><tr>
  <th>Day</th><th>Trades</th><th>W/L</th><th>Net P&L</th><th>Commission</th><th>Win %</th><th>Accounts</th>
</tr></thead><tbody>{day_rows}</tbody></table></div>"""

    # ── Biggest Wins / Losses ──────────────────────────────────────────────────
    winners_section = ''
    if week_trades:
        sorted_trades = sorted(week_trades, key=lambda t: t['pnl'], reverse=True)

        def mini_rows(ts, limit=3):
            rows = ''
            for t in ts[:limit]:
                d_str   = fmt_weekday_month_day(t['open_dt'].date()) + ' ' + t['open_dt'].strftime('%H:%M')
                pnl_cls = pnl_class(t['pnl'])
                dir_cls = 'long' if t['direction'] == 'Long' else 'short'
                acct_s  = get_account_label(t['account'])
                instr_s = t['instrument'].split()[0] if t['instrument'] else '?'
                rows += (f'<tr>'
                         f'<td style="font-size:11px">{d_str}</td>'
                         f'<td>{acct_s}</td>'
                         f'<td>{instr_s}</td>'
                         f'<td><span class="tag tag-{dir_cls}">{t["direction"]}</span></td>'
                         f'<td>{exit_type_tag(t["exit_type"])}</td>'
                         f'<td class="{pnl_cls}">{pnl_fmt(t["pnl"])}</td>'
                         f'</tr>')
            return rows or '<tr><td colspan="6" class="muted">None</td></tr>'

        wins_rows   = mini_rows(sorted_trades)
        losses_rows = mini_rows(list(reversed(sorted_trades)))
        col_heads   = '<th>Time</th><th>Acct</th><th>Symbol</th><th>Dir</th><th>Exit</th><th>P&L</th>'
        winners_section = f"""<div class="section two-col">
<div><h2>Biggest Wins</h2>
<table><thead><tr>{col_heads}</tr></thead><tbody>{wins_rows}</tbody></table></div>
<div><h2>Biggest Losses</h2>
<table><thead><tr>{col_heads}</tr></thead><tbody>{losses_rows}</tbody></table></div>
</div>"""

    # ── Direction Breakdown ────────────────────────────────────────────────────
    breakdowns = ''
    if week_trades:
        breakdowns = f"""<div class="section"><h2>Breakdowns</h2><div class="two-col">
<div class="card"><h3>Direction</h3>
  {bar_stat_row('Long',  stats['long_wins'],  stats['long_trades'],  stats['long_pnl'])}
  {bar_stat_row('Short', stats['short_wins'], stats['short_trades'], stats['short_pnl'])}
</div>
<div class="card"><h3>Exit Type (all trades)</h3>
  {_exit_type_breakdown(week_trades)}
</div></div></div>"""

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

    year       = friday.year
    instr_part = f' · {instr}' if instr else ''
    footer = (f'<div class="footer">Generated {now_stamp()} · AUGUR Trade Intelligence v{VERSION}{instr_part}'
              f' · <a href="{HUB_REL}" style="color:var(--accent)">← Templum</a>'
              f'<br>Copyright &copy; {year} GreyBeard Consulting &nbsp;·&nbsp; '
              f'<a href="{WEBSITE}" style="color:var(--accent)">{WEBSITE}</a></div>')

    body = '\n'.join([header, kpis, day_table, winners_section,
                      breakdowns, acct_section, symbol_section, footer])
    return page_wrap(f"AUGUR Weekly – {friday.strftime('%Y-%m-%d')}", body)


def _exit_type_breakdown(trades: List[dict]) -> str:
    from log_parser import exit_type_stats, compute_stats
    by_exit = exit_type_stats(trades)
    rows = ''
    for et, s in by_exit.items():
        if s['trades'] > 0:
            rows += bar_stat_row(exit_type_tag(et), s['wins'], s['trades'], s['pnl'])
    return rows or '<div class="muted" style="padding:8px 0">No data</div>'


def generate_weekly(friday: date, all_trades: List[dict],
                    index: dict, reports_dir: Path) -> Path:
    days        = week_dates(friday)
    by_day      = group_by_session(all_trades)
    week_trades = []
    for d in days:
        week_trades.extend(by_day.get(d, []))

    print(f"  Weekly {friday} ({len(week_trades)} trades)")
    html  = build_weekly_html(friday, all_trades, index, reports_dir)
    fname = f"weekly_{friday.strftime('%Y%m%d')}.html"
    out   = reports_dir / fname
    with open(out, 'w', encoding='utf-8') as f:
        f.write(html)
    register_weekly(index, friday, week_trades)
    print(f"  → {out}")
    return out
