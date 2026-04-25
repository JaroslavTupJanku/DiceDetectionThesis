# DiceDetectionThesis

Master's thesis project focused on computer vision methods for dice detection using deep learning. The repository contains TensorFlow training scripts in Python and a C# application for model inference.

## Dice Detection using Deep Learning

This repository contains the implementation of a master's thesis focused on automatic dice detection using computer vision and deep learning.

The project combines a TensorFlow-based detection model developed in Python with a C# desktop application for running inference and visualizing results.

---

## Project Structure

```
DiceDetectionThesis/
├── python/       # TensorFlow training and inference
├── csharp/       # C# desktop application
├── data/         # Dataset (not included in repo)
├── models/       # Trained models (not included in repo)
├── docs/         # Documentation and diagrams
├── experiments/  # Training outputs, logs (ignored)
└── README.md
```

---

## Features

* Dice detection using deep learning
* TensorFlow model training and evaluation
* Image preprocessing and dataset handling
* C# desktop application for model inference
* Visualization of detection results

---

## Technologies

* Python
* TensorFlow / Keras
* OpenCV
* C#
* .NET

---

## Dataset

The dataset is **not included** in this repository due to its size.

To run the project, place your dataset into:

```
data/raw/
```

You may also create additional folders such as:

```
data/classification/
data/detection/
data/splits/
```

---

## Trained Models

Trained models are also **not included** in the repository.

Place trained weights into:

```
models/
```

---

## Setup

### Python (training)

```
pip install -r requirements.txt
```

Run training scripts from:

```
python/
```

---

### C# Application

Open the `csharp/` project in Visual Studio and run the application.

Make sure the trained model path is correctly configured.

---

## Notes

* Large files such as datasets, trained models, and experiment outputs are excluded via `.gitignore`.
* The repository is structured to separate training (Python) and inference (C#).

---

## Author

Jaroslav TupJanku
Master's Thesis Project
