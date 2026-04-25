using System.IO;

namespace DiceDetector.Models
{
    public static class InferenceSettings
    {
        public const int DetectorSize = 640;
        public const int ClassifierWidth = 384;
        public const int ClassifierHeight = 384;

        public const float DetectorConfidenceThreshold = 0.45f;
        public const float DetectorIouThreshold = 0.50f;
        public const float CropMargin = 0.12f;

        public const int RegMax = 16;
        public const bool EnableDebugLogs = true;

        public static string DetectorModelPath =>
            Path.Combine(AppContext.BaseDirectory, "OnnxModels", "dice_detector.onnx");

        public static string ClassifierModelPath =>
            Path.Combine(AppContext.BaseDirectory, "OnnxModels", "dice_classifier.onnx");
    }
}
