using DiceDetector.Services.Interfaces;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace DiceDetector.Services
{
    public class CameraService : ICameraService, IDisposable
    {
        private DispatcherTimer? _timer;
        private Action<BitmapSource>? _frameCallback;
        private BitmapSource? _lastFrame;
        private readonly Dispatcher _dispatcher;
        private readonly Random _random = new();

        public bool IsAvailable { get; private set; } = true;

        public CameraService()
        {
            _dispatcher = Application.Current.Dispatcher;
        }

        public Task StartAsync(Action<BitmapSource> onFrameCallback)
        {
            return Task.Run(() =>
            {
                _dispatcher.Invoke(() =>
                {
                    _frameCallback = onFrameCallback;
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(33)
                    };
                    _timer.Tick += SimulateFrame;
                    _timer.Start();
                });
            });
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                _dispatcher.Invoke(() =>
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Tick -= SimulateFrame;
                        _timer = null;
                    }
                    _frameCallback = null;
                });
            });
        }

        public Task<BitmapSource?> CaptureFrameAsync()
        {
            return Task.FromResult(_lastFrame);
        }

        private void SimulateFrame(object? sender, EventArgs e)
        {
            try
            {
                var width = 640;
                var height = 480;
                var dpi = 96;

                var pixels = new byte[width * height * 4];
                _random.NextBytes(pixels);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = (byte)(_random.Next(100, 200));
                    pixels[i + 1] = (byte)(_random.Next(100, 200));
                    pixels[i + 2] = (byte)(_random.Next(100, 200));
                    pixels[i + 3] = 255;
                }

                var bitmap = BitmapSource.Create(
                    width, height,
                    dpi, dpi,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    width * 4);

                bitmap.Freeze();
                _lastFrame = bitmap;
                _frameCallback?.Invoke(bitmap);
            }
            catch
            {
                // Ignore frame errors
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}
