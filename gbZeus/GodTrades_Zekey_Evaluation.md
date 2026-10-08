# GodTrades — Sneaky_Zekey version evaluation

> **Credits and scope, up front.** The **"God Trades" methodology is TraderOracle's** ([God Trade Masterclass](https://www.youtube.com/watch?v=Saa2YTtIV7E)). The **GodTrades indicator is Sneaky_Zekey's**, and this evaluation finds it **well built and faithful to the method** — it is the strongest piece of the stack and the one we kept.
>
> What tests poorly below is **not the indicator**. It is the *PredatorX execution template* — a separate order-entry configuration whose fixed-tick brackets, breakeven, and missing session filter throw away the band-to-band winners the method depends on. The criticism is of that configuration's settings, not of anyone's coding. A good signal engine wired to the wrong exits still loses.

Evaluation of the GodTrades stack in `NinjaScript/TOGodMode/NT8 code/`:

- `GodTrades.cs` **v16.6** — signal-only indicator (gap engine → plots).
- `1.0.Godtrades-Predator.xml` — TradeSaber **PredatorX** (`PredatorXOrderEntryLT`) execution template that reads the indicator's `SignalCode` plot and manages the orders.
- `GodTrades-Indicator.xml` — the indicator's saved parameter template (note: on a **500-tick** NQ chart).

Tested empirically in the Python backtester (`strategies/god_trades_zekey.py`) over the full available NQ history: **2024-12-16 → 2026-07-03 (417 trading days)**, 1 contract, Apex-style commissions.

---

## 1. Architecture — this is the right shape

The old monolithic `GodTradesStrategy.cs` (fixed 40/30-tick brackets baked into one strategy) is gone. The replacement cleanly separates **signal** from **execution**:

- The indicator emits `SignalCode` (`±1` = Bollinger Gap, `±2` = Fill-Continuation) plus context/`Suggested*` plots and never trades.
- PredatorX consumes `SignalCode` — **Entry 1** on `±1` (BG), **Entry 2** on `±2` (FC), read one bar back — and owns brackets, breakeven, session, sizing.

The indicator itself is **well built and faithful** to the methodology: gap lifecycle with ≥3-bar validity, early-touch invalidation, valid-only touch plots, BG doji/ATR huge-candle filters gating **entries only** (the gap line still forms — correct reading), FC confirmation (close-beyond-full-zone + correct approach + signal-candle direction + Bollinger-midpoint location), spiderweb warning, VPS-safe `IsSuspendedWhileInactive=false`. **OBR was dropped** relative to the older `GodTrades21.cs` — `SignalCode` is BG/FC only.

The problem is **not the signals. It is the PredatorX execution template.**

---

## 2. Where the template deviates from the methodology

| Dimension | Methodology (deck/video) | PredatorX template as shipped |
|---|---|---|
| Stop | Back of the signal candle (tiny, variable) | **Fixed 50 ticks** both sides; ignores the indicator's `SuggestedStopPrice` |
| Target | Opposite Bollinger band, dynamic, band-to-band | **Fixed ticks** — scale-out 2/1/1/1 at 60/50/100/200t |
| Breakeven | "Never — break-even is suicidal" | **ON**: 40-tick trigger → stop to entry +3t, on bar close |
| Session | 10:15–15:00 ET only | **No filter anywhere** (`TimeFilterSelector=NoTimeFilters`, indicator `UseSignalTimeFilter=false`) |
| Spiderweb | Stand aside when ≥5 lines cluster | `SpiderwebWarning` plot **not wired** as a filter — visual only |
| Size | 1 NQ (deck says NQ not MNQ) | **5 contracts** (2/1/1/1 scale-out) |

The dynamic band target is the crux: the indicator *computes* it (`SuggestedTargetPrice`, re-priced per bar), but the indicator's own property help says **"PredatorX does not read it"** — PredatorX brackets are tick/RR/ATR/trail-based and cannot consume a per-bar plot price as a target. So **the methodology-faithful exit is not reachable in this stack**, which is presumably why it fell back to fixed ticks.

---

## 3. Backtest results (417 days, NQ, 1 contract)

| Config | Net P&L | PF | Win rate | Avg win / Avg loss | Sharpe | Max DD | Trades |
|---|---:|---:|---:|---:|---:|---:|---:|
| **Methodology-faithful** (band exit, 10:15–15:00) | **-$1,331** | **1.00** | 30.8% | **$730 / -$326** | -0.03 | -68% | 3,469 |
| **Zekey / PredatorX @ 1000t** | **-$31,984** | **0.94** | 46.0% | $283 / -$255 | -1.40 | -66% | 4,206 |
| **Zekey / PredatorX @ 500t** (his actual chart) | **-$115,587** | **0.90** | 46.0% | $270 / -$254 | -3.54 | -231% | 8,512 |

All three breach a $2,000 Apex-style trailing threshold almost immediately at **1** contract; the template's **5** contracts breach ~5× faster.

### Why the template loses — it caps the only edge

The faithful version is a break-even system built on **fat winners**: average win **$730** vs average loss **$326** (2.24 : 1). At a 30.8% win rate that reward:risk is exactly break-even (break-even win rate for 2.24:1 ≈ 30.9%).

Replacing the band-to-band target with a fixed **60-tick** target:

- **Raises** win rate to 46% (a 60t target is hit more often than a full band move), but
- **Collapses** the average win to **$283** while the average loss barely shrinks ($255). Reward:risk falls to **1.11 : 1**, whose break-even win rate is **47.4%** — and the system only delivers 46%. Negative expectancy by construction.

Breakeven-at-40t clips winners further; the missing session filter adds ~700 lower-quality all-day trades and commission drag (both the 1000t and 500t runs lose **gross**, before commissions — this is not a fee problem). Dropping to 500t doubles the trade count and the loss.

**In one line:** the fixed target throws away the large band-to-band winners that are the *only* thing keeping the system afloat, and no amount of higher win rate compensates.

---

## 4. Verdict

- **Indicator (`GodTrades.cs` v16.6): keep.** It is the strongest, cleanest artifact — a faithful signal engine suitable for manual/discretionary use or as the front end for a *different* executor.
- **PredatorX template as shipped: do not trade it.** It converts an already-marginal edge (§3 faithful ≈ break-even, consistent with the earlier `strategy/GodTrades.md` §6 finding) into a **reliable loser** (-$32k at 1000t, -$116k at 500t over 417 days), for three compounding reasons in priority order:
  1. **Fixed 60-tick target** instead of opposite-band → kills the fat winners (dominant cause).
  2. **Breakeven-on** → clips the survivors.
  3. **No 10:15–15:00 session gate** → adds all-day low-quality trades and drag.
- The **500-tick chart** in the indicator template makes it materially worse than the methodology's 1000-tick and should be reverted.

### If the goal is to salvage it inside PredatorX

Band-to-band isn't reachable there, but the losses are dominated by exit geometry, so the highest-leverage changes are:
1. Turn the **session filter on** (10:15–15:00 ET) — indicator `UseSignalTimeFilter=true` or a PredatorX time filter.
2. Turn **breakeven off**.
3. Use a **single, much larger** fixed/RR target (or a wide trail) instead of a 60t cap, so winners can approximate the band move; drop to **1 contract**.
4. Move to **1000-tick**.

Even fully tuned, the ceiling is the §3/§6 result: a break-even-to-marginal mechanical edge, not a validated strategy. Recommend either fresh out-of-sample confirmation or treating this as a research artifact — not a deploy.

---

## Reproduce

```
cd Python/backtester
.venv/Scripts/python.exe cli.py strategies/god_trades_zekey.py --no-report              # 1000t
.venv/Scripts/python.exe cli.py strategies/god_trades_zekey.py --period 500t --no-report # 500t
.venv/Scripts/python.exe cli.py strategies/god_trades.py --no-report                     # faithful baseline
```

Config mapping and fidelity caveats (tick-body BG filter, single-target modeling of the scale-out, 1-bar entry delay) are documented in the header of `strategies/god_trades_zekey.py`.
