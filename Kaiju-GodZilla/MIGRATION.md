# Migration Guide — Namespace Change

## What Changed

The indicator namespace was simplified:

| Before | After |
|---|---|
| `NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla` | `NinjaTrader.NinjaScript.Indicators.GreyBeard` |

The strategy namespace (`NinjaTrader.NinjaScript.Strategies.Playr101`) is unchanged.

---

## Impact

NT8 uses the namespace for internal serialization of chart templates and ATM settings. **All saved chart templates and ATM configurations that reference these indicators will fail to load silently.**

---

## How to Migrate Existing Setups

### Chart Templates

1. Open NinjaTrader and load your existing chart template. It will likely fail to restore the indicators.
2. Manually add each indicator back from the Indicators dialog.
3. Re-enter all parameter values.
4. Save the template under the same name to overwrite the old one.

### ATM Strategy Settings

ATM strategy files saved with indicator-linked settings do not reference the indicator namespace directly — ATM templates should be unaffected.

### Workspace Layouts

If you have saved workspaces that include charts with these indicators:
1. Open the workspace. Charts with the old indicators will show errors.
2. Remove the broken indicator instances.
3. Re-add each indicator with your previous settings.
4. Re-save the workspace.

---

## Recompiling the Indicators

After loading the updated files into NT8, compile in this order:

1. All six sub-indicators (any order among themselves)
2. GodZuki
3. GodZillaKilla

If a sub-indicator fails to compile, GodZuki and GodZillaKilla will fail with misleading errors. Fix sub-indicators first.
