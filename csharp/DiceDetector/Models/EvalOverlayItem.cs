using System.Windows;
using System.Windows.Media;

namespace DiceDetector.Models
{
    public enum EvalOverlayType
    {
        TruePositive,
        WrongClassification,
        FalsePositive,
        Missed
    }

    public class EvalOverlayItem
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required string Label { get; init; }
        public required EvalOverlayType Type { get; init; }
        public required Brush Color { get; init; }
        public required Thickness StrokeThickness { get; init; }
        public required CornerRadius BoxCornerRadius { get; init; }
        public required double LabelFontSize { get; init; }
        public required Thickness LabelPadding { get; init; }
        public required Thickness LabelMargin { get; init; }
        public required CornerRadius LabelCornerRadius { get; init; }

        public double CenterFontSize => Math.Min(Width, Height) * 0.5;
        public bool IsDashed => Type == EvalOverlayType.Missed;
        public bool IsNotMissed => Type != EvalOverlayType.Missed;
    }
}
