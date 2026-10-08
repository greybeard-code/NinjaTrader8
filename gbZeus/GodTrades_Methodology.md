# God Trades (TraderOracle) — Methodology Reference

**Purpose of this file:** a faithful, exhaustive capture of TraderOracle's
"God Trades" system as taught in his two training materials, so a human or a
later AI session can reconstruct the rules without re-watching/re-reading the
sources. This is a *description of what the trainer teaches*, including his
own hedges and contradictions — it is **not** trading advice and not a claim
that the edge is real. For the automation status and backtested reality, see
`C:\Dev\programming-projects\Python\backtester\strategy\GodTrades.md`.

## Credits

The **"God Trades" methodology is the work of TraderOracle** — this file only
records it. Primary source, and the thing to watch before trading any of this:
**[God Trade Masterclass](https://www.youtube.com/watch?v=Saa2YTtIV7E)**.

The **GodTrades NinjaTrader indicator is by Sneaky_Zekey** (used unmodified,
with permission). The `gbZeus` strategy that executes these signals is the only
original work in this repo.

## Sources

| # | Source | What it is | How captured |
|---|---|---|---|
| 1 | Canva deck "The God Trades" (14 slides) | The written rule set | Read slide-by-slide in browser (Canva/YouTube block WebFetch — both are JS SPAs) |
| 2 | [YouTube "God Trade Masterclass"](https://www.youtube.com/watch?v=Saa2YTtIV7E) | 28-min live class, discretionary color | Auto-caption transcript via `yt-dlp` (channel: "Trading for Rent Money", video id `Saa2YTtIV7E`) |

> Retrieval note for future sessions: WebFetch returns only the SPA shell for
> both Canva and YouTube. Use the in-app/Chrome browser for Canva slides;
> pull YouTube transcripts with `yt-dlp --skip-download --ignore-no-formats-error`
> then fetch the `automatic_captions` json3 URL. The Chrome extension blocks
> the `youtube.com` domain outright; `yt-dlp` from Bash works.

---

## 1. Core premise

Price is constantly "seeking a good deal" and normally examines every price
level thoroughly. Occasionally the algos rush price in one direction so fast
that levels get **skipped** — leaving a **candle gap** (a body-to-body gap
between two consecutive candles; he also calls these "volume imbalances").

Price later **returns to fill** these gaps ("a quick coffee break to see
what's there"). At the fill it reacts: either reverses hard, or — if nothing
interesting is found — resumes what it was doing. A "God Trade" is defined in
the video as **"a reversal at a predictable place."**

Because this keys on reversals, it is pitched as a **ranging-day system**
("ranging days are ~70% of days"). Mid-day the trend often reverses into a
range, which is when these setups work best.

---

## 2. Hard rules (deck slide 2 "Introduction" + video "Hard rules")

- **Instrument/chart:** NQ on a **1000-tick** chart (ES = **2000-tick**).
- **Use NQ, not MNQ** (he notes you can set up cross-trading — trade the
  micro while charting the full-size).
- **Session:** only trade **10:15am – 3:00pm ET**.
- **Gap validity:** a candle gap's line must have existed **at least 3
  candles** before being filled. **The longer the gap line has existed, the
  more intense the reversal.**
- **Frequency:** expect **5–10 God Trades per ticker per day** (≈40/day
  across 4 tickers, but that's "serious multitasking"). You don't need to
  watch until price approaches a Bollinger band or an old gap line.

### Risk rules (video, emphasized repeatedly)

- **Stop loss = a single candle** — the back of the candle you entered on.
- **Break-even is "your worst enemy … suicidal on this particular trade."**
  He attributes most of his own recent losses to moving to break-even too
  early and getting swept out. Claim: *"you'll increase your profits by 20%
  if you just stop going break-even."* Keep the tiny stop and leave it.
- Expect **3–4 full stops per full session**; that's fine because a stop is
  only ~1 candle. The average winner must exceed ~3 candle-stops.
- If a single-candle stop can blow your account, **you are overleveraged** —
  that's the user's problem, not the system's.
- **Re-enter after a stop-hunt:** he *always* re-enters after being stopped;
  specifically, if price snaps back to the **original entry price**, he
  re-enters on that candle. (He notes the "only trade the predictable
  stop-hunt snapback" idea is too rare — 3–4×/day — to rely on exclusively.)

---

## 3. The Bollinger band

All setups reference a **standard 20-period, 2-standard-deviation Bollinger
band** ("20/2"). Two roles:

1. **Location filter** — momentum gaps (BG) must form *at* a band; reversals
   are stronger when price has pushed *beyond* a band (overextended).
2. **Profit target** — take profit is **band-to-band**: exit at the
   **opposite** Bollinger band touch. While in a trade you are "soaking
   until it reaches the other band, or your stop." Ignore opposite signals
   while a trade is open.

---

## 4. The setups

There are **three** trade types. The deck presents BG, Fill & Reverse, and
Fill & Continue as the named trades; the NT8 indicator additionally
implements an engulfing reversal ("OBR") that matches the video's
"engulfing candle at the band" reversal language.

### 4.1 Bollinger Gap (BG) — momentum (deck slides 3–4)

The "price is in a hurry" trade. Price pushes against a Bollinger band, snaps
off it, and leaves a gap in the direction of the snap.

**Requirements:**
- Price must form **2 same-color candles with a gap in between** (deck shows
  2 greens for a long).
- Price must be **touching (or VERY NEAR) the band** the move is coming off
  (bottom band for a long, top band for a short). Closer is better; ideally
  candles are touching or beyond the band.
- **Enter when the second (gap-confirming) candle closes** — "long once this
  candle closes, verifying the gap."
- **Stop loss = the bottom of the gap candle** (the far end of the first/top
  candle of the pair).
- **No doji signal candles** — *"Fuck no, your premise is 'in a hurry'. A
  doji shows confusion in the market."*

**Stupid shit to avoid (deck slide 4):**
- Trading when nowhere near the band.
- Trading a **huge** candle (a single-candle stop on a huge candle can
  bankrupt you).
- Not holding long enough — "it's in a hurry, just let it ride."
- Not waiting for the candle to **close** (if it flips red you lost cash).

**Common questions (deck slide 4):**
- *Why is the stop so small?* The premise is a launch; any yankback
  invalidates the premise, so kill the trade.
- *How close to the Bollinger?* The closer the better — ideally candles
  touching or below the band.

### 4.2 Fill & Reverse (F&R) — reversal (deck slides 6–9)

Price left a gap earlier; the indicator extends a **line** from it that
persists **forever until price returns** to examine that level. Price returns,
**touches the line, and reverses** back the way it came.

**Requirements:**
- An **old candle gap** with a line extended from it (respect the ≥3-candle
  age rule).
- Market **returns to that area, touches it, then reverses**.
- Enter on the reversal candle's close.
- **Stop = back of the signal candle.** *Moving to break-even is suicidal —
  ensure a tiny stop and leave it.*
- **TP = opposite band** (band-to-band).

**Helpers / confluence (deck slide 7, "not requirements, but handy"):**
- The **approach leg** into the line is often **big red (or big green)
  candles with big volume and A LOT of delta** — a "final push then giving
  up" (the team scoring a touchdown and getting exhausted, in his football
  metaphor).
- The **first 1–2 reversal candles contain VERY little delta and volume**
  (the other side hasn't warmed up yet — his "green is out with an injury,
  needs a minute" metaphor for why reversals stall/doji before moving).
- **Reverses beyond the band** — the more overextended (OS/OB) the RSI, the
  more likely the reversal scores.
- A **reversal candle that engulfs** the previous candle (bigger body) is a
  "HUGE help."
- **Dragonfly / shaved close** reversal candle = a WIN signal.
- **"Equal high / equal low"** (video): the reversal green candle closes at
  the *precise* price the prior red candle opened (or vice versa) at the
  band — a clean handover level. A strong reversal signal.
- **Shaved candles** (close/open with no wick) show one side is dominating
  with no pushback ("snapback") — he reads dynamic candles as they form and
  enters when he sees no snapback and a shaved close.

**NQ stop-loss hunts (deck slide 9):** NQ frequently does an annoying
stop-hunt snapback right after this setup. He re-enters if price achieves the
original entry price again — "and today was profitable."

### 4.3 Fill & Continue (F&C) — trend continuation (deck slides 10–11)

Same as F&R but in a **trend continuation** context: the trend is up, price
pulls back a little to **fill a previous gap**, then **continues** with the
trend.

- **Claimed 84% success** when he uses it ("don't sleep on this trade").
- Same mechanics: enter once the confirming candle closes; **stop = back of
  the signal candle**; **never go break-even, tiny stop, leave it**.
- Not every white gap line is a trade — **if it doesn't follow the rules,
  don't trade it** (deck slide 11).

### 4.4 Engulfing reversal at the band (OBR in the NT8 code)

The video repeatedly enters reversals on a **red engulfing candle with a
shaved close** at the band (or a bullish mirror). The NT8 indicator
formalizes this as an **opposite-direction body engulf** at the Bollinger
band (bearish: current red body fully covers the prior green body while
touching/near the upper band; bullish mirror at the lower band). Treat this
as the mechanical proxy for his "engulfing candle" reversal reads.

---

## 5. Advanced concepts

### 5.1 Spiderwebs (deck slide 12)

Once a **"spiderweb"** forms — a shitload of gap lines everywhere — use
**EXTREME CAUTION**. You'll get false signals. Better to wait for a clean,
easy-to-read signal. (NT8 code models this as ≥5 valid gap lines clustered
within ~100 ticks → warning / stand-aside.)

### 5.2 The Final Line (deck slide 13, "Advanced")

The **"final line"** = a series of candle gaps all being filled in a row.
Price slowly grinds up toward that one obvious, **alone** line left from
earlier in the day, filling each dropped gap along the way. **Once that final
line is filled, expect an insane RUSH in the opposite direction** — price has
no reason to stay in the area. These are described as "easy, quick,
predictable moves."

### 5.3 Treasure Map metaphor + "Missed Trade" fix (deck slide 14)

Price has "searched everything on this side of the treasure map — no reason
to stay, we've got more areas to explore," i.e. once a region's gaps are all
examined, price leaves fast. **Missed-trade fix:** because these big reverses
(like the Final Line) are so predictable, you can **schedule around them** —
step away, come back, and watch for the reaction at the level rather than
staring all day.

### 5.4 Combining strategies (video)

- **Do** combine with **reversal-confirming bounce lines/zones** (London
  low/high, other line-sellers' levels — he name-checks "greatest nothing,"
  "kill pips," "trader smart" lines). A line-bounce **to the pixel** at a
  gap fill is strong extra evidence.
- **Don't** clutter with non-reversal tools (e.g. volume profile) — "it's
  more shit to think about" and you stop using the method. His strat is
  **reversal-focused**; he cares about trend but doesn't worship it.
- Take profit at a predictable place (e.g. a London low acting as a
  trampoline); optionally reverse there.

### 5.5 "No trade" cases (video)

- **Fill and keeps going:** if a gap fills and price just keeps trucking in
  the same direction (no reversal candle), **he does not trade it** — "I'm
  betting on green; if the whole roulette table is black except one red, I'm
  not betting red." (i.e., don't fade overwhelming one-sided dominance.)
- **Shitty green/red trend:** a listless, doji-laden, low-volume push into a
  level with no incentive is a skip.
- **Attack angle matters:** he wants price to approach a level like a plane
  landing — steep, touch-and-go — not drift sideways in a tight range for
  ages before nudging into it.
- Endless dojis = going nowhere = terrible place to trade (unless right at a
  drawn zone with a clean inverted-hammer/shaved reversal).

---

## 6. Reading dynamic candles (video — a promised follow-up class)

A big part of his discretionary edge is reading candles **as they form**
(before close):
- **Shaved close** (no wick on the close side) = that side is dominating hard.
- **No snapback** while forming (doesn't repeatedly retrace and reform) =
  clean domination, enter.
- **Delta/volume:** heavy delta on the exhaustion push in, tiny delta on the
  first reversal candles.
- **Doji** = confusion; wait.

This is inherently discretionary and is the hardest part to automate
faithfully — the mechanical port approximates it with shaved-close, doji,
engulf-size, and delta-sign toggles.

---

## 7. Quick reference — mechanical summary

| Element | Rule |
|---|---|
| Chart | NQ 1000-tick (ES 2000-tick) |
| Hours | 10:15–15:00 ET only |
| Indicator | 20/2 standard Bollinger + gap lines |
| Gap def | body gap between 2 same-color candles |
| Gap validity | line must survive ≥3 candles; older = stronger |
| BG entry | 2-candle gap forming AT the band; enter on 2nd candle close |
| F&R entry | old gap line touched & reversed; enter on reversal candle close |
| F&C entry | pullback fills gap in a trend, then continues; enter on close |
| OBR entry | opposite-direction engulf w/ shaved close at the band |
| Stop | back of the single signal candle (tiny) |
| Break-even | NEVER move to break-even |
| Target | opposite Bollinger band (band-to-band) |
| While in trade | ignore opposite signals; soak to band or stop |
| Re-entry | after a stop-hunt, re-enter if original entry price prints again |
| Stand aside | spiderweb (many clustered lines); dojis/no attack angle |
| Special | Final Line fill → violent opposite rush |
| Confluence (opt) | bounce lines/zones, RSI extreme, delta, engulf, dragonfly |

---

## 8. Important caveats (do not lose these)

- **The "91% on NQ 1000 tick" and "84% Fill & Continue" are the trainer's
  discretionary, self-reported numbers.** They bundle his live candle-reading
  and trade-skipping. Mechanically ported and backtested on NQ (May–Jun
  2026), the win rate is **~32%**, though still net-positive because
  band-to-band winners are ~2.5× the single-candle stops. See the backtester
  doc for the real numbers per setup (BG is the only strong one; OBR ≈ noise).
- The system's math depends entirely on the **asymmetric R:R** (tiny stop,
  band-sized target), NOT on a high hit rate. Anything that widens the stop
  or caps the target (including break-even) breaks the premise — which is
  exactly why he bans break-even.
- Much of the edge he claims is **discretionary filtering** (attack angle,
  delta reads, "shitty green" skips, spiderweb avoidance) that a fully
  mechanical strategy cannot reproduce 1:1.

## 9. Related files

- `NinjaScript/TOGodMode/NT8 code/GodTrades21.cs` — NT8 signal indicator
  (gap lifecycle, BG/FC/OBR, spiderweb). The faithful signal engine.
- `NinjaScript/TOGodMode/NT8 code/GodTradesStrategy.cs` — NT8 wrapper
  strategy. **Deviates from the methodology:** trades fixed 40-tick TP /
  30-tick SL and ignores the indicator's candle-stop/band-target. Slated to
  be patched with a band-exit mode.
- `Python/backtester/strategies/god_trades.py` — Python port
  (`GapSignalEngine` + `GodTrades`) with methodology-faithful exits and the
  discretionary filters as toggles.
- `Python/backtester/strategy/GodTrades.md` — port status + backtest results.
