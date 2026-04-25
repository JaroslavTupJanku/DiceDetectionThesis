namespace DiceDetector.Models
{
    public class AnnotationObject
    {
        public required int Value { get; init; }
        public required double XMin { get; init; }
        public required double YMin { get; init; }
        public required double XMax { get; init; }
        public required double YMax { get; init; }

        public double Width => XMax - XMin;
        public double Height => YMax - YMin;
        public double CenterX => (XMin + XMax) / 2;
        public double CenterY => (YMin + YMax) / 2;
    }
}
