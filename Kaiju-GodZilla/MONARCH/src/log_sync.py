"""
MONARCH Intelligence Report System – Log Sync
==============================================
Finds all GodZilla_*.csv files in the NT8 folder tree and moves
them into MONARCH/logs/, removing the originals from NT8.

Rules:
  - Include:  GodZilla_*.csv  anywhere under the NT8 root
  - Exclude:  GodZuki_*.csv   (strategy state files, not trade logs)
  - Move behaviour: always copy src → dst (overwriting any existing file),
    then delete src. Copy-first ensures the original survives a partial write.
"""

import shutil
from pathlib import Path
from typing import List, Tuple


def find_source_logs(nt8: Path) -> List[Path]:
    """
    Recursively find all GodZilla_*.csv files under the NT8 root,
    excluding the MONARCH/logs/ destination to prevent self-moves.
    """
    monarch_logs = nt8 / 'MONARCH' / 'logs'
    found = []
    for f in nt8.rglob('GodZilla_*.csv'):
        try:
            f.relative_to(monarch_logs)
            continue   # already in the destination – skip
        except ValueError:
            pass
        if f.is_file():
            found.append(f)
    return sorted(found)


def sync_logs(nt8: Path, logs_dir: Path) -> Tuple[int, int]:
    """
    Move GodZilla CSVs from the NT8 tree into logs_dir.
    Existing files in logs_dir are overwritten. Originals are deleted
    from NT8 after a successful copy.

    Returns:
        (moved_count, failed_count)
    """
    sources = find_source_logs(nt8)
    moved  = 0
    failed = 0

    for src in sources:
        dst = logs_dir / src.name
        try:
            shutil.copy2(src, dst)   # copy2 preserves metadata; overwrites dst
            src.unlink()             # only delete after successful copy
            moved += 1
        except Exception as e:
            print(f"  [warn] Could not move {src.name}: {e}")
            failed += 1
            # dst may be partially written – remove it to avoid corrupt data
            try:
                dst.unlink(missing_ok=True)
            except Exception:
                pass

    return moved, failed
