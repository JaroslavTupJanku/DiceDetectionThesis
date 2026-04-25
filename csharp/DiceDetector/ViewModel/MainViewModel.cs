using CommunityToolkit.Mvvm.Input;
using DiceDetector.Models;
using DiceDetector.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;

namespace DiceDetector.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IImageDialogService _imageDialogService;
        private readonly IInferenceService _inferenceService;
        private readonly IOverlayRenderer _overlayRenderer;
        private readonly ICameraService? _cameraService;
        private readonly IThemeService _themeService;

        private BitmapSource? _displayedImage;
        private string? _currentImagePath;
        private bool _isBusy;
        private bool _isImageLoaded;
        private bool _isCameraActive;
        private bool _isLightTheme;
        private bool _isVibrantOverlay;
        private string _statusText = "Připraveno";
        private string _inferenceTimeText = "-";
        private int _detectionCount;
        private string _totalValueText = "-";
        private string _averageConfidenceText = "-";
        private double _progressValue;
        private bool _showProgress;
        private string _uncertainCountText = "-";
        private bool _hasDetections;

        public MainViewModel(
            IImageDialogService imageDialogService,
            IInferenceService inferenceService,
            IOverlayRenderer overlayRenderer,
            IThemeService themeService,
            ICameraService? cameraService = null)
        {
            _imageDialogService = imageDialogService;
            _inferenceService = inferenceService;
            _overlayRenderer = overlayRenderer;
            _cameraService = cameraService;
            _themeService = themeService;

            OpenImageCommand = new RelayCommand(OpenImage);
            ClearCommand = new RelayCommand(Clear);
            RunInferenceCommand = new AsyncRelayCommand(RunInferenceAsync, () => CanRunInference);
            ToggleCameraCommand = new AsyncRelayCommand(ToggleCameraAsync);
            CaptureFromCameraCommand = new AsyncRelayCommand(CaptureFromCameraAsync, () => _isCameraActive);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            ShowDiceDetailCommand = new RelayCommand<DetectionResult>(ShowDiceDetail);

            // Nastavit výchozí stav podle aktuálního tématu
            _isLightTheme = _themeService.CurrentTheme == AppTheme.Light;
        }

        public BitmapSource? DisplayedImage
        {
            get => _displayedImage;
            private set => SetProperty(ref _displayedImage, value);
        }

        public bool IsImageLoaded
        {
            get => _isImageLoaded;
            private set
            {
                if (SetProperty(ref _isImageLoaded, value))
                {
                    RaisePropertyChanged(nameof(CanRunInference));
                    RunInferenceCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanRunInference => IsImageLoaded && !_isBusy;

        public bool IsCameraActive
        {
            get => _isCameraActive;
            private set
            {
                if (SetProperty(ref _isCameraActive, value))
                {
                    CaptureFromCameraCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (SetProperty(ref _isLightTheme, value))
                {
                    _themeService.SetTheme(value ? AppTheme.Light : AppTheme.Dark);
                }
            }
        }

        public bool IsVibrantOverlay
        {
            get => _isVibrantOverlay;
            set
            {
                if (SetProperty(ref _isVibrantOverlay, value))
                {
                    _overlayRenderer.UseVibrantColors = value;
                    RefreshOverlay();
                }
            }
        }

        public bool ShowProgress
        {
            get => _showProgress;
            private set => SetProperty(ref _showProgress, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            private set => SetProperty(ref _progressValue, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string InferenceTimeText
        {
            get => _inferenceTimeText;
            private set => SetProperty(ref _inferenceTimeText, value);
        }

        public int DetectionCount
        {
            get => _detectionCount;
            private set => SetProperty(ref _detectionCount, value);
        }

        public string TotalValueText
        {
            get => _totalValueText;
            private set => SetProperty(ref _totalValueText, value);
        }

        public string AverageConfidenceText
        {
            get => _averageConfidenceText;
            private set => SetProperty(ref _averageConfidenceText, value);
        }

        public ObservableCollection<DetectionResult> Results { get; } = new();
        public ObservableCollection<OverlayItem> OverlayItems { get; } = new();
        public ObservableCollection<DiceValueCount> ValueDistribution { get; } = new();

        public string UncertainCountText
        {
            get => _uncertainCountText;
            private set => SetProperty(ref _uncertainCountText, value);
        }

        public bool HasDetections
        {
            get => _hasDetections;
            private set => SetProperty(ref _hasDetections, value);
        }

        public RelayCommand OpenImageCommand { get; }
        public RelayCommand ClearCommand { get; }
        public AsyncRelayCommand RunInferenceCommand { get; }
        public AsyncRelayCommand ToggleCameraCommand { get; }
        public AsyncRelayCommand CaptureFromCameraCommand { get; }
        public RelayCommand ToggleThemeCommand { get; }
        public RelayCommand<DetectionResult> ShowDiceDetailCommand { get; }

        public event Action<DetectionResult>? RequestShowDiceDetail;

        private void OpenImage()
        {
            var path = _imageDialogService.OpenImage();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            _currentImagePath = path;
            DisplayedImage = LoadBitmap(path);
            IsImageLoaded = true;
            StatusText = $"Načten obrázek: {Path.GetFileName(path)}";

            Results.Clear();
            OverlayItems.Clear();
            DetectionCount = 0;
            InferenceTimeText = "-";
        }

        private async Task RunInferenceAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentImagePath))
            {
                return;
            }

            try
            {
                _isBusy = true;
                ShowProgress = true;
                ProgressValue = 0;
                RaisePropertyChanged(nameof(CanRunInference));
                RunInferenceCommand.NotifyCanExecuteChanged();
                StatusText = "Probíhá inference...";

                ProgressValue = 30;
                var stopwatch = Stopwatch.StartNew();
                var result = await _inferenceService.RunAsync(_currentImagePath);
                stopwatch.Stop();

                ProgressValue = 60;
                Results.Clear();
                foreach (var item in result.Detections)
                {
                    Results.Add(item);
                }

                ProgressValue = 80;
                OverlayItems.Clear();
                var overlays = _overlayRenderer.Build(result.Detections, DisplayedImage);
                foreach (var overlay in overlays)
                {
                    OverlayItems.Add(overlay);
                }

                // Propagate index + color back to results for the list panel
                for (var i = 0; i < Results.Count && i < overlays.Count; i++)
                {
                    Results[i].ColorBrush = overlays[i].Color;
                }

                ProgressValue = 100;
                DetectionCount = result.Detections.Count;
                InferenceTimeText = $"{stopwatch.ElapsedMilliseconds} ms";

                UpdateStatistics(result.Detections);

                StatusText = $"✓ Detekce dokončena - nalezeno {result.Detections.Count} kostek";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ Chyba: {ex.Message}";
            }
            finally
            {
                _isBusy = false;
                ShowProgress = false;
                RaisePropertyChanged(nameof(CanRunInference));
                RunInferenceCommand.NotifyCanExecuteChanged();
            }
        }

        private void UpdateStatistics(IReadOnlyList<DetectionResult> detections)
        {
            if (!detections.Any())
            {
                TotalValueText = "-";
                AverageConfidenceText = "-";
                UncertainCountText = "-";
                HasDetections = false;
                ValueDistribution.Clear();
                return;
            }

            var totalValue = 0;
            var counts = new int[6];
            var uncertainCount = 0;

            foreach (var detection in detections)
            {
                totalValue += detection.DiceValue;
                if (detection.DiceValue >= 1 && detection.DiceValue <= 6)
                    counts[detection.DiceValue - 1]++;
                if (detection.ClsConfidence < 0.70f)
                    uncertainCount++;
            }

            var avgConfidence = detections.Average(d => d.Confidence);
            TotalValueText = totalValue.ToString();
            AverageConfidenceText = $"{avgConfidence:P0}";

            var maxCount = counts.Max();
            const double maxBarHeight = 60.0;

            ValueDistribution.Clear();
            for (var i = 0; i < 6; i++)
            {
                ValueDistribution.Add(new DiceValueCount
                {
                    Value = i + 1,
                    Count = counts[i],
                    BarHeight = maxCount > 0 && counts[i] > 0
                        ? Math.Max(3, (counts[i] / (double)maxCount) * maxBarHeight)
                        : 0
                });
            }

            UncertainCountText = $"{uncertainCount}";
            HasDetections = true;
        }

        private void ShowDiceDetail(DetectionResult? result)
        {
            if (result != null)
                RequestShowDiceDetail?.Invoke(result);
        }

        private async Task ToggleCameraAsync()
        {
            if (_cameraService == null)
            {
                StatusText = "✗ Kamera není k dispozici";
                return;
            }

            try
            {
                if (_isCameraActive)
                {
                    await _cameraService.StopAsync();
                    IsCameraActive = false;
                    StatusText = "Kamera zastavena";
                }
                else
                {
                    Clear();
                    await _cameraService.StartAsync(frame =>
                    {
                        DisplayedImage = frame;
                    });
                    IsCameraActive = true;
                    StatusText = "✓ Kamera aktivní - zmáčkněte 'Zachytit snímek'";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"✗ Chyba kamery: {ex.Message}";
                IsCameraActive = false;
            }
        }

        private async Task CaptureFromCameraAsync()
        {
            if (_cameraService == null || !_isCameraActive)
                return;

            try
            {
                var frame = await _cameraService.CaptureFrameAsync();
                if (frame != null)
                {
                    await _cameraService.StopAsync();
                    IsCameraActive = false;

                    var tempPath = Path.Combine(Path.GetTempPath(), $"camera_capture_{Guid.NewGuid()}.png");
                    SaveBitmapToFile(frame, tempPath);

                    _currentImagePath = tempPath;
                    DisplayedImage = frame;
                    IsImageLoaded = true;
                    StatusText = "✓ Snímek zachycen z kamery";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"✗ Chyba při zachycení: {ex.Message}";
            }
        }

        private void ToggleTheme()
        {
            IsLightTheme = !IsLightTheme;
        }

        private void RefreshOverlay()
        {
            if (Results.Count == 0) return;

            OverlayItems.Clear();
            var overlays = _overlayRenderer.Build(Results.ToList(), DisplayedImage);
            foreach (var overlay in overlays)
            {
                OverlayItems.Add(overlay);
            }

            // Force UI refresh: DetectionResult has no INPC, so re-insert items
            var snapshot = Results.ToList();
            Results.Clear();
            for (var i = 0; i < snapshot.Count; i++)
            {
                if (i < overlays.Count) snapshot[i].ColorBrush = overlays[i].Color;
                Results.Add(snapshot[i]);
            }
        }

        private void Clear()
        {
            if (_isCameraActive && _cameraService != null)
            {
                _ = _cameraService.StopAsync();
                IsCameraActive = false;
            }

            _currentImagePath = null;
            DisplayedImage = null;
            IsImageLoaded = false;
            Results.Clear();
            OverlayItems.Clear();
            ValueDistribution.Clear();
            DetectionCount = 0;
            InferenceTimeText = "-";
            TotalValueText = "-";
            AverageConfidenceText = "-";
            UncertainCountText = "-";
            HasDetections = false;
            StatusText = "Připraveno";
        }

        private static BitmapImage LoadBitmap(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static void SaveBitmapToFile(BitmapSource bitmap, string path)
        {
            using var fileStream = new FileStream(path, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fileStream);
        }
    }
}
