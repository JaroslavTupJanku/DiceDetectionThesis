namespace DiceDetector.Models
{
    public class EvalResult
    {
        public int TruePositives { get; init; }
        public int FalsePositives { get; init; }
        public int FalseNegatives { get; init; }
        public int CorrectClassifications { get; init; }
        public int TotalMatched { get; init; }

        public double Precision => TruePositives + FalsePositives > 0
            ? (double)TruePositives / (TruePositives + FalsePositives) : 0;

        public double Recall => TruePositives + FalseNegatives > 0
            ? (double)TruePositives / (TruePositives + FalseNegatives) : 0;

        public double ClassificationAccuracy => TotalMatched > 0
            ? (double)CorrectClassifications / TotalMatched : 0;

        public double AverageIoU { get; init; }

        public IReadOnlyList<EvalMatch> Matches { get; init; } = [];
        public IReadOnlyList<AnnotationObject> MissedObjects { get; init; } = [];
        public IReadOnlyList<DetectionResult> FalsePositiveDetections { get; init; } = [];
    }

    public class EvalMatch
    {
        public required AnnotationObject Annotation { get; init; }
        public required DetectionResult Detection { get; init; }
        public required double IoU { get; init; }
        public bool IsCorrectClass => Annotation.Value == Detection.DiceValue;
    }
}
