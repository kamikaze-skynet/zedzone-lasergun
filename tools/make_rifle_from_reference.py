import sys, numpy as np, cv2
from PIL import Image
src, out_sprite, out_full = sys.argv[1:4]
img = cv2.imread(src)                       # BGR 1000x286
h, w = img.shape[:2]
# 1) paint out the insignia and lettering: fill smoothly from the surrounding paint (inpaint),
#    then put back paint grain taken from a clean part of the receiver so it doesn't look smeared
mask = np.zeros((h, w), np.uint8)
for x0, y0, x1, y1 in ((190, 50, 428, 114), (188, 108, 418, 137), (648, 66, 798, 94)):
    mask[y0:y1, x0:x1] = 255
filled = cv2.inpaint(img, mask, 15, cv2.INPAINT_TELEA)
filled = cv2.GaussianBlur(filled, (0, 0), 6)
clean = img[100:146, 505:600].astype(np.float32)
grain = clean - cv2.GaussianBlur(clean, (0, 0), 3)
reps = (h // grain.shape[0] + 1, w // grain.shape[1] + 1, 1)
grain = np.tile(grain, reps)[:h, :w]
patch = np.clip(filled.astype(np.float32) + grain * 0.5, 0, 255)
m3 = cv2.GaussianBlur(mask.astype(np.float32) / 255, (0, 0), 3)[..., None]
img = (img * (1 - m3) + patch * m3).astype(np.uint8)
# 2) cut out the white background (flood from the border) with a soft edge
near_white = (img.min(axis=2) > 225).astype(np.uint8)
ff = near_white.copy()
fmask = np.zeros((h + 2, w + 2), np.uint8)
for x in range(w):
    for y in (0, h - 1):
        if ff[y, x] == 1: cv2.floodFill(ff, fmask, (x, y), 2)
for y in range(h):
    for x in (0, w - 1):
        if ff[y, x] == 1: cv2.floodFill(ff, fmask, (x, y), 2)
bg = (ff == 2).astype(np.uint8)
bg = cv2.morphologyEx(bg, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
alpha = (1 - bg) * 255
alpha = cv2.GaussianBlur(alpha.astype(np.float32), (0, 0), 0.8).clip(0, 255).astype(np.uint8)
rgba = np.dstack([img[..., ::-1], alpha])
full = Image.fromarray(rgba, "RGBA").transpose(Image.FLIP_LEFT_RIGHT)   # muzzle to the right, like every game gun
full.save(out_full)
# 3) game sprite: 304x144 canvas at 0.27 scale, pistol grip at (75, 99) like the Boltgun rifle
k = 0.27
small = full.resize((round(w * k), round(h * k)), Image.LANCZOS)
canvas = Image.new("RGBA", (304, 144), (0, 0, 0, 0))
canvas.alpha_composite(small, (6, 45))
canvas.save(out_sprite)
print(small.size, canvas.getbbox())
