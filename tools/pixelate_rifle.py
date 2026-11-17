import sys, numpy as np, cv2
from PIL import Image
full = Image.open(sys.argv[1])            # cleaned, flipped cut-out (1000x286 RGBA)
out = sys.argv[2]
k = 0.27
W, H = round(full.width * k), round(full.height * k)
# 1) simplify the painting before shrinking: kill brush strokes, keep edges
a = np.array(full)
rgb = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2BGR)
for _ in range(3): rgb = cv2.bilateralFilter(rgb, 9, 40, 9)
rgb = cv2.cvtColor(rgb, cv2.COLOR_BGR2RGB)
small = np.array(Image.fromarray(np.dstack([rgb, a[..., 3]]), "RGBA").resize((W, H), Image.LANCZOS)).astype(np.float32)
alpha = small[..., 3] > 110
# white paper showing through closed gaps (trigger guard) is background too
alpha &= ~((small[..., :3].min(-1) > 215))
# 2) sort pixels into materials, then shade each material with a few flat tones (game style)
px = small[..., :3]
r, g, bch = px[..., 0], px[..., 1], px[..., 2]
lum = 0.3 * r + 0.59 * g + 0.11 * bch
mx, mn = px.max(-1), px.min(-1)
sat = (mx - mn) / np.maximum(mx, 1)
olive = alpha & (sat > 0.22) & (lum > 45) & (g >= bch * 1.3) & (g >= r * 0.8)          # painted body (incl. brown wear, painted over)
steel = alpha & ~olive & (lum > 135)
dark = alpha & ~olive & ~steel
# smooth the brightness so brush strokes and camo blotches disappear, keep big light/shade shapes
lumS = cv2.GaussianBlur(lum.astype(np.float32), (0, 0), 2.2)
OLIVE = np.array([(66, 76, 32), (96, 110, 44), (126, 142, 60), (160, 174, 84)], np.float32)
DARK = np.array([(24, 25, 29), (40, 42, 49), (64, 67, 76)], np.float32)
STEEL = np.array([(122, 126, 134), (172, 176, 184), (220, 224, 230)], np.float32)
def ramp(mask, tones, cuts):
    lv = np.digitize(lumS, cuts)
    return mask[..., None] * tones[np.clip(lv, 0, len(tones) - 1)]
q = ramp(olive, OLIVE, [70, 100, 135]) + ramp(dark, DARK, [40, 75]) + ramp(steel, STEEL, [165, 205])
# a few panel lines: keep strong edges of the original as a darker olive
edges = cv2.Canny(cv2.cvtColor(np.array(Image.fromarray(rgb).resize((W, H), Image.LANCZOS)), cv2.COLOR_RGB2GRAY), 60, 140) > 0
q[edges & dark] = DARK[0]
# 4) 1px dark outline around the silhouette, light rim on top edges
img = np.zeros((H + 2, W + 2, 4), np.uint8)
img[1:-1, 1:-1, :3] = q; img[1:-1, 1:-1, 3] = alpha * 255
A = img[..., 3] > 0
OUT = (18, 18, 20)
edge = np.zeros_like(A)
for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
    edge |= np.roll(np.roll(A, dy, 0), dx, 1) & ~A
img[edge, :3] = OUT; img[edge, 3] = 255
inner_top = A & ~np.roll(A, 1, 0)          # opaque pixel whose upper neighbour is empty
for y, x in zip(*np.nonzero(inner_top)):
    c = img[y, x, :3].astype(int); img[y, x, :3] = np.clip(c + 34, 0, 255)
inner_bot = A & ~np.roll(A, -1, 0)
for y, x in zip(*np.nonzero(inner_bot)):
    c = img[y, x, :3].astype(int); img[y, x, :3] = np.clip(c - 30, 0, 255)
# 5) place on the 304x144 game canvas, grip at (75, 99) (one pixel of outline added on each side)
canvas = Image.new("RGBA", (304, 144), (0, 0, 0, 0))
canvas.alpha_composite(Image.fromarray(img, "RGBA"), (5, 44))
canvas.save(out)
