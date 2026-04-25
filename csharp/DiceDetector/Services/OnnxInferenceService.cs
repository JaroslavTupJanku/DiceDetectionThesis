using DiceDetector.Models;
using DiceDetector.Services.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
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

        public OnnxInferenceService(IPreprocessingService preprocessingService)
        {
            this.preprocessingService = preprocessingService;

            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            detectorSession = new InferenceSession(InferenceSettings.DetectorModelPath, sessionOptions);
            classifierSession = new InferenceSession(InferenceSettings.ClassifierModelPath, sessionOptions);
        }

        public Task<InferenceResult> RunAsync(string imagePath)
        {
            return Task.Run(() =>
            {
                var bitmap = LoadBitmap(imagePath);
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
                        X = detection.X,
                        Y = detection.Y,
                        Width = detection.Width,
                        Height = detection.Height,
                        DiceValue = predictedValue,
                        DetConfidence = detection.DetConfidence,
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

            var inputTensor = new DenseTensor<float>(
                letterbox.Tensor,
                [1, InferenceSettings.DetectorSize, 
                    InferenceSettings.DetectorSize, 
                    3]);

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

            var nms = ApplyNms(decoded, InferenceSettings.DetectorIouThreshold);
            var results = new List<DetectionResult>(nms.Count);

            foreach (var det in nms)
            {
                var expanded = ExpandBox(det.X1, det.Y1, det.X2, det.Y2, 
                    originalWidth, originalHeight, 
                    InferenceSettings.CropMargin);

                if (expanded == null)
                {
                    continue;
                }

                var box = expanded;

                results.Add(new DetectionResult
                {
                    Index = results.Count + 1,
                    X = box.X,
                    Y = box.Y,
                    Width = box.Width,
                    Height = box.Height,
                    DiceValue = 0,
                    DetConfidence = det.Score,
                    ClsConfidence = 0f
                });
            }

            return results;
        }

        private (int PredictedValue, float Confidence, IReadOnlyList<ClassPrediction> TopPredictions) RunClassifier(BitmapSource crop)
        {
            var inputData = PrepareClassifierTensor(crop);
            var inputTensor = new DenseTensor<float>(
                inputData, [1, 
                    InferenceSettings.ClassifierHeight, 
                    InferenceSettings.ClassifierWidth, 
                    3]);

            var inputName = classifierSession.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = classifierSession.Run(inputs);
            var output = outputs[0].AsTensor<float>();
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

        private static float[] PrepareClassifierTensor(BitmapSource bitmap)
        {
            var classifierWidth = InferenceSettings.ClassifierWidth;
            var classifierHeight = InferenceSettings.ClassifierHeight;
            var resized = ResizeBitmap(EnsureRgb24(bitmap), classifierWidth, classifierHeight);

            var stride = classifierWidth * 3;
            var pixels = new byte[classifierHeight * stride];
            resized.CopyPixels(pixels, stride, 0);

            var tensor = new float[classifierWidth * classifierHeight * 3];
            for (var y = 0; y < classifierHeight; y++)
            {
                for (var x = 0; x < classifierWidth; x++)
                {
                    var pixelIndex = y * stride + x * 3;
                    var flatIndex = (y * classifierWidth + x) * 3;

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

        private static List<DecodedDetection> DecodeDetections(
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
                        if (score < InferenceSettings.DetectorConfidenceThreshold)
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
            var regMax = InferenceSettings.RegMax;
            Span<float> logits = stackalloc float[regMax];

            for (var i = 0; i < regMax; i++)
            {
                logits[i] = boxOutput[0, anchorIndex, offset + i];
            }

            var maxLogit = logits[0];
            for (var i = 1; i < regMax; i++)
            {
                if (logits[i] > maxLogit)
                {
                    maxLogit = logits[i];
                }
            }

            var sum = 0f;
            Span<float> exps = stackalloc float[regMax];
            for (var i = 0; i < regMax; i++)
            {
                exps[i] = MathF.Exp(logits[i] - maxLogit);
                sum += exps[i];
            }

            var expected = 0f;
            for (var i = 0; i < regMax; i++)
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
            var detectorSize = InferenceSettings.DetectorSize;
            var source = EnsureRgb24(bitmap);

            var originalWidth = source.PixelWidth;
            var originalHeight = source.PixelHeight;
            var scale = Math.Min(detectorSize / (float)originalWidth, detectorSize / (float)originalHeight);

            var resizedWidth = (int)Math.Round(originalWidth * scale);
            var resizedHeight = (int)Math.Round(originalHeight * scale);
            var resized = ResizeBitmap(source, resizedWidth, resizedHeight);

            var resizedStride = resizedWidth * 3;
            var resizedPixels = new byte[resizedHeight * resizedStride];
            resized.CopyPixels(resizedPixels, resizedStride, 0);

            var canvasStride = detectorSize * 3;
            var canvasPixels = new byte[detectorSize * canvasStride];

            var padLeft = (detectorSize - resizedWidth) / 2;
            var padTop = (detectorSize - resizedHeight) / 2;

            for (var y = 0; y < resizedHeight; y++)
            {
                var srcOffset = y * resizedStride;
                var dstOffset = ((y + padTop) * canvasStride) + padLeft * 3;
                Buffer.BlockCopy(resizedPixels, srcOffset, canvasPixels, dstOffset, resizedStride);
            }

            var tensor = new float[detectorSize * detectorSize * 3];

            for (var y = 0; y < detectorSize; y++)
            {
                for (var x = 0; x < detectorSize; x++)
                {
                    var pixelIndex = y * canvasStride + x * 3;
                    var flatIndex = (y * detectorSize + x) * 3;

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