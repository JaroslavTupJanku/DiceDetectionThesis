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
            (0x1E, 0x90, 0xFF),   // DodgerBlue
            (0xDC, 0x14, 0x3C),   // Crimson
            (0x32, 0xCD, 0x32),   // LimeGreen
            (0xFF, 0xA5, 0x00),   // Orange
            (0x93, 0x70, 0xDB),   // MediumPurple
            (0xFF, 0x14, 0x93),   // DeepPink
            (0x00, 0xE5, 0xFF),   // Cyan
            (0xFF, 0xD7, 0x00),   // Gold
            (0xFF, 0x00, 0xFF)    // Magenta
        );

        private static readonly Brush[] ElegantPalette = CreatePalette(
            (0x2E, 0x50, 0x90),   // Corporate navy
            (0xA0, 0x30, 0x40),   // Burgundy wine
            (0x1A, 0x7A, 0x5A),   // Deep emerald
            (0xB8, 0x86, 0x20),   // Antique gold
            (0x5A, 0x45, 0x8A),   // Royal indigo
            (0x90, 0x40, 0x60),   // Dusty rose
            (0x28, 0x78, 0x90),   // Ocean teal
            (0x70, 0x60, 0x38),   // Warm bronze
            (0x48, 0x60, 0x80)    // Steel blue
        );

        public bool UseVibrantColors { get; set; }

        public IReadOnlyList<OverlayItem> Build(IReadOnlyList<DetectionResult> detections, BitmapSource? image)
        {
            var items = new List<OverlayItem>(detections.Count);
            var colors = UseVibrantColors ? VibrantPalette : ElegantPalette;

            // Base scale from image size so overlays stay visible inside the Viewbox
            var imageSize = Math.Max(image?.PixelWidth ?? 800, image?.PixelHeight ?? 800);
            var globalScale = imageSize / 800.0;

            for (var i = 0; i < detections.Count; i++)
            {
                var detection = detections[i];
                var brush = colors[i % colors.Length];
                var diceValue = detection.ValueText.Replace("Hodnota: ", "");
                var index = i + 1;

                // Per-box: cap overlay sizes so small boxes don't get overwhelmed
                var boxMin = Math.Min(detection.Width, detection.Height);
                var borderPx = Math.Clamp(5 * globalScale, 3, boxMin * 0.08);
                var fontSize = Math.Clamp(18 * globalScale, 14, boxMin * 0.35);
                var padH = Math.Clamp(10 * globalScale, 4, fontSize * 0.6);
                var padV = Math.Clamp(5 * globalScale, 2, fontSize * 0.35);
                var corner = Math.Clamp(5 * globalScale, 3, boxMin * 0.06);
                var tagOffset = fontSize + 2 * padV + 4;

                items.Add(new OverlayItem
                {
                    Index = index,
                    X = detection.X,
                    Y = detection.Y,
                    Width = detection.Width,
                    Height = detection.Height,
                    Label = $"#{index}",
                    DiceValue = diceValue,
                    Stroke = brush,
                    Color = brush,
                    StrokeThickness = new Thickness(borderPx),
                    LabelFontSize = fontSize,
                    LabelPadding = new Thickness(padH, padV, padH, padV),
                    LabelMargin = new Thickness(-2, -tagOffset, 0, 0),
                    BoxCornerRadius = new CornerRadius(corner),
                    LabelCornerRadius = new CornerRadius(corner, corner, corner, 0)
                });
            }

            return items;
        }

        private static Brush[] CreatePalette(params (byte R, byte G, byte B)[] rgb)
        {
            var brushes = new Brush[rgb.Length];
            for (var i = 0; i < rgb.Length; i++)
            {
                var b = new SolidColorBrush(Color.FromRgb(rgb[i].R, rgb[i].G, rgb[i].B));
                b.Freeze();
                brushes[i] = b;
            }
            return brushes;
        }
    }
}
