using System.Windows.Media.Imaging;

namespace DiceDetector.Services.Interfaces
{
    public interface ICameraService
    {
        Task StartAsync(Action<BitmapSource> onFrameCallback);
        Task StopAsync();
        Task<BitmapSource?> CaptureFrameAsync();
        bool IsAvailable { get; }
    }
}
