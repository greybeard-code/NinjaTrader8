"""
Generate monarch.ico for the MONARCH exe.
Requires Pillow: pip install pillow

Creates a 256x256 dark icon with a glowing 'M' on an indigo background,
saved as a multi-resolution ICO (16, 32, 48, 256 px).
"""

try:
    from PIL import Image, ImageDraw, ImageFont, ImageFilter
except ImportError:
    import subprocess, sys
    print("Installing Pillow...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow"])
    from PIL import Image, ImageDraw, ImageFont, ImageFilter

from pathlib import Path

OUT = Path(__file__).parent / "monarch.ico"

# ── Colours (match the HTML theme) ───────────────────────────────────────────
BG_DARK   = (15,  17,  23)   # --bg
INDIGO    = (99,  102, 241)  # --accent
INDIGO_DIM= (49,  51,  120)
TEAL      = (45,  212, 191)  # --teal
WHITE     = (226, 230, 243)  # --text


def make_frame(size: int) -> Image.Image:
    img  = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    pad = size // 8
    r   = size // 6   # corner radius

    # ── Rounded rectangle background ─────────────────────────────────────────
    draw.rounded_rectangle(
        [pad, pad, size - pad, size - pad],
        radius=r,
        fill=BG_DARK,
    )

    # ── Indigo border ─────────────────────────────────────────────────────────
    border = max(1, size // 32)
    draw.rounded_rectangle(
        [pad, pad, size - pad, size - pad],
        radius=r,
        outline=INDIGO,
        width=border,
    )

    # ── Letter M centred ──────────────────────────────────────────────────────
    # Try a bold system font; fall back to default
    font_size = int(size * 0.58)
    font = None
    for fname in [
        "arialbd.ttf", "Arial Bold.ttf", "DejaVuSans-Bold.ttf",
        "verdanab.ttf", "calibrib.ttf",
    ]:
        try:
            font = ImageFont.truetype(fname, font_size)
            break
        except Exception:
            pass
    if font is None:
        font = ImageFont.load_default()

    letter = "M"
    bbox   = draw.textbbox((0, 0), letter, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = (size - tw) // 2 - bbox[0]
    ty = (size - th) // 2 - bbox[1] - int(size * 0.04)

    # Glow layer (blur a teal copy behind the main letter)
    if size >= 48:
        glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        gd   = ImageDraw.Draw(glow)
        gd.text((tx, ty), letter, font=font, fill=(*TEAL, 180))
        glow = glow.filter(ImageFilter.GaussianBlur(radius=max(2, size // 24)))
        img  = Image.alpha_composite(img, glow)
        draw = ImageDraw.Draw(img)

    # Main letter in white/indigo gradient approximation (draw twice)
    draw.text((tx + 1, ty + 1), letter, font=font, fill=(*INDIGO, 200))
    draw.text((tx, ty),         letter, font=font, fill=WHITE)

    # ── Small teal dot bottom-right (signal indicator) ────────────────────────
    if size >= 32:
        dot_r  = max(2, size // 10)
        dot_cx = size - pad - dot_r
        dot_cy = size - pad - dot_r
        draw.ellipse(
            [dot_cx - dot_r, dot_cy - dot_r, dot_cx + dot_r, dot_cy + dot_r],
            fill=TEAL,
        )

    return img


sizes  = [16, 32, 48, 256]
frames = [make_frame(s) for s in sizes]

frames[0].save(
    OUT,
    format="ICO",
    sizes=[(s, s) for s in sizes],
    append_images=frames[1:],
)

print(f"Icon saved: {OUT}")
print("Run build.ps1 to recompile with the new icon.")
