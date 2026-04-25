# Dice Detection Thesis

Master's thesis project focused on automatic dice detection using deep learning.

The project consists of two main parts:

* **Python pipeline** – training, evaluation and ONNX export
* **C# WPF application** – real-time inference and visualization

---

## Overview

The system performs:

1. Dice detection (object detection model)
2. Crop extraction
3. Dice value classification (1–6)
4. Visualization and statistics

---

## AI Pipeline

```
Image → Detector → Bounding Boxes → Crop → Classifier → Result
```

* **Detector**: ONNX model (YOLO-like)
* **Classifier**: CNN (EfficientNet-based)
* **Runtime**: ONNX Runtime (C#)

---

## Project Structure

```
DiceDetectionThesis/
├── python/          # Training & experiments
├── csharp/          # WPF inference app
├── docs/            # Documentation
├── data/            # Dataset (ignored)
├── models/          # Trained models (ignored)
└── README.md
```

---

## Technologies

### Python

* TensorFlow / Keras
* KerasCV
* NumPy, OpenCV
* ONNX, tf2onnx

### C#

* .NET (WPF)
* ONNX Runtime
* MVVM (CommunityToolkit)

---

## Features

* Dice detection and classification
* Real-time camera support
* Visualization with overlays
* Statistics (count, sum, confidence)
* ONNX deployment pipeline

---

## Notes

* Dataset and trained models are **not included**
* Models must be placed into:

  ```
  models/
  ```

---

## Author

Master’s Thesis Project
