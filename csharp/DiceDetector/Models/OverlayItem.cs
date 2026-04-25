using System.Windows;
using System.Windows.Media;

namespace DiceDetector.Models
{
    public class OverlayItem
    {
        public required int Index { get; init; }
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required string Label { get; init; }
        public required string DiceValue { get; init; }
        public required Brush Stroke { get; init; }
        public required Brush Color { get; init; }

        // Dynamic sizing based on image dimensions
        public required Thickness StrokeThickness { get; init; }
        public required double LabelFontSize { get; init; }
        public required Thickness LabelPadding { get; init; }
        public required Thickness LabelMargin { get; init; }
        public required CornerRadius BoxCornerRadius { get; init; }
        public required CornerRadius LabelCornerRadius { get; init; }
    }
}
