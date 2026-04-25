using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiceDetector.Models
{
    public class DetectionResult
    {
        public required int Index { get; init; }
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }

        public float Confidence => DetConfidence;
        public int DiceValue { get; init; }
        public float DetConfidence { get; init; }
        public float ClsConfidence { get; init; }

        public IReadOnlyList<ClassPrediction> TopPredictions { get; init; } = [];
        public BitmapSource? CropImage { get; init; }
        public Brush? ColorBrush { get; set; }

        public string Label => $"Kostka {Index}";
        public string ValueText => DiceValue > 0 ? $"Hodnota: {DiceValue}" : "Hodnota: ?";
        public string ConfidenceText => $"Det: {DetConfidence:0.00} | Cls: {ClsConfidence:0.00}";
        public string BoxText => $"Box: x={(int)X}, y={(int)Y}, w={(int)Width}, h={(int)Height}";
    }
}
