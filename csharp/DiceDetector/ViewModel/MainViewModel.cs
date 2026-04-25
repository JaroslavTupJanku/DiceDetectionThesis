using CommunityToolkit.Mvvm.Input;
using DiceDetector.Models;
using DiceDetector.Services;
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
        private string _totalValueBreakdownText = "";
        private string _averageConfidenceText = "-";
        private double _progressValue;
        private bool _showProgress;
        private string _uncertainCountText = "-";
        private bool _hasDetections;
        private readonly AnnotationService _annotationService = new();
        private bool _isEvalMode;
        private string _evalStatusText = "";
        private EvalResult? _lastEvalResult;
        private string _confidenceRangeText = "";
        private string _confidenceDetailText = "";
        private string _dominantValueText = "";
        private string _detectionSizeRangeText = "";
        private string _missedCountText = "";
        private int _evalAnnotationCount;
        private int _missedDiceCount;
        private bool _hasMissedDice;

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
            LoadAnnotationsCommand = new RelayCommand(LoadAnnotations);
            DisableEvalModeCommand = new RelayCommand(DisableEvalMode);

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

        public string TotalValueBreakdownText
        {
            get => _totalValueBreakdownText;
            private set => SetProperty(ref _totalValueBreakdownText, value);
        }

        public string AverageConfidenceText
        {
            get => _averageConfidenceText;
            private set => SetProperty(ref _averageConfidenceText, value);
        }

        public ObservableCollection<DetectionResult> Results { get; } = new();
        public ObservableCollection<OverlayItem> OverlayItems { get; } = new();
        public ObservableCollection<EvalOverlayItem> EvalOverlayItems { get; } = new();
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

        public bool IsEvalMode
        {
            get => _isEvalMode;
            private set => SetProperty(ref _isEvalMode, value);
        }

        public string EvalStatusText
        {
            get => _evalStatusText;
            private set => SetProperty(ref _evalStatusText, value);
        }

        public EvalResult? LastEvalResult
        {
            get => _lastEvalResult;
            private set => SetProperty(ref _lastEvalResult, value);
        }

        public string ConfidenceRangeText
        {
            get => _confidenceRangeText;
            private set => SetProperty(ref _confidenceRangeText, value);
        }

        public string ConfidenceDetailText
        {
            get => _confidenceDetailText;
            private set => SetProperty(ref _confidenceDetailText, value);
        }

        public string DominantValueText
        {
            get => _dominantValueText;
            private set => SetProperty(ref _dominantValueText, value);
        }

        public string DetectionSizeRangeText
        {
            get => _detectionSizeRangeText;
            private set => SetProperty(ref _detectionSizeRangeText, value);
        }

        public string MissedCountText
        {
            get => _missedCountText;
            private set => SetProperty(ref _missedCountText, value);
        }

        public int EvalAnnotationCount
        {
            get => _evalAnnotationCount;
            private set => SetProperty(ref _evalAnnotationCount, value);
        }

        public int MissedDiceCount
        {
            get => _missedDiceCount;
            private set => SetProperty(ref _missedDiceCount, value);
        }

        public bool HasMissedDice
        {
            get => _hasMissedDice;
            private set => SetProperty(ref _hasMissedDice, value);
        }

        public RelayCommand OpenImageCommand { get; }
        public RelayCommand ClearCommand { get; }
        public AsyncRelayCommand RunInferenceCommand { get; }
        public AsyncRelayCommand ToggleCameraCommand { get; }
        public AsyncRelayCommand CaptureFromCameraCommand { get; }
        public RelayCommand ToggleThemeCommand { get; }
        public RelayCommand<DetectionResult> ShowDiceDetailCommand { get; }
        public RelayCommand LoadAnnotationsCommand { get; }
        public RelayCommand DisableEvalModeCommand { get; }

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

                // Clear eval overlay from previous inference
                EvalOverlayItems.Clear();
                MissedDiceCount = 0;
                HasMissedDice = false;

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

                // Eval mode - compare with annotations if loaded
                EvalOverlayItems.Clear();
                if (_annotationService.IsLoaded && _currentImagePath != null)
                {
                    var annotation = _annotationService.FindAnnotation(_currentImagePath);
                    if (annotation != null)
                    {
                        LastEvalResult = _annotationService.Evaluate(result.Detections, annotation);
                        MissedCountText = $"Chybějící: {LastEvalResult.FalseNegatives}";
                        MissedDiceCount = LastEvalResult.FalseNegatives;
                        HasMissedDice = LastEvalResult.FalseNegatives > 0;
                        EvalStatusText = $"TP: {LastEvalResult.TruePositives}  FP: {LastEvalResult.FalsePositives}  FN: {LastEvalResult.FalseNegatives}\n" +
                                         $"Precision: {LastEvalResult.Precision:P0}  Recall: {LastEvalResult.Recall:P0}\n" +
                                         $"Accuracy: {LastEvalResult.ClassificationAccuracy:P0}  IoU: {LastEvalResult.AverageIoU:F2}";

                        // Propagate eval data to detection results
                        foreach (var match in LastEvalResult.Matches)
                        {
                            match.Detection.EvalIoU = match.IoU;
                            match.Detection.EvalGroundTruth = match.Annotation.Value;
                            match.Detection.EvalMatchType = match.IsCorrectClass ? "TP" : "WrongClass";
                        }
                        foreach (var fp in LastEvalResult.FalsePositiveDetections)
                        {
                            fp.EvalMatchType = "FP";
                        }

                        var evalOverlays = _overlayRenderer.BuildEvalOverlay(LastEvalResult, DisplayedImage);
                        foreach (var eo in evalOverlays)
                        {
                            EvalOverlayItems.Add(eo);
                        }
                    }
                    else
                    {
                        LastEvalResult = null;
                        EvalStatusText = "Anotace pro tento obrázek nenalezena";
                    }
                }

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
                TotalValueBreakdownText = "";
                AverageConfidenceText = "-";
                UncertainCountText = "-";
                ConfidenceRangeText = "";
                ConfidenceDetailText = "";
                DominantValueText = "";
                DetectionSizeRangeText = "";
                MissedCountText = "";
                HasDetections = false;
                ValueDistribution.Clear();
                return;
            }

            var totalValue = 0;
            var counts = new int[6];
            var uncertainCount = 0;
            var values = new List<string>();

            foreach (var detection in detections)
            {
                totalValue += detection.DiceValue;
                values.Add(detection.DiceValue.ToString());
                if (detection.DiceValue >= 1 && detection.DiceValue <= 6)
                    counts[detection.DiceValue - 1]++;
                if (detection.FinalConfidence < 0.70f)
                    uncertainCount++;
            }

            var avgFinalConfidence = detections.Average(d => d.FinalConfidence);
            var minConf = detections.Min(d => d.FinalConfidence);
            var maxConf = detections.Max(d => d.FinalConfidence);
            var avgDet = detections.Average(d => (double)d.DetConfidence);
            var avgCls = detections.Average(d => (double)d.ClsConfidence);

            TotalValueText = totalValue.ToString();
            TotalValueBreakdownText = detections.Count > 1
                ? $"{string.Join(" + ", values)} = {totalValue}"
                : "";
            AverageConfidenceText = $"{avgFinalConfidence:P0}";
            ConfidenceRangeText = $"Rozsah: {minConf:P0} – {maxConf:P0}";
            ConfidenceDetailText = $"Det: {avgDet:P0}  Cls: {avgCls:P0}";

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

            UncertainCountText = uncertainCount > 0 
                ? $"{uncertainCount} ({(uncertainCount * 100.0 / detections.Count):F0}%)"
                : "0";

            // Find most frequent value
            var mostFrequentValue = counts
                .Select((count, index) => new { Value = index + 1, Count = count })
                .OrderByDescending(x => x.Count)
                .First();
            DominantValueText = mostFrequentValue.Count > 0
                ? $"Nejčastější: {mostFrequentValue.Value} ({mostFrequentValue.Count}×)"
                : "";

            HasDetections = true;
        }

        private void ShowDiceDetail(DetectionResult? result)
        {
            if (result != null)
                RequestShowDiceDetail?.Invoke(result);
        }

        private void LoadAnnotations()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Vyberte složku s XML anotacemi (Pascal VOC)"
            };

            if (dialog.ShowDialog() == true)
            {
                _annotationService.LoadFolder(dialog.FolderName);
                IsEvalMode = _annotationService.IsLoaded;
                EvalAnnotationCount = _annotationService.AnnotationCount;
                StatusText = _annotationService.IsLoaded
                    ? $"✓ Eval mode: {_annotationService.AnnotationCount} anotací z {Path.GetFileName(dialog.FolderName)}"
                    : "✗ Žádné anotace nenalezeny";

                // If we already have detections and image, re-evaluate with annotations
                if (_annotationService.IsLoaded && Results.Count > 0 && _currentImagePath != null)
                {
                    var annotation = _annotationService.FindAnnotation(_currentImagePath);
                    if (annotation != null)
                    {
                        LastEvalResult = _annotationService.Evaluate(Results.ToList(), annotation);
                        MissedCountText = $"Chybějící: {LastEvalResult.FalseNegatives}";
                        MissedDiceCount = LastEvalResult.FalseNegatives;
                        HasMissedDice = LastEvalResult.FalseNegatives > 0;
                        EvalStatusText = $"TP: {LastEvalResult.TruePositives}  FP: {LastEvalResult.FalsePositives}  FN: {LastEvalResult.FalseNegatives}\n" +
                                         $"Precision: {LastEvalResult.Precision:P0}  Recall: {LastEvalResult.Recall:P0}\n" +
                                         $"Accuracy: {LastEvalResult.ClassificationAccuracy:P0}  IoU: {LastEvalResult.AverageIoU:F2}";

                        // Propagate eval data to detection results
                        foreach (var match in LastEvalResult.Matches)
                        {
                            match.Detection.EvalIoU = match.IoU;
                            match.Detection.EvalGroundTruth = match.Annotation.Value;
                            match.Detection.EvalMatchType = match.IsCorrectClass ? "TP" : "WrongClass";
                        }
                        foreach (var fp in LastEvalResult.FalsePositiveDetections)
                        {
                            fp.EvalMatchType = "FP";
                        }

                        EvalOverlayItems.Clear();
                        var evalOverlays = _overlayRenderer.BuildEvalOverlay(LastEvalResult, DisplayedImage);
                        foreach (var eo in evalOverlays)
                        {
                            EvalOverlayItems.Add(eo);
                        }

                        StatusText = $"✓ Eval aktualizováno pro aktuální obrázek";
                    }
                    else
                    {
                        EvalStatusText = "Anotace pro tento obrázek nenalezena";
                        LastEvalResult = null;
                        MissedDiceCount = 0;
                        HasMissedDice = false;
                        EvalOverlayItems.Clear();
                    }
                }
                else
                {
                    EvalStatusText = "";
                    LastEvalResult = null;
                    MissedDiceCount = 0;
                    HasMissedDice = false;
                    EvalOverlayItems.Clear();
                }
            }
        }

        private void DisableEvalMode()
        {
            _annotationService.Clear();
            IsEvalMode = false;
            EvalStatusText = "";
            LastEvalResult = null;
            MissedDiceCount = 0;
            HasMissedDice = false;
            EvalOverlayItems.Clear();

            // Clear eval data from all existing detection results
            foreach (var result in Results)
            {
                result.EvalMatchType = null;
                result.EvalIoU = null;
                result.EvalGroundTruth = null;
            }

            StatusText = "Eval mode vypnut";
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
            EvalOverlayItems.Clear();
            ValueDistribution.Clear();
            DetectionCount = 0;
            InferenceTimeText = "-";
            TotalValueText = "-";
            TotalValueBreakdownText = "";
            AverageConfidenceText = "-";
            UncertainCountText = "-";
            ConfidenceRangeText = "";
            ConfidenceDetailText = "";
            DominantValueText = "";
            DetectionSizeRangeText = "";
            MissedCountText = "";
            HasDetections = false;
            LastEvalResult = null;
            EvalStatusText = "";
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
