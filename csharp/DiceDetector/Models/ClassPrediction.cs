namespace DiceDetector.Models
{
    public class ClassPrediction
    {
        public required int Value { get; init; }
        public required float Confidence { get; init; }
        public string DisplayText => $"{Confidence:0.00}";
        public double BarWidth => Confidence * 200;
    }
}
