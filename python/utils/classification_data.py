from pathlib import Path
import cv2
import numpy as np


def resolve_image_path(image_path_str, project_root):
    p = Path(image_path_str)
    if p.exists():
        return p

    candidate = project_root / image_path_str
    if candidate.exists():
        return candidate

    raise FileNotFoundError(f"Image not found: {image_path_str}")


def jitter_box_xyxy(box, jitter_ratio=0.0):
    x1, y1, x2, y2 = map(float, box)
    bw = x2 - x1
    bh = y2 - y1

    if bw <= 1 or bh <= 1:
        return None

    if jitter_ratio > 0:
        dx1 = np.random.uniform(-jitter_ratio, jitter_ratio) * bw
        dy1 = np.random.uniform(-jitter_ratio, jitter_ratio) * bh
        dx2 = np.random.uniform(-jitter_ratio, jitter_ratio) * bw
        dy2 = np.random.uniform(-jitter_ratio, jitter_ratio) * bh

        x1 += dx1
        y1 += dy1
        x2 += dx2
        y2 += dy2

    if x2 <= x1 or y2 <= y1:
        return None

    return [x1, y1, x2, y2]


def crop_box(image, box, margin=0.12):
    h, w = image.shape[:2]
    x1, y1, x2, y2 = map(float, box)

    bw = x2 - x1
    bh = y2 - y1
    if bw <= 1 or bh <= 1:
        return None

    x1 -= bw * margin
    y1 -= bh * margin
    x2 += bw * margin
    y2 += bh * margin

    x1 = int(max(0, round(x1)))
    y1 = int(max(0, round(y1)))
    x2 = int(min(w, round(x2)))
    y2 = int(min(h, round(y2)))

    if x2 <= x1 or y2 <= y1:
        return None

    crop = image[y1:y2, x1:x2]
    if crop.size == 0:
        return None

    return crop