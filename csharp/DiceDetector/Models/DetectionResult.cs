using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiceDetector.Models
{
    public class DetectionResult
    {
        public required int Index { get; init; }
        public required string Label { get; init; }
        public required string ValueText { get; init; }
        public required string ConfidenceText { get; init; }
        public required string BoxText { get; init; }

        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }

        public float Confidence { get; init; }
        public Brush? ColorBrush { get; set; }

        public int DiceValue { get; init; }
        public float DetConfidence { get; init; }
        public float ClsConfidence { get; init; }
        public IReadOnlyList<ClassPrediction> TopPredictions { get; init; } = [];
        public BitmapSource? CropImage { get; init; }
    }
}
