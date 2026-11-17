import sys, numpy as np, cv2
from PIL import Image, ImageFilter
src, out_sprite, out_full = sys.argv[1:4]
img = cv2.imread(src); h, w = img.shape[:2]
# paint out the winged-skull emblem and the lettering with the flat body green
mask = np.zeros((h, w), np.uint8)
cv2.rectangle(mask, (1090, 110), (1535, 218), 255, -1)
cv2.rectangle(mask, (1120, 212), (1530, 262), 255, -1)
img = cv2.inpaint(img, mask, 7, cv2.INPAINT_TELEA)
m3 = cv2.GaussianBlur(mask.astype(np.float32) / 255, (0, 0), 6)[..., None]
img = (img * (1 - m3) + cv2.GaussianBlur(img, (0, 0), 10) * m3).astype(np.uint8)
# cut out the white background (flood from the border; also closed white gaps like the trigger guard)
white = (img.min(axis=2) > 232).astype(np.uint8)
n, lab = cv2.connectedComponents(white)
bg = np.zeros_like(white)
for i in range(1, n):
    comp = lab == i
    ys, xs = np.nonzero(comp)
    touches = ys.min() == 0 or xs.min() == 0 or ys.max() == h - 1 or xs.max() == w - 1
    if touches or comp.sum() > 400: bg |= comp.astype(np.uint8)
alpha = ((1 - bg) * 255).astype(np.float32)
alpha = cv2.GaussianBlur(alpha, (0, 0), 0.7)
full = Image.fromarray(np.dstack([img[..., ::-1], alpha.clip(0, 255).astype(np.uint8)]), "RGBA")
full.save(out_full)
# game sprite: 304x144 canvas, 0.1408 scale, pistol grip (ref 470,380) at (75,99), muzzle at the right
k = 0.1408
small = full.resize((round(w * k), round(h * k)), Image.LANCZOS).filter(ImageFilter.UnsharpMask(1, 60, 2))
canvas = Image.new("RGBA", (304, 144), (0, 0, 0, 0))
canvas.alpha_composite(small, (round(75 - 470 * k), round(99 - 380 * k)))
canvas.save(out_sprite)
print(small.size, canvas.getbbox())
