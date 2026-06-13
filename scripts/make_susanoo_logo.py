from PIL import Image
import numpy as np
import sys

src = sys.argv[1]
dst = sys.argv[2]

img = Image.open(src).convert("RGBA")
arr = np.array(img, dtype=np.float32)
r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]

lum = 0.299 * r + 0.587 * g + 0.114 * b
max_c = np.maximum(np.maximum(r, g), b)
min_c = np.minimum(np.minimum(r, g), b)
sat = max_c - min_c
balance = 255.0 - (np.abs(r - g) + np.abs(g - b) + np.abs(r - b)) / 3.0

logo_strength = np.clip((lum - 95.0) / 70.0, 0.0, 1.0)
logo_strength *= np.clip(1.0 - sat / 90.0, 0.0, 1.0)
logo_strength *= np.clip(balance / 200.0, 0.0, 1.0)

alpha = (logo_strength * 255.0).astype(np.uint8)
alpha = np.where(lum > 210, 255, alpha)

out = np.zeros(arr.shape, dtype=np.uint8)
out[:, :, 0] = 255
out[:, :, 1] = 255
out[:, :, 2] = 255
out[:, :, 3] = alpha

Image.fromarray(out).save(dst)
print(dst)
