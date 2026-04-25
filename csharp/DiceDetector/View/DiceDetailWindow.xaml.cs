using DiceDetector.Models;
using System.Windows;
using System.Windows.Media;

namespace DiceDetector.View
{
    public partial class DiceDetailWindow : Window
    {
        public DiceDetailWindow(DetectionResult result)
        {
            InitializeComponent();

            TitleText.Text = result.Label;
            CropImageControl.Source = result.CropImage;
            PredictedValueText.Text = result.DiceValue.ToString();
            DetConfidenceText.Text = $"{result.DetConfidence:P1}";
            ClsConfidenceText.Text = $"{result.ClsConfidence:P1}";
            FinalConfidenceText.Text = $"{result.FinalConfidence:P1}";
            StatusText.Text = result.IsUncertain ? "⚠ Nejistá detekce" : "✓ Spolehlivá detekce";
            StatusText.Foreground = result.StatusBrush;
            BoxPositionText.Text = $"x={result.X:0}, y={result.Y:0}";
            CropSizeText.Text = result.CropImage != null
                ? $"{result.CropImage.PixelWidth} × {result.CropImage.PixelHeight} px"
                : "N/A";
            AreaText.Text = $"{result.Width * result.Height:N0} px²";
            TopPredictionsList.ItemsSource = result.TopPredictions;

            // Show evaluation metrics if available (only when eval data is present)
            if (!string.IsNullOrEmpty(result.EvalMatchType))
            {
                EvalMetricsPanel.Visibility = Visibility.Visible;
                GroundTruthText.Text = result.EvalGroundTruth?.ToString() ?? "-";
                IoUText.Text = result.EvalIoU.HasValue ? $"{result.EvalIoU.Value:P0}" : "-";

                switch (result.EvalMatchType)
                {
                    case "TP":
                        MatchTypeText.Text = "✓ Správné";
                        MatchTypeText.Foreground = new SolidColorBrush(Color.FromRgb(0x32, 0xCD, 0x32)); // Green
                        break;
                    case "WrongClass":
                        MatchTypeText.Text = "≈ Špatná třída";
                        MatchTypeText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)); // Orange
                        break;
                    case "FP":
                        MatchTypeText.Text = "✗ False Positive";
                        MatchTypeText.Foreground = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)); // Red
                        break;
                }
            }
            else
            {
                EvalMetricsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
