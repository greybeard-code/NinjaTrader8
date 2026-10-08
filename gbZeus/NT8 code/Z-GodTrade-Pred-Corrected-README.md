# Z-GodTrade-Pred-Corrected.xml — what changed & how to load

Corrected PredatorX (`PredatorXOrderEntryLT`) template derived from
`1.0.Godtrades-Predator.xml`. Goal: pull the shipped template out of the
losing exit regime identified in `../GodTrades_Zekey_Evaluation.md` while
staying inside what PredatorX can actually do.

The file is byte-identical to the original except for the values below (BOM +
CRLF preserved, +16 bytes total). All edits are integer-value fields, applied
to **both** the optimizer `<Parameter>` list and the serialized strategy
block so the two stay consistent. No enum literals were guessed (a wrong enum
would break the whole template load).

## Changes applied in the XML

| Field | Was | Now | Why |
|---|---:|---:|---|
| `QTY1NumberProp` | 2 | **1** | Single contract (methodology: 1 NQ). |
| `QTY2NumberProp` | 1 | **0** | Disable scale-out target 2. |
| `QTY3NumberProp` | 1 | **0** | Disable scale-out target 3. |
| `QTY4NumberProp` | 1 | **0** | Disable scale-out target 4. |
| `TGT1ValueProp` (+ Long/Short) | 60 | **120** | Sole target at 120t → R:R 2.4 vs the 50t stop, mirroring the faithful band-to-band geometry (avg win $730 / avg loss $326 ≈ 2.24:1). Fixes the dominant defect: the 60t target capped the fat winners. |
| `BreakevenTickTriggerAmountSync` | 40 | **1000000** | Breakeven neutralized (trigger unreachable). Methodology: never go to break-even. Done via the trigger, not the selector enum, so the file is guaranteed to load. |

Unchanged (deliberately): `StopLossOffsetValueProp = 50` (PredatorX can't read
the indicator's candle-back `SuggestedStopPrice`, so a fixed stop stays);
entries still `SignalCode == ±1` (BG) / `±2` (FC).

## You MUST do these two things in NinjaTrader (can't be done safely in the XML)

1. **Session gate 10:15–15:00 ET — set it on the INDICATOR.**
   The PredatorX template has *no* serialized start/end time-filter fields to
   edit (only `TimeFilterSelector=NoTimeFilters`), so authoring a PredatorX
   time window blind would risk an invalid file. Instead, on the `GodTrades`
   indicator attached to the chart set:
   - `Use Signal Time Filter = true`
   - `Signal Start Time HHmmss = 101500`
   - `Signal End Time HHmmss = 150000`
   Because PredatorX only trades off `SignalCode`, gating the indicator gates
   the whole system. (Times are the chart timezone — set the chart to ET.)

2. **Verify the single-target / single-contract behavior loads cleanly.**
   Disabling targets via `QTY = 0` is the expected PredatorX idiom, but I
   can't load-test it here. On a **Sim101** account, arm the strategy and
   confirm: one entry contract, one 120t target order, one 50t stop, and no
   0-qty target rejections in the log. If PredatorX errors on the 0-qty
   targets, set the target count / quantities in the PredatorX UI instead
   (Order Qty Type → 1 target, Qty 1) and re-save the template.

## Optional (cleaner, if you prefer the UI over the trigger hack)
- Set the breakeven **selector** to its OFF option in the PredatorX UI and
  re-save — cosmetically cleaner than the unreachable-trigger approach used
  here. Behavior is identical either way.

## Reality check
This is a stopgap, not a fix. Even fully corrected, PredatorX still can't do
the methodology's dynamic opposite-band target — 120t is a static proxy — and
the backtested ceiling of the faithful config is ~break-even (see
`../../../Python/backtester/strategy/GodTrades.md` §6). The real solution is a
native strategy that owns the exits; see `../gbZeus_Strategy_Plan.md`.
