# Regenerates assets/midday-shift.png (the 3-shift-mode middle icon).
# The base sun sprite is eMka's `first-shift` texture inside the mod's own
# asset bundle; it is extracted to a temp file, never committed. The composed
# output (sun + cloud) is transformative new art.
#
# Usage:  py make-midday-icon.py [path-to-secondshift_win]
#         (default: ..\around-the-clock\version-1.1\AssetBundles\secondshift_win)
# Requires: pip install pillow unitypy
import io
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw

TAN = (199, 174, 123, 255)  # the mod icons' color, sampled from first-shift
BLK = (0, 0, 0, 255)
SCALE = 4  # supersample factor, downsampled back to 52px


def extract_sun(bundle_path: Path) -> Image.Image:
    import UnityPy

    env = UnityPy.load(str(bundle_path))
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        data = obj.read()
        if data.m_Name == "first-shift":
            return data.image.convert("RGBA")
    raise SystemExit("first-shift texture not found in bundle")


def main() -> None:
    default = Path(__file__).resolve().parents[2] / "around-the-clock" / \
        "version-1.1" / "AssetBundles" / "secondshift_win"
    bundle = Path(sys.argv[1]) if len(sys.argv) > 1 else default
    sun = extract_sun(bundle)
    ref = Path(tempfile.gettempdir()) / "first-shift-extracted.png"
    sun.save(ref)

    img = Image.new("RGBA", (52 * SCALE, 52 * SCALE), (0, 0, 0, 0))
    sized = sun.resize((int(52 * 0.7) * SCALE, int(52 * 0.7) * SCALE), Image.LANCZOS)
    img.alpha_composite(sized, (0, 0))

    def cloud(draw: ImageDraw.ImageDraw, dx: int, dy: int, color: tuple) -> None:
        draw.rounded_rectangle(
            [10 * SCALE + dx, 38 * SCALE + dy, 49 * SCALE + dx, 46 * SCALE + dy],
            radius=4 * SCALE, fill=color)
        draw.ellipse([(12 * SCALE + dx, 30 * SCALE + dy), (27 * SCALE + dx, 41 * SCALE + dy)], fill=color)
        draw.ellipse([(21 * SCALE + dx, 27 * SCALE + dy), (38 * SCALE + dx, 41 * SCALE + dy)], fill=color)
        draw.ellipse([(34 * SCALE + dx, 31 * SCALE + dy), (48 * SCALE + dx, 41 * SCALE + dy)], fill=color)

    d = ImageDraw.Draw(img)
    cloud(d, 0, 2, BLK)   # silhouette offset for the under-shadow look
    cloud(d, 0, 0, TAN)
    img = img.resize((52, 52), Image.LANCZOS)
    out = Path(__file__).resolve().parent / "midday-shift.png"
    img.save(out)
    print(f"sun ref (temp): {ref}\nwrote: {out}")


if __name__ == "__main__":
    main()
