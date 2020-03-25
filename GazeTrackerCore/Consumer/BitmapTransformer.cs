using OpenCVWrappers;
using System.Windows.Media.Imaging;

namespace GazeTrackerCore.Consumer
{
    public class BitmapTransformer
    {
        private WriteableBitmap bitmap;

        public WriteableBitmap ConvertToWritableBitmap(RawImage frame)
        {
            if (bitmap == null)
                bitmap = frame.CreateWriteableBitmap();

            frame.UpdateWriteableBitmap(bitmap);
            return bitmap;
        }
    }
}
