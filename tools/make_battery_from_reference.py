"""Laser Battery Pack sprite from the author's reference (art-src/laser-battery-pack-reference.png):
cuts out the white background and scales it onto a 128x144 canvas (ppu 400 in build_items.py)."""
import sys, numpy as np, cv2
from PIL import Image, ImageFilter
src, out_sprite = sys.argv[1:3]
img = cv2.imread(src); h, w = img.shape[:2]
white = (img.min(axis=2) > 232).astype(np.uint8)
n, lab = cv2.connectedComponents(white)
bg = np.zeros_like(white)
for i in range(1, n):
    comp = lab == i
    ys, xs = np.nonzero(comp)
    if ys.min() == 0 or xs.min() == 0 or ys.max() == h - 1 or xs.max() == w - 1: bg |= comp.astype(np.uint8)
alpha = cv2.GaussianBlur(((1 - bg) * 255).astype(np.float32), (0, 0), 0.7)
full = Image.fromarray(np.dstack([img[..., ::-1], alpha.clip(0, 255).astype(np.uint8)]), "RGBA")
full = full.crop(full.getbbox())
k = min(124 / full.width, 140 / full.height)
small = full.resize((round(full.width * k), round(full.height * k)), Image.LANCZOS).filter(ImageFilter.UnsharpMask(1, 60, 2))
canvas = Image.new("RGBA", (128, 144), (0, 0, 0, 0))
canvas.alpha_composite(small, ((128 - small.width) // 2, (144 - small.height) // 2))
canvas.save(out_sprite)
print(small.size, canvas.getbbox())
