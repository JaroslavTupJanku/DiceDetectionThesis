# 🎲 Detekce a analýza hracích kostek

Moderní WPF aplikace pro detekci a klasifikaci hracích kostek využívající ONNX Runtime a AI modely.

## ✨ Funkce

### 🖼️ Zpracování obrazu
- **Načítání z souborů** - Podpora běžných formátů obrázků (JPG, PNG, BMP)
- **Live kamera** - Živý náhled z kamery s možností zachycení snímku
- **Přehledné zobrazení** - Viewport s overlay detekčními boxy

### 🤖 AI Inference
- **Dvoustupňová detekce**:
  1. **Object Detection** - YOLO model pro detekci pozice kostek (`dice_detector.onnx`)
  2. **Classification** - Klasifikační model pro rozpoznání hodnoty 1-6 (`dice_classifier.onnx`)
- **ONNX Runtime** - Optimalizovaný běh modelů
- **Real-time zpracování** - Rychlá inference s měřením času

### 📊 Statistiky a analýza
- **Počet kostek** - Celkový počet detekovaných objektů
- **Součet hodnot** - Automatický součet hodnot všech kostek
- **Průměrná jistota** - Průměrná confidence detekce
- **Čas inference** - Měření výkonu v ms

### 🎨 Moderní UI
- **Material Design** - Moderní vzhled s kartami a stíny
- **Barevné indikátory** - Každá kostka má vlastní barvu pro přehlednost
- **Progress bar** - Vizuální indikace průběhu zpracování
- **Responsive layout** - Přizpůsobivý layout pro různé velikosti oken

## 🏗️ Architektura

### MVVM Pattern
```
┌─────────────────┐
│   View (XAML)   │ ← Binding
├─────────────────┤
│   ViewModel     │ ← Commands, Properties
├─────────────────┤
│    Services     │ ← Business Logic
└─────────────────┘
```

### Services
- **IInferenceService** - ONNX inference logika
- **IPreprocessingService** - Předzpracování obrázků pro AI model
- **IOverlayRenderer** - Generování detection boxů
- **ICameraService** - Práce s kamerou
- **IImageDialogService** - File dialogy

### Models
- **DetectionResult** - Výsledek detekce (bbox + confidence)
- **InferenceResult** - Kompletní výsledek inference
- **OverlayItem** - Vizuální reprezentace detekce

## 🚀 Použití

### Načtení obrázku
1. Klikněte na **"📁 Načíst obrázek"**
2. Vyberte obrázek s kostkami
3. Klikněte na **"🚀 Spustit AI"**

### Použití kamery
1. Klikněte na **"📷 Kamera"** pro aktivaci
2. Umístěte kostky do záběru
3. Klikněte na **"📸 Zachytit"** pro zachycení snímku
4. Klikněte na **"🚀 Spustit AI"** pro detekci

### Vyčištění
- Klikněte na **"🗑️ Vyčistit"** pro reset aplikace

## 📦 Technologie

- **.NET 10** - Nejnovější .NET framework
- **WPF** - Windows Presentation Foundation
- **MVVM Toolkit** - CommunityToolkit.Mvvm
- **ONNX Runtime** - Microsoft.ML.OnnxRuntime
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection

## 📁 Struktura projektu

```
DiceDetector/
├── Models/                    # Data modely
│   ├── DetectionResult.cs
│   ├── InferenceResult.cs
│   └── OverlayItem.cs
├── Services/                  # Business logika
│   ├── Interfaces/
│   │   ├── IInferenceService.cs
│   │   ├── IPreprocessingService.cs
│   │   ├── IOverlayRenderer.cs
│   │   ├── ICameraService.cs
│   │   └── IImageDialogService.cs
│   ├── OnnxInferenceService.cs
│   ├── PreprocessingService.cs
│   ├── OverlayRenderer.cs
│   ├── CameraService.cs
│   └── ImageDialogService.cs
├── ViewModel/                 # MVVM ViewModels
│   ├── ViewModelBase.cs
│   └── MainViewModel.cs
├── View/                      # UI komponenty
│   └── Converters/
│       └── BoolToVisibilityConverter.cs
├── Themes/                    # Styly
│   └── AppStyles.xaml
├── Models/                    # ONNX modely
│   ├── dice_detector.onnx
│   └── dice_classifier.onnx
└── MainWindow.xaml           # Hlavní okno
```

## 🎯 Výsledky

Aplikace zobrazuje:
- **Detekční boxy** - Barevné rámečky kolem každé kostky
- **Hodnoty** - Rozpoznaná čísla (1-6)
- **Confidence skóre** - Jistota detekce
- **Souřadnice** - Pozice a velikost bbox
- **Celkové statistiky** - Agregované metriky

## 🔧 Konfigurace

Parametry inference lze nastavit v `OnnxInferenceService.cs`:
```csharp
private const int DetectorWidth = 640;
private const int DetectorHeight = 640;
private const int ClassifierWidth = 224;
private const int ClassifierHeight = 224;
private const float ConfidenceThreshold = 0.45f;
```

## 📝 Poznámky

- Modely musí být umístěny ve složce `Models/` v build directory
- Kamera je nyní simulovaná (pro skutečnou kameru použijte AForge.NET nebo DirectShow)
- Aplikace používá GPU akceleraci pokud je k dispozici (ONNX Runtime)

## 👨‍💻 Autor

Vytvořeno jako diplomová práce - WPF + MVVM + ONNX Runtime AI aplikace.
