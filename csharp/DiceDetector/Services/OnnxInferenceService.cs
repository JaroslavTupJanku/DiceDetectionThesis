using DiceDetector.Models;
using DiceDetector.Services.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiceDetector.Services
{
    public class OnnxInferenceService : IInferenceService, IDisposable
    {
        private readonly IPreprocessingService preprocessingService;
        private readonly InferenceSession detectorSession;
        private readonly InferenceSession classifierSession;

        private const int DetectorSize = 640;
        private const int ClassifierWidth = 384;
        private const int ClassifierHeight = 384;

        private const float DetectorConfidenceThreshold = 0.45f;
        private const float DetectorIouThreshold = 0.5f;
        private const float CropMargin = 0.12f;

        private const int RegMax = 16;
        private const bool EnableDebugLogs = true;

        public OnnxInferenceService(IPreprocessingService preprocessingService)
        {
            this.preprocessingService = preprocessingService;

            var baseDir = AppContext.BaseDirectory;
            var detectorPath = Path.Combine(baseDir, "OnnxModels", "dice_detector.onnx");
            var classifierPath = Path.Combine(baseDir, "OnnxModels", "dice_classifier.onnx");

            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            detectorSession = new InferenceSession(detectorPath, sessionOptions);
            classifierSession = new InferenceSession(classifierPath, sessionOptions);
        }

        public Task<InferenceResult> RunAsync(string imagePath)
        {
            return Task.Run(() =>
            {
                var bitmap = LoadBitmap(imagePath);

                if (EnableDebugLogs)
                {
                    Debug.WriteLine($"RunAsync image: {imagePath}");
                    Debug.WriteLine($"Original bitmap size: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                }

                var detections = RunDetector(bitmap);
                var classified = new List<DetectionResult>(detections.Count);

                foreach (var detection in detections)
                {
                    var crop = CropBitmap(bitmap, detection.X, detection.Y, detection.Width, detection.Height);
                    crop.Freeze();
                    var (predictedValue, clsConfidence, topPredictions) = RunClassifier(crop);

                    classified.Add(new DetectionResult
                    {
                        Index = classified.Count + 1,
                        Label = $"Kostka {classified.Count + 1}",
                        ValueText = $"Hodnota: {predictedValue}",
                        ConfidenceText = $"Det: {detection.Confidence:0.00} | Cls: {clsConfidence:0.00}",
                        BoxText = $"Box: x={(int)detection.X}, y={(int)detection.Y}, w={(int)detection.Width}, h={(int)detection.Height}",
                        X = detection.X,
                        Y = detection.Y,
                        Width = detection.Width,
                        Height = detection.Height,
                        Confidence = detection.Confidence,
                        DiceValue = predictedValue,
                        DetConfidence = detection.Confidence,
                        ClsConfidence = clsConfidence,
                        TopPredictions = topPredictions,
                        CropImage = crop
                    });
                }

                return new InferenceResult
                {
                    Detections = classified
                };
            });
        }

        private List<DetectionResult> RunDetector(BitmapSource bitmap)
        {
            var original = EnsureRgb24(bitmap);
            var originalWidth = original.PixelWidth;
            var originalHeight = original.PixelHeight;
            var letterbox = PrepareLetterboxedDetectorInput(original);

            if (EnableDebugLogs)
            {
                Debug.WriteLine("=== DETECTOR DEBUG ===");
                Debug.WriteLine($"Original size: {originalWidth}x{originalHeight}");
                Debug.WriteLine($"Letterbox scale: {letterbox.Scale:0.######}");
                Debug.WriteLine($"Letterbox padLeft: {letterbox.PadLeft}, padTop: {letterbox.PadTop}");
                LogArrayMinMax("Detector input tensor", letterbox.Tensor);
            }

            var inputTensor = new DenseTensor<float>(
                letterbox.Tensor,
                [1, DetectorSize, DetectorSize, 3]);

            var inputName = detectorSession.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = detectorSession.Run(inputs);
            var boxOutput = outputs.First(o => o.Name == "box").AsTensor<float>();
            var classOutput = outputs.First(o => o.Name == "class").AsTensor<float>();

            var decoded = DecodeDetections(
                boxOutput,
                classOutput,
                letterbox.Scale,
                letterbox.PadLeft,
                letterbox.PadTop,
                originalWidth,
                originalHeight);

            var nms = ApplyNms(decoded, DetectorIouThreshold);
            var results = new List<DetectionResult>(nms.Count);

            foreach (var det in nms)
            {
                var expanded = ExpandBox(det.X1, det.Y1, det.X2, det.Y2, originalWidth, originalHeight, CropMargin);
                if (expanded == null)
                {
                    continue;
                }

                var box = expanded;

                if (EnableDebugLogs && results.Count < 10)
                {
                    Debug.WriteLine(
                        $"Det #{results.Count + 1}: score={det.Score:0.####}, " +
                        $"xyxy=({det.X1:0.0},{det.Y1:0.0},{det.X2:0.0},{det.Y2:0.0}), " +
                        $"expanded=({box.X:0.0},{box.Y:0.0},{box.Width:0.0},{box.Height:0.0})");
                }

                results.Add(new DetectionResult
                {
                    Index = results.Count + 1,
                    Label = "Kostka",
                    ValueText = "Hodnota: ?",
                    ConfidenceText = $"Confidence: {det.Score:0.00}",
                    BoxText = $"Box: x={(int)box.X}, y={(int)box.Y}, w={(int)box.Width}, h={(int)box.Height}",
                    X = box.X,
                    Y = box.Y,
                    Width = box.Width,
                    Height = box.Height,
                    Confidence = det.Score
                });
            }

            return results;
        }

        private (int PredictedValue, float Confidence, IReadOnlyList<ClassPrediction> TopPredictions) RunClassifier(BitmapSource crop)
        {
            var inputData = PrepareClassifierTensor(crop);
            var inputTensor = new DenseTensor<float>(
                inputData, [1, ClassifierHeight, ClassifierWidth, 3]);

            var inputName = classifierSession.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = classifierSession.Run(inputs);
            var output = outputs.First().AsTensor<float>();
            var probs = output.ToArray();

            var maxIndex = 0;
            var maxValue = probs[0];
            for (var i = 1; i < probs.Length; i++)
            {
                if (probs[i] > maxValue)
                {
                    maxValue = probs[i];
                    maxIndex = i;
                }
            }

            var topPredictions = probs
                .Select((p, i) => new ClassPrediction { Value = i + 1, Confidence = p })
                .OrderByDescending(p => p.Confidence)
                .ToList();

            return (maxIndex + 1, maxValue, topPredictions);
        }

        private float[] PrepareClassifierTensor(BitmapSource bitmap)
        {
            var resized = ResizeBitmap(EnsureRgb24(bitmap), ClassifierWidth, ClassifierHeight);

            var stride = ClassifierWidth * 3;
            var pixels = new byte[ClassifierHeight * stride];
            resized.CopyPixels(pixels, stride, 0);

            var tensor = new float[ClassifierWidth * ClassifierHeight * 3];
            for (var y = 0; y < ClassifierHeight; y++)
            {
                for (var x = 0; x < ClassifierWidth; x++)
                {
                    var pixelIndex = y * stride + x * 3;
                    var flatIndex = (y * ClassifierWidth + x) * 3;

                    tensor[flatIndex + 0] = pixels[pixelIndex + 0]; // R
                    tensor[flatIndex + 1] = pixels[pixelIndex + 1]; // G
                    tensor[flatIndex + 2] = pixels[pixelIndex + 2]; // B
                }
            }

            return tensor;
        }

        private static void LogArrayMinMax(string name, float[] data)
        {
            if (data.Length == 0)
            {
                Debug.WriteLine($"{name}: empty");
                return;
            }

            var min = data[0];
            var max = data[0];
            for (var i = 1; i < data.Length; i++)
            {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }
        }

        private List<DecodedDetection> DecodeDetections(
            Tensor<float> boxOutput,
            Tensor<float> classOutput,
            float scale,
            float padLeft,
            float padTop,
            int originalWidth,
            int originalHeight)
        {
            var results = new List<DecodedDetection>();
            var anchorCount = boxOutput.Dimensions[1];

            if (boxOutput.Dimensions[2] != 64)
            {
                throw new InvalidOperationException($"Unexpected detector box output width: {boxOutput.Dimensions[2]}. Expected 64.");
            }

            if (classOutput.Dimensions[1] != anchorCount)
            {
                throw new InvalidOperationException("Detector outputs have incompatible anchor counts.");
            }

            var strides = new[] { 8, 16, 32 };
            var gridSizes = new[] { 80, 40, 20 };
            var anchorIndex = 0;

            for (var level = 0; level < strides.Length; level++)
            {
                var stride = strides[level];
                var grid = gridSizes[level];

                for (var gy = 0; gy < grid; gy++)
                {
                    for (var gx = 0; gx < grid; gx++)
                    {
                        var score = classOutput[0, anchorIndex, 0];
                        if (score < DetectorConfidenceThreshold)
                        {
                            anchorIndex++;
                            continue;
                        }

                        var distances = DecodeDflDistances(boxOutput, anchorIndex);
                        var centerX = (gx + 0.5f) * stride;
                        var centerY = (gy + 0.5f) * stride;

                        var x1 = centerX - distances.Left * stride;
                        var y1 = centerY - distances.Top * stride;
                        var x2 = centerX + distances.Right * stride;
                        var y2 = centerY + distances.Bottom * stride;

                        var mapped = MapBoxToOriginal(
                            x1, y1, x2, y2,
                            scale, padLeft, padTop,
                            originalWidth, originalHeight);

                        if (mapped.Width <= 1 || mapped.Height <= 1)
                        {
                            anchorIndex++;
                            continue;
                        }

                        results.Add(new DecodedDetection
                        {
                            X1 = mapped.X1,
                            Y1 = mapped.Y1,
                            X2 = mapped.X2,
                            Y2 = mapped.Y2,
                            Score = score
                        });

                        anchorIndex++;
                    }
                }
            }

            if (anchorIndex != anchorCount)
            {
                throw new InvalidOperationException($"Decoded anchor count mismatch. Expected {anchorCount}, decoded {anchorIndex}.");
            }

            return results;
        }

        private static (float Left, float Top, float Right, float Bottom) DecodeDflDistances(Tensor<float> boxOutput, int anchorIndex)
        {
            var left = DecodeSingleSide(boxOutput, anchorIndex, 0);
            var top = DecodeSingleSide(boxOutput, anchorIndex, 16);
            var right = DecodeSingleSide(boxOutput, anchorIndex, 32);
            var bottom = DecodeSingleSide(boxOutput, anchorIndex, 48);

            return (left, top, right, bottom);
        }

        private static float DecodeSingleSide(Tensor<float> boxOutput, int anchorIndex, int offset)
        {
            Span<float> logits = stackalloc float[RegMax];
            for (var i = 0; i < RegMax; i++)
            {
                logits[i] = boxOutput[0, anchorIndex, offset + i];
            }

            var maxLogit = logits[0];
            for (var i = 1; i < RegMax; i++)
            {
                if (logits[i] > maxLogit)
                {
                    maxLogit = logits[i];
                }
            }

            var sum = 0f;
            Span<float> exps = stackalloc float[RegMax];
            for (var i = 0; i < RegMax; i++)
            {
                exps[i] = MathF.Exp(logits[i] - maxLogit);
                sum += exps[i];
            }

            var expected = 0f;
            for (var i = 0; i < RegMax; i++)
            {
                var p = exps[i] / sum;
                expected += p * i;
            }

            return expected;
        }

        private static List<DecodedDetection> ApplyNms(List<DecodedDetection> detections, float iouThreshold)
        {
            var sorted = detections
                .OrderByDescending(d => d.Score)
                .ToList();

            var kept = new List<DecodedDetection>();

            while (sorted.Count > 0)
            {
                var current = sorted[0];
                kept.Add(current);
                sorted.RemoveAt(0);

                for (var i = sorted.Count - 1; i >= 0; i--)
                {
                    var iou = ComputeIoU(current, sorted[i]);
                    if (iou > iouThreshold)
                    {
                        sorted.RemoveAt(i);
                    }
                }
            }

            return kept;
        }

        private static float ComputeIoU(DecodedDetection a, DecodedDetection b)
        {
            var interX1 = Math.Max(a.X1, b.X1);
            var interY1 = Math.Max(a.Y1, b.Y1);
            var interX2 = Math.Min(a.X2, b.X2);
            var interY2 = Math.Min(a.Y2, b.Y2);

            var interW = Math.Max(0f, interX2 - interX1);
            var interH = Math.Max(0f, interY2 - interY1);
            var interArea = interW * interH;

            var areaA = Math.Max(0f, a.X2 - a.X1) * Math.Max(0f, a.Y2 - a.Y1);
            var areaB = Math.Max(0f, b.X2 - b.X1) * Math.Max(0f, b.Y2 - b.Y1);

            var union = areaA + areaB - interArea;
            if (union <= 0f)
            {
                return 0f;
            }

            return interArea / union;
        }

        private static MappedBox MapBoxToOriginal(
            float x1, float y1, float x2, float y2,
            float scale, float padLeft, float padTop,
            int originalWidth,
            int originalHeight)
        {
            var mappedX1 = (x1 - padLeft) / scale;
            var mappedY1 = (y1 - padTop) / scale;
            var mappedX2 = (x2 - padLeft) / scale;
            var mappedY2 = (y2 - padTop) / scale;

            mappedX1 = Clamp(mappedX1, 0f, originalWidth);
            mappedY1 = Clamp(mappedY1, 0f, originalHeight);
            mappedX2 = Clamp(mappedX2, 0f, originalWidth);
            mappedY2 = Clamp(mappedY2, 0f, originalHeight);

            return new MappedBox
            {
                X1 = mappedX1,
                Y1 = mappedY1,
                X2 = mappedX2,
                Y2 = mappedY2,
                Width = Math.Max(0f, mappedX2 - mappedX1),
                Height = Math.Max(0f, mappedY2 - mappedY1)
            };
        }

        private static ExpandedBox? ExpandBox(
            float x1, float y1, float x2, float y2,
            int imageWidth, int imageHeight,
            float margin)
        {
            var bw = x2 - x1;
            var bh = y2 - y1;

            x1 -= bw * margin;
            y1 -= bh * margin;
            x2 += bw * margin;
            y2 += bh * margin;

            var rx1 = (int)Math.Round(Clamp(x1, 0f, imageWidth));
            var ry1 = (int)Math.Round(Clamp(y1, 0f, imageHeight));
            var rx2 = (int)Math.Round(Clamp(x2, 0f, imageWidth));
            var ry2 = (int)Math.Round(Clamp(y2, 0f, imageHeight));

            if (rx2 <= rx1 || ry2 <= ry1)
            {
                return null;
            }

            return new ExpandedBox
            {
                X = rx1,
                Y = ry1,
                Width = rx2 - rx1,
                Height = ry2 - ry1
            };
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private LetterboxResult PrepareLetterboxedDetectorInput(BitmapSource bitmap)
        {
            var source = EnsureRgb24(bitmap);

            var originalWidth = source.PixelWidth;
            var originalHeight = source.PixelHeight;

            var scale = Math.Min(DetectorSize / (float)originalWidth, DetectorSize / (float)originalHeight);

            var resizedWidth = (int)Math.Round(originalWidth * scale);
            var resizedHeight = (int)Math.Round(originalHeight * scale);

            var resized = ResizeBitmap(source, resizedWidth, resizedHeight);

            var resizedStride = resizedWidth * 3;
            var resizedPixels = new byte[resizedHeight * resizedStride];
            resized.CopyPixels(resizedPixels, resizedStride, 0);

            var canvasStride = DetectorSize * 3;
            var canvasPixels = new byte[DetectorSize * canvasStride];

            var padLeft = (DetectorSize - resizedWidth) / 2;
            var padTop = (DetectorSize - resizedHeight) / 2;

            for (var y = 0; y < resizedHeight; y++)
            {
                var srcOffset = y * resizedStride;
                var dstOffset = ((y + padTop) * canvasStride) + padLeft * 3;
                Buffer.BlockCopy(resizedPixels, srcOffset, canvasPixels, dstOffset, resizedStride);
            }

            var tensor = new float[DetectorSize * DetectorSize * 3];

            for (var y = 0; y < DetectorSize; y++)
            {
                for (var x = 0; x < DetectorSize; x++)
                {
                    var pixelIndex = y * canvasStride + x * 3;
                    var flatIndex = (y * DetectorSize + x) * 3;

                    tensor[flatIndex + 0] = canvasPixels[pixelIndex + 0] / 255f;
                    tensor[flatIndex + 1] = canvasPixels[pixelIndex + 1] / 255f;
                    tensor[flatIndex + 2] = canvasPixels[pixelIndex + 2] / 255f;
                }
            }

            return new LetterboxResult
            {
                Tensor = tensor,
                Scale = scale,
                PadLeft = padLeft,
                PadTop = padTop
            };
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

        private static BitmapSource EnsureRgb24(BitmapSource source)
        {
            if (source.Format == PixelFormats.Rgb24)
            {
                return source;
            }

            var converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = source;
            converted.DestinationFormat = PixelFormats.Rgb24;
            converted.EndInit();
            converted.Freeze();
            return converted;
        }

        private static BitmapSource ResizeBitmap(BitmapSource source, int width, int height)
        {
            var scaleX = width / (double)source.PixelWidth;
            var scaleY = height / (double)source.PixelHeight;

            var resized = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
            resized.Freeze();
            return resized;
        }

        private static CroppedBitmap CropBitmap(BitmapSource source, double x, double y, double width, double height)
        {
            var safeX = Math.Max(0, (int)Math.Round(x));
            var safeY = Math.Max(0, (int)Math.Round(y));
            var safeW = Math.Min(source.PixelWidth - safeX, Math.Max(1, (int)Math.Round(width)));
            var safeH = Math.Min(source.PixelHeight - safeY, Math.Max(1, (int)Math.Round(height)));

            return new CroppedBitmap(source, new Int32Rect(safeX, safeY, safeW, safeH));
        }

        public void Dispose()
        {
            detectorSession.Dispose();
            classifierSession.Dispose();
        }

        private sealed class LetterboxResult
        {
            public required float[] Tensor { get; init; }
            public required float Scale { get; init; }
            public required int PadLeft { get; init; }
            public required int PadTop { get; init; }
        }

        private sealed class DecodedDetection
        {
            public required float X1 { get; init; }
            public required float Y1 { get; init; }
            public required float X2 { get; init; }
            public required float Y2 { get; init; }
            public required float Score { get; init; }
        }

        private sealed class MappedBox
        {
            public required float X1 { get; init; }
            public required float Y1 { get; init; }
            public required float X2 { get; init; }
            public required float Y2 { get; init; }
            public required float Width { get; init; }
            public required float Height { get; init; }
        }

        private sealed class ExpandedBox
        {
            public required double X { get; init; }
            public required double Y { get; init; }
            public required double Width { get; init; }
            public required double Height { get; init; }
        }
    }
}