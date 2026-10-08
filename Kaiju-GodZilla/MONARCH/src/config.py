"""
MONARCH Intelligence Report System – Configuration
====================================================
Path detection, folder initialisation, and shared constants.
"""

import os
import json
from pathlib import Path

# ── Branding ──────────────────────────────────────────────────────────────────
VERSION  = "1.0.4"
APP_NAME = "MONARCH Intelligence Report System"
HUB_FILE = "CastleBravo.html"
AUTHOR   = "GreyBeard"
EMAIL    = "greybeard@greybeardconsulting.net"
WEBSITE  = "https://greybeardconsulting.net"

# ── Account label / grade maps ────────────────────────────────────────────────
# Populated at runtime from the local config.json (in NinjaTrader 8\MONARCH\),
# which lives OUTSIDE this repo — no personal account numbers are committed here.
# See apply_account_config(). Unknown accounts fall back to the last 6 chars of
# the account name (label) and '?' (grade), so MONARCH works with no config.
ACCT_LABEL: dict = {}
ACCT_GRADE: dict = {}


def apply_account_config(local_cfg: dict):
    """Merge account label/grade maps from config.json into the module maps.

    config.json shape:
      {
        "accounts": {
          "<ACCOUNT_ID>": { "label": "084", "grade": "G4" },
          ...
        }
      }
    Called once at startup after load_local_config().
    """
    accounts = local_cfg.get('accounts', {})
    if not isinstance(accounts, dict):
        return
    for acct_id, info in accounts.items():
        if not isinstance(info, dict):
            continue
        if info.get('label'):
            ACCT_LABEL[str(acct_id)] = str(info['label'])
        if info.get('grade'):
            ACCT_GRADE[str(acct_id)] = str(info['grade'])

# ── Trading-session boundary ──────────────────────────────────────────────────
# Hour (0-23, in the chart's local time) at which a new trading session begins.
# Default 18 (6 PM) is correct when NT8 charts render in ET. Override in
# config.json ("session_boundary_hour") or with --session-hour if your charts
# use a different timezone.
SESSION_BOUNDARY_HOUR = 18


def get_session_boundary_hour(local_cfg: dict) -> int:
    """Resolve the session-start hour from local config.json, clamped to 0-23.
    Falls back to SESSION_BOUNDARY_HOUR (18) if unset or invalid."""
    try:
        h = int(local_cfg.get('session_boundary_hour', SESSION_BOUNDARY_HOUR))
        if 0 <= h <= 23:
            return h
    except (ValueError, TypeError):
        pass
    return SESSION_BOUNDARY_HOUR


def get_account_label(account: str) -> str:
    """Short display label: known accounts use the ACCT_LABEL map, others use last 6 chars."""
    return ACCT_LABEL.get(account, account[-6:] if len(account) >= 6 else account)


def get_account_grade(account: str) -> str:
    """Grade threshold string: known accounts from ACCT_GRADE map, others '?'."""
    return ACCT_GRADE.get(account, '?')


# ── NT8 folder detection ──────────────────────────────────────────────────────

def _windows_documents_path() -> Path | None:
    """
    Ask Windows where the current user's Documents folder actually lives.
    This is the authoritative source – it reflects OneDrive redirection,
    folder moves, and any other shell customisation.
    Returns None on non-Windows or if the registry read fails.
    """
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


def _candidate_documents_dirs() -> list[Path]:
    """
    Build an ordered list of directories to search for NinjaTrader 8.
    Covers: registry-reported Documents, OneDrive (personal and business),
    literal Documents, and all user profiles as a last resort.
    """
    home     = Path.home()
    username = os.environ.get('USERNAME') or os.environ.get('USER') or ''
    seen     = set()
    candidates: list[Path] = []

    def add(p: Path):
        if p not in seen:
            seen.add(p)
            candidates.append(p)

    # 1. Registry – the only truly reliable source on Windows
    reg_docs = _windows_documents_path()
    if reg_docs:
        add(reg_docs)

    # 2. OneDrive variants (personal and business/tenant)
    #    OneDrive folder names vary: "OneDrive", "OneDrive - Personal",
    #    "OneDrive - CompanyName", etc.
    for entry in home.iterdir() if home.exists() else []:
        if entry.is_dir() and entry.name.lower().startswith('onedrive'):
            add(entry / 'Documents')

    # 3. Literal Documents (no OneDrive, or as a fallback)
    add(home / 'Documents')
    add(Path(f'C:/Users/{username}/Documents'))

    return candidates


def find_nt8_folder(override: str = None) -> Path:
    """
    Locate the NinjaTrader 8 Documents folder.

    Search order:
      1. --nt8-path CLI argument
      2. NT8_PATH environment variable
      3. Windows registry Shell Folders (handles OneDrive redirection)
      4. OneDrive sub-folders under the user's home directory
      5. Literal Documents paths
    """
    # 1. Explicit override
    if override:
        p = Path(override)
        if p.exists():
            return p
        raise FileNotFoundError(f"NT8 path not found: {override}")

    # 2. Environment variable
    env_path = os.environ.get('NT8_PATH')
    if env_path:
        p = Path(env_path)
        if p.exists():
            return p

    # 3-5. Walk candidate Documents directories
    for docs_dir in _candidate_documents_dirs():
        candidate = docs_dir / 'NinjaTrader 8'
        if candidate.exists():
            return candidate

    raise FileNotFoundError(
        "NinjaTrader 8 folder not found. "
        "Searched: Documents, OneDrive/Documents, registry Shell Folders. "
        "Use --nt8-path to specify the path, "
        "or set the NT8_PATH environment variable."
    )


# ── MONARCH folder structure ──────────────────────────────────────────────────

def init_monarch_dirs(nt8: Path):
    """
    Create the MONARCH folder structure inside the NT8 directory.

    Returns:
        (monarch_dir, logs_dir, reports_dir)
    """
    monarch = nt8 / 'MONARCH'
    logs    = monarch / 'logs'
    reports = monarch / 'reports'
    for d in (monarch, logs, reports):
        d.mkdir(parents=True, exist_ok=True)
    return monarch, logs, reports


# ── Per-installation config file ─────────────────────────────────────────────

def load_local_config(monarch_dir: Path) -> dict:
    """Load optional config.json from the MONARCH folder."""
    cfg_file = monarch_dir / 'config.json'
    if cfg_file.exists():
        try:
            with open(cfg_file, encoding='utf-8') as f:
                return json.load(f)
        except Exception:
            pass
    return {}


def save_local_config(monarch_dir: Path, cfg: dict):
    with open(monarch_dir / 'config.json', 'w', encoding='utf-8') as f:
        json.dump(cfg, f, indent=2)


def ensure_local_config_template(monarch_dir: Path):
    """Write a starter config.json into the MONARCH folder if none exists, so the
    user has something to edit (account labels/grades, session hour). Never
    overwrites an existing file. This file lives in NinjaTrader 8\\MONARCH\\ —
    outside the source repo — so personal account numbers stay private.
    """
    cfg_file = monarch_dir / 'config.json'
    if cfg_file.exists():
        return
    template = {
        "session_boundary_hour": 18,
        "accounts": {
            "YOUR_ACCOUNT_ID_HERE": {"label": "L1", "grade": "G4"}
        }
    }
    try:
        with open(cfg_file, 'w', encoding='utf-8') as f:
            json.dump(template, f, indent=2)
    except Exception:
        pass