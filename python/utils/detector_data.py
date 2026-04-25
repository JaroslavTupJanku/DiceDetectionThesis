from pathlib import Path
import numpy as np
import tensorflow as tf


def validate_and_fix_sample(sample):
    required_keys = {"image_path", "boxes", "class_ids"}
    missing = required_keys - set(sample.keys())
    if missing:
        raise ValueError(f"Sample missing keys: {missing}")

    image_path = Path(sample["image_path"])
    if not image_path.exists():
        raise FileNotFoundError(f"Image not found: {image_path}")

    boxes = np.asarray(sample["boxes"], dtype=np.float32).reshape(-1, 4)
    original_classes = np.asarray(sample["class_ids"], dtype=np.float32).reshape(-1)

    if len(boxes) != len(original_classes):
        raise ValueError(
            f"Boxes/classes mismatch for {image_path}: {len(boxes)} vs {len(original_classes)}"
        )

    if len(boxes) > 0:
        if not np.all(np.isfinite(boxes)):
            raise ValueError(f"Boxes contain NaN/inf for {image_path}")
        if not np.all(boxes[:, 0] <= boxes[:, 2]):
            raise ValueError(f"xmin > xmax for {image_path}")
        if not np.all(boxes[:, 1] <= boxes[:, 3]):
            raise ValueError(f"ymin > ymax for {image_path}")

    detector_classes = np.zeros((len(boxes),), dtype=np.float32)
    return str(image_path), boxes.astype(np.float32), detector_classes


def build_ragged_arrays(data):
    image_paths = []
    boxes_list = []
    classes_list = []

    for sample in data:
        image_path, boxes, classes = validate_and_fix_sample(sample)
        image_paths.append(image_path)
        boxes_list.append(boxes)
        classes_list.append(classes)

    image_paths = tf.constant(image_paths)
    boxes_rt = tf.ragged.constant(boxes_list, dtype=tf.float32)
    classes_rt = tf.ragged.constant(classes_list, dtype=tf.float32)
    return image_paths, boxes_rt, classes_rt


def load_image(image_path):
    image = tf.io.read_file(image_path)
    image = tf.image.decode_jpeg(image, channels=3)
    image = tf.cast(image, tf.float32)
    return image


def load_example(image_path, classes, boxes):
    image = load_image(image_path)
    bounding_boxes = {
        "classes": tf.cast(classes, tf.float32),
        "boxes": tf.cast(boxes, tf.float32),
    }
    return {"images": image, "bounding_boxes": bounding_boxes}


def filter_nonempty_batch(inputs):
    row_lengths = inputs["bounding_boxes"]["boxes"].row_lengths()
    return tf.reduce_any(row_lengths > 0)


def dict_to_tuple(inputs):
    return inputs["images"], inputs["bounding_boxes"]