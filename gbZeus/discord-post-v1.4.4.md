**gbZeus v1.4.4 — five trading windows, reverse-signal handling, cleaner settings grid**

A few releases' worth of changes to the God Trades executor. The session-window work, the settings-grid cleanup and the chart-time labelling are **Playr101's** contribution (v1.4.0–v1.4.3) — thanks.

**Five independent entry windows, plus a skip window.** Same layout as GodZillaKilla. Entries are allowed inside any enabled window, never inside the skip. Each window has its own Flatten At Window End flag, and flatten doesn't fire if another enabled window still covers that time — back-to-back windows hand the trade over instead of flushing it. The skip window **never flattens**; it only stands aside from new entries, so a running trade keeps its stop and moving band target straight through it.

Times are **chart time**, not hard-wired to Eastern. Set your chart to ET if you want the times to mean what you think they mean.

Only **window 1 is on**, and it's still the 10:15–3:00 session — nothing changes unless you turn something on. Windows 2–5 ship off with one-hour placeholders at 07:00, 11:00, 13:00 and 21:00; those hours are suggestions, not tested settings, so put your own in.

**On Reverse Signal.** When GodTrades prints a signal against your open trade:

```
DoNothing        ride it to the stop or band target  ← default
ClosePosition    flatten
CloseAndReverse  flatten and take the opposite trade on that signal
```

Close and reverse are gated differently, on purpose. **Closing ignores your entry filters** — a reverse signal after you've hit your daily trade cap, or mid-spiderweb, still gets you out. Those filters decide whether to *open* risk. **Reversing is a brand-new position**, so it has to clear every one of them (window, AUTO, daily limits, trade cap, spiderweb, Max Stop Ticks) and falls back to a plain close if it can't.

**Settings grid now hides what it can't use.** Turn off a window, a signal family, the dashboard or a daily limit and its child settings disappear rather than sitting there doing nothing. Exit Mode swaps its own settings too. Nothing is lost — flip the switch back and your values are still there.

**The new features are not backtested.** It compiles clean through NT8's own compiler, but nothing added here has been *measured*. The numbers I've published before (417 days, profit factor ~1.00) were on a single 10:15–3:00 window with the trade held to its stop or band target — which is exactly what still ships by default, so out of the box you're on the measured configuration. The moment you enable a second window, the skip window, or either active reverse mode, you're outside it. `CloseAndReverse` in particular roughly doubles trade count in chop, which is where the candle-back stop is tightest and commissions bite hardest. Sim it before you trust it.

Requires the GodTrades indicator v16.6 (Sneaky_Zekey's, unmodified). Method is TraderOracle's.
