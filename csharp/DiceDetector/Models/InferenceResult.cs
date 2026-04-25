namespace DiceDetector.Models
{
    public class InferenceResult
    {
        public required IReadOnlyList<DetectionResult> Detections { get; init; }
    }
}
