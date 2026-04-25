using DiceDetector.Models;
using System.Windows;

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
            BoxPositionText.Text = $"x={result.X:0}, y={result.Y:0}";
            CropSizeText.Text = result.CropImage != null
                ? $"{result.CropImage.PixelWidth} × {result.CropImage.PixelHeight} px"
                : "N/A";
            AreaText.Text = $"{result.Width * result.Height:N0} px²";
            TopPredictionsList.ItemsSource = result.TopPredictions;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
