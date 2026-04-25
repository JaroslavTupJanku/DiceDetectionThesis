using DiceDetector.Services.Interfaces;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiceDetector.Services
{
    public class PreprocessingService : IPreprocessingService
    {
        public float[] PrepareImageTensor(BitmapSource bitmap, int width, int height, bool nchw = true, bool normalize01 = true)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            var formatted = EnsureRgb24(bitmap);
            var resized = ResizeBitmap(formatted, width, height);

            var stride = width * 3;
            var pixels = new byte[height * stride];
            resized.CopyPixels(pixels, stride, 0);

            var tensor = new float[width * height * 3];

            if (nchw)
            {
                var hw = width * height;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var pixelIndex = y * stride + x * 3;
                        var flatIndex = y * width + x;

                        var r = pixels[pixelIndex + 0];
                        var g = pixels[pixelIndex + 1];
                        var b = pixels[pixelIndex + 2];

                        tensor[flatIndex] = normalize01 ? r / 255f : r;
                        tensor[hw + flatIndex] = normalize01 ? g / 255f : g;
                        tensor[2 * hw + flatIndex] = normalize01 ? b / 255f : b;
                    }
                }
            }
            else
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var pixelIndex = y * stride + x * 3;
                        var flatIndex = (y * width + x) * 3;

                        var r = pixels[pixelIndex + 0];
                        var g = pixels[pixelIndex + 1];
                        var b = pixels[pixelIndex + 2];

                        tensor[flatIndex + 0] = normalize01 ? r / 255f : r;
                        tensor[flatIndex + 1] = normalize01 ? g / 255f : g;
                        tensor[flatIndex + 2] = normalize01 ? b / 255f : b;
                    }
                }
            }

            return tensor;
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
    }
}