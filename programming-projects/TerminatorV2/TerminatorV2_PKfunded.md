# Terminator_V2 — "PK funded(1)" Template Evaluation

> **Note 2026-07-09.** This ablation was run on an older, smaller slice of
> data (154 days) than the current main-report recommendation (510 days).
> The component ablation directions below (profit lock costly, loss limit
> helpful, BE neutral, 2 contracts over-levered) are structural findings
> about the PK template itself and still apply, but the "code defaults"
> and "recommended config" dollar figures used for comparison in this file
> are from that older dataset/config and are **not** the current
> recommendation. The current recommended config (full Globex-day session,
> ATR 28×3.25, 100-tick stop) is in [TerminatorV2.md](TerminatorV2.md) §3 —
> a fresh apples-to-apples comparison of this PK template against it, on
> the current 510-day dataset, has not been run.

**Source:** `templates\Strategy\Terminator_V2\PK funded(1).xml` (MNQ 09-26,
ninZaRenko 100/4). **Tested:** MNQ `r100-4`, RTH 09:30–16:55 ET, $50K, Apex
$2,500 trailing, 2025-12-15 → 2026-06-17 (154 days), tick-level fills.
**Port:** `strategies/terminator_pk.py` (extends `terminator_v2.py`).
Companion report: [TerminatorV2.md](TerminatorV2.md). Evaluated 2026-07-04.

## Template configuration

| Setting | Value | vs. code default |
|---|---|---|
| Signal | ATR(14) × **2.0** | much faster than 20 × 4.0 |
| Quantity | **2 contracts** | 1 |
| Stop / target | SL **1×ATR**, TP **2×ATR** | none (pure SAR) |
| Breakeven | at 1×ATR profit, offset 0 | off |
| Daily lock | **−$500 / +$400, flatten** | off |
| Session | full ETH, exit on session close | (tested RTH here) |

Two things about how this template was validated in NT8 matter a lot:
`IncludeCommission=false` and `OrderFillResolution=Standard` on Renko bars
(synthetic-price fantasy fills). This config trades ~5.3×/day with tight
brackets — precisely the profile where those two flags flatter results most.
The template's own analyzer window was 3 days (2026-06-26→29). Our test pays
real spread + $1.04/RT commission on every fill across 6 months.

## Results — template exactly as configured (RTH)

| Metric | PK template (2 contracts) | Code defaults (1 contract, ref) |
|---|---|---|
| Net P&L | $4,854 (gross $6,558, comm. $1,704) | $7,060 (comm. $360) |
| Trades / win rate | 819 / 31.7% | 346 / 48.3% |
| Profit factor | 1.11 | 1.34 |
| Sharpe / Calmar | 1.66 / 2.58 | 2.49 / 4.43 |
| Max drawdown | −$3,081 | −$2,607 |
| Apex $2.5K trailing | **BREACHED 2026-02-04** | survived ($719 headroom) |
| MC P(breach) | **42.8%** | 16.6% |
| MC P(pass $3K eval) | 61.5% | 83.9% |

The fast 2×ATR(14) trail more than doubles trade count; the 1×ATR stop +
BE-at-1×ATR turns many would-be winners into scratches (win rate 32%), and
commission takes 26% of gross. Run at 2 contracts, the drawdowns are big
enough that **the template blew the Apex eval in the actual sequence** —
and does so in 43% of Monte Carlo orderings.

## Component ablation (full range, exact template ± one piece)

| Variant | Net | Sharpe | Verdict |
|---|---|---|---|
| Template as-is | $4,854 | 1.66 | baseline |
| **remove +$400 daily profit lock** | **$8,520** | **2.46** | the big leak — caps the trend days that pay for the whipsaw |
| remove −$500 daily loss too | $6,631 | 1.69 | loss limit was *helping* — keep it |
| remove breakeven (locks kept) | $5,821 | 2.00 | BE ≈ neutral (Davey's warning: mild here) |
| remove BE + profit lock, keep loss limit | $8,046 | 2.42 | ≈ tied with best |

Attribution: **daily profit lock −$2,700 to −$3,700 of P&L in every
pairing; daily loss limit +$1,900; breakeven ±$500.** If the +$400 lock
exists for payout-consistency reasons, price that policy consciously — it
costs roughly 40% of the edge.

## Fair comparison at 1 contract

Best PK-family variant (profit lock off, loss limit $250, BE on) at 1
contract: net $4,260, Sharpe 2.46, maxDD −$1,587, MC P(breach) 6.3%,
P(pass eval) 79.5%.

The main report's current recommended config (full Globex-day session,
ATR 28×3.25 SAR, 100-tick stop — see [TerminatorV2.md](TerminatorV2.md) §3)
was tuned and tested on a much larger, more recent dataset (510 days) than
this ablation (154 days), so a direct number-for-number comparison against
it isn't apples-to-apples. Qualitatively, the same conclusion holds: the
plain SAR with a single tuned hard stop has outperformed every PK-template
variant tested here at 1 contract.

## Verdict

The PK template's *signal engine* is fine — it's the same robust SAR — but
this parameterization fights it: the fast trail churns, the tight brackets
scratch out winners, the profit lock amputates the payoff days, and 2
contracts against a $2,500 trailing threshold is over-levered (the eval
breached in the real sequence). Its good reputation likely comes from
commission-free, fantasy-fill NT8 analyzer runs over short windows, plus
genuinely decent recent live weeks (June was net positive here too).

Recommended changes, in impact order:
1. **Drop to 1 contract** while under an Apex trailing threshold (or use
   `vol_target_contracts`); 2 contracts is the breach driver.
2. **Remove the +$400 daily profit lock** (keep the −$500 loss limit —
   scale it with contracts).
3. Prefer the slower engine: ATR 20×4 pure SAR with a 200-tick hard stop
   (see main report) — it dominates this template at equal size.
4. If keeping PK's brackets, retest ETH separately before trading it, and
   re-run its NT8 analyzer with commissions on and tick-replay fills to see
   the honest version of what you've been looking at.

## Reproduce

```powershell
.venv\Scripts\python cli.py strategies\terminator_pk.py --mc-target 3000
.venv\Scripts\python sweep.py strategies\terminator_pk.py --param be_atr=0,1 --param daily_profit=0,400 --param daily_loss=0,500
```
