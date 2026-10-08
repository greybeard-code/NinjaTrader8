"""
MONARCH Intelligence Report System – Castle Bravo Hub
======================================================
Generates CastleBravo.html – the command-centre index page.
Lives in MONARCH/ (one level above reports/).

Castle Bravo was the 1954 nuclear test MONARCH was secretly monitoring
when Godzilla first surfaced. It's the founding moment of the organisation.
"""

from datetime import date, timedelta
from pathlib import Path
from typing import List

from config import get_account_label, get_account_grade, HUB_FILE, VERSION, WEBSITE
from log_parser import compute_stats, account_stats, instrument_label, ticker_stats, group_by_session
from date_utils import fmt_month_day, fmt_weekday_month_day, get_friday_of_week
from templates import (
    page_wrap, kpi_card, pnl_fmt, pnl_class, pf_fmt, rr_fmt, now_stamp,
)


def build_hub_html(all_trades: List[dict], index: dict,
                   reports_dir: Path) -> str:
    """Generate the full CastleBravo.html string."""
    today    = date.today()
    cumul    = index.get('cumulative', {})
    by_day   = group_by_session(all_trades)
    recs     = index.get('recommendations', [])

    # ── Header ─────────────────────────────────────────────────────────────────
    instr       = instrument_label(all_trades)
    header = f"""<div class="header">
  <div>
    <h1><span class="monarch-badge">MONARCH</span>Castle Bravo – Intelligence Hub</h1>
    <div class="meta">Automated Surveillance Active</div>
  </div>
</div>"""

    # ── Cumulative KPIs ────────────────────────────────────────────────────────
    kpis = ''
    c = cumul
    if c.get('trades'):
        # Row 1: overall performance metrics
        overall_grid = f"""<div class="kpi-grid">
  {kpi_card("All-Time P&L",  pnl_fmt(c.get('pnl',0)),        pnl_class(c.get('pnl',0)),   f"{c.get('trades',0)} trades")}
  {kpi_card("Win Rate",      f"{c.get('win_rate',0)*100:.1f}%", 'green' if c.get('win_rate',0)>=.7 else 'yellow', f"{c.get('wins',0)}W / {c.get('losses',0)}L")}
  {kpi_card("Profit Factor", pf_fmt(c.get('profit_factor',0)), 'green' if c.get('profit_factor',0)>1.5 else ('yellow' if c.get('profit_factor',0)>1 else 'red'), "Target: >1.5")}
  {kpi_card("Avg Win",       f"${c.get('avg_win',0):.2f}",    'green')}
  {kpi_card("Avg Loss",      f"${c.get('avg_loss',0):.2f}",   'red',  "Target: <$100")}
  {kpi_card("R:R Ratio",     rr_fmt(c.get('rr',0)),           'green' if c.get('rr',0)>=1 else 'red', "Target: >1.0")}
</div>"""

        # Row 2: one card per account
        per_acct_cards = ''
        for acct, pa in sorted(c.get('per_account', {}).items()):
            label = get_account_label(acct)
            grade = get_account_grade(acct)
            title = f"{label} ({grade})" if grade != '?' else label
            wr    = pa.get('win_rate', 0)
            per_acct_cards += kpi_card(
                title,
                pnl_fmt(pa.get('pnl', 0)),
                pnl_class(pa.get('pnl', 0)),
                f"{pa.get('trades', 0)} trades · {wr*100:.0f}% win",
            )
        acct_grid = f'<div class="kpi-grid">{per_acct_cards}</div>' if per_acct_cards else ''

        # Row 3: one card per base ticker symbol
        by_tkr     = ticker_stats(all_trades)
        tkr_cards  = ''
        subhead    = '<h3 style="margin:16px 0 8px;font-size:13px;color:var(--muted);letter-spacing:.05em;text-transform:uppercase">'
        for tkr, s in by_tkr.items():
            tkr_cards += kpi_card(
                tkr,
                pnl_fmt(s['pnl']),
                pnl_class(s['pnl']),
                f"{s['trades']} trades · {s['win_rate']*100:.0f}% win",
            )
        tkr_grid = f'<div class="kpi-grid">{tkr_cards}</div>' if tkr_cards else ''

        kpis = f"""<div class="section"><h2>Cumulative Performance</h2>
{overall_grid}
{subhead + 'Account Totals</h3>' + acct_grid if acct_grid else ''}
{subhead + 'Symbol Totals</h3>' + tkr_grid if tkr_grid else ''}
</div>"""

    # ── Recommendations ────────────────────────────────────────────────────────
    recs_section = ''
    if recs:
        rec_html = ''
        emoji_map = {'critical': '🔴', 'warn': '🟡', 'positive': '🟢', 'info': '🔵'}
        label_map = {'critical': 'Critical', 'warn': 'Watch', 'positive': 'Finding', 'info': 'Info'}
        color_map = {'critical': 'red', 'warn': 'yellow', 'positive': 'green', 'info': 'blue'}
        for r in recs:
            cls = r.get('type', 'info')
            rec_html += f"""<div class="rec {cls}">
  <div class="priority" style="color:var(--{color_map.get(cls,'blue')})">{emoji_map.get(cls,'🔵')} {label_map.get(cls,'Info')}</div>
  <div class="rec-title">{r.get('title','')}</div>
  <div class="rec-body">{r.get('body','')}</div>
</div>"""
        recs_section = f'<div class="section"><h2>Current Recommendations</h2>{rec_html}</div>'

    # ── 4-Week Calendar ────────────────────────────────────────────────────────
    latest_friday  = get_friday_of_week(today)

    # Collect 4 weeks oldest→newest
    weeks = []
    for w in range(3, -1, -1):
        fri = latest_friday - timedelta(weeks=w)
        mon = fri - timedelta(days=4)
        weeks.append((mon, fri))

    cal = '<div class="section"><h2>4-Week Calendar</h2><div class="cal-wrap"><div class="cal-grid">'
    # Column headers
    cal += '<div class="cal-head"></div>'
    for dname in ['Mon', 'Tue', 'Wed', 'Thu', 'Fri']:
        cal += f'<div class="cal-head">{dname}</div>'
    cal += '<div class="cal-head">Week</div>'

    for idx, (mon, fri) in enumerate(weeks):
        cal += f'<div class="cal-wlabel">W{idx+1}</div>'

        week_pnl     = 0.0
        week_count   = 0
        week_has_data = False

        for day_offset in range(5):
            d          = mon + timedelta(days=day_offset)
            day_trades = by_day.get(d, [])
            is_today   = (d == today)
            is_future  = (d > today)
            fname      = f"reports/daily_{d.strftime('%Y%m%d')}.html"
            has_rpt    = (reports_dir / f"daily_{d.strftime('%Y%m%d')}.html").exists()
            d_lbl      = fmt_month_day(d)

            cell_cls = 'cal-cell'
            if is_today:
                cell_cls += ' today'
            elif is_future:
                cell_cls += ' future'

            if day_trades:
                s = compute_stats(day_trades)
                week_pnl    += s['pnl']
                week_count  += s['trades']
                week_has_data = True
                pnl_c = pnl_class(s['pnl'])
                cell_cls += ' clickable'
                onclick = f'onclick="location.href=\'{fname}\'"' if has_rpt else ''
                cal += (f'<div class="{cell_cls}" {onclick}>'
                        f'<div class="cal-date">{d_lbl}</div>'
                        f'<div class="cal-pnl {pnl_c}">{pnl_fmt(s["pnl"])}</div>'
                        f'<div class="cal-sub">{s["trades"]}T {s["wins"]}W/{s["losses"]}L</div>'
                        f'</div>')
            elif is_future:
                cal += (f'<div class="{cell_cls} future">'
                        f'<div class="cal-date">{d_lbl}</div>'
                        f'<div class="cal-pnl muted">–</div></div>')
            else:
                cal += (f'<div class="{cell_cls} no-trade">'
                        f'<div class="cal-date">{d_lbl}</div>'
                        f'<div class="cal-pnl muted">–</div></div>')

        # Week total cell
        wfname   = f"reports/weekly_{fri.strftime('%Y%m%d')}.html"
        has_wrpt = (reports_dir / f"weekly_{fri.strftime('%Y%m%d')}.html").exists()
        if week_has_data:
            pnl_c   = pnl_class(week_pnl)
            wonclick = f'onclick="location.href=\'{wfname}\'"' if has_wrpt else ''
            wtcls   = 'cal-wtotal clickable' if has_wrpt else 'cal-wtotal'
            cal += (f'<div class="{wtcls}" {wonclick}>'
                    f'<div class="wt-label">Week</div>'
                    f'<div class="wt-pnl {pnl_c}">{pnl_fmt(week_pnl)}</div>'
                    f'<div class="wt-sub">{week_count} trades</div>'
                    f'</div>')
        else:
            cal += '<div class="cal-wtotal"><div class="wt-label">Week</div><div class="wt-pnl muted">–</div></div>'

    cal += '</div></div></div>'  # close grid, wrap, section

    # ── Recent Sessions list ───────────────────────────────────────────────────
    recent_days = sorted(
        [d for d in by_day if by_day[d]],
        reverse=True
    )[:10]

    sessions_section = ''
    if recent_days:
        session_rows = ''
        for d in recent_days:
            day_trades = by_day[d]
            s      = compute_stats(day_trades)
            fname  = f"reports/daily_{d.strftime('%Y%m%d')}.html"
            has_rpt= (reports_dir / f"daily_{d.strftime('%Y%m%d')}.html").exists()
            dname  = fmt_weekday_month_day(d) + f", {d.year}"
            link_o = f'<a href="{fname}" style="color:var(--accent)">' if has_rpt else ''
            link_c = '</a>' if has_rpt else ''
            accts  = ', '.join(sorted({get_account_label(t['account']) for t in day_trades}))
            row_cls = 'win-row' if s['pnl'] >= 0 else 'loss-row'
            session_rows += (f'<tr class="{row_cls}">'
                             f'<td>{link_o}{dname}{link_c}</td>'
                             f'<td>{accts}</td>'
                             f'<td>{s["trades"]}</td>'
                             f'<td>{s["wins"]}/{s["losses"]}</td>'
                             f'<td class="{pnl_class(s["pnl"])}">{pnl_fmt(s["pnl"])}</td>'
                             f'<td>{s["win_rate"]*100:.0f}%</td>'
                             f'</tr>')
        sessions_section = f"""<div class="section"><h2>Recent Sessions</h2>
<table><thead><tr>
  <th>Date</th><th>Accounts</th><th>Trades</th><th>W/L</th><th>P&L</th><th>Win %</th>
</tr></thead><tbody>{session_rows}</tbody></table></div>"""

    # ── Footer ─────────────────────────────────────────────────────────────────
    year       = date.today().year
    instr_part = f' · {instr}' if instr else ''
    footer = (f'<div class="footer">Updated {now_stamp()} · MONARCH Intelligence Report System v{VERSION}{instr_part}'
              f'<br>Copyright &copy; {year} GreyBeard Consulting &nbsp;·&nbsp; '
              f'<a href="{WEBSITE}" style="color:var(--accent)">{WEBSITE}</a></div>')

    body = '\n'.join([header, kpis, recs_section, cal, sessions_section, footer])
    return page_wrap("MONARCH – Castle Bravo Intelligence Hub", body)


def generate_hub(all_trades: List[dict], index: dict,
                 monarch_dir: Path, reports_dir: Path) -> Path:
    """Build and write CastleBravo.html. Returns the output path."""
    print("  Generating CastleBravo.html")
    html = build_hub_html(all_trades, index, reports_dir)
    out  = monarch_dir / HUB_FILE
    with open(out, 'w', encoding='utf-8') as f:
        f.write(html)
    print(f"  → {out}")
    return out
