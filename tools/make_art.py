"""Draws the Laser Gun item art (original, not traced from any existing artwork).
Sprites are drawn at 4x and downsampled. The rifle canvas and sprite regions match the
Boltgun rifle so the same hand / muzzle / magazine offsets line up.
Run:  python tools/make_art.py"""
import os, random
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ITEMS = os.path.join(HERE, "..", "items")
S = 4  # supersampling

# Palette
OLIVE      = (122, 138, 52)
OLIVE_LT   = (160, 176, 78)
OLIVE_DK   = (78, 90, 30)
OLIVE_DDK  = (52, 60, 22)
BLACK      = (30, 31, 34)
BLACK_LT   = (62, 64, 70)
BLACK_DK   = (14, 14, 16)
STEEL      = (170, 174, 180)
STEEL_LT   = (225, 228, 232)
STEEL_DK   = (96, 100, 108)
RED        = (255, 60, 40)
RED_GLOW   = (255, 150, 90)
OUTLINE    = (18, 18, 20)


def P(pts):
    return [(x * S, y * S) for x, y in pts]


def R(x0, y0, x1, y1):
    return [x0 * S, y0 * S, x1 * S, y1 * S]


def shaded_poly(d, pts, base, lt, dk, outline=OUTLINE, bevel=2):
    """Filled polygon with a dark outline, a light top edge and a dark bottom edge."""
    d.polygon(P(pts), fill=base, outline=None)
    # light top band / dark bottom band, approximated with offset outlines
    d.line(P(pts + [pts[0]]), fill=outline, width=S + 1)
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def box(d, x0, y0, x1, y1, base, lt, dk, edge=1.2):
    d.rectangle(R(x0, y0, x1, y1), fill=base)
    d.rectangle(R(x0, y0, x1, y0 + edge), fill=lt)
    d.rectangle(R(x0, y1 - edge, x1, y1), fill=dk)
    d.rectangle(R(x0, y0, x1, y1), outline=OUTLINE, width=S + 1)


def grime(img, mask_color_fn, seed, amount=0.06):
    """Subtle paint wear: random darker / lighter speckles on opaque pixels."""
    rnd = random.Random(seed)
    px = img.load()
    w, h = img.size
    for _ in range(int(w * h * amount)):
        x = rnd.randrange(w); y = rnd.randrange(h)
        r, g, b, a = px[x, y]
        if a < 200 or not mask_color_fn((r, g, b)):
            continue
        k = rnd.choice((-18, -12, 10, 14))
        px[x, y] = (max(0, min(255, r + k)), max(0, min(255, g + k)), max(0, min(255, b + k)), a)


def is_olive(c):
    r, g, b = c
    return g > r * 0.95 and g > b * 1.5 and g > 50


def scratches(d, rnd, area, n, color):
    x0, y0, x1, y1 = area
    for _ in range(n):
        x = rnd.uniform(x0, x1); y = rnd.uniform(y0, y1)
        L = rnd.uniform(3, 9)
        dx = rnd.uniform(0.6, 1.0) * L; dy = -rnd.uniform(0.4, 1.1) * L
        d.line(P([(x, y), (x + dx, y + dy)]), fill=color, width=max(1, S // 2))


def rivet(d, x, y, r=1.4):
    d.ellipse(R(x - r, y - r, x + r, y + r), fill=STEEL_DK, outline=OUTLINE, width=2)
    d.ellipse(R(x - r * 0.5, y - r * 0.6, x + r * 0.1, y), fill=STEEL_LT)


def rifle():
    W, H = 256, 144
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rnd = random.Random(7)

    # --- stock (black wedge with a cut-out, behind everything) ---
    stock = [(4, 28), (12, 28), (40, 32), (40, 52), (16, 70), (12, 84), (4, 84)]
    d.polygon(P(stock), fill=BLACK)
    d.polygon(P([(12, 28), (40, 32), (40, 35), (12, 31)]), fill=BLACK_LT)
    d.line(P(stock + [stock[0]]), fill=OUTLINE, width=S + 2)
    cut = [(16, 38), (34, 40), (34, 48), (18, 60)]
    d.polygon(P(cut), fill=(0, 0, 0, 0), outline=OUTLINE)
    d.line(P(cut + [cut[0]]), fill=OUTLINE, width=S + 1)
    d.rectangle(R(4, 28, 9, 84), fill=BLACK_DK, outline=OUTLINE, width=S)  # butt pad
    for y in range(32, 82, 4):
        d.line(P([(4.5, y), (8.5, y)]), fill=BLACK_LT, width=S)

    # --- silver under-rods at the front (like rails / cooling tubes) ---
    box(d, 186, 57, 240, 62, STEEL, STEEL_LT, STEEL_DK, 1)
    d.ellipse(R(237, 56.5, 243, 62.5), fill=STEEL_DK, outline=OUTLINE, width=3)

    # --- receiver (olive, stepped top) ---
    body = [(36, 30), (64, 30), (64, 22), (100, 22), (104, 27), (150, 27), (150, 20), (186, 20),
            (190, 27), (204, 27), (204, 58), (186, 58), (180, 66), (146, 66), (140, 86), (100, 86),
            (98, 76), (66, 76), (40, 64), (36, 52)]
    d.polygon(P(body), fill=OLIVE)
    # top light, bottom shade
    d.polygon(P([(36, 30), (64, 30), (64, 22), (100, 22), (104, 27), (150, 27), (150, 20), (186, 20), (190, 27), (204, 27),
                 (204, 30), (188, 30), (184, 23), (152, 23), (152, 30), (102, 30), (98, 25), (66, 25), (66, 33), (38, 33)]), fill=OLIVE_LT)
    d.polygon(P([(36, 52), (40, 64), (66, 76), (98, 76), (100, 86), (140, 86), (146, 66), (180, 66), (186, 58), (204, 58),
                 (204, 55), (184, 55), (178, 63), (144, 63), (138, 83), (102, 83), (100, 73), (67, 73), (42, 61), (39, 52)]), fill=OLIVE_DK)
    d.line(P(body + [body[0]]), fill=OUTLINE, width=S + 2)
    # panel lines
    d.line(P([(104, 30), (104, 64)]), fill=OLIVE_DDK, width=S)
    d.line(P([(150, 30), (150, 56)]), fill=OLIVE_DDK, width=S)
    d.line(P([(66, 33), (66, 72)]), fill=OLIVE_DDK, width=S)
    # angled cut on the lower front (as on the reference's style), with chipped steel edge
    d.line(P([(150, 60), (162, 48)]), fill=OLIVE_DDK, width=S)
    d.line(P([(151, 61), (163, 49)]), fill=STEEL_LT, width=max(1, S // 2))

    # --- front shroud and emitter ---
    shroud = [(204, 30), (236, 30), (236, 34), (240, 34), (240, 54), (236, 54), (236, 56), (204, 56)]
    d.polygon(P(shroud), fill=OLIVE)
    d.rectangle(R(204, 30, 236, 32.5), fill=OLIVE_LT)
    d.rectangle(R(204, 53, 236, 56), fill=OLIVE_DK)
    d.line(P(shroud + [shroud[0]]), fill=OUTLINE, width=S + 2)
    for x in (212, 220, 228):  # vent slots
        d.rounded_rectangle(R(x, 36, x + 4, 50), radius=S, fill=OLIVE_DDK, outline=OUTLINE, width=2)
    # emitter crown
    d.rectangle(R(240, 36, 250, 52), fill=STEEL_DK, outline=OUTLINE, width=S + 1)
    d.rectangle(R(240, 36, 250, 38), fill=STEEL)
    d.rectangle(R(250, 39, 253, 49), fill=BLACK_DK, outline=OUTLINE, width=3)
    d.ellipse(R(250.5, 41.5, 253, 46.5), fill=RED)  # focusing lens glint

    # --- silver top rail (charging rod) ---
    box(d, 104, 23, 150, 26.5, STEEL, STEEL_LT, STEEL_DK, 0.8)
    box(d, 190, 23, 202, 26.5, STEEL, STEEL_LT, STEEL_DK, 0.8)

    # --- power cell indicator (MovingPart region 99,31,45,17): a lit charge strip ---
    d.rounded_rectangle(R(108, 34, 142, 44), radius=S, fill=BLACK_DK, outline=OUTLINE, width=S)
    for i in range(6):
        x = 110.5 + i * 5.2
        d.rectangle(R(x, 36.5, x + 3.6, 41.5), fill=RED if i < 5 else (90, 30, 25))
    d.line(P([(110, 36.5), (140, 36.5)]), fill=RED_GLOW, width=max(1, S // 2))

    # --- side details: slot, rivets, gauge ---
    d.rounded_rectangle(R(160, 34, 190, 38), radius=S, fill=OLIVE_DDK, outline=OUTLINE, width=2)
    d.rectangle(R(162, 35, 176, 37), fill=STEEL_LT)
    for x, y in ((70, 36), (96, 36), (70, 68), (156, 44), (196, 34), (196, 52), (44, 38)):
        rivet(d, x, y)
    d.ellipse(R(76, 40, 92, 56), fill=OLIVE_DK, outline=OUTLINE, width=S)  # focusing dial
    d.ellipse(R(79, 43, 89, 53), fill=STEEL, outline=OUTLINE, width=3)
    d.line(P([(84, 48), (87.5, 44.5)]), fill=BLACK_DK, width=S)
    # scratches / chipped paint showing steel
    scratches(d, rnd, (110, 46, 200, 62), 6, STEEL_LT)
    scratches(d, rnd, (44, 40, 100, 66), 4, STEEL_LT)

    # --- battery well (dark slot the battery sprite plugs into, x 100-136) ---
    d.rectangle(R(102, 78, 136, 94), fill=BLACK, outline=OUTLINE, width=S + 1)
    d.rectangle(R(104, 79, 134, 81), fill=BLACK_LT)
    d.rectangle(R(106, 86, 132, 88), fill=STEEL_DK)

    # --- trigger guard + trigger ---
    d.line(P([(70, 76), (70, 88), (96, 92), (100, 86)]), fill=STEEL_DK, width=S * 3)
    d.line(P([(70, 76), (70, 88), (96, 92), (100, 86)]), fill=STEEL, width=S)
    d.polygon(P([(80, 76), (84, 76), (82, 86)]), fill=STEEL_LT, outline=OUTLINE)

    # --- pistol grip (black, chequered) ---
    grip = [(42, 66), (66, 76), (68, 80), (60, 120), (62, 126), (46, 128), (40, 124), (44, 82), (38, 70)]
    d.polygon(P(grip), fill=BLACK)
    d.line(P(grip + [grip[0]]), fill=OUTLINE, width=S + 2)
    for t in range(9):  # chequering
        y = 84 + t * 4.4
        d.line(P([(46 - t * 0.05, y), (62 - t * 0.45, y + 2)]), fill=BLACK_LT, width=max(1, S // 2))
    d.line(P([(44, 82), (40, 124)]), fill=BLACK_LT, width=S)
    d.polygon(P([(40, 124), (62, 126), (63, 129), (40, 129)]), fill=STEEL_DK, outline=OUTLINE)  # grip cap

    img = img.resize((W, H), Image.LANCZOS)
    grime(img, is_olive, 11, 0.035)
    return img


def battery():
    """Battery pack on the Boltgun magazine canvas (256x144, region 96,7,67,132), top-aligned."""
    W, H = 256, 144
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    x0, x1, y0, y1 = 99, 160, 8, 74
    d.rectangle(R(x0 + 6, y0, x1 - 6, y0 + 8), fill=STEEL_DK, outline=OUTLINE, width=S)  # contact block
    for x in range(x0 + 10, x1 - 8, 7):
        d.rectangle(R(x, y0 + 2, x + 3, y0 + 6), fill=(220, 170, 60))  # brass contacts
    d.rectangle(R(x0, y0 + 8, x1, y1), fill=BLACK)
    d.polygon(P([(x0, y0 + 8), (x1, y0 + 8), (x1, y0 + 11), (x0, y0 + 11)]), fill=BLACK_LT)
    # the diagonal steel strip across the face
    d.polygon(P([(x0 + 18, y1), (x1, y0 + 26), (x1, y0 + 31), (x0 + 24, y1)]), fill=STEEL)
    d.line(P([(x0 + 19, y1 - 1), (x1 - 1, y0 + 27)]), fill=STEEL_LT, width=S)
    # charge window
    d.rounded_rectangle(R(x0 + 6, y0 + 16, x0 + 30, y0 + 24), radius=S, fill=BLACK_DK, outline=OUTLINE, width=S)
    for i in range(4):
        d.rectangle(R(x0 + 8 + i * 5.5, y0 + 18, x0 + 11.5 + i * 5.5, y0 + 22), fill=RED)
    d.rectangle(R(x0, y0 + 8, x1, y1), outline=OUTLINE, width=S + 2)
    d.rectangle(R(x0 + 2, y1 - 4, x1 - 2, y1 - 2), fill=BLACK_DK)
    img = img.resize((W, H), Image.LANCZOS)
    return img


def cell():
    """Laser charge cell (ammo), 256x100 canvas like the .75 Bolt."""
    W, H = 256, 100
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(R(30, 30, 200, 72), radius=6 * S, fill=BLACK, outline=OUTLINE, width=S + 2)
    d.rectangle(R(200, 40, 222, 62), fill=STEEL, outline=OUTLINE, width=S + 1)  # terminal
    d.rectangle(R(222, 45, 230, 57), fill=(220, 170, 60), outline=OUTLINE, width=S)
    d.rectangle(R(44, 30, 60, 72), fill=OLIVE, outline=OUTLINE, width=S)       # olive band
    d.rectangle(R(170, 30, 186, 72), fill=OLIVE, outline=OUTLINE, width=S)
    d.rounded_rectangle(R(74, 44, 156, 58), radius=2 * S, fill=(120, 20, 15), outline=OUTLINE, width=S)
    d.rounded_rectangle(R(76, 46, 154, 52), radius=2 * S, fill=RED)
    d.line(P([(78, 47), (150, 47)]), fill=RED_GLOW, width=S)
    d.line(P([(36, 34), (196, 34)]), fill=BLACK_LT, width=S)
    img = img.resize((W, H), Image.LANCZOS)
    return img


def preview(sprite, bg=(40, 44, 38)):
    """512x512 jpg preview: sprite centred on a dark panel."""
    out = Image.new("RGB", (512, 512), bg)
    g = Image.new("RGB", (512, 512), bg)
    gd = ImageDraw.Draw(g)
    for r in range(256, 0, -8):
        k = int(28 * (1 - r / 256))
        gd.ellipse([256 - r, 256 - r, 256 + r, 256 + r], fill=(bg[0] + k, bg[1] + k, bg[2] + k))
    out.paste(g)
    bb = sprite.getbbox()
    s = sprite.crop(bb)
    scale = min(460 / s.width, 300 / s.height)
    s = s.resize((int(s.width * scale), int(s.height * scale)), Image.LANCZOS)
    out.paste(s, ((512 - s.width) // 2, (512 - s.height) // 2), s)
    return out


def workshop_preview(rifle_img, battery_img):
    """512x512 Workshop preview: the rifle with its battery fitted, firing a beam."""
    W = 512
    out = Image.new("RGBA", (W, W), (28, 32, 26, 255))
    g = ImageDraw.Draw(out)
    for r in range(360, 0, -6):
        k = int(34 * (1 - r / 360))
        g.ellipse([200 - r, 260 - r, 200 + r, 260 + r], fill=(28 + k, 32 + k, 26 + k, 255))
    k = 230 / 400
    gun = Image.new("RGBA", (256, 144), (0, 0, 0, 0))
    bat = battery_img.crop((99, 8, 161, 75))
    bat = bat.resize((int(bat.width * k), int(bat.height * k)), Image.LANCZOS)
    gun.alpha_composite(bat, (102, 86)); gun.alpha_composite(rifle_img)
    scale = 1.75
    big = gun.resize((int(256 * scale), int(144 * scale)), Image.LANCZOS)
    ox, oy = 4, 150
    # beam from the emitter to the right edge, with glow
    beam = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    bd = ImageDraw.Draw(beam)
    y = oy + 44 * scale
    x0 = ox + 252 * scale
    for w_, a_ in ((22, 40), (12, 90), (6, 200)):
        bd.line([(x0, y), (W, y)], fill=(255, 70, 40, a_), width=w_)
    bd.line([(x0, y), (W, y)], fill=(255, 235, 220, 255), width=2)
    bd.ellipse([x0 - 14, y - 14, x0 + 14, y + 14], fill=(255, 120, 80, 120))
    beam = beam.filter(ImageFilter.GaussianBlur(1.2))
    out.alpha_composite(beam)
    out.alpha_composite(big, (ox, oy))
    try:
        from PIL import ImageFont
        f = ImageFont.truetype("DejaVuSans-Bold.ttf", 54)
        f2 = ImageFont.truetype("DejaVuSans-Bold.ttf", 22)
    except Exception:
        f = f2 = None
    g = ImageDraw.Draw(out)
    g.text((W // 2, 70), "LASER GUN", font=f, fill=(255, 92, 60, 255), anchor="mm", stroke_width=3, stroke_fill=(15, 15, 15, 255))
    g.text((W // 2, 420), "350 damage  |  50-shot battery", font=f2, fill=(220, 226, 200, 255), anchor="mm", stroke_width=2, stroke_fill=(15, 15, 15, 255))
    g.text((W // 2, 452), "sets targets on fire", font=f2, fill=(220, 226, 200, 255), anchor="mm", stroke_width=2, stroke_fill=(15, 15, 15, 255))
    return out.convert("RGB")


def save(kind, key, img):
    d = os.path.join(ITEMS, kind, key)
    os.makedirs(d, exist_ok=True)
    img.save(os.path.join(d, "sprite.png"))
    preview(img).save(os.path.join(d, "preview.jpg"), quality=92)


def main():
    r = rifle(); b = battery(); c = cell()
    # rifle sprite now comes from tools/make_rifle_from_reference.py
    # battery pack sprite now comes from tools/make_battery_from_reference.py
    save("Ammo", "cell", c)
    print("art written")


if __name__ == "__main__":
    main()
