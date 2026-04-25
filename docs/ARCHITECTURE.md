# Architecture

## 1. System Overview

The project consists of two main parts:

```
Python (training) → ONNX export → C# (inference)
```

---

## 2. AI Pipeline

```
Image
 ↓
Preprocessing (resize, normalize)
 ↓
Detector (ONNX)
 ↓
Bounding boxes
 ↓
Crop extraction
 ↓
Classifier (ONNX)
 ↓
Final predictions
```

---

## 3. Python Pipeline

Located in `/python`

### Includes:

* Dataset preparation
* Augmentation
* Detector training
* Classifier training
* ONNX export
* Verification notebooks

---

## 4. C# Application

### Architecture: MVVM

```
View (XAML)
   ↓ binding
ViewModel
   ↓ services
Services (Inference, Camera, Overlay)
```

---

## 5. Key Components

### ViewModel

* Controls UI state
* Executes commands
* Aggregates results

### Services

* `OnnxInferenceService`

  * Runs detector + classifier
  * Handles full pipeline

* `OverlayRenderer`

  * Converts detections to UI overlays

* `CameraService`

  * Handles camera input

---

## 6. ONNX Inference

### Detector

* Input: `[1, 640, 640, 3]`
* Output:

  * boxes
  * class scores

### Classifier

* Input: `[1, 384, 384, 3]`
* Output:

  * probabilities (6 classes)

---

## 7. Detection Logic

* Decode model output (DFL)
* Map boxes to original image
* Apply NMS
* Expand bounding boxes
* Crop and classify

---

## 8. Design Principles

* MVVM separation
* Dependency Injection
* Async inference
* Immutable image data (`Freeze()`)

---

## 9. Deployment Flow

```
Training → Export ONNX → Copy to C# app → Runtime inference
```
