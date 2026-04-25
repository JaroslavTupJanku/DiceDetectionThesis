Soubory v této složce:
- obr_7_pipeline_detailni.png ........ detailní diagram inferenční pipeline
- obr_7_detekce_metriky.png .......... graf precision / recall / F1-score detektoru
- obr_7_klasifikace_tridy.png ........ graf přesnosti klasifikátoru podle tříd
- obr_7_latence_sablona.png .......... šablona grafu latence
- notebook_export_onnx.py ............ export TensorFlow/Keras modelů do ONNX
- notebook_verify_onnx_classifier.py . porovnání TensorFlow vs ONNX pro klasifikátor
- notebook_latency_pipeline.py ....... měření latence Python inference pipeline

Poznámka:
Detektor založený na KerasCV YOLOv8 může při exportu do ONNX vyžadovat drobné úpravy.
Klasifikátor EfficientNet bývá převoditelný přímo.