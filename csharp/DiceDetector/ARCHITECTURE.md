# 🏗️ Technická dokumentace

## Architektura aplikace

### 1. MVVM Pattern

Aplikace používá standardní MVVM (Model-View-ViewModel) pattern:

#### View (MainWindow.xaml)
- Deklarativní UI definované v XAML
- Data binding na ViewModel properties
- Command binding pro uživatelské akce
- Žádná business logika v code-behind

#### ViewModel (MainViewModel.cs)
```csharp
public class MainViewModel : ViewModelBase
{
    // Commands
    public RelayCommand OpenImageCommand { get; }
    public AsyncRelayCommand RunInferenceCommand { get; }
    public AsyncRelayCommand ToggleCameraCommand { get; }
    public AsyncRelayCommand CaptureFromCameraCommand { get; }
    public RelayCommand ClearCommand { get; }

    // Observable Properties
    public BitmapSource? DisplayedImage { get; }
    public ObservableCollection<DetectionResult> Results { get; }
    public ObservableCollection<OverlayItem> OverlayItems { get; }
    public int DetectionCount { get; }
    public string TotalValueText { get; }
    public string AverageConfidenceText { get; }
    
    // Dependencies
    private readonly IImageDialogService _imageDialogService;
    private readonly IInferenceService _inferenceService;
    private readonly IOverlayRenderer _overlayRenderer;
    private readonly ICameraService _cameraService;
}
```

#### Model (Services + Data Models)
- Business logika v services
- Data transfer objects (DTOs) pro přenos dat

### 2. Dependency Injection

Registrace v `App.xaml.cs`:

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    // Services - Singleton lifecycle
    services.AddSingleton<IImageDialogService, ImageDialogService>();
    services.AddSingleton<IPreprocessingService, PreprocessingService>();
    services.AddSingleton<IInferenceService, OnnxInferenceService>();
    services.AddSingleton<IOverlayRenderer, OverlayRenderer>();
    services.AddSingleton<ICameraService, CameraService>();
    
    // ViewModels
    services.AddSingleton<MainViewModel>();
}
```

**Výhody DI:**
- Loose coupling mezi komponentami
- Snadné unit testování (mockování závislostí)
- Centralizovaná správa životního cyklu objektů

### 3. ONNX Inference Pipeline

#### Workflow detekce a klasifikace:

```
┌──────────────┐
│ Input Image  │
└──────┬───────┘
       │
       ▼
┌──────────────────────┐
│ Preprocessing        │
│ - Resize to 640x640  │
│ - Normalize [0-1]    │
│ - Convert to NCHW    │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ YOLO Detector        │
│ (dice_detector.onnx) │
│ → Bounding Boxes     │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ For each detection:  │
│ - Crop region        │
│ - Resize to 224x224  │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Classifier           │
│(dice_classifier.onnx)│
│ → Dice value (1-6)   │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ DetectionResult[]    │
│ - Label, Value       │
│ - BBox coordinates   │
│ - Confidence score   │
└──────────────────────┘
```

#### OnnxInferenceService implementace:

```csharp
public async Task<InferenceResult> RunAsync(string imagePath)
{
    // 1. Load image
    var bitmap = LoadBitmap(imagePath);
    
    // 2. Run detector (YOLO)
    var detections = RunDetector(bitmap);
    
    // 3. Classify each detection
    var classified = new List<DetectionResult>();
    foreach (var detection in detections)
    {
        var crop = CropBitmap(bitmap, detection.X, detection.Y, 
                             detection.Width, detection.Height);
        var predictedValue = RunClassifier(crop);
        classified.Add(/* ... */);
    }
    
    return new InferenceResult { Detections = classified };
}
```

### 4. Preprocessing Service

Převod obrázku na tensor pro ONNX model:

```csharp
public float[] PrepareImageTensor(BitmapSource source, 
                                  int targetWidth, 
                                  int targetHeight, 
                                  bool nchw = true)
{
    // 1. Resize
    var resized = new TransformedBitmap(source, 
        new ScaleTransform(
            targetWidth / (double)source.PixelWidth,
            targetHeight / (double)source.PixelHeight));
    
    // 2. Extract pixels
    var pixels = new byte[targetWidth * targetHeight * 4];
    resized.CopyPixels(pixels, stride, 0);
    
    // 3. Normalize & Convert to float
    var tensor = new float[3 * targetWidth * targetHeight];
    
    if (nchw) // Channel-first format (C,H,W)
    {
        for (int i = 0; i < pixels.Length; i += 4)
        {
            tensor[rIdx++] = pixels[i + 2] / 255f; // R
            tensor[gIdx++] = pixels[i + 1] / 255f; // G
            tensor[bIdx++] = pixels[i + 0] / 255f; // B
        }
    }
    
    return tensor;
}
```

### 5. Camera Service

Implementace live camera preview:

```csharp
public class CameraService : ICameraService
{
    private DispatcherTimer? _timer;
    private Action<BitmapSource>? _frameCallback;
    
    public async Task StartAsync(Action<BitmapSource> onFrameCallback)
    {
        _frameCallback = onFrameCallback;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += SimulateFrame;
        _timer.Start();
    }
    
    public async Task StopAsync()
    {
        _timer?.Stop();
        _frameCallback = null;
    }
    
    public async Task<BitmapSource?> CaptureFrameAsync()
    {
        return _lastFrame; // Snapshot of current frame
    }
}
```

**Note:** Aktuální implementace je simulovaná. Pro reálnou kameru použijte:
- **AForge.NET** - Cross-platform video capture
- **DirectShow** - Windows native API
- **MediaCapture** - UWP API (vyžaduje Windows.Media.Capture)

### 6. UI Components

#### Statistics Cards
Zobrazení key metrics v reálném čase:
- Počet detekovaných kostek
- Součet hodnot (pro hry)
- Průměrná confidence
- Inference time (performance metriky)

#### Overlay Renderer
Vizualizace detection boxů na obraze:

```csharp
public IReadOnlyList<OverlayItem> Build(IReadOnlyList<DetectionResult> detections, 
                                        BitmapSource? image)
{
    var colors = new[] { Blue, Red, Green, Orange, Purple, ... };
    
    for (var i = 0; i < detections.Count; i++)
    {
        items.Add(new OverlayItem
        {
            X = detection.X,
            Y = detection.Y,
            Width = detection.Width,
            Height = detection.Height,
            Label = detection.ValueText,
            Color = colors[i % colors.Length]
        });
    }
}
```

XAML rendering pomocí ItemsControl a Canvas:

```xaml
<ItemsControl ItemsSource="{Binding OverlayItems}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Canvas/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border BorderBrush="{Binding Color}" 
                    BorderThickness="3"
                    Width="{Binding Width}" 
                    Height="{Binding Height}">
                <TextBlock Text="{Binding Label}" 
                          Background="{Binding Color}"/>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 7. Performance Optimizations

#### ONNX Runtime optimalizace:
```csharp
var sessionOptions = new SessionOptions
{
    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
};
```

#### Async/Await pattern:
```csharp
public async Task RunInferenceAsync()
{
    _isBusy = true;
    ShowProgress = true;
    
    var result = await _inferenceService.RunAsync(_currentImagePath);
    
    // Update UI on completion
    DetectionCount = result.Detections.Count;
    UpdateStatistics(result.Detections);
    
    _isBusy = false;
    ShowProgress = false;
}
```

#### Bitmap freezing:
```csharp
bitmap.Freeze(); // Make bitmap thread-safe and immutable
```

### 8. Error Handling

```csharp
try
{
    var result = await _inferenceService.RunAsync(_currentImagePath);
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
}
```

## Rozšíření a customizace

### Přidání nového AI modelu
1. Umístit `.onnx` soubor do `Models/` složky
2. Vytvořit nový service implementující `IInferenceService`
3. Zaregistrovat v DI containeru

### Změna UI tématu
Upravit `Themes/AppStyles.xaml`:
```xaml
<SolidColorBrush x:Key="Brush.Primary" Color="#YOUR_COLOR" />
```

### Přidání nových statistik
1. Přidat property do `MainViewModel`
2. Aktualizovat v `UpdateStatistics()` metodě
3. Přidat UI element s bindingem v XAML

## Best Practices použité v projektu

✅ **SOLID principles** - Single Responsibility, Interface Segregation  
✅ **Dependency Injection** - Loose coupling  
✅ **Async/Await** - Non-blocking UI  
✅ **MVVM Pattern** - Separation of concerns  
✅ **Data Binding** - Reactive UI updates  
✅ **Command Pattern** - Decoupled user actions  
✅ **Resource Management** - Proper Dispose pattern  

## Požadavky

- .NET 10 SDK
- Windows 10/11
- ONNX Runtime package
- 2+ GB RAM (pro AI inference)
- Optional: Webkamera pro live capture
