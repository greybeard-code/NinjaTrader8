"""
AUGUR Trade Intelligence – Configuration
==========================================
Branding, instrument point values, account helpers, folder initialisation.
"""

import os
import json
from pathlib import Path

# ── Branding ──────────────────────────────────────────────────────────────────
VERSION  = "1.0.0"
APP_NAME = "AUGUR Trade Intelligence"
HUB_FILE = "Templum.html"
AUTHOR   = "GreyBeard"
EMAIL    = "greybeard@greybeardconsulting.net"
WEBSITE  = "https://greybeardconsulting.net"

# ── Instrument point values ───────────────────────────────────────────────────
# Dollar value per 1 full point move per contract.
POINT_VALUES: dict[str, float] = {
    'MNQ':  2.0,       # Micro E-mini NASDAQ-100
    'NQ':   20.0,      # E-mini NASDAQ-100
    'ES':   50.0,      # E-mini S&P 500
    'MES':  5.0,       # Micro E-mini S&P 500
    'YM':   5.0,       # E-mini Dow Jones
    'MYM':  0.5,       # Micro E-mini Dow Jones
    'RTY':  50.0,      # E-mini Russell 2000
    'M2K':  5.0,       # Micro E-mini Russell 2000
    'GC':   100.0,     # Gold
    'MGC':  10.0,      # Micro Gold
    'SI':   5000.0,    # Silver
    'CL':   1000.0,    # Crude Oil (WTI)
    'MCL':  100.0,     # Micro Crude Oil
    'NG':   10000.0,   # Natural Gas
    'HG':   250.0,     # Copper
    'ZB':   1000.0,    # 30-Year US Treasury Bond
    'ZN':   1000.0,    # 10-Year US Treasury Note
    'ZF':   1000.0,    # 5-Year US Treasury Note
}


def get_point_value(instrument: str) -> float:
    """Return $/point for an instrument like 'MNQ 06-26'. Falls back to 1.0."""
    ticker = instrument.split()[0].upper() if instrument else ''
    val = POINT_VALUES.get(ticker)
    if val is None:
        print(f"  [warn] Unknown instrument '{ticker}' – P&L calculated at $1/point. "
              f"Add it to POINT_VALUES in config.py.")
        return 1.0
    return val


def is_live_account(account: str) -> bool:
    """Sim* accounts are sim; everything else (LFE*, APEX*, etc.) is live."""
    return not account.strip().lower().startswith('sim')


def get_account_label(account: str) -> str:
    """Short display label: last 6 chars for long account numbers, full name otherwise."""
    a = account.strip()
    if len(a) > 12:
        return a[-6:]
    return a


# ── NT8 folder detection ──────────────────────────────────────────────────────

def _windows_documents_path() -> Path | None:
    try:
        import winreg
        key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r'Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders',
        )
        docs, _ = winreg.QueryValueEx(key, 'Personal')
        winreg.CloseKey(key)
        return Path(docs)
    except Exception:
        return None


def find_augur_dir(override: str = None) -> Path:
    """
    Locate or create the AUGUR folder.

    Search order:
      1. --augur-path CLI argument
      2. AUGUR_PATH environment variable
      3. Windows registry Documents path + 'AUGUR'
      4. ~/Documents/AUGUR
    """
    if override:
        p = Path(override)
        p.mkdir(parents=True, exist_ok=True)
        return p

    env_path = os.environ.get('AUGUR_PATH')
    if env_path:
        p = Path(env_path)
        p.mkdir(parents=True, exist_ok=True)
        return p

    reg_docs = _windows_documents_path()
    if reg_docs and reg_docs.exists():
        p = reg_docs / 'AUGUR'
        p.mkdir(parents=True, exist_ok=True)
        return p

    # Fallback
    p = Path.home() / 'Documents' / 'AUGUR'
    p.mkdir(parents=True, exist_ok=True)
    return p


def init_augur_dirs(augur_dir: Path) -> tuple[Path, Path, Path]:
    """
    Create the AUGUR folder structure.

    Returns:
        (augur_dir, imports_dir, reports_dir)
    """
    imports = augur_dir / 'imports'
    reports = augur_dir / 'reports'
    for d in (imports, reports):
        d.mkdir(parents=True, exist_ok=True)
    return augur_dir, imports, reports


# ── Per-installation config file ─────────────────────────────────────────────

def load_local_config(augur_dir: Path) -> dict:
    cfg_file = augur_dir / 'config.json'
    if cfg_file.exists():
        try:
            with open(cfg_file, encoding='utf-8') as f:
                return json.load(f)
        except Exception:
            pass
    return {}


def save_local_config(augur_dir: Path, cfg: dict):
    with open(augur_dir / 'config.json', 'w', encoding='utf-8') as f:
        json.dump(cfg, f, indent=2)
