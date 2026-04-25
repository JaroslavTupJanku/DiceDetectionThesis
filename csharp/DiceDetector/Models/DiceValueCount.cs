namespace DiceDetector.Models
{
    public class DiceValueCount
    {
        public required int Value { get; init; }
        public required int Count { get; init; }
        public required double BarHeight { get; init; }
        public string CountText => Count.ToString();
    }
}
