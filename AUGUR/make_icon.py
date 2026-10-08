"""
AUGUR Trade Intelligence – Icon Generator
==========================================
Generates augur.ico for the compiled exe.

Design: dark indigo background, large white 'A' with teal glow,
small teal eye/star in lower-right (the augur's prophetic sight).
Colors match the CSS theme in templates.py.

Run once before building:
    python make_icon.py
"""

def make_icon():
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        import subprocess, sys
        subprocess.check_call([sys.executable, '-m', 'pip', 'install', 'Pillow'])
        from PIL import Image, ImageDraw, ImageFont

    sizes = [16, 32, 48, 256]
    frames = []

    BG      = (15,  17,  23)    # --bg
    BORDER  = (46,  50,  80)    # --border
    WHITE   = (226, 230, 243)   # --text
    TEAL    = (45,  212, 191)   # --teal
    ACCENT  = (99,  102, 241)   # --accent

    for size in sizes:
        img  = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Rounded rect background
        r    = max(2, size // 8)
        pad  = max(1, size // 16)
        draw.rounded_rectangle([pad, pad, size - pad - 1, size - pad - 1],
                               radius=r, fill=BG, outline=BORDER,
                               width=max(1, size // 32))

        # Teal glow behind the 'A' (slightly larger, blurred by offset)
        if size >= 32:
            glow_size  = int(size * 0.68)
            glow_x     = (size - glow_size) // 2
            glow_y     = int(size * 0.12)
            glow_color = (*TEAL, 40)
            try:
                gf = ImageFont.truetype("arialbd.ttf", glow_size)
            except Exception:
                gf = ImageFont.load_default()
            draw.text((glow_x + 1, glow_y + 1), 'A', font=gf, fill=glow_color)

        # Main 'A' in white
        letter_size = int(size * 0.65)
        try:
            font = ImageFont.truetype("arialbd.ttf", letter_size)
        except Exception:
            font = ImageFont.load_default()

        bbox = draw.textbbox((0, 0), 'A', font=font)
        lw   = bbox[2] - bbox[0]
        lh   = bbox[3] - bbox[1]
        lx   = (size - lw) // 2 - bbox[0]
        ly   = int(size * 0.12) - bbox[1]
        draw.text((lx, ly), 'A', font=font, fill=WHITE)

        # Small teal dot in lower-right (the augur's eye)
        if size >= 32:
            dot_r = max(2, size // 10)
            dot_x = size - pad - dot_r * 2 - 2
            dot_y = size - pad - dot_r * 2 - 2
            draw.ellipse([dot_x, dot_y, dot_x + dot_r * 2, dot_y + dot_r * 2],
                         fill=TEAL)

        frames.append(img)

    frames[0].save(
        'augur.ico',
        format='ICO',
        sizes=[(s, s) for s in sizes],
        append_images=frames[1:],
    )
    print("  Wrote augur.ico")


if __name__ == '__main__':
    print("\n  AUGUR – Generating augur.ico...\n")
    make_icon()
    print("\n  Done. Run build.ps1 to compile AUGUR.exe.\n")
