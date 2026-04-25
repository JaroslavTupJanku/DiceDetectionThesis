# Python Pipeline

## Overview

Python part handles training and model preparation.

---

## 1. Steps

### 1. Dataset preparation

* Image loading
* Annotation parsing
* Train/val/test split

### 2. Training

#### Detector

* Bounding box detection
* YOLO-like architecture

#### Classifier

* Dice face classification (1–6)
* EfficientNet-based

---

## 2. Inference pipeline (notebook)

```
Image → Detector → Crop → Classifier → Visualization
```

---

## 3. ONNX Export

* `tf2onnx`
* Separate export for:

  * detector
  * classifier

---

## 4. Verification

* Compare TensorFlow vs ONNX outputs
* Metrics:

  * max_abs_diff
  * mean_abs_diff
  * argmax consistency

---

## 5. Outputs

* `.onnx` models
* CSV results
* visualization plots
