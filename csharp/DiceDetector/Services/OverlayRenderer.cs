using DiceDetector.Models;
using DiceDetector.Services.Interfaces;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiceDetector.Services
{
    public class OverlayRenderer : IOverlayRenderer
    {
        private static readonly Brush[] VibrantPalette = CreatePalette(
            (0x1E, 0x90, 0xFF),
            (0xDC, 0x14, 0x3C),
            (0x32, 0xCD, 0x32),
            (0xFF, 0xA5, 0x00),
            (0x93, 0x70, 0xDB),
            (0xFF, 0x14, 0x93),
            (0x00, 0xE5, 0xFF),
            (0xFF, 0xD7, 0x00),
            (0xFF, 0x00, 0xFF)
        );

        private static readonly Brush[] ElegantPalette = CreatePalette(
            (0x2E, 0x50, 0x90),
            (0xA0, 0x30, 0x40),
            (0x1A, 0x7A, 0x5A),
            (0xB8, 0x86, 0x20),
            (0x5A, 0x45, 0x8A),
            (0x90, 0x40, 0x60),
            (0x28, 0x78, 0x90),
            (0x70, 0x60, 0x38),
            (0x48, 0x60, 0x80)
        );

        private static readonly Brush EvalCorrectBrush = FreezeBrush(0x32, 0xCD, 0x32);
        private static readonly Brush EvalWrongClsBrush = FreezeBrush(0xFF, 0xA5, 0x00);
        private static readonly Brush EvalFalsePosBrush = FreezeBrush(0xDC, 0x14, 0x3C);
        private static readonly Brush EvalMissedBrush = FreezeBrush(0xDC, 0x14, 0x3C);

        public bool UseVibrantColors { get; set; }

        public IReadOnlyList<OverlayItem> Build(IReadOnlyList<DetectionResult> detections, BitmapSource? image)
        {
            var items = new List<OverlayItem>(detections.Count);
            var colors = UseVibrantColors ? VibrantPalette : ElegantPalette;
            var globalScale = GetGlobalScale(image);

            foreach (var detection in detections)
            {
                var brush = colors[(detection.Index - 1) % colors.Length];
                var diceValue = detection.DiceValue > 0 ? detection.DiceValue.ToString() : "?";
                var sizing = CreateSizing(detection.Width, detection.Height, globalScale, isEval: false);

                items.Add(new OverlayItem
                {
                    Index = detection.Index,
                    X = detection.X,
                    Y = detection.Y,
                    Width = detection.Width,
                    Height = detection.Height,
                    Label = $"#{detection.Index}",
                    DiceValue = diceValue,
                    Stroke = brush,
                    Color = brush,
                    StrokeThickness = new Thickness(sizing.Border),
                    LabelFontSize = sizing.FontSize,
                    LabelPadding = new Thickness(sizing.PaddingHorizontal, sizing.PaddingVertical, sizing.PaddingHorizontal, sizing.PaddingVertical),
                    LabelMargin = new Thickness(-2, -sizing.TagOffset, 0, 0),
                    BoxCornerRadius = new CornerRadius(sizing.Corner),
                    LabelCornerRadius = new CornerRadius(sizing.Corner, sizing.Corner, sizing.Corner, 0)
                });
            }

            return items;
        }

        public IReadOnlyList<EvalOverlayItem> BuildEvalOverlay(EvalResult evalResult, BitmapSource? image)
        {
            var items = new List<EvalOverlayItem>();
            var globalScale = GetGlobalScale(image);

            foreach (var match in evalResult.Matches)
            {
                var detection = match.Detection;
                var isCorrect = match.IsCorrectClass;

                items.Add(CreateEvalItem(
                    detection.X,
                    detection.Y,
                    detection.Width,
                    detection.Height,
                    isCorrect ? $"✓ {detection.DiceValue}" : $"✗ {detection.DiceValue} (GT:{match.Annotation.Value})",
                    isCorrect ? EvalOverlayType.TruePositive : EvalOverlayType.WrongClassification,
                    isCorrect ? EvalCorrectBrush : EvalWrongClsBrush,
                    globalScale));
            }

            foreach (var falsePositive in evalResult.FalsePositiveDetections)
            {
                items.Add(CreateEvalItem(
                    falsePositive.X,
                    falsePositive.Y,
                    falsePositive.Width,
                    falsePositive.Height,
                    $"FP: {falsePositive.DiceValue}",
                    EvalOverlayType.FalsePositive,
                    EvalFalsePosBrush,
                    globalScale));
            }

            foreach (var missed in evalResult.MissedObjects)
            {
                items.Add(CreateEvalItem(
                    missed.XMin,
                    missed.YMin,
                    missed.Width,
                    missed.Height,
                    "✕",
                    EvalOverlayType.Missed,
                    EvalMissedBrush,
                    globalScale));
            }

            return items;
        }

        private static EvalOverlayItem CreateEvalItem(
            double x,
            double y,
            double width,
            double height,
            string label,
            EvalOverlayType type,
            Brush brush,
            double globalScale)
        {
            var sizing = CreateSizing(width, height, globalScale, isEval: true);

            return new EvalOverlayItem
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Label = label,
                Type = type,
                Color = brush,
                StrokeThickness = new Thickness(sizing.Border),
                BoxCornerRadius = new CornerRadius(sizing.Corner),
                LabelFontSize = sizing.FontSize,
                LabelPadding = new Thickness(sizing.PaddingHorizontal, sizing.PaddingVertical, sizing.PaddingHorizontal, sizing.PaddingVertical),
                LabelMargin = new Thickness(-2, -sizing.TagOffset, 0, 0),
                LabelCornerRadius = new CornerRadius(sizing.Corner, sizing.Corner, sizing.Corner, 0)
            };
        }

        private static OverlaySizing CreateSizing(double width, double height, double globalScale, bool isEval)
        {
            var boxMin = Math.Max(1.0, Math.Min(width, height));

            var baseBorder = (isEval ? 4.0 : 5.0) * globalScale;
            var baseFont = (isEval ? 16.0 : 18.0) * globalScale;
            var basePadH = (isEval ? 8.0 : 10.0) * globalScale;
            var basePadV = (isEval ? 4.0 : 5.0) * globalScale;
            var baseCorner = (isEval ? 4.0 : 5.0) * globalScale;

            var border = ClampAdaptive(baseBorder, min: isEval ? 2.0 : 3.0, max: boxMin * 0.08);
            var fontSize = ClampAdaptive(baseFont, min: isEval ? 12.0 : 14.0, max: boxMin * (isEval ? 0.30 : 0.35));
            var paddingHorizontal = ClampAdaptive(basePadH, min: isEval ? 3.0 : 4.0, max: fontSize * 0.60);
            var paddingVertical = ClampAdaptive(basePadV, min: 2.0, max: fontSize * 0.35);
            var corner = ClampAdaptive(baseCorner, min: isEval ? 2.0 : 3.0, max: boxMin * 0.06);

            return new OverlaySizing
            {
                Border = border,
                FontSize = fontSize,
                PaddingHorizontal = paddingHorizontal,
                PaddingVertical = paddingVertical,
                Corner = corner,
                TagOffset = fontSize + 2 * paddingVertical + 4
            };
        }

        private static double ClampAdaptive(double value, double min, double max)
        {
            if (max <= 0)
                return 0;

            if (max < min)
                return max;

            return Math.Clamp(value, min, max);
        }

        private static double GetGlobalScale(BitmapSource? image)
        {
            var imageSize = Math.Max(image?.PixelWidth ?? 800, image?.PixelHeight ?? 800);
            return imageSize / 800.0;
        }

        private static Brush[] CreatePalette(params (byte R, byte G, byte B)[] rgb)
        {
            var brushes = new Brush[rgb.Length];

            for (var i = 0; i < rgb.Length; i++)
            {
                var brush = new SolidColorBrush(Color.FromRgb(rgb[i].R, rgb[i].G, rgb[i].B));
                brush.Freeze();
                brushes[i] = brush;
            }

            return brushes;
        }

        private static SolidColorBrush FreezeBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private sealed class OverlaySizing
        {
            public required double Border { get; init; }
            public required double FontSize { get; init; }
            public required double PaddingHorizontal { get; init; }
            public required double PaddingVertical { get; init; }
            public required double Corner { get; init; }
            public required double TagOffset { get; init; }
        }
    }
}