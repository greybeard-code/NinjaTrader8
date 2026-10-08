# Terminator_V2 — ETH Session & Time-Filter Analysis

**Purpose:** show where the strategy's edge actually lives across the full
Globex session, justifying the entry-window choice in the main report's
recommended config ([TerminatorV2.md](TerminatorV2.md)).

**Data:** 2024-12-16 → 2026-07-03 (510 days), tick-level L1 replay, MNQ
`r100-4` bars. Attribution run below uses ATR 20×4 / 200-tick stop (the
config used when this table was generated); the main report's recommended
config uses ATR 28×3.25 / 100-tick stop — this table is about *timing*,
not an exact P&L match to that config. Port: `strategies/terminator_eth.py`.
All times US/Eastern. Evaluated 2026-07-09.

## 1. Full ETH, no entry filter: still not tradeable as-is

Running the full Globex session (`session=("18:00","16:55")`) with **no**
entry-time filter: net **+$2,405** but **breached Apex 2025-03-10** — an
unfiltered 24-hour SAR is net positive on this data but its drawdown path
fails. Some entry-time filtering is required; §2 shows where to put it.

## 2. Net P&L by ET entry hour (full 510-day dataset)

`17:00` has no bars (CME daily maintenance halt).

| ET hr | net $ | trades | avg/tr | | ET hr | net $ | trades | avg/tr |
|---|---|---|---|---|---|---|---|---|
| 00 | −91 | 49 | −1.9 | | 12 | −638 | 248 | −2.6 |
| 01 | +801 | 43 | +18.6 | | 13 | −1,181 | 242 | −4.9 |
| 02 | +1,568 | 61 | +25.7 | | **14** | +279 | 214 | +1.3 |
| 03 | −2,032 | 80 | −25.4 | | **15** | **+3,880** | 254 | +15.3 |
| 04 | −442 | 85 | −5.2 | | 16 | +159 | 123 | +1.3 |
| 05 | −1,204 | 60 | −20.1 | | **18** | **+4,278** | 142 | +30.1 |
| 06 | +781 | 76 | +10.3 | | 19 | +1,144 | 63 | +18.2 |
| 07 | −1,379 | 118 | −11.7 | | 20 | +1,037 | 110 | +9.4 |
| 08 | **−2,804** | 166 | −16.9 | | 21 | +2,278 | 68 | +33.5 |
| 09 | −1,930 | 494 | −3.9 | | 22 | +660 | 53 | +12.5 |
| 10 | −363 | 519 | −0.7 | | 23 | −514 | 34 | −15.1 |
| 11 | −1,881 | 307 | −6.1 | | | | | |

**The edge is the US afternoon + the 18:00 ET Globex reopen** (14:00–22:00
ET positive, strongest 15:00 and 18:00 ET). **The US morning 07:00–13:00 ET
bleeds every hour** — 08:00 ET is the single worst hour (−$2,804).

Reproduce the table: run `strategies/terminator_eth.py` and bucket
`entry_time_utc` in its `_trades.csv` by ET entry hour.

## 3. From attribution to the recommended entry windows

Per-entry-hour P&L is not the same as windowed P&L (a position opened late
can still be managed well past the entry cutoff), so the entry windows in
the main report were swept and plateau-checked directly rather than read
off this table alone. Result: entries in **15:30–16:55 ET** (afternoon) and
**18:00–22:55 ET** (evening reopen) — both squarely inside this table's
profitable zone, with the US morning excluded entirely. Full config,
robustness, and walk-forward results: [TerminatorV2.md](TerminatorV2.md) §3–6.

## 4. Why a single 18:00→16:55 ET session, not a short daytime box

CME's own trading day already runs 18:00 ET (prior calendar day) → 17:00 ET
next day, with the maintenance halt marking the boundary. A single engine
session spanning **18:00 ET (prior day) → 16:55 ET (same trading day)**,
flattening once at the end, never holds a position through *any* halt —
verified directly on the recommended config (0 of 990 trades touch the
17:00–18:00 ET halt) — while still covering both the 15:00 ET and 18:00 ET
peaks above. Restricting to a short daytime-only box instead needlessly
gives up the 18:00 ET reopen, the single best hour in the dataset, for no
compliance benefit.

## 5. NT8 settings translation

Needs a **session template spanning 18:00 ET → 16:55 ET next day** (flatten
positions/cancel orders at session end — this is what enforces flat-by-16:55,
independent of the entry windows) plus **Terminator_V2 v2.4.3+**'s dual windows
+ *Time Filter Entries Only* mode: Use Time Filter = true, Entries Only = true,
Flatten At Window End = false, and **two windows, each bounded by the 16:55
close** — Filter 1 **153000–165500**, Use Time Filter 2 = true, Filter 2
**180000–225500** (a single window may never span the close). The entries-only
mode is required — the older window modes lose 28% of P&L or breach the Apex
floor; see [TerminatorV2.md](TerminatorV2.md) §9 for the measured comparison.

SL Mode = Ticks, Value = 100. ATR 28 / Mult 3.25 (see
[TerminatorV2.md](TerminatorV2.md) §8 for the full recommended config).

## Reproduce

```powershell
.venv\Scripts\python cli.py strategies\terminator_eth.py --mc-target 3000
# hourly attribution: bucket reports\TerminatorETH_MNQ_r100-4_trades.csv
# by ET entry hour (entry_time_utc column, converted to America/New_York)
```
