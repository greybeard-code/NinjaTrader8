"""
MONARCH Intelligence Report System – Windows Version Info Generator
====================================================================
Generates version_info.txt in PyInstaller's VSVersionInfo format.
The file is passed to PyInstaller via --version-file and embeds
metadata into the exe's Windows Properties > Details tab.

Run once before building, or let build.ps1 call it automatically:
    python make_version_file.py

Output: version_info.txt  (listed in .gitignore – rebuild artifact)
"""

import sys
from pathlib import Path

# ── Pull constants from src/config.py without importing the full module ────────
_src = Path(__file__).parent / 'src'
if str(_src) not in sys.path:
    sys.path.insert(0, str(_src))

from config import VERSION, APP_NAME, AUTHOR, EMAIL, WEBSITE


def _version_tuple(ver_str: str) -> tuple:
    """Convert '1.0.1' → (1, 0, 1, 0). Always returns a 4-tuple."""
    parts = [int(x) for x in ver_str.split('.') if x.isdigit()]
    parts += [0] * (4 - len(parts))
    return tuple(parts[:4])


def make_version_file(out_path: Path):
    major, minor, patch, build = _version_tuple(VERSION)
    ver_str  = f"{major}.{minor}.{patch}.{build}"
    year     = __import__('datetime').date.today().year
    copyright_str = f"© {year} {AUTHOR} – {WEBSITE}"

    content = f"""\
# UTF-8
VSVersionInfo(
  ffi=FixedFileInfo(
    filevers=({major}, {minor}, {patch}, {build}),
    prodvers=({major}, {minor}, {patch}, {build}),
    mask=0x3f,
    flags=0x0,
    OS=0x40004,
    fileType=0x1,
    subtype=0x0,
    date=(0, 0),
  ),
  kids=[
    StringFileInfo([
      StringTable(
        '040904B0',
        [
          StringStruct('CompanyName',      '{AUTHOR}'),
          StringStruct('FileDescription',  '{APP_NAME}'),
          StringStruct('FileVersion',      '{ver_str}'),
          StringStruct('InternalName',     'MONARCH'),
          StringStruct('LegalCopyright',   '{copyright_str}'),
          StringStruct('OriginalFilename', 'MONARCH.exe'),
          StringStruct('ProductName',      '{APP_NAME}'),
          StringStruct('ProductVersion',   '{VERSION}'),
          StringStruct('Comments',         '{WEBSITE}'),
        ]
      )
    ]),
    VarFileInfo([VarStruct('Translation', [0x0409, 1200])])
  ]
)
"""
    out_path.write_text(content, encoding='utf-8')
    print(f"  Version info written: {out_path}")
    print(f"  Version : {VERSION}")
    print(f"  Author  : {AUTHOR}")
    print(f"  Web     : {WEBSITE}")


if __name__ == '__main__':
    make_version_file(Path(__file__).parent / 'version_info.txt')
