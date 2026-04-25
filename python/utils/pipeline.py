from pathlib import Path
import numpy as np
import tensorflow as tf
import keras
import cv2


def load_image_bgr(image_path):
    image = cv2.imread(str(image_path))
    if image is None:
        raise FileNotFoundError(f"Could not read image: {image_path}")
    return image


def bgr_to_rgb(image_bgr):
    return cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)


def preprocess_for_detector(image_rgb, detector_target_size):
    orig_h, orig_w = image_rgb.shape[:2]

    scale = min(detector_target_size / orig_w, detector_target_size / orig_h)
    new_w = int(round(orig_w * scale))
    new_h = int(round(orig_h * scale))

    resized = cv2.resize(image_rgb, (new_w, new_h), interpolation=cv2.INTER_LINEAR)

    canvas = np.zeros((detector_target_size, detector_target_size, 3), dtype=np.uint8)

    pad_left = (detector_target_size - new_w) // 2
    pad_top = (detector_target_size - new_h) // 2

    canvas[pad_top:pad_top + new_h, pad_left:pad_left + new_w] = resized

    image_resized = tf.convert_to_tensor(canvas, dtype=tf.float32) / 255.0

    return image_resized, float(scale), float(pad_left), float(pad_top), orig_h, orig_w


def map_box_to_original(box_xyxy, scale, pad_left, pad_top, orig_h, orig_w):
    x1, y1, x2, y2 = map(float, box_xyxy)

    x1 = (x1 - pad_left) / scale
    y1 = (y1 - pad_top) / scale
    x2 = (x2 - pad_left) / scale
    y2 = (y2 - pad_top) / scale

    x1 = max(0.0, min(orig_w, x1))
    y1 = max(0.0, min(orig_h, y1))
    x2 = max(0.0, min(orig_w, x2))
    y2 = max(0.0, min(orig_h, y2))

    return np.array([x1, y1, x2, y2], dtype=np.float32)


def expand_box(box_xyxy, image_shape, margin=0.12):
    h, w = image_shape[:2]
    x1, y1, x2, y2 = map(float, box_xyxy)

    bw = x2 - x1
    bh = y2 - y1

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

    return np.array([x1, y1, x2, y2], dtype=np.int32)


def crop_from_box(image_rgb, box_xyxy):
    x1, y1, x2, y2 = map(int, box_xyxy)
    crop = image_rgb[y1:y2, x1:x2]
    if crop.size == 0:
        return None
    return crop


def preprocess_crop_for_classifier(crop_rgb, classifier_img_size):
    crop_resized = cv2.resize(crop_rgb, classifier_img_size, interpolation=cv2.INTER_LINEAR)
    crop_resized = crop_resized.astype(np.float32)
    crop_resized = keras.applications.efficientnet.preprocess_input(crop_resized)
    return crop_resized