# Remove-GodZillaSuite.ps1 -- Cleanup Script

Finds old or misplaced GodZilla Suite source files (`.cs`) anywhere under your
NinjaTrader 8 folder and moves them to a staging folder in your Downloads
directory for review before deletion.

Run this before installing a fresh copy of the GodZilla Suite to ensure you
are starting from a clean slate.

---

## Destination Folder

All found files are moved to:

```
%USERPROFILE%\Downloads\OldGreybeard-Delete\
```

Review the contents of that folder before deleting it. Nothing is permanently
deleted by this script.

---

## How to Run

1. **Close NinjaTrader 8 completely** before running. Open files cannot be moved.

2. Open **PowerShell** (search "PowerShell" in the Start menu).

3. Run one of the following commands:

   **Standard** (if your execution policy allows local scripts):
   ```powershell
   & "$env:USERPROFILE\Downloads\Remove-GodZillaSuite.ps1"
   ```

   **Bypass policy** (if you get an "execution policy" error):
   ```powershell
   PowerShell -ExecutionPolicy Bypass -File "%USERPROFILE%\Downloads\Remove-GodZillaSuite.ps1"
   ```

4. Review the output. The script lists every file it found and moved, and
   reports any failures.

---

## Files This Script Targets

**Sub-indicators:**
- `gbBarStatus.cs`
- `gbKingOrderBlock.cs`
- `gbPANAKanal.cs`
- `gbSumoPullback.cs`
- `gbSuperJumpBoost.cs`
- `gbThunderZilla.cs`
- `gbNobleCloud.cs`
- `gbNobelCloud.cs` *(alternate spelling found on some installs)*
- `NewsSignals.cs`

**Wrapper / strategy files (retired):**
- `gbKingPanaZilla.cs`
- `gbKingPanaZillaKillah.cs`
- `GodZilla.cs`
- `GodZillaKilla.cs`
- `GodZuki.cs`

---

## Troubleshooting

**"File cannot be moved" or "Access denied"**
NinjaTrader 8 is still running and has the file locked. Close NT8 fully and
run the script again.

**"Execution of scripts is disabled on this system"**
Use the Bypass command shown in step 3 above.

**"NinjaTrader 8 folder not found"**
NT8 is not installed in the default Documents location. The script only
searches `Documents\NinjaTrader 8\`.

**File appears twice in the destination** (e.g. `GodZillaKilla_Playr101.cs`)
The same filename was found in two different subfolders. The script appends
the source folder name to avoid overwriting. Both copies are preserved --
delete both.

---

## After Running

Once the `OldGreybeard-Delete` folder has been reviewed, delete the entire
folder. Then install the current GodZilla Suite package following the
instructions in the install guide.

---

Support: greybeard@greybeardconsulting.net | [greybeardconsulting.net](https://greybeardconsulting.net)
