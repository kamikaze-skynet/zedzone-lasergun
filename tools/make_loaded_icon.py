"""Inventory picture of the rifle with its battery pack fitted (src/Resources/rifle_loaded.png, 299x118, the same
box as the rifle sprite's Main region so the game draws it at exactly the same place and size as the bare gun).
Made from the user's picture of the loaded rifle (art-src/laser-rifle-cover-reference.png), scaled onto the game
sprite so both line up."""
import os, sys, numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_cover import cutout, REPO
REGION = (5, 14, 299, 118)

def main():
    sp = Image.open(os.path.join(REPO, "items", "RangedWeapons", "rifle", "sprite.png")).convert("RGBA")
    a = np.array(sp)[..., 3] > 128
    xs = np.nonzero(a.any(0))[0]; ys = np.nonzero(a.any(1))[0]
    x0, x1, y0 = xs.min(), xs.max() + 1, ys.min()
    art = cutout("laser-rifle-cover-reference.png")
    k = (x1 - x0) / art.width
    art = art.resize((x1 - x0, round(art.height * k)), Image.LANCZOS)
    canvas = Image.new("RGBA", sp.size, (0, 0, 0, 0))
    canvas.alpha_composite(art, (int(x0), int(y0)))
    out = canvas.crop((REGION[0], REGION[1], REGION[0] + REGION[2], REGION[1] + REGION[3]))
    out.save(os.path.join(REPO, "src", "Resources", "rifle_loaded.png"))
    # check picture: bare sprite over the loaded one, 4x
    chk = Image.new("RGBA", (sp.width, sp.height * 2), (60, 66, 52, 255))
    chk.alpha_composite(canvas, (0, 0)); chk.alpha_composite(sp, (0, sp.height))
    return chk.resize((chk.width * 3, chk.height * 3), Image.LANCZOS), art.height, int(y0)

if __name__ == "__main__":
    im, h, y0 = main(); print("art height", h, "top", y0, "bottom", y0 + h)
    im.save(sys.argv[1] if len(sys.argv) > 1 else "/tmp/loaded_check.png")
