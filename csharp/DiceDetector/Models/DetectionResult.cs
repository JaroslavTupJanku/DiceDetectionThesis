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
        public float FinalConfidence => Math.Min(1f, DetConfidence * ClsConfidence + 0.08f);
        public bool IsUncertain => FinalConfidence < 0.70f;

        public IReadOnlyList<ClassPrediction> TopPredictions { get; init; } = [];
        public BitmapSource? CropImage { get; init; }
        public Brush? ColorBrush { get; set; }

        public string Label => $"Kostka {Index}";
        public string ValueText => DiceValue > 0 ? $"Hodnota: {DiceValue}" : "Hodnota: ?";
        public string ConfidenceText => $"Det: {DetConfidence:P0}  Cls: {ClsConfidence:P0}  Final: {FinalConfidence:P0}";
        public string StatusIcon => IsUncertain ? "!" : "OK";

        public Brush StatusBrush => IsUncertain
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00))
            : new SolidColorBrush(Color.FromRgb(0x32, 0xCD, 0x32));

        public float ConfidenceMargin => TopPredictions.Count >= 2
            ? TopPredictions[0].Confidence - TopPredictions[1].Confidence
            : TopPredictions.Count == 1 ? TopPredictions[0].Confidence : 0;


        public string BoxText => $"{(int)Width}×{(int)Height} px";
        public double Area => Width * Height;
        public string AreaText => $"{Area:N0} px²";

        public double? EvalIoU { get; set; }
        public int? EvalGroundTruth { get; set; }
        public string? EvalMatchType { get; set; } 
    }
}
