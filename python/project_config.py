from pathlib import Path
import random
import numpy as np
import tensorflow as tf

# =========================================================
# PROJECT PATHS
# =========================================================
PROJECT_ROOT = Path.cwd()
if PROJECT_ROOT.name in {"training", "experiments"}:
    PROJECT_ROOT = PROJECT_ROOT.parent

DATA_DIR = PROJECT_ROOT / "data"
RAW_DIR = DATA_DIR / "raw"
SPLITS_DIR = DATA_DIR / "splits"
CLASSIFICATION_DIR = DATA_DIR / "classification"
MODELS_DIR = PROJECT_ROOT / "models"
DOCS_DIR = PROJECT_ROOT / "docs"

IMAGES_DIR = RAW_DIR / "images"
ANNOTATIONS_DIR = RAW_DIR / "annotations"

TRAIN_JSON = SPLITS_DIR / "train.json"
VAL_JSON = SPLITS_DIR / "val.json"
TEST_JSON = SPLITS_DIR / "test.json"

MODELS_DIR.mkdir(parents=True, exist_ok=True)

# =========================================================
# GLOBAL SETTINGS
# =========================================================
SEED = 42
AUTOTUNE = tf.data.AUTOTUNE

# =========================================================
# DETECTOR SETTINGS
# =========================================================
DETECTOR_CLASS_NAME = "dice"
NUM_DETECTOR_CLASSES = 1
BOUNDING_BOX_FORMAT = "xyxy"
DETECTOR_ID_TO_CLASS_NAME = {0: DETECTOR_CLASS_NAME}

DETECTOR_BATCH_SIZE = 1
DETECTOR_TARGET_SIZE = 640
DETECTOR_LEARNING_RATE = 1e-4
DETECTOR_EPOCHS = 30

# =========================================================
# DEBUG / EVALUATION
# =========================================================
DEBUG_CONFIDENCE_THRESHOLD = 0.30
DEBUG_IOU_THRESHOLD = 0.50
MATCH_IOU_THRESHOLD = 0.50

MAX_SHOW_FP = 5
MAX_SHOW_FN = 5
MAX_SHOW_GOOD = 3

BEST_DEBUG_MODEL_PATH = MODELS_DIR / "dice_detector_best.keras"

# =========================================================
# CLASSIFIER SETTINGS
# =========================================================
CLASSIFIER_IMG_SIZE = (384, 384)
CLASSIFIER_BATCH_SIZE = 32
CLASSIFIER_EPOCHS = 20
CLASS_NAMES = ["1", "2", "3", "4", "5", "6"]

# =========================================================
# REPRODUCIBILITY
# =========================================================
def setup_reproducibility(seed: int = SEED) -> None:
    random.seed(seed)
    np.random.seed(seed)
    tf.random.set_seed(seed)
    tf.random.set_global_generator(tf.random.Generator.from_seed(seed))
    tf.config.optimizer.set_jit(False)

# =========================================================
# INFERENCE SETTINGS
# =========================================================
DETECTOR_MODEL_PATH = MODELS_DIR / "dice_detector_best_20260330_114353.keras"
CLASSIFIER_MODEL_PATH = MODELS_DIR / "dice_classifier_best.keras"

DETECTOR_CONFIDENCE_THRESHOLD = 0.45
DETECTOR_IOU_THRESHOLD = 0.50
CROP_MARGIN = 0.12

INFERENCE_IMAGE_PATH = DATA_DIR / "raw" / "images" / "i.rf.617cdfb3584190a9e1d4c25d43612900.jpg"
INFERENCE_RESULTS_CSV = DATA_DIR / "inference_results" / "pipeline_results.csv"

# =========================================================
# ONNX EXPORT SETTINGS
# =========================================================
ONNX_EXPORT_DIR = MODELS_DIR / "onnx"
ONNX_EXPORT_DIR.mkdir(parents=True, exist_ok=True)

DETECTOR_ONNX_PATH = ONNX_EXPORT_DIR / "dice_detector.onnx"
CLASSIFIER_ONNX_PATH = ONNX_EXPORT_DIR / "dice_classifier.onnx"

DETECTOR_INPUT_SHAPE = (1, DETECTOR_TARGET_SIZE, DETECTOR_TARGET_SIZE, 3)
CLASSIFIER_INPUT_SHAPE = (
    1,
    CLASSIFIER_IMG_SIZE[0],
    CLASSIFIER_IMG_SIZE[1],
    3,
)
ONNX_OPSET = 17


# =========================================================
# REPORT SETTINGS
# =========================================================
REPORTS_DIR = MODELS_DIR / "reports"
REPORTS_DIR.mkdir(parents=True, exist_ok=True)

CLASSIFIER_TF_VS_ONNX_FIGURE = REPORTS_DIR / "classifier_tf_vs_onnx.png"
CLASSIFIER_ONNX_ABS_DIFF_FIGURE = REPORTS_DIR / "classifier_abs_diff.png"

ONNX_VERIFICATION_THRESHOLD = 1e-3