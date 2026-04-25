using DiceDetector.Models;
using System.Windows.Media.Imaging;

namespace DiceDetector.Services.Interfaces
{
    public interface IOverlayRenderer
    {
        bool UseVibrantColors { get; set; }
        IReadOnlyList<OverlayItem> Build(IReadOnlyList<DetectionResult> detections, BitmapSource? image);
    }

}
