"""Workshop cover (workshop/preview.png): the rifle with its battery pack fitted, firing a beam.
Uses art-src/laser-rifle-clean.png (make_rifle_from_reference.py) and the pack sprite cutout."""
import os, numpy as np, cv2
from PIL import Image, ImageDraw, ImageFilter, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.join(HERE, "..")
K = 0.1408                      # full art -> game sprite scale
ORIGIN = (75 - 466 * K, 99 - 376 * K)   # full art (0,0) in sprite canvas px
PACK_CENTRE = (154.5 - 38.5, 73 + 36)   # sprite canvas px (matches Battery.PackCentrePx)
PACK_W = 26

def pack_cutout():
    img = cv2.imread(os.path.join(REPO, "art-src", "laser-battery-pack-reference.png")); h, w = img.shape[:2]
    white = (img.min(axis=2) > 232).astype(np.uint8)
    n, lab = cv2.connectedComponents(white); bg = np.zeros_like(white)
    for i in range(1, n):
        ys, xs = np.nonzero(lab == i)
        if ys.min() == 0 or xs.min() == 0 or ys.max() == h - 1 or xs.max() == w - 1: bg |= (lab == i).astype(np.uint8)
    a = cv2.GaussianBlur(((1 - bg) * 255).astype(np.float32), (0, 0), 0.7).clip(0, 255).astype(np.uint8)
    im = Image.fromarray(np.dstack([img[..., ::-1], a]), "RGBA")
    return im.crop(im.getbbox())

def gun_with_pack():
    gun = Image.open(os.path.join(REPO, "art-src", "laser-rifle-clean.png"))
    pk = pack_cutout()
    pw = PACK_W / K; ph = pk.height * pw / pk.width
    pk = pk.resize((round(pw), round(ph)), Image.LANCZOS)
    cx = (PACK_CENTRE[0] - ORIGIN[0]) / K; cy = (PACK_CENTRE[1] - ORIGIN[1]) / K
    W = max(gun.width, round(cx + pw / 2) + 2); H = max(gun.height, round(cy + ph / 2) + 2)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.alpha_composite(pk, (round(cx - pw / 2), round(cy - ph / 2)))
    out.alpha_composite(gun)
    return out

def main():
    full = gun_with_pack()
    a = np.array(full)[..., 3]; xr = np.nonzero(a.max(0) > 128)[0].max()
    ys = np.nonzero(a[:, xr - 15:xr].max(1) > 128)[0]; my = (ys.min() + ys.max()) / 2
    W = 512; out = Image.new("RGBA", (W, W), (28, 32, 26, 255)); g = ImageDraw.Draw(out)
    for r in range(360, 0, -6):
        k = int(34 * (1 - r / 360)); g.ellipse([200 - r, 260 - r, 200 + r, 260 + r], fill=(28 + k, 32 + k, 26 + k, 255))
    bb = full.getbbox(); gun = full.crop(bb); sc = 470 / gun.width
    gun = gun.resize((round(gun.width * sc), round(gun.height * sc)), Image.LANCZOS); ox = 6; oy = round(250 - gun.height / 2)
    beam = Image.new("RGBA", (W, W), (0, 0, 0, 0)); bd = ImageDraw.Draw(beam)
    y = oy + (my - bb[1]) * sc; x0 = ox + (xr - bb[0]) * sc
    for w_, a_ in ((22, 40), (12, 90), (6, 200)): bd.line([(x0, y), (W, y)], fill=(255, 70, 40, a_), width=w_)
    bd.line([(x0, y), (W, y)], fill=(255, 235, 220, 255), width=2); bd.ellipse([x0 - 14, y - 14, x0 + 14, y + 14], fill=(255, 120, 80, 120))
    out.alpha_composite(beam.filter(ImageFilter.GaussianBlur(1.2))); out.alpha_composite(gun, (ox, oy))
    f = ImageFont.truetype("DejaVuSans-Bold.ttf", 54); f2 = ImageFont.truetype("DejaVuSans-Bold.ttf", 22); g = ImageDraw.Draw(out)
    g.text((W // 2, 70), "LASER GUN", font=f, fill=(255, 92, 60, 255), anchor="mm", stroke_width=3, stroke_fill=(15, 15, 15, 255))
    g.text((W // 2, 420), "350 damage  |  2000-energy packs", font=f2, fill=(220, 226, 200, 255), anchor="mm", stroke_width=2, stroke_fill=(15, 15, 15, 255))
    g.text((W // 2, 452), "sets targets on fire", font=f2, fill=(220, 226, 200, 255), anchor="mm", stroke_width=2, stroke_fill=(15, 15, 15, 255))
    out.convert("RGB").save(os.path.join(REPO, "workshop", "preview.png"))
    # also a check render of the in-game sprite with the pack (4x)
    sp = Image.open(os.path.join(REPO, "items", "RangedWeapons", "rifle", "sprite.png"))
    pk = pack_cutout(); ph = pk.height * PACK_W / pk.width
    pk = pk.resize((PACK_W, round(ph)), Image.LANCZOS)
    bg = Image.new("RGBA", sp.size, (60, 66, 52, 255))
    bg.alpha_composite(pk, (round(PACK_CENTRE[0] - PACK_W / 2), round(PACK_CENTRE[1] - ph / 2))); bg.alpha_composite(sp)
    return bg.resize((sp.width * 4, sp.height * 4), Image.NEAREST)

if __name__ == "__main__":
    main().save("/tmp/ingame_pack.png")
