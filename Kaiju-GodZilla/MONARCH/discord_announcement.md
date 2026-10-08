# Discord Announcement — MONARCH Intelligence Report System

---

**Paste this into Discord:**

---

@everyone

**Introducing MONARCH – Intelligence Reports for GodZilla traders** 🗂️

If you've been running GodZilla or GodZuki, you now have a dedicated report generator that turns your trade logs into clean, browser-based performance reports — automatically.

---

**What MONARCH does:**

- Reads your GodZilla CSV logs directly from NinjaTrader 8
- Generates a **daily report** for every session — P&L, win rate, profit factor, signal combo breakdown, per-account stats, and a full trade log
- Generates a **weekly summary** every Friday automatically
- Builds **Castle Bravo** — a command-centre hub page with a 4-week calendar, cumulative all-time stats, and per-account totals
- Handles multiple accounts (prop firm live and Sim copy-trade leaders) side by side
- Runs as a single `.exe` — no Python, no installs, nothing else required

---

**Setup — 2 steps:**

**1.** Download `MONARCH.exe` from the link below

**2.** Save it here (create the folder if it doesn't exist):
```
C:\Users\<YourName>\Documents\NinjaTrader 8\MONARCH\MONARCH.exe
```
> If your Documents folder is on OneDrive it'll look something like:
> `C:\Users\<YourName>\OneDrive\Documents\NinjaTrader 8\MONARCH\MONARCH.exe`
> Either location works — MONARCH finds it automatically.

That's it. Double-click `MONARCH.exe` and it takes care of the rest.

---

**First run:**
- MONARCH scans your NinjaTrader 8 folder for all `GodZilla_*.csv` files
- Moves them into `NinjaTrader 8\MONARCH\logs\` (keeps them out of your NT8 tree)
- Generates daily reports for every date it finds data for
- Opens a `CastleBravo.html` hub page — bookmark it in your browser

---

**Want it to run automatically every day?**

Set up Windows Task Scheduler to run it Mon–Fri at 5:00 PM with the `-d` flag:
```
Program:   MONARCH.exe
Arguments: -d
```
Friday at 5pm generates both the daily report and the weekly summary. Done.

---

**Questions?** Drop them here or reach out directly:
🌐 greybeardconsulting.net
📧 greybeard@greybeardconsulting.net

*MONARCH Intelligence Report System v1.0.2 — GreyBeard Consulting*

---
