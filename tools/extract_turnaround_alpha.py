from collections import deque
from pathlib import Path
import sys

import numpy as np
from PIL import Image


def main(source: str, destination: str) -> None:
    image = Image.open(source).convert("RGBA")
    pixels = np.asarray(image).copy()
    rgb = pixels[:, :, :3].astype(np.int16)

    # The generated backdrop is near-neutral and near-white. Restrict removal to
    # border-connected pixels so pale, enclosed costume details stay opaque.
    minimum = rgb.min(axis=2)
    spread = rgb.max(axis=2) - minimum
    candidate = (minimum >= 220) & (spread <= 12)
    height, width = candidate.shape
    background = np.zeros_like(candidate, dtype=bool)
    queue = deque()

    for x in range(width):
        queue.extend(((0, x), (height - 1, x)))
    for y in range(height):
        queue.extend(((y, 0), (y, width - 1)))

    while queue:
        y, x = queue.popleft()
        if not (0 <= y < height and 0 <= x < width) or background[y, x] or not candidate[y, x]:
            continue
        background[y, x] = True
        queue.extend(((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)))

    pixels[:, :, 3] = np.where(background, 0, 255).astype(np.uint8)
    Image.fromarray(pixels, "RGBA").save(destination)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
