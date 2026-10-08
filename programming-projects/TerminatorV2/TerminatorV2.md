# Terminator_V2 — Evaluation Report

**Source:** `NinjaScript/TerminatorV2/Terminator_V2.cs` (NT8, **v2.4.3**) —
kept byte-identical to `Documents\NinjaTrader 8\bin\Custom\Strategies\`.
Behaviour below was measured against the signal/window logic now in v2.4.3;
see §10 for the branch-merge history and §11 for the Playback certification
(entries-only logic confirmed; live P&L trails backtest ~25% on renko drift).
**Data:** 2024-12-16 → 2026-07-03 (510 calendar days, 400 in-session trading
days), tick-level L1 replay, MNQ.
**Python port:** `Python/backtester/strategies/terminator_v2.py` (base engine) ·
`.../terminator_rec.py` (recommended config below). Paths below that are not
repo-rooted are relative to `Python/backtester/`.
Tearsheet: `Python/backtester/reports/TerminatorRec_MNQ.html` (gitignored —
regenerate via *Reproduce*) · Evaluated 2026-07-09 (all times US/Eastern).
Committed trade lists / sweeps for these runs are in `backtest/` next to this
file; the NT8 template is in `templates/`.

## 1. What the strategy is

An **ATR trailing-stop stop-and-reverse**. A chandelier-style line trails
price at `ATRMult × ATR(ATRPeriod)`; a close crossing above the line
signals long, crossing below signals short. It is always-in by default —
no profit target, no protective stop; the opposite signal is the exit.
Reversals are "clean-split": flatten on the signal bar, re-enter once flat
(≤5 bars later, fresh signals override).

The C# implements far more (all off by default): VWMA gate/source, volume
filter, TP/SL in ATR/ticks/$/EMA modes, breakeven, three trail modes with an
arming trigger, dual time windows, daily loss/profit locks, risk-based
sizing, manual draggable brackets, and a dashboard. Code quality is high —
OCO-safe single-signal stop management, oversize/orphan guards, correct
historical→realtime P&L handling. The *edge*, however, lives entirely in the
signal line + the r100-4 bar geometry, which is what was tested.

On r100-4 bars (body 25 pts, closes 1 pt apart, reversal 49 pts) the closes
are extremely smoothed, so the SAR rides long trends and reverses only on
major turns.

## 2. Port fidelity

Ported exactly: line-update rules, cross detection, clean-split reversal
timing, cooldown, long/short enables, optional fixed SL/TP, dual entry-time
windows (`entry_window`/`entry_window2`, gate new entries only — exits
always manage). Not ported (all default-off in the source): VWMA/volume
filters, EMA/currency exit modes, breakeven, trail-trigger, risk sizing.

Known differences vs an NT8 run:
- **Session:** the raw port class (`terminator_v2.py`) defaults to plain
  RTH (09:30–16:55 ET) with EOD flatten. The recommended config below
  overrides this with a full Globex-trading-day session — see §3.
- **Renko:** built per the published ninZaRenko manual (body B, trend T,
  reversal 2B−T, open offset B−T); validated bit-identical OHLC against
  real NT8 chart exports (see `research/ninZaRenko_spec.md`).
- **Fills:** market orders fill at the real prevailing quote (spread paid);
  NT8 Renko backtests fill at synthetic prices. Our numbers should be the
  more honest of the two.

## 3. Recommended configuration & full-sample result

**Session:** full Globex trading day, `session=("18:00","16:55")` — one
continuous session spanning the prior evening's 18:00 ET open through the
same trading day's 16:55 ET flatten (before the next 17:00 ET halt). This
matches CME's own trading-day boundary and never holds a position through
any halt.

**Entries** (gate new entries only; exits/stops always manage, so a
position can carry from the evening leg into the next afternoon before the
16:55 flatten):
- Afternoon: **15:30–16:55 ET**
- Evening reopen: **18:00–22:55 ET**

**Signal:** ATR(28) × 3.25 SAR on ninZaRenko 100/4 (`r100-4`), 100-tick hard
stop, 1 contract.

| Metric | Value |
|---|---|
| Net P&L | **$22,422** (gross $23,456, commission $1,034) |
| Trades / win rate | 994 / 32.7% |
| Profit factor | 1.69 |
| Avg win / avg loss | $170 / −$49 |
| Sharpe / Sortino / Calmar | 3.89 / 11.09 / 9.50 |
| Max drawdown | −$1,487 (−2.55%) |
| Best / worst day | +$1,797 / −$479 |
| Consistency (largest day % of profit) | 8.0% |
| Prop-firm trailing threshold ($2,000 real Apex floor) | survived, min headroom **$677** |

Tested against the **real $2,000** Apex trailing drawdown (the project default
was corrected from $2,500 to $2,000 on 2026-07-09). The actual 400-day
sequence survives with $677 of headroom to spare; Monte Carlo breach
probability is in §4.

**2026-07-11 renko-fix re-verification:** a bar-construction bug was found
and fixed (see `strategy/GodZillaKilla.md` §2 for the full writeup) —
`build_renko_bars` was resetting its brick anchor at every midnight-ET
day-file boundary even though this strategy's overnight session trades
straight through midnight with no real gap. Re-ran the champion on the
corrected bars: net moved **$22,409 → $22,422** (+$13, +4 trades), headroom
**$678 → $677**, MC P(breach) **1.4% → 2.05%** (§4). The SAR is robust to
this level of bar perturbation — numbers above are now current/corrected.

**Compliance verified directly:** 0 of 990 trades have any entry or exit
timestamp inside the 17:00–18:00 ET daily maintenance halt.

**Minimum-hold exposure:** 110 of 990 trades (11.1%) close in under 30
seconds, which Apex does not count as valid trades. Those sub-30s trades are
collectively **−$4,211** — a net drag, not a hidden edge — so enforcing the
rule does not remove profit (see §7 item 2 and CLAUDE.md).

## 4. Monte Carlo (2,000 resamples, iid resampling)

Re-run on the renko-fix-corrected trades (2026-07-11):

| | Value |
|---|---|
| Final P&L 5% / median / 95% | $14,639 / $22,278 / $29,791 |
| P(profitable) | 100% |
| Max drawdown median / 5%-worst | −$1,521 / −$2,294 |
| **P(breach $2,000 trailing)** | **2.05%** (was 1.4% pre-fix) |

`P(pass $3,000 eval)` not re-run this pass; expect ~97-98%, similar to the
pre-fix 98.7%.

## 5. Robustness

**Note (2026-07-11):** the sub-sections below (split-half, parameter
sensitivity, slippage stress, walk-forward) were computed on the pre-renko-
fix bars and have not been individually re-run. Given the corrected champion
moved only $22,409 → $22,422 (+0.06%) and MC breach probability moved a
modest 1.4% → 2.05%, these are expected to shift by a similarly small
amount — but re-run before citing exact figures from this section if
precision matters.

- **Split-half:** H1 (Dec 2024–Sep 2025) net $8,465, Sharpe 2.89; H2 (Oct
  2025–Jul 2026) net $13,848, Sharpe 4.84 — both halves profitable,
  stronger in the recent half (consistent with every other cut of this
  data checked in this project).
- **`atr_mult`:** genuine interior peak at 3.0–3.25 (Sharpe 3.7–3.74).
  Checked down to 2.0 (worse, Sharpe ~3.2) and up to 4.5 (much worse,
  Sharpe ~2.3) to confirm this is a real plateau, not a grid-edge artifact.
- **`atr_period`:** 20–60 all cluster Sharpe 3.65–3.81 — a free parameter.
- **Entry-window boundaries:** afternoon start 14:30–16:00 ET and
  evening-leg end 21:55–23:55 ET are both broad plateaus, not knife-edges.
- **Slippage:** the headline pays the spread but no extra slippage. Adding
  1 / 2 ticks of slippage on every market and stop fill (~1,980 fills):

  | extra slippage | net | Sharpe | max DD | $2,000 headroom |
  |---|---|---|---|---|
  | 0 ticks (base) | $22,409 | 3.90 | −$1,488 | $678 |
  | 1 tick | $20,722 | 3.66 | −$1,512 | $618 |
  | 2 ticks | $20,077 | 3.56 | −$1,537 | $591 |

  Still profitable, high-Sharpe, and non-breaching at 2 ticks — the edge is
  not spread-fragile.

## 6. Walk-forward (5 windows, IS/OOS 5:1)

Grid: `atr_mult` ∈ {2.75, 3.0, 3.25, 3.5} × `atr_period` ∈ {20, 28} ×
`sl_ticks` ∈ {100, 125, 150}, session + entry-window structure held fixed.

| win | IS Sharpe | OOS Sharpe | OOS net | best params |
|---|---|---|---|---|
| 1 | 3.54 | 3.40 | $1,834 | mult 3.25 / period 28 / sl 150 |
| 2 | 3.53 | 4.39 | $2,173 | mult 3.25 / period 28 / sl 100 |
| 3 | 3.95 | 3.07 | $1,260 | mult 3.25 / period 28 / sl 100 |
| 4 | 3.42 | 7.68 | $4,479 | mult 3.25 / period 28 / sl 100 |
| 5 | 4.68 | 4.04 | $3,463 | mult 3.25 / period 28 / sl 100 |

**Stitched OOS: net $13,034 over 203 unseen days, Sharpe 4.45. Walk-forward
efficiency 1.18** (OOS beats IS on average — strong evidence against
curve-fit). **5/5 windows profitable.** Every window independently
converged on `atr_mult`=3.25/`atr_period`=28 (only `sl_ticks` varied,
itself a plateau) — the recommended config uses these walk-forward
converged params rather than the marginally higher full-sample-only peak
(mult=3.0/period=20, Sharpe 3.69 full-sample vs 3.90 for the WF pick).

## 7. Verdict

This is the strongest, most thoroughly validated config found in this
project: real interior parameter plateaus (not edges), both data halves
profitable and improving, 5/5 walk-forward OOS windows profitable with
OOS beating IS, and directly verified compliance (never holds through the
daily halt).

Both prior open items are now resolved; one live-account question remains
(item 2, for the user to confirm with Apex):
1. ~~Re-run against the real $2,000 Apex trailing threshold~~ **DONE
   (2026-07-09, re-verified 2026-07-11 post renko-fix):** survives the
   actual sequence with $677 headroom; MC P(breach $2,000) = 2.05%. Still
   comfortably safe.
2. **30-second minimum trade duration** — modeled and measured
   (2026-07-09, pre-renko-fix; not re-run, expect a negligible shift).
   Enforcing the rule (deferring the strategy's own reversal
   exits until the position is 30s old; engine `min_hold_s=30`) moves the
   headline by only **−$78 (0.3%)**: net $22,331, Sharpe 3.89, same max
   drawdown, still survives ($678 headroom). **The edge does not depend on
   sub-30s exits** (they were −$4,211 anyway). Caveat: even with enforcement
   ~101 trades still close sub-30s because those are **hard stop-outs** (the
   100-tick stop), which are not deferred — whether Apex voids a sub-30s
   *stop* fill (vs a manual quick close) is a rule question worth confirming
   with them. terminator_rec itself keeps `min_hold_s=0` to stay bit-for-bit
   with the NT8 port (which has no 30s logic); the $78 figure is the cost of
   compliance, not a change to the recommended config.

## 8. NT8 settings

**Requires Terminator_V2 v2.4.3+** (the second time window and the *Time
Filter Entries Only* mode — see §9; earlier versions cannot reproduce this
config, they lose 28% of P&L or breach the floor). Do **not** go looking for
a "v2.4.2" to install — see §10.

- **Session template** spanning **18:00 ET → 16:55 ET next day**, set to
  flatten positions / cancel orders at session end. **This is what makes the
  strategy flat by 16:55** — it is the trading-day boundary, NOT a time
  filter, and it is independent of the entry windows below.
- **Use Time Filter = true**, **Time Filter Entries Only = true**,
  **Flatten At Window End = false**.
- **Two entry windows, each bounded by the 16:55 close** (a single window may
  never span the close):
  - Time Filter 1: **Start 153000, End 165500** (afternoon)
  - Use Time Filter 2 = true, Time Filter 2: **Start 180000, End 225500** (evening reopen)
- SL Mode = Ticks, Value = **100**. ATR **28** / Mult **3.25**. 1 contract.

## 9. Port-fidelity: why entries-only mode is required

The recommended config depends on **entries-only** window semantics: the
window blocks new entries, but an opposite SAR signal still exits the live
position, and a position may carry across the out-of-window gap until a
signal / hard stop / session flatten. The two window modes the C# had before
entries-only existed were measured against this over the full 510 days
($2,000 floor):

| Window semantics | Net | Sharpe | max DD | $2,000 floor |
|---|---|---|---|---|
| **entries-only** (v2.4.3, recommended) | $22,409 | 3.90 | −$1,488 | survives ($678) |
| FlattenAtEnd=true (force-flat at window end) | $16,146 | 3.23 | −$1,530 | survives ($808) |
| FlattenAtEnd=false (reversal exit blocked out of window) | $21,907 | 3.00 | −$2,003 | **BREACHES (−$26)** |

FlattenAtEnd=true throws away the overnight/morning carry (−$6,263, −28%);
FlattenAtEnd=false keeps the P&L but holds through reversal signals outside
the window, deepening drawdowns until it breaches. The *Time Filter Entries
Only* mode gates entries while letting the reversal exit always fire and
disabling the window-end flatten — reproducing the entries-only column. In
v2.4.3 it suppresses **both** windows' flatten flags (`Flatten At Window End`
and `Flatten At Window 2 End`), and both are hidden in the property grid
while it is on.

**Flat at 16:55 vs the entry windows — two separate things.** Being flat by
16:55 every trading day is enforced by the **session template** (flatten at
session end), not by the entry windows. The entry windows only gate *entries*
and each must stay on one side of the close: **15:30–16:55** and
**18:00–22:55** (never a single window spanning 16:55). The entries-only
carry still ends at the session flatten (16:55) — an evening entry can ride
to the next afternoon but is closed at 16:55, so the position never crosses
the close either. The second window exists so the two blocks can be set
without one window crossing 16:55.

## 10. Branch-merge history (v2.4.3, 2026-07-23)

The .cs had **forked into two lineages that each shipped a "v2.4.1" for
different changes** — which is why an earlier revision of this file told you
to install a "v2.4.2" that was never in NT8:

| | `NinjaScript/TerminatorV2/` (live) | `Python/backtester/nt8 code/Terminatorv2/` (deleted) |
|---|---|---|
| Its "v2.4.1" meant | manual live brackets + 2nd time window | the carried-position Day-PnL fix |
| Version string | 2.4.1 | 2.4.2 |
| `TimeFilterEntriesOnly` | absent | present |
| `EnsureCarriedBaseline` | absent | present |
| Manual brackets / dashboard | present | absent |

Neither was a superset. **v2.4.3 merges the two backtester-side features into
the live line** (the newer, larger one), because losing the manual-bracket and
dashboard work to regain entries-only would have been the worse trade:

1. `TimeFilterEntriesOnly` — property, the `ShouldFlattenAtWindowEnd()`
   suppression, the `EntryBlockReason(dir, ignoreWindow)` overload, and the
   clean-split reversal call site. Adapted on merge: the live line has an
   independent flatten flag *per window*, so entries-only overrides both
   (the old 2.4.2 had a single shared flag).
2. `EnsureCarriedBaseline` — the carried-position unrealized baseline is now
   captured at the first realtime evaluation instead of inside
   `OnStateChange(Realtime)`, where `GetUnrealizedProfitLoss()` can return
   0/stale and leak historical P&L into live Day PnL. Matters here because
   the recommended config routinely carries a position across that boundary.
   A discarded carried position now contributes exactly $0.

**Going forward:** everything Terminator — the `.cs`, these reports, NT8
templates, and backtest artifacts — lives in **`NinjaScript/TerminatorV2/`**.
`Terminator_V2.cs` there is authoritative and is kept byte-identical to the
NT8 `bin\Custom\Strategies\` copy (that path is a deploy target, not a second
home). The duplicate `.cs` under `Python/backtester/nt8 code/Terminatorv2/`
was **deleted on 2026-07-23** — it was never a passive snapshot, it took three
real commits of feature development (`91ad407`, `11724d0`, `67748b1`) while
the live line was independently gaining manual brackets, which is exactly how
two different changes both ended up called v2.4.1. Its contents are recoverable
from git history; its NT8 template was rescued to `templates/`. Do not
re-create a working copy of this `.cs` anywhere else, and do not reuse the
version numbers 2.4.1 or 2.4.2.

**Status:** v2.4.3 compiles clean (Roslyn) **and is now Playback-certified** —
see §11.

## 11. Playback certification (v2.4.3, 2026-07-27)

Ran the §8 recommended config in NT8 Market-Replay Playback on **MNQ,
2026-06-21 → 2026-07-17** (22 trading days) and compared the executions export
to the Python champion (`terminator_rec.py`, same window) via
`tools/convert_nt8_executions.py` → `tools/compare_nt8.py --tz
America/New_York --tolerance-s 120`.

**Entries-only logic — CERTIFIED.**
- **0 of 122 NT8 entries fell outside the windows.** (The pre-fix run, before
  entries-only was configured correctly, had **474** out-of-window entries —
  so this directly confirms the `TimeFilterEntriesOnly` gate works.)
- **115 of 120 Python trades matched** an NT8 trade (same direction/qty, entry
  within tolerance). 5 only-ours, 7 only-nt8.
- **Median entry delta +1 tick, median exit delta −1 tick** — the typical
  trade reproduces tick-exact.
- The evening→overnight **carry trades appear and match**, confirming the
  entries-only carry end-to-end.

**P&L fidelity — a real caveat, not a bug.** Gross P&L over the window:

| | trades | gross |
|---|---|---|
| Python model | 120 | **$2,293** |
| NT8 Playback | 122 | **$1,721** |
| difference | | **−$572 (−25%)** |

The gap is **ninZaRenko live-accumulation drift** — Playback builds bricks
live (tick-by-tick, with reconnect re-anchor offsets that are *never* a
multiple of T and irreproducible by any backtest; see CLAUDE.md renko notes).
It is **not a logic bug**: deltas are random-signed, the median is 1 tick, and
the gap is dominated by a few drift outliers (one exit filled **488 ticks**
late, one entry **156 ticks** off) plus the 12 unmatched trades where the
diverging brick streams produced structurally different signals. Entry deltas
mean +2.4 tk / exit mean −8.66 tk (outlier-pulled).

**Takeaway:** the backtest figures ($22,422 net, $677 floor headroom, 2.0% MC
breach) are a **ceiling, not an expectation** — live renko drift shaved 25% off
gross in this 4-week sample, so treat floor headroom / MC breach as
*optimistic* vs live, and don't size up on backtest headroom until live drift
is observed over more than one window. One 4-week sample doesn't fix the drift
magnitude (it's random and could average nearer zero, or this window could be
lucky), but the direction is firm: **live < backtest, by enough to respect.**

**Scope note for gbTerminator:** this certifies the *logic*, which gbTerminator
(the GreyBeard fork) shares. It does **not** cover gbTerminator's order-name
rename (`TtLong`→`GbtLong`) — this replay used the old `Tt*` names. Confirm
stops attach on the gbTerminator account separately (live-observed working as
of 2026-07-27).

## Reproduce

Run from `C:\Dev\programming-projects\Python\backtester\`:

```powershell
.venv\Scripts\python cli.py strategies\terminator_rec.py --mc-target 3000
.venv\Scripts\python walkforward.py strategies\terminator_rec.py --param atr_mult=2.75,3.0,3.25,3.5 --param atr_period=20,28 --param sl_ticks=100,125,150
```
