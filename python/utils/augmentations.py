import cv2
import numpy as np


def letterbox_resize(image, boxes, target_size=960, pad_value=114):
    h, w = image.shape[:2]

    scale = min(target_size / w, target_size / h)

    new_w = int(round(w * scale))
    new_h = int(round(h * scale))

    resized_image = cv2.resize(image, (new_w, new_h))

    canvas = np.full((target_size, target_size, 3), pad_value, dtype=np.uint8)

    pad_x = (target_size - new_w) // 2
    pad_y = (target_size - new_h) // 2

    canvas[pad_y:pad_y + new_h, pad_x:pad_x + new_w] = resized_image

    resized_boxes = []
    for xmin, ymin, xmax, ymax in boxes:
        xmin = xmin * scale + pad_x
        xmax = xmax * scale + pad_x
        ymin = ymin * scale + pad_y
        ymax = ymax * scale + pad_y
        resized_boxes.append([xmin, ymin, xmax, ymax])

    return canvas, np.array(resized_boxes, dtype=np.float32), scale, pad_x, pad_y


def horizontal_flip(image, boxes):
    flipped_image = np.fliplr(image).copy()

    _, w = image.shape[:2]
    flipped_boxes = []

    for xmin, ymin, xmax, ymax in boxes:
        new_xmin = w - xmax
        new_xmax = w - xmin
        flipped_boxes.append([new_xmin, ymin, new_xmax, ymax])

    return flipped_image, np.array(flipped_boxes, dtype=np.float32)


def random_brightness(image, min_factor=0.7, max_factor=1.3):
    factor = np.random.uniform(min_factor, max_factor)
    image = image.astype(np.float32) * factor
    image = np.clip(image, 0, 255).astype(np.uint8)
    return image