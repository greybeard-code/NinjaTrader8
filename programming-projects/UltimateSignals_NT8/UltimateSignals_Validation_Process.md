# Vendor Indicator Validation Process — applied to UltimateSignals

A repeatable acceptance process for a **closed-source, unauditable** NT8 indicator. It exists because
`UltimateSignals.dll` is packed and licence-locked, so its logic cannot be read
([UltimateSignals_Review.md](UltimateSignals_Review.md) §2) — the only way to know what it does is to
measure it.

Work the stages in order. Each stage has an explicit **gate**; a failed gate stops the process rather
than being noted and worked around. All times are **Eastern**.

---

## Stage 0 — Entry criteria

Do not start until all three are true:

- [ ] A licence/serial that activates on **this** machine (the DLL is HWID-locked and self-terminates
      on activation failure — Review §5).
- [ ] A **sim account** and a workspace you are willing to lose.
- [ ] A written statement of what "working" means, in numbers. *"The sell signals don't work"* is not
      testable. *"Short signals should not lose on more than 60 % of fills over 100 trades"* is.

**Gate 0:** if there is no numeric definition of success, stop and write one. Everything downstream
compares against it.

---

## Stage 1 — Sandbox install and blast-radius check

This DLL bundles a private copy of NinjaTrader.Custom's entire DrawingTools namespace and can collide
with your other vendor DLLs (Review §5, F7). Install it somewhere you can undo.

1. **Back up** `Documents\NinjaTrader 8\bin\Custom\` and your `workspaces\` folder.
2. Record the "before" state: open a chart, place one of each drawing tool you actually use
   (Fibonacci retracement, regression channel, risk/reward, text), and screenshot it.
3. Import `UltimateSignals_NT8.zip` — *Tools → Import → NinjaScript Add-On*. Restart NinjaTrader.
4. Confirm the import: `Documents\NinjaTrader 8\bin\Custom\UltimateSignals.dll` must now exist.
   *(As of 2026-07-31 it does not — the indicator has never been installed. Review §6.2.)*
5. Watch the **Log** and **Output** windows through a full restart cycle. Look for activation traffic,
   assembly-binding failures, and duplicate-type warnings.
6. Re-open the "before" chart. Every drawing tool must still render and still be editable.
7. Open one strategy from each other vendor pack (PredatorX, Maverick, Infinity) and confirm they
   still compile and load.

**Gate 1:** clean restart, no new log errors, drawing tools intact, other vendors intact. If anything
regressed, uninstall and stop — no signal is worth breaking your drawing tools and three other packs.

---

## Stage 2 — Confirm capturability, the plot map, and repaint

This is the stage that produces facts. Everything in
[UltimateSignals_Signals.md](UltimateSignals_Signals.md) §2 marked `CONFIRM` is settled here, the
repaint question (Review §4) is answered, and the reported SELL-capture failure (Review §6) is
characterised.

1. Import [gbSignalProbe.cs](gbSignalProbe.cs) into `bin\Custom\Indicators\GreyBeard\`. It is
   **late-bound** — no compile-time reference to the vendor assembly — so it compiles and runs on a
   machine where UltimateSignals is absent or unlicensed.
2. Put UltimateSignals on an MNQ chart on the bar type you intend to trade, then add the probe to the
   **same chart and same series**. Set `Target Indicator` to `UltimateSignals`, or leave it blank for
   a one-shot inventory of every indicator on the chart.
3. **Read the capturability line first.** The probe prints `Values.Length` (series reachable from C#)
   against `Plots.Length` (series a condition builder can list). If `Values > Plots`, the missing
   series **cannot be selected in Infinity, PredatorX or the Strategy Builder at all** — a bridge is
   mandatory, not optional.
4. Read the plot map: live index, `Name`, `Brush`, `PlotStyle`, and whether each index is a real plot.
   **That is the authoritative map** — replace the inferred table in the Signals doc with it.
   Compare the buy row against the sell row: an unnamed, duplicate-named or plot-less sell row is the
   direct explanation for the capture failure.
5. Note the idle sentinel: whether inactive bars hold `NaN` or `0`. Set the bridge's `Idle Is NaN`
   and fix the active test in the Signals doc §2.1 accordingly.
6. Leave the probe running **live, forward, for one full session.** It writes
   `gbSignalProbe_<instrument>_<stamp>.csv` with one **write-once** row per plot per bar, captured the
   first time a value appears, plus a `REVISION` row every time an already-written bar's value later
   changes.

**Gate 2 — the repaint gate.** Count `REVISION` rows in the CSV.

| Result | Verdict |
|---|---|
| 0 revisions over a full session | Non-repainting. `ConfirmationBars = 0` is safe. |
| Revisions confined to the last N bars | Repaints. Set `ConfirmationBars > N` and continue. |
| Revisions reaching arbitrarily far back | **Stop.** Historical charts and backtests are fiction; the only valid evidence is this forward log. Do not proceed to any backtest-based stage. |

Do not skip to Stage 3 on the basis of a historical chart looking good. That is the exact trap
UltimateAI2 set (Review §4).

---

## Stage 3 — Signal inventory

From the Stage 2 forward log, **not** from a chart scroll-back:

- Count buy and sell markers separately, per session.
- Median bars between signals, per side.
- Time-of-day distribution, per side (ET).
- Confirm both sides fire at all.

**Gate 3:** if one side produces zero markers over a full session, the problem is signal generation
and belongs back with the vendor. If both sides fire, the complaint is about **fills or
profitability**, not signals — proceed, and re-read Review §6.3, which is where the current SELL
complaint actually lands.

---

## Stage 4 — Regime split (this is where "SELL doesn't work" gets settled)

The engine is a Stochastic overbought/oversold fade gated by a 3-MA trend state (Review §3). Its
predicted failure mode is that the counter-trend side bleeds in a trending tape. Test that directly
instead of arguing about it.

1. Label each session in the forward log as **trend** or **range** with one mechanical rule fixed in
   advance — e.g. `|close − open| / session range > 0.5` is a trend day.
2. Split every marker into four buckets: buy-in-trend-up, buy-in-range, sell-in-trend-up,
   sell-in-range.
3. Score each bucket on a fixed exit rule (§6's bracket), not on discretion.

Predicted result, from the screenshots supplied on 2026-07-31: screenshot 2 covers a ~280-point MNQ
advance in under an hour into which the system fired **10 sells and 8 buys**. If the sell-in-trend-up
bucket is the only losing bucket, **the sell logic is not broken — it is a fade being run into a
trend**, and the fix is a trend filter on the short side, not a vendor bug report.

**Gate 4:** a bucket that loses on ≥ 60 % of signals is disabled by filter, not traded and hoped over.

---

## Stage 5 — Bar-type A/B

The screenshots are on **SaberRenko 70/4**. Renko-family bars synthesise OHLC and the forming brick is
provisional, so a ZigZag reading `High[]`/`Low[]` will place and retract pivots as bricks resolve —
a repaint source independent of the indicator's own logic.

Run the probe simultaneously on:

- SaberRenko 70/4 (the working chart)
- A time bar of comparable average duration
- A tick or volume bar of comparable average duration

Compare `REVISION` counts and Stage 4 bucket scores across the three.

**Gate 5:** if the bar type is responsible for the revisions, either move to the clean bar type or
raise `ConfirmationBars` until the revision rate on Renko matches it.

---

## Stage 6 — Execution wiring, both sides, sim only

Only now connect anything to an order.

1. Wire long and short as a **mirrored pair in a single edit** (Signals doc §3). Never one side at a
   time.
2. Fixed bracket, no discretion — e.g. MNQ stop 60 / target 60 — so Stage 4's scoring stays valid.
3. Force one buy and one sell manually in sim. Confirm **both** produce a fill.
4. If using Infinity Algo Engine, walk the per-side gate table in Signals doc §4 *before* concluding
   the signal is at fault. `ShortSwitchedOn` alone explains a completely dead sell side while markers
   keep drawing.
5. Run sim forward for a defined number of trades — the number from Gate 0, not "until it looks good".

**Gate 6:** long and short fill counts must be within a factor of two of their Stage 3 marker counts.
A large gap is an execution problem (limit pricing, cooldowns, `WaitUntilFlat`), not a signal problem.

---

## Stage 7 — Go / no-go

Promote to live only when **all** hold:

- [ ] Gate 1 clean — nothing else on the install regressed
- [ ] Gate 2 quantified — revision depth known, `ConfirmationBars` set from it
- [ ] Gate 3 — both sides generate signals
- [ ] Gate 4 — no bucket losing ≥ 60 %, or the losing bucket is filtered off
- [ ] Gate 6 — both sides fill in sim
- [ ] Stage 0's numeric target met on **forward sim data only**
- [ ] Licence survives an NT8 restart, and you know what happens when the vendor's activation server
      is unreachable

Any unchecked box is a no-go. Record the outcome below and keep it with the repo.

---

## Sign-off record

| Stage | Date | Result | Notes |
|---|---|---|---|
| 0 Entry criteria | | | Success defined as: |
| 1 Sandbox install | | | |
| 2 Plot map / repaint | | | Revisions: ___ Depth: ___ bars |
| 3 Signal inventory | | | Buys: ___ Sells: ___ |
| 4 Regime split | | | Worst bucket: |
| 5 Bar-type A/B | | | |
| 6 Execution wiring | | | Long fills: ___ Short fills: ___ |
| 7 Go / no-go | | | |

---

## Current status — 2026-08-01

The indicator is licensed and running on the **licence holder's** machine; the screenshots come from
there. It is **not installed on this machine**, so nothing here can be reproduced locally — which is
why `gbSignalProbe` and `gbUltimateSignalsBridge` are both late-bound and compile without the vendor
DLL. Build them here, hand them over, run Stage 2 there.

The reported SELL-capture failure is diagnosed in Review §6: PredatorX and Infinity identify an
external signal by drawing-object **tag** and **colour**, and UltimateSignals paints both its BUY and
its SELL arrow in the same magenta `#FF00FF`. Stage 2 confirms whether a plot-level path exists as
well; [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs) fixes it either way. That work is
independent of Stages 3–7, which remain about whether the signal is worth trading at all.
