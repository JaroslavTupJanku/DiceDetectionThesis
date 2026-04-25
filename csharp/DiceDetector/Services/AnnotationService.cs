using DiceDetector.Models;
using System.IO;
using System.Xml.Linq;

namespace DiceDetector.Services
{
    public class AnnotationService
    {
        private readonly Dictionary<string, ImageAnnotation> _annotations = new(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded => _annotations.Count > 0;
        public int AnnotationCount => _annotations.Count;
        public string? FolderPath { get; private set; }

        public void LoadFolder(string folderPath)
        {
            _annotations.Clear();
            FolderPath = folderPath;

            foreach (var xmlFile in Directory.GetFiles(folderPath, "*.xml"))
            {
                try
                {
                    var annotation = ParseVocXml(xmlFile);
                    if (annotation != null)
                    {
                        _annotations[annotation.Filename] = annotation;
                    }
                }
                catch
                {
                    // Skip malformed files
                }
            }
        }

        public ImageAnnotation? FindAnnotation(string imagePath)
        {
            var filename = Path.GetFileName(imagePath);
            return _annotations.TryGetValue(filename, out var annotation) ? annotation : null;
        }

        public void Clear()
        {
            _annotations.Clear();
            FolderPath = null;
        }

        public EvalResult Evaluate(IReadOnlyList<DetectionResult> detections, ImageAnnotation annotation, double iouThreshold = 0.3)
        {
            var gtObjects = annotation.Objects.ToList();
            var detList = detections.ToList();
            var matched = new List<EvalMatch>();
            var usedGt = new HashSet<int>();
            var usedDet = new HashSet<int>();

            var pairs = new List<(int detIdx, int gtIdx, double iou)>();
            for (var d = 0; d < detList.Count; d++)
            {
                for (var g = 0; g < gtObjects.Count; g++)
                {
                    var iou = ComputeIoU(detList[d], gtObjects[g]);
                    if (iou >= iouThreshold)
                    {
                        pairs.Add((d, g, iou));
                    }
                }
            }

            foreach (var (detIdx, gtIdx, iou) in pairs.OrderByDescending(p => p.iou))
            {
                if (usedDet.Contains(detIdx) || usedGt.Contains(gtIdx))
                    continue;

                usedDet.Add(detIdx);
                usedGt.Add(gtIdx);
                matched.Add(new EvalMatch
                {
                    Detection = detList[detIdx],
                    Annotation = gtObjects[gtIdx],
                    IoU = iou
                });
            }

            var tp = matched.Count;
            var fp = detList.Count - tp;
            var fn = gtObjects.Count - tp;
            var correctCls = matched.Count(m => m.IsCorrectClass);
            var avgIoU = matched.Count > 0 ? matched.Average(m => m.IoU) : 0;

            var missedObjects = gtObjects.Where((_, i) => !usedGt.Contains(i)).ToList();
            var fpDetections = detList.Where((_, i) => !usedDet.Contains(i)).ToList();

            return new EvalResult
            {
                TruePositives = tp,
                FalsePositives = fp,
                FalseNegatives = fn,
                CorrectClassifications = correctCls,
                TotalMatched = tp,
                AverageIoU = avgIoU,
                Matches = matched,
                MissedObjects = missedObjects,
                FalsePositiveDetections = fpDetections
            };
        }

        private static double ComputeIoU(DetectionResult det, AnnotationObject gt)
        {
            var detXMin = det.X;
            var detYMin = det.Y;
            var detXMax = det.X + det.Width;
            var detYMax = det.Y + det.Height;

            var interXMin = Math.Max(detXMin, gt.XMin);
            var interYMin = Math.Max(detYMin, gt.YMin);
            var interXMax = Math.Min(detXMax, gt.XMax);
            var interYMax = Math.Min(detYMax, gt.YMax);

            var interW = Math.Max(0, interXMax - interXMin);
            var interH = Math.Max(0, interYMax - interYMin);
            var interArea = interW * interH;

            var detArea = det.Width * det.Height;
            var gtArea = gt.Width * gt.Height;
            var unionArea = detArea + gtArea - interArea;

            return unionArea > 0 ? interArea / unionArea : 0;
        }

        private static ImageAnnotation? ParseVocXml(string path)
        {
            var doc = XDocument.Load(path);
            var root = doc.Element("annotation");
            if (root == null) return null;

            var filename = root.Element("filename")?.Value ?? Path.GetFileNameWithoutExtension(path);
            var size = root.Element("size");
            var width = int.Parse(size?.Element("width")?.Value ?? "0");
            var height = int.Parse(size?.Element("height")?.Value ?? "0");

            var objects = root.Elements("object").Select(obj =>
            {
                var name = obj.Element("name")?.Value ?? "0";
                var bndbox = obj.Element("bndbox");
                return new AnnotationObject
                {
                    Value = int.TryParse(name, out var v) ? v : 0,
                    XMin = double.Parse(bndbox?.Element("xmin")?.Value ?? "0"),
                    YMin = double.Parse(bndbox?.Element("ymin")?.Value ?? "0"),
                    XMax = double.Parse(bndbox?.Element("xmax")?.Value ?? "0"),
                    YMax = double.Parse(bndbox?.Element("ymax")?.Value ?? "0")
                };
            }).ToList();

            return new ImageAnnotation
            {
                Filename = filename,
                ImageWidth = width,
                ImageHeight = height,
                Objects = objects
            };
        }
    }
}
