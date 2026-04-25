namespace DiceDetector.Models
{
    public class ImageAnnotation
    {
        public required string Filename { get; init; }
        public required int ImageWidth { get; init; }
        public required int ImageHeight { get; init; }
        public required IReadOnlyList<AnnotationObject> Objects { get; init; }
    }
}
