using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace DiceDetector.Services.Interfaces
{
    public interface IPreprocessingService
    {
        float[] PrepareImageTensor(BitmapSource bitmap, int width, int height, bool nchw = true, bool normalize01 = true);
    }
}
