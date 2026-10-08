"""
MONARCH Intelligence Report System – Shared Templates & HTML Helpers
=====================================================================
CSS theme, page wrapper, and reusable component functions used by
daily_report.py, weekly_report.py, and hub.py.
"""

from datetime import datetime


# ── CSS theme (identical visual style, MONARCH branding) ─────────────────────

CSS = """
  :root {
    --bg:#0f1117; --surface:#1a1d27; --surface2:#22263a; --border:#2e3250;
    --text:#e2e6f3; --muted:#7b82a6; --green:#22c55e; --red:#ef4444;
    --yellow:#f59e0b; --blue:#60a5fa; --purple:#a78bfa; --accent:#6366f1; --teal:#2dd4bf;
  }
  *{box-sizing:border-box;margin:0;padding:0}
  body{background:var(--bg);color:var(--text);font-family:'Segoe UI',system-ui,sans-serif;
       font-size:14px;padding:24px;max-width:1280px;margin:0 auto}
  a{color:inherit;text-decoration:none} a:hover{opacity:.8}

  .header{border-bottom:1px solid var(--border);padding-bottom:16px;margin-bottom:24px;
          display:flex;justify-content:space-between;align-items:flex-start}
  .header h1{font-size:22px;font-weight:700}
  .header .meta{color:var(--muted);font-size:12px;margin-top:5px}
  .nav{font-size:12px;color:var(--muted);text-align:right}
  .nav a{color:var(--accent)}
  .badge{display:inline-block;padding:2px 8px;border-radius:4px;font-size:11px;
         font-weight:600;margin-left:8px}
  .badge-pos{background:rgba(34,197,94,.15);color:var(--green);border:1px solid rgba(34,197,94,.3)}
  .badge-neg{background:rgba(239,68,68,.15);color:var(--red);border:1px solid rgba(239,68,68,.3)}
  .badge-info{background:rgba(96,165,250,.15);color:var(--blue);border:1px solid rgba(96,165,250,.3)}
  .badge-none{background:rgba(123,130,166,.1);color:var(--muted);border:1px solid rgba(123,130,166,.2)}

  .kpi-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:12px;margin-bottom:24px}
  .kpi{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:14px 16px}
  .kpi .label{font-size:11px;color:var(--muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:6px}
  .kpi .value{font-size:22px;font-weight:700}
  .kpi .sub{font-size:11px;color:var(--muted);margin-top:3px}

  .section{margin-bottom:28px}
  .section h2{font-size:13px;font-weight:700;color:var(--muted);text-transform:uppercase;
              letter-spacing:.07em;margin-bottom:12px;padding-bottom:8px;border-bottom:1px solid var(--border)}

  table{width:100%;border-collapse:collapse;background:var(--surface);border-radius:8px;
        overflow:hidden;border:1px solid var(--border);font-size:12.5px}
  th{background:var(--surface2);color:var(--muted);font-size:10.5px;text-transform:uppercase;
     letter-spacing:.05em;padding:9px 12px;text-align:left;font-weight:600}
  td{padding:9px 12px;border-top:1px solid var(--border);vertical-align:middle}
  tr.win-row  td:first-child{border-left:3px solid var(--green)}
  tr.loss-row td:first-child{border-left:3px solid var(--red)}
  tr.flat-row td:first-child{border-left:3px solid var(--muted)}

  .tag{display:inline-block;padding:1px 7px;border-radius:4px;font-size:11px;font-weight:600}
  .tag-win  {background:rgba(34,197,94,.12);color:var(--green)}
  .tag-loss {background:rgba(239,68,68,.12);color:var(--red)}
  .tag-long {background:rgba(96,165,250,.12);color:var(--blue)}
  .tag-short{background:rgba(167,139,250,.12);color:var(--purple)}
  .tag-ko   {background:rgba(245,158,11,.12);color:var(--yellow)}
  .tag-g3   {background:rgba(45,212,191,.12);color:var(--teal)}
  .tag-g4   {background:rgba(99,102,241,.12);color:#a5b4fc}
  .tag-g5   {background:rgba(99,102,241,.2);color:#818cf8;border:1px solid rgba(99,102,241,.35)}

  .two-col{display:grid;grid-template-columns:1fr 1fr;gap:14px}
  .three-col{display:grid;grid-template-columns:1fr 1fr 1fr;gap:14px}
  @media(max-width:800px){.two-col,.three-col{grid-template-columns:1fr}}

  .card{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:16px}
  .card h3{font-size:11px;color:var(--muted);text-transform:uppercase;letter-spacing:.06em;
           margin-bottom:12px;font-weight:700}
  .stat-row{display:flex;justify-content:space-between;align-items:center;padding:6px 0;
            border-bottom:1px solid var(--border);font-size:12.5px}
  .stat-row:last-child{border-bottom:none}
  .bar-wrap{flex:1;height:5px;background:var(--surface2);border-radius:3px;margin:0 10px}
  .bar{height:5px;border-radius:3px}

  .rec{background:var(--surface);border:1px solid var(--border);border-radius:8px;
       padding:14px 16px;margin-bottom:10px}
  .rec.critical{border-left:3px solid var(--red)}
  .rec.warn    {border-left:3px solid var(--yellow)}
  .rec.positive{border-left:3px solid var(--green)}
  .rec.info    {border-left:3px solid var(--blue)}
  .rec .priority{font-size:10px;font-weight:700;text-transform:uppercase;
                 margin-bottom:5px;letter-spacing:.06em}
  .rec .rec-title{font-weight:600;font-size:13px;margin-bottom:5px}
  .rec .rec-body {font-size:12px;color:var(--muted);line-height:1.65}

  /* Calendar */
  .cal-wrap{overflow-x:auto;margin-bottom:24px}
  .cal-grid{display:grid;grid-template-columns:52px repeat(5,1fr) 100px;gap:4px;min-width:600px}
  .cal-head{font-size:10px;font-weight:700;color:var(--muted);text-transform:uppercase;
            text-align:center;padding:6px 2px}
  .cal-wlabel{font-size:10px;color:var(--muted);display:flex;align-items:center;
              justify-content:flex-end;padding-right:6px}
  .cal-cell{background:var(--surface);border:1px solid var(--border);border-radius:6px;
            padding:8px 6px;text-align:center;min-height:62px;display:flex;
            flex-direction:column;justify-content:center}
  .cal-cell.clickable{cursor:pointer;transition:border-color .15s}
  .cal-cell.clickable:hover{border-color:var(--accent)}
  .cal-cell.today{border-color:var(--accent);background:rgba(99,102,241,.06)}
  .cal-cell.no-trade{opacity:.45}
  .cal-cell.future{opacity:.2}
  .cal-date{font-size:10px;color:var(--muted);margin-bottom:3px}
  .cal-pnl {font-size:14px;font-weight:700}
  .cal-sub {font-size:10px;color:var(--muted);margin-top:2px}
  .cal-wtotal{background:var(--surface2);border:1px solid var(--border);border-radius:6px;
              padding:8px 10px;display:flex;flex-direction:column;justify-content:center}
  .cal-wtotal .wt-label{font-size:10px;color:var(--muted);text-transform:uppercase;margin-bottom:2px}
  .cal-wtotal .wt-pnl{font-size:15px;font-weight:700}
  .cal-wtotal .wt-sub{font-size:10px;color:var(--muted);margin-top:2px}

  .green{color:var(--green)} .red{color:var(--red)} .yellow{color:var(--yellow)}
  .blue{color:var(--blue)}   .teal{color:var(--teal)} .muted{color:var(--muted)}

  .callout{border-radius:8px;padding:12px 16px;margin-bottom:20px}
  .callout-green {background:rgba(34,197,94,.07);border:1px solid rgba(34,197,94,.25)}
  .callout-green h3{color:var(--green);font-size:13px}
  .callout-green p{color:#86efac;font-size:12px;margin-top:5px;line-height:1.6}
  .callout-blue  {background:rgba(96,165,250,.07);border:1px solid rgba(96,165,250,.2)}
  .callout-blue h3{color:var(--blue);font-size:13px}
  .callout-blue p{color:#93c5fd;font-size:12px;margin-top:5px;line-height:1.6}
  .callout-yellow{background:rgba(245,158,11,.07);border:1px solid rgba(245,158,11,.2)}
  .callout-yellow h3{color:var(--yellow);font-size:13px}
  .callout-yellow p{color:#fcd34d;font-size:12px;margin-top:5px;line-height:1.6}

  .footer{border-top:1px solid var(--border);padding-top:14px;margin-top:24px;
          color:var(--muted);font-size:11px}
  code{background:var(--surface2);padding:1px 5px;border-radius:3px;font-size:11px}
  .acct-card{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:14px 16px}
  .acct-card h3{font-size:11px;color:var(--muted);font-weight:600;text-transform:uppercase;margin-bottom:10px}
  .acct-stat{display:flex;justify-content:space-between;font-size:12.5px;padding:4px 0}
  .acct-stat .k{color:var(--muted)}

  /* MONARCH header accent */
  .monarch-badge{display:inline-block;background:rgba(99,102,241,.2);color:#a5b4fc;
                 border:1px solid rgba(99,102,241,.4);border-radius:4px;
                 font-size:10px;font-weight:700;letter-spacing:.1em;
                 padding:2px 8px;margin-right:10px;vertical-align:middle}
"""


# ── Formatting helpers ────────────────────────────────────────────────────────

def pnl_fmt(v: float) -> str:
    return f"+${v:.2f}" if v >= 0 else f"−${abs(v):.2f}"


def pnl_class(v: float) -> str:
    return 'green' if v > 0 else ('red' if v < 0 else 'muted')


def pf_fmt(v: float) -> str:
    return "∞" if v == float('inf') else f"{v:.2f}"


def rr_fmt(v: float) -> str:
    return "∞" if v == float('inf') else f"{v:.2f}"


def grade_tag(g: str) -> str:
    return f'<span class="tag tag-{g.lower()}">{g}</span>'


# ── Component builders ────────────────────────────────────────────────────────

def page_wrap(title: str, body: str) -> str:
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>{title}</title>
<style>{CSS}</style>
</head>
<body>
{body}
</body>
</html>"""


def kpi_card(label: str, value: str, cls: str = '', sub: str = '') -> str:
    sub_html = f'<div class="sub">{sub}</div>' if sub else ''
    return (f'<div class="kpi">'
            f'<div class="label">{label}</div>'
            f'<div class="value {cls}">{value}</div>'
            f'{sub_html}</div>')


def bar_stat_row(label: str, wins: int, total: int, pnl: float) -> str:
    pct   = int(100 * wins / total) if total > 0 else 0
    color = 'var(--green)' if pnl >= 0 else 'var(--red)'
    return (f'<div class="stat-row">'
            f'<span>{label}</span>'
            f'<div class="bar-wrap"><div class="bar" style="width:{pct}%;background:{color}"></div></div>'
            f'<span class="{pnl_class(pnl)}">{wins}/{total} · {pnl_fmt(pnl)}</span>'
            f'</div>')


def now_stamp() -> str:
    return datetime.now().strftime('%Y-%m-%d %H:%M')
